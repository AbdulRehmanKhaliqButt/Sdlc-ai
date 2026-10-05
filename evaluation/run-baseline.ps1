$ErrorActionPreference = "Stop"

Write-Host "Scenario 01 - User deactivation"
dotnet test "$PSScriptRoot/scenarios/01-user-deactivation/tests/Evaluation.UserDeactivation.Tests.csproj"

Write-Host "Scenario 02 - Pagination"
dotnet test "$PSScriptRoot/scenarios/02-pagination/tests/Evaluation.Pagination.Tests.csproj"

Write-Host "Scenario 03 - Node monorepo"
npm --prefix "$PSScriptRoot/scenarios/03-monorepo" test

Write-Host "All evaluation baselines passed."
