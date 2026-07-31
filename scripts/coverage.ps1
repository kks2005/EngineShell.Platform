[CmdletBinding()]
param(
    [switch]$SkipUi,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath(
    (Split-Path -Parent $PSScriptRoot))
$runId = Get-Date -Format "yyyyMMdd-HHmmss"
$runRoot = Join-Path $repositoryRoot ".coverage\runs\$runId"
$coverageRoot = Join-Path $runRoot "raw"
$reportRoot = Join-Path $runRoot "non-ui-report"
$uiReportRoot = Join-Path $runRoot "ui-report"
$overallReportRoot = Join-Path $runRoot "overall-report"
$settingsPath = Join-Path $repositoryRoot "coverage.runsettings"
$uiSettingsPath = Join-Path $repositoryRoot "ui-coverage.config"
$nativeSettingsPath = Join-Path $repositoryRoot "native-coverage.runsettings"
$solutionPath = Join-Path $repositoryRoot "ClientAgnostic.sln"

$nonUiProjects = [ordered]@{
    "application-unit" = "tests\Unit\Application.Tests\Application.Tests.csproj"
    "presentation-unit" = "tests\Unit\Presentation.Tests\Presentation.Tests.csproj"
    "cli-client-unit" = "tests\Unit\CliClient.Tests\CliClient.Tests.csproj"
    "workflow" = "tests\Integration\Workflow.Tests\Integration.Tests.csproj"
    "headless" = "tests\Headless\Headless.Tests.csproj"
}

$nativeProjects = [ordered]@{
    "pinvoke-adapter" = "tests\Integration\PInvoke.Adapter.Tests\PInvoke.Adapter.Tests.csproj"
    "cppcli-adapter" = "tests\Integration\CppCli.Adapter.Tests\CppCli.Adapter.Tests.csproj"
}

$uiProjects = [ordered]@{
    "wpf-ui" = "tests\UI\Wpf.UiTests\Wpf.UiTests.csproj"
    "winui-ui" = "tests\UI\WinUI.UiTests\WinUI.UiTests.csproj"
    "maui-ui" = "tests\UI\Maui.UiTests\Maui.UiTests.csproj"
}

function Assert-LastCommandSucceeded {
    param([Parameter(Mandatory)][string]$Message)

    if ($LASTEXITCODE -ne 0) {
        throw $Message
    }
}

function Get-VisualStudioMSBuild {
    $msbuild = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($null -ne $msbuild) {
        return $msbuild.Source
    }

    $vswhere = Join-Path ${env:ProgramFiles(x86)} `
        "Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path -LiteralPath $vswhere) {
        $discovered = & $vswhere -latest `
            -requires Microsoft.Component.MSBuild `
            -find "MSBuild\**\Bin\MSBuild.exe" |
            Select-Object -First 1

        if ($discovered) {
            return $discovered
        }
    }

    throw "Visual Studio MSBuild was not found. Install Visual Studio 2022 with Desktop development with C++, or run with -SkipBuild after building Debug|x64."
}

function Invoke-VisualStudioBuild {
    param([Parameter(Mandatory)][string]$MSBuildPath)

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $MSBuildPath
    $startInfo.WorkingDirectory = $repositoryRoot
    $startInfo.UseShellExecute = $false
    $startInfo.Arguments = @(
        "`"$solutionPath`""
        "/t:Build"
        "/p:Configuration=Debug"
        "/p:Platform=x64"
        "/nologo"
    ) -join " "

    # Some hosts expose the same Windows environment key with different casing.
    # VC++ rejects that dictionary, so pass MSBuild a case-insensitive,
    # de-duplicated copy.
    $environment = [System.Environment]::GetEnvironmentVariables()
    $startInfo.EnvironmentVariables.Clear()
    $seenKeys = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($key in $environment.Keys) {
        $name = [string]$key
        if ($seenKeys.Add($name)) {
            $startInfo.EnvironmentVariables[$name] =
                [string]$environment[$key]
        }
    }

    $process = [System.Diagnostics.Process]::Start($startInfo)
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) {
        throw "Building the Debug|x64 solution failed with exit code $($process.ExitCode)."
    }
}

function Assert-TestResult {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Layer
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "The '$Layer' test result was not created: $Path"
    }

    [xml]$result = Get-Content -LiteralPath $Path
    $counters = $result.TestRun.ResultSummary.Counters
    $total = [int]$counters.total
    $failed = [int]$counters.failed

    if ($total -eq 0) {
        throw "No tests were discovered for '$Layer'."
    }

    if ($failed -ne 0) {
        throw "'$Layer' reported $failed failed test(s)."
    }

    Write-Host "Validated $Layer`: $total test(s), 0 failed."
}

