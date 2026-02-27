param (
    [Parameter(Mandatory=$false)]
    [ValidateSet("All", "Backend", "Frontend", "Clean", "Run")]
    [string]$Target = "All"
)

$ErrorActionPreference = "Stop"

function Write-Header ([string]$Message) {
    Write-Host "`n=== $Message ===" -ForegroundColor Cyan
}

function Build-Backend {
    Write-Header "Building Backend (.NET)"
    dotnet build knowledgebase-platform.slnx -c Debug
    if ($LASTEXITCODE -ne 0) { throw "Backend build failed" }
}

function Build-Frontend {
    Write-Header "Building Frontend (Browser Extension)"
    Push-Location browser-extension
    try {
        npm install
        npm run build
        if ($LASTEXITCODE -ne 0) { throw "Frontend build failed" }
    }
    finally {
        Pop-Location
    }
}

function Clean-All {
    Write-Header "Cleaning Projects"
    
    Write-Host "Cleaning Backend..."
    dotnet clean knowledgebase-platform.slnx
    
    Write-Host "Cleaning Frontend..."
    Push-Location browser-extension
    try {
        npm run clean
    }
    finally {
        Pop-Location
    }
}

function Run-Environment {
    Write-Header "Starting Development Environment"
    
    Write-Host "Ensuring Docker containers are running..."
    Push-Location backend
    try {
        docker-compose up -d
        if ($LASTEXITCODE -ne 0) { throw "Docker-compose failed" }
    }
    finally {
        Pop-Location
    }

    Write-Host "Building Frontend..."
    Build-Frontend

    Write-Host "Launching Browser with Extension..."
    Push-Location browser-extension
    try {
        if (!(Test-Path node_modules)) {
            npm install
        }
        npx playwright install chromium
        node launch-browser.js
    }
    finally {
        Pop-Location
    }
}

try {
    switch ($Target) {
        "All" {
            Build-Backend
            Build-Frontend
            Write-Host "`nFull build completed successfully!" -ForegroundColor Green
        }
        "Backend" {
            Build-Backend
            Write-Host "`nBackend build completed successfully!" -ForegroundColor Green
        }
        "Frontend" {
            Build-Frontend
            Write-Host "`nFrontend build completed successfully!" -ForegroundColor Green
        }
        "Clean" {
            Clean-All
            Write-Host "`nCleaning completed!" -ForegroundColor Green
        }
        "Run" {
            Run-Environment
        }
    }
}
catch {
    Write-Host "`nBuild FAILED: $_" -ForegroundColor Red
    exit 1
}
