[CmdletBinding()]
param(
    [switch]$SkipUi
)

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$coverageRoot = Join-Path $repositoryRoot "artifacts\coverage"
$reportRoot = Join-Path $repositoryRoot "artifacts\coverage-report"
$uiReportRoot = Join-Path $repositoryRoot "artifacts\ui-coverage-report"
$overallReportRoot = Join-Path $repositoryRoot "artifacts\overall-coverage-report"
$settingsPath = Join-Path $repositoryRoot "coverage.runsettings"
$uiSettingsPath = Join-Path $repositoryRoot "ui-coverage.config"

$resolvedRepositoryRoot = [System.IO.Path]::GetFullPath($repositoryRoot)
$resolvedCoverageRoot = [System.IO.Path]::GetFullPath($coverageRoot)
$resolvedReportRoot = [System.IO.Path]::GetFullPath($reportRoot)
$resolvedUiReportRoot = [System.IO.Path]::GetFullPath($uiReportRoot)
$resolvedOverallReportRoot = [System.IO.Path]::GetFullPath($overallReportRoot)

$outputPaths = @(
    $resolvedCoverageRoot
    $resolvedReportRoot
    $resolvedUiReportRoot
    $resolvedOverallReportRoot
)

foreach ($outputPath in $outputPaths) {
    if (-not $outputPath.StartsWith(
            $resolvedRepositoryRoot,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Coverage output paths must remain inside the repository."
    }

    if (Test-Path -LiteralPath $outputPath) {
        Remove-Item -LiteralPath $outputPath -Recurse -Force
    }
}

New-Item -ItemType Directory -Path $resolvedCoverageRoot -Force | Out-Null

$testProjects = [ordered]@{
    "application-unit" = "tests\Unit\Application.Tests\Application.Tests.csproj"
    "presentation-unit" = "tests\Unit\Presentation.Tests\Presentation.Tests.csproj"
    "integration" = "tests\Integration\Workflow.Tests\Integration.Tests.csproj"
    "headless" = "tests\Headless\Headless.Tests.csproj"
}
$uiTestProject = Join-Path $resolvedRepositoryRoot "tests\UI\Wpf.UiTests\Wpf.UiTests.csproj"

dotnet tool restore
if ($LASTEXITCODE -ne 0) {
    throw "Restoring local .NET tools failed."
}

foreach ($project in $testProjects.Values) {
    $projectPath = Join-Path $resolvedRepositoryRoot $project
    Write-Host "Restoring $project..."
    dotnet restore $projectPath

    if ($LASTEXITCODE -ne 0) {
        throw "NuGet restore failed for '$project'."
    }
}

if (-not $SkipUi) {
    Write-Host "Restoring tests\UI\Wpf.UiTests\Wpf.UiTests.csproj..."
    dotnet restore $uiTestProject

    if ($LASTEXITCODE -ne 0) {
        throw "NuGet restore failed for the WPF UI test project."
    }
}

foreach ($layer in $testProjects.GetEnumerator()) {
    $projectPath = Join-Path $resolvedRepositoryRoot $layer.Value
    $resultsDirectory = Join-Path $resolvedCoverageRoot $layer.Key

    Write-Host "Collecting $($layer.Key) coverage..."
    dotnet test $projectPath `
        --no-restore `
        --collect:"XPlat Code Coverage" `
        --settings $settingsPath `
        --results-directory $resultsDirectory `
        --logger "trx;LogFileName=test-results.trx"

    if ($LASTEXITCODE -ne 0) {
        throw "Tests or coverage collection failed for '$($layer.Key)'."
    }

    [xml]$testResult = Get-Content (
        Join-Path $resultsDirectory "test-results.trx")
    if ([int]$testResult.TestRun.ResultSummary.Counters.total -eq 0) {
        throw "No tests were discovered for '$($layer.Key)'."
    }
}

$nonUiReportPatterns = $testProjects.Keys |
    ForEach-Object {
        Join-Path $resolvedCoverageRoot "$_\**\coverage.cobertura.xml"
    }
$nonUiReports = $nonUiReportPatterns -join ";"

dotnet tool run reportgenerator `
    "-reports:$nonUiReports" `
    "-targetdir:$resolvedReportRoot" `
    "-reporttypes:Html;Cobertura;TextSummary"

if ($LASTEXITCODE -ne 0) {
    throw "Generating the non-UI coverage report failed."
}

Write-Host "Non-UI coverage report:"
Write-Host (Join-Path $resolvedReportRoot "index.html")

if ($SkipUi) {
    Write-Host "UI coverage was skipped."
    exit 0
}

$uiCoverageDirectory = Join-Path $resolvedCoverageRoot "ui"
$uiCoverageFile = Join-Path $uiCoverageDirectory "coverage.cobertura.xml"
$uiTestResults = Join-Path $uiCoverageDirectory "test-results"
New-Item -ItemType Directory -Path $uiCoverageDirectory -Force | Out-Null

Write-Host "Collecting out-of-process WPF UI coverage..."
dotnet tool run dotnet-coverage collect `
    --settings $uiSettingsPath `
    --output $uiCoverageFile `
    --output-format cobertura `
    dotnet test $uiTestProject `
    --no-restore `
    --results-directory $uiTestResults `
    --logger "trx;LogFileName=test-results.trx"

if ($LASTEXITCODE -ne 0) {
    throw "UI tests or process-level coverage collection failed."
}

[xml]$uiTestResult = Get-Content (
    Join-Path $uiTestResults "test-results.trx")
if ([int]$uiTestResult.TestRun.ResultSummary.Counters.total -eq 0) {
    throw "No WPF UI tests were discovered."
}

dotnet tool run reportgenerator `
    "-reports:$uiCoverageFile" `
    "-targetdir:$resolvedUiReportRoot" `
    "-reporttypes:Html;Cobertura;TextSummary"

if ($LASTEXITCODE -ne 0) {
    throw "Generating the UI coverage report failed."
}

$overallReports = "$nonUiReports;$uiCoverageFile"
dotnet tool run reportgenerator `
    "-reports:$overallReports" `
    "-targetdir:$resolvedOverallReportRoot" `
    "-reporttypes:Html;Cobertura;TextSummary"

if ($LASTEXITCODE -ne 0) {
    throw "Generating the overall coverage report failed."
}

Write-Host "UI-driven coverage report:"
Write-Host (Join-Path $resolvedUiReportRoot "index.html")
Write-Host "Overall coverage report:"
Write-Host (Join-Path $resolvedOverallReportRoot "index.html")
