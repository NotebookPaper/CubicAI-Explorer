param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RalphArgs
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir
$bashScript = Join-Path $scriptDir "ralph-loop-codex.sh"

if (-not (Test-Path $bashScript)) {
    throw "Ralph script not found: $bashScript"
}

function Get-GitBash {
    $candidates = @(
        "C:\Program Files\Git\bin\bash.exe",
        "C:\Program Files\Git\usr\bin\bash.exe",
        "C:\Program Files (x86)\Git\bin\bash.exe",
        "C:\Program Files (x86)\Git\usr\bin\bash.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    $command = Get-Command bash.exe -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    return $null
}

function Format-BashArgument {
    param([string]$Value)

    if ($null -eq $Value) {
        return "''"
    }

    $escapedSingleQuote = [string]::Concat("'", [char]34, "'", [char]34, "'")
    return "'" + ($Value -replace "'", $escapedSingleQuote) + "'"
}

$gitBash = Get-GitBash

if ($gitBash) {
    Push-Location $projectDir
    try {
        & $gitBash $bashScript @RalphArgs
        exit $LASTEXITCODE
    }
    finally {
        Pop-Location
    }
}

$wsl = Get-Command wsl.exe -ErrorAction SilentlyContinue
if ($wsl) {
    $joinedArgs = ($RalphArgs | ForEach-Object { Format-BashArgument $_ }) -join " "
    $wslProjectDir = "/mnt/" + $projectDir.Substring(0, 1).ToLowerInvariant() + $projectDir.Substring(2).Replace("\", "/")
    $wslCommand = "cd $(Format-BashArgument $wslProjectDir) && ./scripts/ralph-loop-codex.sh"
    if (-not [string]::IsNullOrWhiteSpace($joinedArgs)) {
        $wslCommand += " $joinedArgs"
    }

    & $wsl.Source bash -lc $wslCommand
    exit $LASTEXITCODE
}

throw "No bash runtime found. Install Git for Windows (Git Bash) or WSL, then rerun scripts\ralph-loop-codex.ps1."
