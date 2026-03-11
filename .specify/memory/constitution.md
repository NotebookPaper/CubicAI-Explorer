# CubicAI Explorer Constitution

> CubicAI Explorer is a modern Windows file manager written in C#/WPF. It is a rewrite of the abandoned Delphi-based CubicExplorer, focused on preserving the original's power-user workflow while modernizing the codebase, keeping the dependency surface small, and shipping a stable native Windows experience.

## Version
1.0.0

## Ralph Upstream

- Source: `https://github.com/fstandhartinger/ralph-wiggum`
- Installed commit: `6022995317363dc3dba3aa0100dc3e40ed83dfff`

---

## Context Detection for AI Agents

This constitution is read by AI agents in two different contexts:

### 1. Interactive Mode
When the user is chatting with you outside of a Ralph loop:
- be concise and practical
- use the existing repo state to answer questions before making assumptions
- help create specs and implementation plans when asked
- preserve the project's current architecture and constraints

### 2. Ralph Loop Mode
When you are running inside a Ralph loop:
- work autonomously within the limits below
- read `IMPLEMENTATION_PLAN.md`, `CONTINUE.md`, and `specs/` before choosing work
- pick the highest-priority incomplete spec or task
- implement it completely
- run validation commands
- only output `<promise>DONE</promise>` when acceptance criteria are fully verified
- output `<promise>ALL_DONE</promise>` when no incomplete specs remain

How to detect:
- if the prompt says to read `IMPLEMENTATION_PLAN.md` or `specs/` and complete work autonomously, you are in Ralph Loop Mode

---

## Core Principles

### I. Faithful Modern Rewrite
Preserve the spirit of CubicExplorer where it matters: tabbed and dual-pane workflow, strong file-management ergonomics, shell-aware behavior, and power-user features. Modernization should improve maintainability and safety without flattening the product into a generic file browser.

### II. Security and File Safety First
All filesystem behavior must stay routed through the project's sanitization and service abstractions. Avoid data-loss paths, command injection, unsafe shell execution, and destructive behavior that is not explicitly intended and validated.

### III. Simplicity and Existing Patterns
Stay on .NET 8 and WPF, use the current MVVM/service structure, and avoid new abstractions or dependencies unless they are clearly justified. Match the surrounding codebase before inventing new patterns.

### IV. Verification Before Completion
Builds and smoke tests are part of the definition of done. Do not claim completion until the relevant validation commands pass and the acceptance criteria are concretely checked.

---

## Technical Stack

| Layer | Technology | Notes |
|-------|------------|-------|
| Framework | .NET 8 WPF | Windows-only desktop app |
| Language | C# | `net8.0-windows` |
| MVVM | CommunityToolkit.Mvvm | ObservableObject, RelayCommand, AsyncRelayCommand |
| Testing | Smoke test harness | `tests/CubicAIExplorer.SmokeTests` |

---

## Project Structure

```text
src/CubicAIExplorer/
  App.xaml(.cs)              application entry and service wiring
  MainWindow.xaml(.cs)       main shell UI
  Models/                    file system, bookmark, tab, settings models
  Services/                  file system, queue, clipboard, settings, shell services
  ViewModels/                MVVM view models
  Views/                     dialogs and supporting windows
tests/CubicAIExplorer.SmokeTests/
  Program.cs                 smoke-test harness
cubicexplorer-src/           original Delphi reference, read-only
```

---

## Ralph Wiggum Configuration

### Autonomy Settings
- **YOLO Mode**: ENABLED
- **Git Autonomy**: ENABLED

### Work Item Source
- **Source**: SpecKit-style specs plus local planning docs
- **Primary locations**:
  - `specs/`
  - `IMPLEMENTATION_PLAN.md`
  - `CONTINUE.md`

### Optional Features
- Telegram notifications: DISABLED
- GitHub Issues as work source: DISABLED
- Completion log artifacts: DISABLED by default

---

## Development Workflow

### Phase 1: Understand current intent
Before changing code, read:
1. `CLAUDE.md`
2. this constitution
3. `IMPLEMENTATION_PLAN.md`
4. `CONTINUE.md`
5. the target spec in `specs/`, if one exists

### Phase 2: Implement
- prefer the existing MVVM and service abstractions
- keep path handling inside `FileSystemService`
- do not modify `cubicexplorer-src/`
- update docs when the product state changes materially

### Phase 3: Validate
Run the relevant commands after implementation:

```bash
dotnet build CubicAIExplorer.sln -v minimal
dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal
tests/CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe
```

Use narrower commands only when the change is clearly isolated and the broader validation would be misleading or impossible.

### Known Test Environment Constraints
- **Symbolic links require admin privileges on Windows.** Smoke tests must NOT attempt to create symlinks without first checking for the privilege (or catching the error silently). Never use modal dialogs or `MessageBox` in smoke tests — they block the headless Ralph loop. If a test cannot run due to missing privileges, skip it with a SKIP message instead of failing or showing a UI prompt.

---

## Specs

Specs live in `specs/` as markdown files. Prefer numbered names such as `001-feature-name.md`.

- lower number = higher priority
- a spec is incomplete unless it explicitly states `## Status: COMPLETE`
- when all specs are complete, re-check `IMPLEMENTATION_PLAN.md` and `CONTINUE.md` before signaling all done

Spec template:
- `https://raw.githubusercontent.com/github/spec-kit/refs/heads/main/templates/spec-template.md`

---

## NR_OF_TRIES

Track attempts per spec using:

```markdown
<!-- NR_OF_TRIES: N -->
```

Increment it on each serious implementation attempt. If a spec reaches 10 attempts, split it into smaller specs instead of brute-forcing.

---

## History

After each spec completion:
- append a one-line summary to `history.md`
- add a detail note in `history/YYYY-MM-DD--spec-name.md` describing decisions, lessons, and failure modes worth preserving

Check history before retrying a difficult spec.

---

## Completion Signal

Only output `<promise>DONE</promise>` when:
- acceptance criteria are fully satisfied
- relevant builds/tests pass
- docs/spec state is updated if required

Only output `<promise>ALL_DONE</promise>` when there is no incomplete spec or planned work item left to execute.
