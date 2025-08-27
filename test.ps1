# Homeschool Manager Testing Script
# Run this script before pushing changes to ensure everything works correctly

Write-Host "Starting Homeschool Manager Test Suite..." -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green

# Step 1: Clean and Restore
Write-Host "Step 1: Cleaning and restoring packages..." -ForegroundColor Yellow
dotnet clean
dotnet restore

# Step 2: Build the solution
Write-Host "Step 2: Building the solution..." -ForegroundColor Yellow
$buildResult = dotnet build --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed! Please fix the errors before continuing." -ForegroundColor Red
    exit 1
}
Write-Host "✅ Build successful!" -ForegroundColor Green

# Step 3: Run unit tests
Write-Host "🧪 Step 3: Running unit tests..." -ForegroundColor Yellow
$testResult = dotnet test --no-build --verbosity normal
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Tests failed! Please fix the failing tests before continuing." -ForegroundColor Red
    exit 1
}
Write-Host "✅ All tests passed!" -ForegroundColor Green

# Step 4: Check for warnings
Write-Host "⚠️  Step 4: Checking for warnings..." -ForegroundColor Yellow
$warnings = dotnet build --no-restore 2>&1 | Select-String "warning"
if ($warnings) {
    Write-Host "⚠️  Warnings found:" -ForegroundColor Yellow
    $warnings | ForEach-Object { Write-Host "   $_" -ForegroundColor Yellow }
} else {
    Write-Host "✅ No warnings found!" -ForegroundColor Green
}

# Step 5: Run the application (optional)
Write-Host "🚀 Step 5: Starting the application for manual testing..." -ForegroundColor Yellow
Write-Host "   The application will start in the background." -ForegroundColor Cyan
Write-Host "   Press Ctrl+C to stop the application when done testing." -ForegroundColor Cyan
Write-Host "   Or close this window to continue with the script." -ForegroundColor Cyan

# Start the application in background
$process = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "src/HomeschoolManager.Web" -PassThru -WindowStyle Hidden

# Wait a moment for the app to start
Start-Sleep -Seconds 5

Write-Host "✅ Application started! You can now test the features manually." -ForegroundColor Green
Write-Host "   URL: https://localhost:5001 or http://localhost:5000" -ForegroundColor Cyan

# Ask user if they want to continue
Write-Host ""
Write-Host "🤔 Manual testing complete? (Y/N): " -NoNewline -ForegroundColor Yellow
$response = Read-Host

if ($response -eq "Y" -or $response -eq "y") {
    # Stop the application
    if ($process -and !$process.HasExited) {
        Stop-Process -Id $process.Id -Force
        Write-Host "✅ Application stopped." -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "🎉 All tests completed successfully!" -ForegroundColor Green
    Write-Host "✅ You can now safely push your changes." -ForegroundColor Green
} else {
    Write-Host "⏸️  Manual testing in progress..." -ForegroundColor Yellow
    Write-Host "   Remember to stop the application when done." -ForegroundColor Cyan
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "🧪 Test Suite Complete!" -ForegroundColor Green
