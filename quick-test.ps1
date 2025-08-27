# Quick Test Script for Homeschool Manager
# Use this for fast feedback during development

Write-Host "Quick Test - Homeschool Manager" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green

# Quick build check
Write-Host "Building..." -ForegroundColor Yellow
$buildResult = dotnet build --no-restore --verbosity quiet
if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful!" -ForegroundColor Green
} else {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Quick test run
Write-Host "Running tests..." -ForegroundColor Yellow
$testResult = dotnet test --no-build --verbosity quiet
if ($LASTEXITCODE -eq 0) {
    Write-Host "Tests passed!" -ForegroundColor Green
} else {
    Write-Host "Tests failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Quick test completed successfully!" -ForegroundColor Green
