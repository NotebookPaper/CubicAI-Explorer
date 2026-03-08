param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Push-Location (Join-Path $PSScriptRoot "..")
try {
    dotnet run --project tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj
}
finally {
    Pop-Location
}
