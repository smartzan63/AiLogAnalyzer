function Clean-Directory($dirPath) {
    Write-Host "Cleaning directory: $dirPath..."
    if (Test-Path $dirPath) {
        Remove-Item -Recurse -Force $dirPath
    }
    New-Item -ItemType Directory -Path $dirPath
}

function Clean-TestResults($testResultsPath) {
    Write-Host "Cleaning test results in: $testResultsPath..."
    if (Test-Path $testResultsPath) {
        Get-ChildItem -Path $testResultsPath -Recurse -Filter "coverage.cobertura.xml" | Remove-Item -Force
        Get-ChildItem -Path $testResultsPath -Recurse -Filter "*.trx" | Remove-Item -Force
    }
}

function Run-TestsWithCoverage() {
    Write-Host "Running tests with code coverage..."
    dotnet test --collect:"XPlat Code Coverage;ExcludeByFile=**\Microsoft.NET.Test.Sdk.Program.cs" --no-build
}

function Get-CoverageFiles($testResultsPath) {
    Write-Host "Searching for coverage reports..."
    $coverageFiles = Get-ChildItem -Path $testResultsPath -Recurse -Filter "coverage.cobertura.xml"

    if ($coverageFiles.Count -eq 0) {
        Write-Host "No coverage reports found."
        exit 1
    }

    # Output found files
    $coverageFiles | ForEach-Object { Write-Host "Found report: $($_.FullName)" }

    return $coverageFiles
}

function Generate-HTMLReport($coverageFiles, $reportDir) {
    Write-Host "Generating HTML report at $reportDir..."

    # Combine all report paths into a single string separated by a semicolon
    $coveragePaths = ($coverageFiles | ForEach-Object { $_.FullName }) -join ";"

    # Run report generator to create the HTML report
    reportgenerator -reports:$coveragePaths -targetdir:$reportDir -reporttypes:Html

    # Check if the report was successfully generated
    if (Test-Path "$reportDir\index.html") {
        Write-Host "HTML report generated successfully at $reportDir\index.html"
    } else {
        Write-Host "Failed to generate HTML report."
        exit 1
    }
}

function Open-HTMLReportInBrowser($reportDir) {
    $reportPath = Join-Path $reportDir "index.html"
    if (Test-Path $reportPath) {
        Write-Host "Opening HTML report in the default browser..."
        Start-Process $reportPath
    } else {
        Write-Host "HTML report not found at $reportPath"
    }
}

# Main script logic

# Get the path to the root of the repository (one level above the Code folder)
$repoRoot = Split-Path $PSScriptRoot -Parent

# Path to the coverage report folder
$reportDir = Join-Path $repoRoot "Code\coverage-report"

# Path to the Tests folder where test projects and results are stored
$testResultsPath = Join-Path $repoRoot "Tests"

# Step 1: Clean the report folder before generating a new report
Clean-Directory $reportDir

# Step 2: Clean previous test results
Clean-TestResults $testResultsPath

# Step 3: Run tests with code coverage
Run-TestsWithCoverage

# Step 4: Retrieve coverage report files
$coverageFiles = Get-CoverageFiles($testResultsPath)

# Step 5: Generate the HTML report
Generate-HTMLReport $coverageFiles $reportDir

# Step 6: Open the HTML report in the default browser
Open-HTMLReportInBrowser $reportDir