function Assert-CoverageFile {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Layer
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "The '$Layer' coverage file was not created: $Path"
    }

    [xml]$coverage = Get-Content -LiteralPath $Path
    $root = $coverage.coverage
    if ($null -eq $root) {
        throw "The '$Layer' coverage file is not valid Cobertura XML."
    }

    $valid = [int]$root.'lines-valid'
    $covered = [int]$root.'lines-covered'
    if ($valid -le 0) {
        throw "The '$Layer' coverage file contains no coverable lines."
    }

    if ($covered -lt 0 -or $covered -gt $valid) {
        throw "The '$Layer' coverage totals are invalid: $covered of $valid."
    }

    $percent = [Math]::Round(($covered / $valid) * 100, 1)
    Write-Host "Validated $Layer coverage: $covered/$valid lines ($percent%)."
}

function Get-RepositoryRelativePath {
    param([Parameter(Mandatory)][string]$Path)

    # Windows PowerShell 5.1 runs on .NET Framework, where
    # System.IO.Path.GetRelativePath is not available.
    $root = [System.IO.Path]::GetFullPath($repositoryRoot).
        TrimEnd(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar)
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $rootPrefix = $root + [System.IO.Path]::DirectorySeparatorChar

    if (-not $fullPath.StartsWith(
        $rootPrefix,
        [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Path is outside the repository: $fullPath"
    }

    return $fullPath.Substring($rootPrefix.Length)
}

function Assert-AllTestProjectsConfigured {
    $configured = @($nonUiProjects.Values) +
        @($nativeProjects.Values) +
        @($uiProjects.Values) |
        ForEach-Object { $_.Replace("/", "\").ToLowerInvariant() }

    $discovered = Get-ChildItem `
        -Path (Join-Path $repositoryRoot "tests") `
        -Filter "*.csproj" `
        -Recurse |
        Where-Object {
            [xml]$project = Get-Content -LiteralPath $_.FullName
            @($project.Project.PropertyGroup.IsTestProject) -contains "true"
        } |
        ForEach-Object {
            $relativePath = Get-RepositoryRelativePath $_.FullName
            $relativePath.Replace("/", "\").ToLowerInvariant()
        }

    $missing = @($discovered | Where-Object { $_ -notin $configured })
    $stale = @($configured | Where-Object { $_ -notin $discovered })

    if ($missing.Count -gt 0 -or $stale.Count -gt 0) {
        $details = @()
        if ($missing.Count -gt 0) {
            $details += "not configured: $($missing -join ', ')"
        }
        if ($stale.Count -gt 0) {
            $details += "missing from disk: $($stale -join ', ')"
        }

        throw "Coverage test-project inventory is out of sync ($($details -join '; '))."
    }

    Write-Host "Validated test-project inventory: $($discovered.Count) projects."
}

Assert-AllTestProjectsConfigured

New-Item -ItemType Directory -Path $coverageRoot -Force | Out-Null
Write-Host "Coverage run directory: $runRoot"

dotnet tool restore
Assert-LastCommandSucceeded "Restoring local .NET tools failed."

if (-not $SkipBuild) {
    $msbuildPath = Get-VisualStudioMSBuild
    Write-Host "Building the complete Debug|x64 solution..."
    Invoke-VisualStudioBuild $msbuildPath
}

$nonUiCoverageFiles = @()
foreach ($layer in $nonUiProjects.GetEnumerator()) {
    $projectPath = Join-Path $repositoryRoot $layer.Value
    $resultsDirectory = Join-Path $coverageRoot $layer.Key
    $testArguments = @(
        "test"
        $projectPath
        "--no-build"
        "-p:Configuration=Debug"
        "--collect:XPlat Code Coverage"
        "--settings"
        $settingsPath
        "--results-directory"
        $resultsDirectory
        "--logger"
        "trx;LogFileName=test-results.trx"
    )
    Write-Host "Collecting $($layer.Key) coverage..."
    dotnet @testArguments
    Assert-LastCommandSucceeded `
        "Tests or coverage collection failed for '$($layer.Key)'."

    Assert-TestResult `
        (Join-Path $resultsDirectory "test-results.trx") `
        $layer.Key

    $coverageFiles = @(
        Get-ChildItem `
            -Path $resultsDirectory `
            -Filter "coverage.cobertura.xml" `
            -Recurse
    )
    if ($coverageFiles.Count -eq 0) {
        throw "No coverage file was produced for '$($layer.Key)'."
    }

    $uniqueCoverageFiles = $coverageFiles |
        Group-Object { (Get-FileHash $_.FullName -Algorithm SHA256).Hash } |
        ForEach-Object { $_.Group | Select-Object -First 1 }

    foreach ($coverageFile in $uniqueCoverageFiles) {
        Assert-CoverageFile $coverageFile.FullName $layer.Key
        $nonUiCoverageFiles += $coverageFile.FullName
    }
}

foreach ($layer in $nativeProjects.GetEnumerator()) {
    $projectPath = Join-Path $repositoryRoot $layer.Value
    $layerDirectory = Join-Path $coverageRoot $layer.Key
    $testResults = Join-Path $layerDirectory "test-results"
    $testOutput = Join-Path `
        (Split-Path -Parent $projectPath) `
        "bin\x64\Debug\net9.0-windows"
    $nativeDll = Join-Path $testOutput "Engine.Native.dll"
    $nativePdb = Join-Path `
        $repositoryRoot `
        "src\Engine.Native\bin\x64\Debug\Engine.Native.pdb"

    if (-not (Test-Path -LiteralPath $nativePdb)) {
        throw "Native symbols were not found at '$nativePdb'. Build Debug|x64 before collecting native coverage."
    }

    if (-not (Test-Path -LiteralPath $testOutput)) {
        throw "The '$($layer.Key)' test output was not found at '$testOutput'. Build Debug|x64 before collecting native coverage."
    }

    if (-not (Test-Path -LiteralPath $nativeDll)) {
        throw "The native runtime was not found at '$nativeDll'. Build Debug|x64 before collecting native coverage."
    }

    Copy-Item `
        -LiteralPath $nativePdb `
        -Destination (Join-Path $testOutput "Engine.Native.pdb") `
        -Force
    New-Item -ItemType Directory -Path $layerDirectory -Force | Out-Null

    Write-Host "Collecting managed and native $($layer.Key) coverage..."
    dotnet test $projectPath `
        --no-build `
        -p:Configuration=Debug `
        -p:Platform=x64 `
        --collect:"Code Coverage;Format=Cobertura" `
        --settings $nativeSettingsPath `
        --results-directory $testResults `
        --logger "trx;LogFileName=test-results.trx"
    Assert-LastCommandSucceeded `
        "Tests or native coverage collection failed for '$($layer.Key)'."

    Assert-TestResult `
        (Join-Path $testResults "test-results.trx") `
        $layer.Key

    $coverageFiles = @(
        Get-ChildItem `
            -Path $testResults `
            -Filter "*.cobertura.xml" `
            -Recurse
    )
    if ($coverageFiles.Count -eq 0) {
        throw "No native coverage file was produced for '$($layer.Key)'."
    }

    $coverageFile = $coverageFiles |
        Group-Object { (Get-FileHash $_.FullName -Algorithm SHA256).Hash } |
        ForEach-Object { $_.Group | Select-Object -First 1 } |
        Select-Object -First 1

    Assert-CoverageFile $coverageFile.FullName $layer.Key
    $nonUiCoverageFiles += $coverageFile.FullName
}

$nonUiReports = $nonUiCoverageFiles -join ";"

dotnet tool run reportgenerator `
    "-reports:$nonUiReports" `
    "-targetdir:$reportRoot" `
    "-reporttypes:Html;Cobertura;TextSummary"
Assert-LastCommandSucceeded "Generating the non-UI coverage report failed."
Assert-CoverageFile `
    (Join-Path $reportRoot "Cobertura.xml") `
    "merged non-UI"

Write-Host "Non-UI coverage report:"
Write-Host (Join-Path $reportRoot "index.html")

if ($SkipUi) {
    Write-Host "UI tests and UI coverage were skipped by request."
    exit 0
}

$uiCoverageFiles = @()
foreach ($layer in $uiProjects.GetEnumerator()) {
    $projectPath = Join-Path $repositoryRoot $layer.Value
    $layerDirectory = Join-Path $coverageRoot $layer.Key
    $coverageFile = Join-Path $layerDirectory "coverage.cobertura.xml"
    $testResults = Join-Path $layerDirectory "test-results"
    New-Item -ItemType Directory -Path $layerDirectory -Force | Out-Null

    Write-Host "Collecting out-of-process $($layer.Key) coverage..."
    dotnet tool run dotnet-coverage collect `
        --settings $uiSettingsPath `
        --output $coverageFile `
        --output-format cobertura `
        dotnet test $projectPath `
        --no-build `
        -p:Configuration=Debug `
        --results-directory $testResults `
        --logger "trx;LogFileName=test-results.trx"
    Assert-LastCommandSucceeded `
        "Tests or process-level coverage collection failed for '$($layer.Key)'."

    Assert-TestResult `
        (Join-Path $testResults "test-results.trx") `
        $layer.Key
    Assert-CoverageFile $coverageFile $layer.Key
    $uiCoverageFiles += $coverageFile
}

$uiReports = $uiCoverageFiles -join ";"
dotnet tool run reportgenerator `
    "-reports:$uiReports" `
    "-targetdir:$uiReportRoot" `
    "-reporttypes:Html;Cobertura;TextSummary"
Assert-LastCommandSucceeded "Generating the merged UI coverage report failed."
Assert-CoverageFile `
    (Join-Path $uiReportRoot "Cobertura.xml") `
    "merged UI"

$overallReports = "$nonUiReports;$uiReports"
dotnet tool run reportgenerator `
    "-reports:$overallReports" `
    "-targetdir:$overallReportRoot" `
    "-reporttypes:Html;Cobertura;TextSummary"
Assert-LastCommandSucceeded "Generating the overall coverage report failed."
Assert-CoverageFile `
    (Join-Path $overallReportRoot "Cobertura.xml") `
    "overall"

Write-Host "UI-driven coverage report:"
Write-Host (Join-Path $uiReportRoot "index.html")
Write-Host "Overall coverage report:"
Write-Host (Join-Path $overallReportRoot "index.html")
