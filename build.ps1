param (
    [Parameter(Mandatory = $false)]
    [ValidateSet("Setup", "Build", "Dev", "Clean", "Backend", "WebFrontend", "Extension", "InfraUp", "InfraDown", "ExtensionBrowser")]
    [string]$Target = "Build",

    [Parameter(Mandatory = $false)]
    [switch]$LaunchExtensionBrowser
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Write-Header ([string]$Message) {
    Write-Host "`n=== $Message ===" -ForegroundColor Cyan
}

function Invoke-InDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][scriptblock]$Script
    )

    Push-Location $Path
    try {
        & $Script
        if ($LASTEXITCODE -ne 0) {
            throw "Command failed in $Path"
        }
    }
    finally {
        Pop-Location
    }
}

function Install-NodeDependencies {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectPath,
        [Parameter(Mandatory = $true)][string]$Label
    )

    Write-Host "Installing dependencies for $Label..."
    Invoke-InDirectory -Path $ProjectPath -Script {
        if (Test-Path "package-lock.json") {
            npm ci
        }
        else {
            npm install
        }
    }
}

function Build-Backend {
    Write-Header "Building Backend (.NET)"
    & dotnet build "$Root\knowledgebase-platform.slnx" -c Debug
    if ($LASTEXITCODE -ne 0) { throw "Backend build failed" }
}

function Build-WebFrontend {
    Write-Header "Building Web Frontend (Angular)"
    $frontendPath = Join-Path $Root "frontend"
    Install-NodeDependencies -ProjectPath $frontendPath -Label "web frontend"
    Invoke-InDirectory -Path $frontendPath -Script {
        npm run build
    }
}

function Build-Extension {
    Write-Header "Building Browser Extension"
    $extensionPath = Join-Path $Root "browser-extension"
    Install-NodeDependencies -ProjectPath $extensionPath -Label "browser extension"
    Invoke-InDirectory -Path $extensionPath -Script {
        npm run build
    }
}

function Setup-All {
    Write-Header "Setting Up Development Dependencies"

    Write-Host "Restoring .NET dependencies..."
    & dotnet restore "$Root\knowledgebase-platform.slnx"
    if ($LASTEXITCODE -ne 0) { throw ".NET restore failed" }

    Install-NodeDependencies -ProjectPath (Join-Path $Root "frontend") -Label "web frontend"
    Install-NodeDependencies -ProjectPath (Join-Path $Root "browser-extension") -Label "browser extension"

    Write-Host "Installing Playwright browser for extension E2E/browser launch..."
    Invoke-InDirectory -Path (Join-Path $Root "browser-extension") -Script {
        npx playwright install chromium
    }
}

function Start-Infra {
    Write-Header "Starting Backend Infrastructure"
    Invoke-InDirectory -Path (Join-Path $Root "backend") -Script {
        docker compose up -d
    }
}

function Stop-Infra {
    Write-Header "Stopping Backend Infrastructure"
    Invoke-InDirectory -Path (Join-Path $Root "backend") -Script {
        docker compose down
    }
}

function Remove-PathIfExists {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (Test-Path $Path) {
        Remove-Item -Path $Path -Recurse -Force
    }
}

function Clean-All {
    Write-Header "Cleaning Projects"

    Write-Host "Cleaning Backend..."
    & dotnet clean "$Root\knowledgebase-platform.slnx"
    if ($LASTEXITCODE -ne 0) { throw "Backend clean failed" }

    Write-Host "Cleaning web frontend dist..."
    Remove-PathIfExists -Path (Join-Path $Root "frontend\dist")

    Write-Host "Cleaning extension build artifacts..."
    Invoke-InDirectory -Path (Join-Path $Root "browser-extension") -Script {
        npm run clean
    }
}

function Start-DevProcess {
    param(
        [Parameter(Mandatory = $true)][string]$Title,
        [Parameter(Mandatory = $true)][string]$WorkingDirectory,
        [Parameter(Mandatory = $true)][string]$Command
    )

    $fullCommand = "Set-Location -LiteralPath '$WorkingDirectory'; $Command"
    Start-Process -FilePath "pwsh" -ArgumentList "-NoExit", "-Command", $fullCommand -WindowStyle Normal | Out-Null
    Write-Host "Started: $Title"
}

function Run-ExtensionBrowser {
    Write-Header "Launching Browser with Extension"
    $extensionPath = Join-Path $Root "browser-extension"

    Invoke-InDirectory -Path $extensionPath -Script {
        if (!(Test-Path "dist/manifest.json")) {
            npm run build
        }

        if (!(Test-Path "node_modules")) {
            if (Test-Path "package-lock.json") { npm ci } else { npm install }
        }

        npx playwright install chromium
        node launch-browser.js
    }
}

function Start-DevEnvironment {
    Write-Header "Starting Full Development Environment"

    Start-Infra

    Install-NodeDependencies -ProjectPath (Join-Path $Root "frontend") -Label "web frontend"
    Install-NodeDependencies -ProjectPath (Join-Path $Root "browser-extension") -Label "browser extension"

    Start-DevProcess -Title "Backend API" `
        -WorkingDirectory (Join-Path $Root "backend") `
        -Command "$env:ASPNETCORE_ENVIRONMENT='Development'; $env:DOTNET_ENVIRONMENT='Development'; dotnet watch run --project src/SentinelKnowledgebase.Api"

    Start-DevProcess -Title "Web Frontend" `
        -WorkingDirectory (Join-Path $Root "frontend") `
        -Command "npm run start"

    Start-DevProcess -Title "Browser Extension Watch" `
        -WorkingDirectory (Join-Path $Root "browser-extension") `
        -Command "npm run watch"

    if ($LaunchExtensionBrowser) {
        Start-DevProcess -Title "Extension Browser" `
            -WorkingDirectory (Join-Path $Root "browser-extension") `
            -Command "node launch-browser.js"
    }

    Write-Host "`nDev environment started in separate terminals." -ForegroundColor Green
    Write-Host "- Backend API: dotnet watch"
    Write-Host "- Web Frontend: npm run start (Angular)"
    Write-Host "- Browser Extension: npm run watch"
    if ($LaunchExtensionBrowser) {
        Write-Host "- Chromium with extension: launch-browser.js"
    }
}

try {
    switch ($Target) {
        "Setup" {
            Setup-All
            Write-Host "`nSetup completed successfully." -ForegroundColor Green
        }
        "Build" {
            Build-Backend
            Build-WebFrontend
            Build-Extension
            Write-Host "`nFull build completed successfully." -ForegroundColor Green
        }
        "Backend" {
            Build-Backend
            Write-Host "`nBackend build completed successfully." -ForegroundColor Green
        }
        "WebFrontend" {
            Build-WebFrontend
            Write-Host "`nWeb frontend build completed successfully." -ForegroundColor Green
        }
        "Extension" {
            Build-Extension
            Write-Host "`nBrowser extension build completed successfully." -ForegroundColor Green
        }
        "InfraUp" {
            Start-Infra
            Write-Host "`nInfrastructure is running." -ForegroundColor Green
        }
        "InfraDown" {
            Stop-Infra
            Write-Host "`nInfrastructure stopped." -ForegroundColor Green
        }
        "Clean" {
            Clean-All
            Write-Host "`nCleaning completed." -ForegroundColor Green
        }
        "ExtensionBrowser" {
            Run-ExtensionBrowser
        }
        "Dev" {
            Start-DevEnvironment
        }
    }
}
catch {
    Write-Host "`nScript FAILED: $_" -ForegroundColor Red
    exit 1
}
