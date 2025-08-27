# Pre-Push Hook Script for Homeschool Manager
# This script runs automatically before pushing changes

Write-Host "🚀 Pre-Push Validation - Homeschool Manager" -ForegroundColor Green
Write-Host "===========================================" -ForegroundColor Green

# Check if there are any uncommitted changes
$status = git status --porcelain
if ($status) {
    Write-Host "⚠️  Warning: You have uncommitted changes!" -ForegroundColor Yellow
    Write-Host "   Consider committing your changes before pushing." -ForegroundColor Yellow
}

# Build the solution
Write-Host "🔨 Building solution..." -ForegroundColor Yellow
$buildResult = dotnet build --no-restore --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed! Cannot push changes." -ForegroundColor Red
    Write-Host "   Please fix build errors before pushing." -ForegroundColor Red
    exit 1
}

# Run tests
Write-Host "🧪 Running tests..." -ForegroundColor Yellow
$testResult = dotnet test --no-build --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Tests failed! Cannot push changes." -ForegroundColor Red
    Write-Host "   Please fix failing tests before pushing." -ForegroundColor Red
    exit 1
}

# Check for critical warnings
Write-Host "⚠️  Checking for warnings..." -ForegroundColor Yellow
$warnings = dotnet build --no-restore 2>&1 | Select-String "warning CS"
if ($warnings) {
    Write-Host "⚠️  Code warnings found:" -ForegroundColor Yellow
    $warnings | ForEach-Object { Write-Host "   $_" -ForegroundColor Yellow }
    Write-Host "   Consider fixing warnings before pushing." -ForegroundColor Yellow
}

Write-Host "✅ Pre-push validation passed!" -ForegroundColor Green
Write-Host "🚀 Ready to push changes!" -ForegroundColor Green
