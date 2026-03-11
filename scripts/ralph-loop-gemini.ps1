#
# Ralph Loop for Google Gemini CLI (PowerShell - native Windows)
#
# Based on Geoffrey Huntley's Ralph Wiggum methodology:
# https://github.com/ghuntley/how-to-ralph-wiggum
#
# Combined with SpecKit-style specifications.
#
# Usage:
#   .\scripts\ralph-loop-gemini.ps1              # Build mode (unlimited)
#   .\scripts\ralph-loop-gemini.ps1 20           # Build mode (max 20 iterations)
#   .\scripts\ralph-loop-gemini.ps1 plan         # Planning mode
#   .\scripts\ralph-loop-gemini.ps1 plan 3       # Planning mode (max 3 iterations)
#   .\scripts\ralph-loop-gemini.ps1 -help        # Show help
#
# Requirements:
#   - Gemini CLI installed: npm install -g @google/gemini-cli
#   - Authenticated via: gemini (interactive login on first run)
#
# NOTE: On Windows, Gemini CLI must run natively from PowerShell (not Git Bash)
#       to avoid ConPTY AttachConsole failures in node-pty.
#

param(
    [Parameter(Position = 0)]
    [string]$ModeOrIterations = "build",
    [Parameter(Position = 1)]
    [int]$MaxIterations = 0,
    [switch]$Help
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir
$logDir = Join-Path $projectDir "logs"
$constitution = Join-Path $projectDir ".specify\memory\constitution.md"

if (-not (Test-Path $logDir)) { New-Item -Path $logDir -ItemType Directory | Out-Null }

# ── Configuration ──────────────────────────────────────────────
$geminiCmd = "gemini"
$geminiModel = $env:GEMINI_MODEL  # leave empty for auto-select
$yoloEnabled = $true
$gitAutonomy = $true
$maxConsecutiveFailures = 3

# Check constitution for autonomy settings
if (Test-Path $constitution) {
    $constitutionText = Get-Content $constitution -Raw -ErrorAction SilentlyContinue
    if ($constitutionText -match "YOLO Mode.*DISABLED") { $yoloEnabled = $false }
    if ($constitutionText -match "Git Autonomy.*DISABLED") { $gitAutonomy = $false }
}

# ── Help ───────────────────────────────────────────────────────
function Show-Help {
    Write-Host @"
Ralph Loop for Google Gemini CLI (PowerShell)

Based on Geoffrey Huntley's Ralph Wiggum methodology + SpecKit specs.
https://github.com/ghuntley/how-to-ralph-wiggum

Usage:
  .\scripts\ralph-loop-gemini.ps1              # Build mode, unlimited iterations
  .\scripts\ralph-loop-gemini.ps1 20           # Build mode, max 20 iterations
  .\scripts\ralph-loop-gemini.ps1 plan         # Planning mode (optional)
  .\scripts\ralph-loop-gemini.ps1 plan 3       # Planning mode, max 3 iterations

Modes:
  build (default)  Pick spec/task and implement
  plan             Create IMPLEMENTATION_PLAN.md from specs (OPTIONAL)

Work Sources (checked in order):
  1. IMPLEMENTATION_PLAN.md - If exists, pick highest priority task
  2. specs/ folder - Otherwise, pick highest priority incomplete spec

Model (default: auto-selects latest, e.g. Gemini 3 Flash):
  Override with: `$env:GEMINI_MODEL = "gemini-2.5-pro"; .\scripts\ralph-loop-gemini.ps1

How it works:
  1. Each iteration passes PROMPT content to Gemini CLI via -p flag
  2. Gemini picks the HIGHEST PRIORITY incomplete spec/task
  3. Gemini implements, tests, and verifies acceptance criteria
  4. Gemini outputs <promise>DONE</promise> ONLY if criteria are met
  5. Loop checks for the magic phrase, then continues or retries
"@
}

if ($Help) { Show-Help; exit 0 }

# ── Parse arguments ────────────────────────────────────────────
$mode = "build"
$maxIters = 0

if ($ModeOrIterations -match '^\d+$') {
    $mode = "build"
    $maxIters = [int]$ModeOrIterations
} elseif ($ModeOrIterations -eq "plan") {
    $mode = "plan"
    if ($MaxIterations -gt 0) { $maxIters = $MaxIterations } else { $maxIters = 1 }
} elseif ($ModeOrIterations -eq "-help" -or $ModeOrIterations -eq "--help" -or $ModeOrIterations -eq "-h") {
    Show-Help; exit 0
} else {
    $mode = $ModeOrIterations
    $maxIters = $MaxIterations
}

Set-Location $projectDir

# ── Check Gemini CLI ──────────────────────────────────────────
$geminiExe = Get-Command $geminiCmd -ErrorAction SilentlyContinue
if (-not $geminiExe) {
    Write-Host "Error: Gemini CLI not found" -ForegroundColor Red
    Write-Host ""
    Write-Host "Install Gemini CLI:"
    Write-Host "  npm install -g @google/gemini-cli"
    Write-Host ""
    Write-Host "Then authenticate by running once interactively:"
    Write-Host "  gemini"
    exit 1
}

# ── Generate prompt files ─────────────────────────────────────
$promptBuild = @"
# Ralph Loop - Build Mode

You are running inside a Ralph Wiggum autonomous loop (Context A).

Read ``.specify/memory/constitution.md`` - it contains all project principles, workflow
instructions, work sources, and completion signal requirements.

Find the highest-priority incomplete work item, implement it completely, verify all
acceptance criteria, commit and push, then output ``<promise>DONE</promise>``.
"@

$promptPlan = @"
# Ralph Loop - Planning Mode

You are running inside a Ralph Wiggum autonomous loop in planning mode.

Read ``.specify/memory/constitution.md`` for project principles.

Study ``specs/`` and compare against the current codebase (gap analysis).
Create or update ``IMPLEMENTATION_PLAN.md`` with a prioritized task breakdown.
Do NOT implement anything.

When the plan is complete, output ``<promise>DONE</promise>``.
"@

if ($mode -eq "plan") {
    $promptFile = "PROMPT_plan.md"
    $promptPlan | Out-File (Join-Path $projectDir "PROMPT_plan.md") -Encoding utf8
} else {
    $promptFile = "PROMPT_build.md"
    $promptBuild | Out-File (Join-Path $projectDir "PROMPT_build.md") -Encoding utf8
}

# ── Check work sources ────────────────────────────────────────
$hasPlan = Test-Path (Join-Path $projectDir "IMPLEMENTATION_PLAN.md")
$specsDir = Join-Path $projectDir "specs"
$hasSpecs = $false
$specCount = 0
if (Test-Path $specsDir) {
    $specFiles = Get-ChildItem -Path $specsDir -Filter "*.md" -File -ErrorAction SilentlyContinue
    if ($specFiles) {
        $specCount = $specFiles.Count
        $hasSpecs = $true
    }
}

# ── Get current branch ────────────────────────────────────────
$currentBranch = "main"
try { $currentBranch = git branch --show-current 2>$null } catch {}

# ── Pull latest changes ──────────────────────────────────────
Write-Host "Checking for remote updates..." -ForegroundColor Cyan
try {
    git fetch origin $currentBranch 2>$null
    $behind = git rev-list --count "HEAD..origin/$currentBranch" 2>$null
    if ($behind -and [int]$behind -gt 0) {
        Write-Host "  Pulling $behind new commit(s) from origin/$currentBranch" -ForegroundColor Yellow
        git pull --ff-only origin $currentBranch
        Write-Host "  Up to date." -ForegroundColor Green
    } else {
        Write-Host "  Already up to date." -ForegroundColor Green
    }
} catch {
    Write-Host "  Could not pull (no remote or network issue, continuing anyway)" -ForegroundColor Yellow
}
Write-Host ""

# ── Session log ───────────────────────────────────────────────
$sessionTimestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$sessionLog = Join-Path $logDir "ralph_gemini_${mode}_session_${sessionTimestamp}.log"

# ── Banner ────────────────────────────────────────────────────
Write-Host ""
Write-Host ([string]::new([char]0x2501, 61)) -ForegroundColor Green
Write-Host "              RALPH LOOP (Gemini CLI) STARTING               " -ForegroundColor Green
Write-Host ([string]::new([char]0x2501, 61)) -ForegroundColor Green
Write-Host ""
Write-Host "Mode:     $mode" -ForegroundColor Cyan
$modelDisplay = if ($geminiModel) { $geminiModel } else { "auto-selected (Gemini 3 Flash)" }
Write-Host "Model:    $modelDisplay" -ForegroundColor Cyan
Write-Host "Prompt:   $promptFile" -ForegroundColor Cyan
Write-Host "Branch:   $currentBranch" -ForegroundColor Cyan
$yoloDisplay = if ($yoloEnabled) { "ENABLED" } else { "DISABLED" }
Write-Host "YOLO:     $yoloDisplay" -ForegroundColor Yellow
$gitDisplay = if ($gitAutonomy) { "ENABLED" } else { "DISABLED" }
Write-Host "Git Push: $gitDisplay" -ForegroundColor Yellow
Write-Host "Log:      $sessionLog" -ForegroundColor Cyan
if ($maxIters -gt 0) { Write-Host "Max:      $maxIters iterations" -ForegroundColor Cyan }
Write-Host ""
Write-Host "Work source:" -ForegroundColor Cyan
if ($hasPlan) {
    Write-Host "  + IMPLEMENTATION_PLAN.md (will use this)" -ForegroundColor Green
} else {
    Write-Host "  o IMPLEMENTATION_PLAN.md (not found, that's OK)" -ForegroundColor Yellow
}
if ($hasSpecs) {
    Write-Host "  + specs/ folder ($specCount specs)" -ForegroundColor Green
} else {
    Write-Host "  x specs/ folder (no .md files found)" -ForegroundColor Red
}
Write-Host ""
Write-Host "The loop checks for <promise>DONE</promise> in each iteration." -ForegroundColor Cyan
Write-Host "Agent must verify acceptance criteria before outputting it." -ForegroundColor Cyan
Write-Host ""
Write-Host "Press Ctrl+C to stop the loop" -ForegroundColor Yellow
Write-Host ""

# ── Main loop ─────────────────────────────────────────────────
$iteration = 0
$consecutiveFailures = 0

while ($true) {
    if ($maxIters -gt 0 -and $iteration -ge $maxIters) {
        Write-Host "Reached max iterations: $maxIters" -ForegroundColor Green
        break
    }

    $iteration++
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $iterTimestamp = Get-Date -Format "yyyyMMdd_HHmmss"

    Write-Host ""
    Write-Host ("=" * 20 + " LOOP $iteration " + "=" * 20) -ForegroundColor Magenta
    Write-Host "[$timestamp] Starting iteration $iteration" -ForegroundColor Cyan
    Write-Host ""

    $logFile = Join-Path $logDir "ralph_gemini_${mode}_iter_${iteration}_${iterTimestamp}.log"

    # Build Gemini arguments
    $promptContent = Get-Content (Join-Path $projectDir $promptFile) -Raw
    $geminiArgs = @("-p", $promptContent)
    if ($yoloEnabled) { $geminiArgs += "--yolo" }
    if ($geminiModel) { $geminiArgs += @("-m", $geminiModel) }

    try {
        # Run Gemini CLI natively (avoids ConPTY issues in Git Bash)
        # Tee-Object streams to console + log file in real time.
        # Do NOT assign to $output — that buffers everything.
        # Read the log file afterward for the done-signal check.
        $stderrFile = "$logFile.stderr"
        $savedEAP = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        & $geminiCmd @geminiArgs --output-format text 2>$stderrFile |
            Tee-Object -FilePath $logFile
        $ErrorActionPreference = $savedEAP

        # Append iteration log to session log
        if (Test-Path $logFile) {
            Get-Content $logFile -Raw -ErrorAction SilentlyContinue |
                Out-File $sessionLog -Append -Encoding utf8
        }

        # Show any stderr (informational, e.g. "YOLO mode is enabled")
        if (Test-Path $stderrFile) {
            $stderrContent = Get-Content $stderrFile -Raw -ErrorAction SilentlyContinue
            if ($stderrContent) {
                Write-Host $stderrContent.Trim() -ForegroundColor DarkGray
            }
            Remove-Item $stderrFile -ErrorAction SilentlyContinue
        }

        # Read log file for signal check
        $outputString = ""
        if (Test-Path $logFile) {
            $outputString = Get-Content $logFile -Raw -ErrorAction SilentlyContinue
            if (-not $outputString) { $outputString = "" }
        }

        Write-Host ""
        Write-Host "Gemini execution completed" -ForegroundColor Green

        if ($outputString -match "<promise>(ALL_)?DONE</promise>") {
            $match = [regex]::Match($outputString, "<promise>(ALL_)?DONE</promise>")
            Write-Host "Completion signal detected: $($match.Value)" -ForegroundColor Green
            Write-Host "Task completed successfully!" -ForegroundColor Green
            $consecutiveFailures = 0

            if ($mode -eq "plan") {
                Write-Host ""
                Write-Host "Planning complete!" -ForegroundColor Green
                Write-Host "Run '.\scripts\ralph-loop-gemini.ps1' to start building." -ForegroundColor Cyan
                break
            }
        } else {
            Write-Host "No completion signal found" -ForegroundColor Yellow
            Write-Host "  Agent did not output <promise>DONE</promise> or <promise>ALL_DONE</promise>" -ForegroundColor Yellow
            Write-Host "  Retrying in next iteration..." -ForegroundColor Yellow
            $consecutiveFailures++

            if ($consecutiveFailures -ge $maxConsecutiveFailures) {
                Write-Host ""
                Write-Host "$maxConsecutiveFailures consecutive iterations without completion." -ForegroundColor Red
                Write-Host "  The agent may be stuck. Consider:" -ForegroundColor Red
                Write-Host "  - Checking the logs in $logDir" -ForegroundColor Red
                Write-Host "  - Simplifying the current spec" -ForegroundColor Red
                Write-Host "  - Manually fixing blocking issues" -ForegroundColor Red
                Write-Host ""
                $consecutiveFailures = 0
            }
        }
    } catch {
        Write-Host "Gemini execution failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Check log: $logFile" -ForegroundColor Yellow
        $consecutiveFailures++

        # Log the error
        $_.Exception.Message | Out-File $logFile -Append -Encoding utf8
        $_.Exception.Message | Out-File $sessionLog -Append -Encoding utf8
    }

    # Push changes after each iteration only when enabled
    if ($gitAutonomy) {
        try {
            git push origin $currentBranch 2>$null
        } catch {
            $unpushed = git log "origin/$currentBranch..HEAD" --oneline 2>$null
            if ($unpushed) {
                Write-Host "Push failed, creating remote branch..." -ForegroundColor Yellow
                try { git push -u origin $currentBranch 2>$null } catch {}
            }
        }
    }

    Write-Host ""
    Write-Host "Waiting 2s before next iteration..." -ForegroundColor Cyan
    Start-Sleep -Seconds 2
}

Write-Host ""
Write-Host ([string]::new([char]0x2501, 61)) -ForegroundColor Green
Write-Host "      RALPH LOOP (Gemini) FINISHED ($iteration iterations)   " -ForegroundColor Green
Write-Host ([string]::new([char]0x2501, 61)) -ForegroundColor Green
