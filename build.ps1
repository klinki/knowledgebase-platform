param (
    [Parameter(Mandatory = $false)]
    [ValidateSet("Setup", "Build", "Dev", "DevWithProxy", "Clean", "Backend", "WebFrontend", "Extension", "InfraUp", "InfraDown", "ExtensionBrowser", "Check")]
    [string]$Target = "Build",

    [Parameter(Mandatory = $false)]
    [switch]$LaunchExtensionBrowser,

    [Parameter(Mandatory = $false)]
    [switch]$SkipInfra,

    [Parameter(Mandatory = $false)]
    [switch]$SkipApi,

    [Parameter(Mandatory = $false)]
    [switch]$SkipWorker,

    [Parameter(Mandatory = $false)]
    [switch]$SkipFrontend,

    [Parameter(Mandatory = $false)]
    [switch]$SkipExtensionWatch
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$FrontendUrl = "http://localhost:4200"
$ApiUrl = "http://localhost:5000"
$OpenApiUrl = "$ApiUrl/openapi/v1.json"
$HangfireUrl = "$ApiUrl/hangfire"
$HealthUrl = "$ApiUrl/health"
$PostgresPort = 5432
$PostgresHost = "localhost"
$BackendEnvExamplePath = Join-Path $Root "backend/.env.example"
$BackendEnvPath = Join-Path $Root "backend/.env"
$BackendApiDevelopmentSettingsPath = Join-Path $Root "backend/src/SentinelKnowledgebase.Api/appsettings.Development.json"
$ProxyEnvExamplePath = Join-Path $Root "deploy/.env.proxy.example"
$ProxyEnvPath = Join-Path $Root "deploy/.env.proxy"
$ProxyComposePath = Join-Path $Root "deploy/docker-compose.proxy.yml"

function Write-Header ([string]$Message) {
    Write-Host "`n=== $Message ===" -ForegroundColor Cyan
}

function Write-Success ([string]$Message) {
    Write-Host $Message -ForegroundColor Green
}

function Write-WarningMessage ([string]$Message) {
    Write-Host $Message -ForegroundColor Yellow
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

function Ensure-NodeDependencies {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectPath,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (Test-Path (Join-Path $ProjectPath "node_modules")) {
        Write-Host "Dependencies already installed for $Label. Skipping npm install."
        return
    }

    Write-WarningMessage "Dependencies missing for $Label. Installing now..."
    Install-NodeDependencies -ProjectPath $ProjectPath -Label $Label
}

function Install-PlaywrightBrowser {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectPath,
        [Parameter(Mandatory = $true)][string]$Label
    )

    Write-Host "Installing Playwright Chromium for $Label..."
    Invoke-InDirectory -Path $ProjectPath -Script {
        npx playwright install chromium
    }
}

function ConvertTo-PlainText {
    param([Parameter(Mandatory = $true)][System.Security.SecureString]$SecureValue)

    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureValue)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function Get-EnvValue {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [Parameter(Mandatory = $true)][string]$Name
    )

    foreach ($line in $Lines) {
        if ($line -match "^\s*#") {
            continue
        }

        if ($line -match "^\s*$([regex]::Escape($Name))=(.*)$") {
            return $Matches[1].Trim()
        }
    }

    return $null
}

function Set-EnvValue {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Value
    )

    $escapedName = [regex]::Escape($Name)
    for ($i = 0; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i] -match "^\s*$escapedName=") {
            $Lines[$i] = "$Name=$Value"
            return
        }
    }

    if ($Lines.Count -gt 0 -and $Lines[$Lines.Count - 1] -ne "") {
        $Lines.Add("")
    }

    $Lines.Add("$Name=$Value")
}

function Write-EnvFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [System.Collections.Generic.List[string]]$Lines
    )

    Set-Content -Path $Path -Value $Lines
}

function Test-EnvValueMissing {
    param(
        [AllowNull()][string]$Value,
        [string[]]$PlaceholderValues = @()
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $true
    }

    foreach ($placeholderValue in $PlaceholderValues) {
        if ($Value -eq $placeholderValue) {
            return $true
        }
    }

    return $false
}

function Read-RequiredEnvValue {
    param(
        [Parameter(Mandatory = $true)][string]$Prompt,
        [switch]$Secret
    )

    while ($true) {
        $value = if ($Secret) {
            ConvertTo-PlainText -SecureValue (Read-Host -Prompt $Prompt -AsSecureString)
        }
        else {
            Read-Host -Prompt $Prompt
        }

        if (-not [string]::IsNullOrWhiteSpace($value)) {
            return $value.Trim()
        }

        Write-WarningMessage "A value is required."
    }
}

function Ensure-BackendEnvFile {
    Write-Header "Preparing Backend Environment Variables"

    if (-not (Test-Path $BackendEnvExamplePath)) {
        throw "Missing backend env template: $BackendEnvExamplePath"
    }

    if (-not (Test-Path $BackendEnvPath)) {
        Copy-Item -Path $BackendEnvExamplePath -Destination $BackendEnvPath
        Write-Success "Created backend/.env from backend/.env.example."
    }
    else {
        Write-Host "Found existing backend/.env. Preserving current values."
    }

    $envLines = [System.Collections.Generic.List[string]]::new()
    foreach ($line in Get-Content $BackendEnvPath) {
        $envLines.Add([string]$line)
    }
    $requiredVariables = @(
        @{
            Name = "OPENAI_API_KEY"
            Prompt = "Enter your OpenAI API key"
            Secret = $true
            PlaceholderValues = @("your-api-key-here")
        }
    )

    foreach ($variable in $requiredVariables) {
        $currentValue = Get-EnvValue -Lines $envLines -Name $variable.Name
        if (-not (Test-EnvValueMissing -Value $currentValue -PlaceholderValues $variable.PlaceholderValues)) {
            Write-Success "Using existing value for $($variable.Name)."
            continue
        }

        $resolvedValue = Read-RequiredEnvValue -Prompt $variable.Prompt -Secret:$variable.Secret
        Set-EnvValue -Lines $envLines -Name $variable.Name -Value $resolvedValue
    }

    Write-EnvFile -Path $BackendEnvPath -Lines $envLines
    Write-Success "Backend environment file is ready at backend/.env."
}

function Ensure-ProxyEnvFile {
    Write-Header "Preparing Shared Proxy Environment Variables"

    if (-not (Test-Path $ProxyEnvExamplePath)) {
        throw "Missing proxy env template: $ProxyEnvExamplePath"
    }

    if (-not (Test-Path $ProxyEnvPath)) {
        Copy-Item -Path $ProxyEnvExamplePath -Destination $ProxyEnvPath
        Write-Success "Created deploy/.env.proxy from deploy/.env.proxy.example."
    }
    else {
        Write-Host "Found existing deploy/.env.proxy. Preserving current values."
    }
}

function Test-CommandAvailable {
    param([Parameter(Mandatory = $true)][string]$CommandName)

    return $null -ne (Get-Command $CommandName -ErrorAction SilentlyContinue)
}

function Test-PortAvailable {
    param([Parameter(Mandatory = $true)][int]$Port)

    try {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $Port)
        $listener.Start()
        $listener.Stop()
        return $true
    }
    catch {
        return $false
    }
}

function Wait-ForPort {
    param(
        [Parameter(Mandatory = $true)][string]$Host,
        [Parameter(Mandatory = $true)][int]$Port,
        [int]$TimeoutSeconds = 60,
        [int]$RetryDelaySeconds = 2
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $client = [System.Net.Sockets.TcpClient]::new()
            $connectTask = $client.ConnectAsync($Host, $Port)
            $connected = $connectTask.Wait([TimeSpan]::FromSeconds(2))
            if ($connected -and $client.Connected) {
                $client.Dispose()
                return
            }

            $client.Dispose()
        }
        catch {
        }

        Start-Sleep -Seconds $RetryDelaySeconds
    }

    throw "Timed out waiting for ${Host}:$Port to accept connections."
}

function Test-DockerAvailable {
    if (-not (Test-CommandAvailable -CommandName "docker")) {
        return $false
    }

    & docker info | Out-Null
    return $LASTEXITCODE -eq 0
}

function Check-Environment {
    Write-Header "Checking Local Development Environment"

    $checksPassed = $true
    foreach ($command in @("dotnet", "node", "npm", "docker")) {
        if (Test-CommandAvailable -CommandName $command) {
            Write-Success "[OK] $command is available."
        }
        else {
            Write-Host "[FAIL] $command is not available." -ForegroundColor Red
            $checksPassed = $false
        }
    }

    if (Test-DockerAvailable) {
        Write-Success "[OK] Docker daemon is reachable."
    }
    else {
        Write-Host "[FAIL] Docker daemon is not reachable. Start Docker Desktop before using Dev or InfraUp." -ForegroundColor Red
        $checksPassed = $false
    }

    foreach ($portInfo in @(
        @{ Port = 80; Label = "Shared Caddy proxy (HTTP)" },
        @{ Port = 443; Label = "Shared Caddy proxy (HTTPS)" },
        @{ Port = 4200; Label = "Angular dev server" },
        @{ Port = 5000; Label = "Backend API" },
        @{ Port = $PostgresPort; Label = "PostgreSQL" }
    )) {
        if (Test-PortAvailable -Port $portInfo.Port) {
            Write-Success "[OK] Port $($portInfo.Port) is currently free for $($portInfo.Label)."
        }
        else {
            Write-WarningMessage "[WARN] Port $($portInfo.Port) is already in use. Confirm that is intentional for $($portInfo.Label)."
        }
    }

    Write-Host "`nExpected local URLs:"
    Write-Host "- Frontend: $FrontendUrl"
    Write-Host "- API: $ApiUrl"
    Write-Host "- OpenAPI: $OpenApiUrl"
    Write-Host "- Hangfire: $HangfireUrl"
    Write-Host "- Health: $HealthUrl"

    if (-not $checksPassed) {
        throw "Environment check failed."
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

    Ensure-BackendEnvFile

    Write-Host "Restoring .NET dependencies..."
    & dotnet restore "$Root\knowledgebase-platform.slnx"
    if ($LASTEXITCODE -ne 0) { throw ".NET restore failed" }

    Install-NodeDependencies -ProjectPath (Join-Path $Root "frontend") -Label "web frontend"
    Install-NodeDependencies -ProjectPath (Join-Path $Root "browser-extension") -Label "browser extension"

    Install-PlaywrightBrowser -ProjectPath (Join-Path $Root "frontend") -Label "web frontend"
    Install-PlaywrightBrowser -ProjectPath (Join-Path $Root "browser-extension") -Label "browser extension"
}

function Get-BackendDevelopmentConnectionString {
    if ($env:ConnectionStrings__DefaultConnection) {
        return $env:ConnectionStrings__DefaultConnection
    }

    if (-not (Test-Path $BackendApiDevelopmentSettingsPath)) {
        throw "Missing backend development settings file: $BackendApiDevelopmentSettingsPath"
    }

    $settings = Get-Content $BackendApiDevelopmentSettingsPath | ConvertFrom-Json
    $connectionString = $settings.ConnectionStrings.DefaultConnection
    if ([string]::IsNullOrWhiteSpace($connectionString)) {
        throw "The backend development connection string is not configured in $BackendApiDevelopmentSettingsPath"
    }

    return $connectionString
}

function Apply-BackendMigrations {
    Write-Header "Applying Backend Database Migrations"

    $connectionString = Get-BackendDevelopmentConnectionString
    Write-Host "Waiting for PostgreSQL on ${PostgresHost}:$PostgresPort..."
    Wait-ForPort -Host $PostgresHost -Port $PostgresPort

    $maxAttempts = 5
    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        try {
            Invoke-InDirectory -Path (Join-Path $Root "backend") -Script {
                $env:ConnectionStrings__DefaultConnection = $connectionString
                dotnet ef database update --project src/SentinelKnowledgebase.Migrations/SentinelKnowledgebase.Migrations.csproj --startup-project src/SentinelKnowledgebase.Migrations/SentinelKnowledgebase.Migrations.csproj
            }

            Write-Success "Database migrations applied."
            return
        }
        catch {
            if ($attempt -eq $maxAttempts) {
                throw
            }

            Write-WarningMessage "Migration attempt $attempt failed. Retrying in 3 seconds..."
            Start-Sleep -Seconds 3
        }
    }
}

function Start-Infra {
    param(
        [switch]$IncludeProxy
    )

    Write-Header "Starting Local Infrastructure"

    if ($IncludeProxy) {
        Ensure-ProxyEnvFile

        Invoke-InDirectory -Path $Root -Script {
            docker compose -f $ProxyComposePath --env-file $ProxyEnvPath up -d
        }
    }

    Invoke-InDirectory -Path (Join-Path $Root "backend") -Script {
        docker compose up -d
    }
}

function Stop-Infra {
    Write-Header "Stopping Local Infrastructure"

    Invoke-InDirectory -Path (Join-Path $Root "backend") -Script {
        docker compose down
    }

    if (Test-Path $ProxyEnvPath) {
        Invoke-InDirectory -Path $Root -Script {
            docker compose -f $ProxyComposePath --env-file $ProxyEnvPath down
        }
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

    Ensure-NodeDependencies -ProjectPath $extensionPath -Label "browser extension"
    Install-PlaywrightBrowser -ProjectPath $extensionPath -Label "browser extension"

    Invoke-InDirectory -Path $extensionPath -Script {
        if (!(Test-Path "dist/manifest.json")) {
            npm run build
        }

        node launch-browser.js
    }
}

function Start-DevEnvironment {
    param(
        [switch]$IncludeProxy
    )

    Write-Header "Starting Full Development Environment"

    $startsBackendProcess = -not $SkipApi -or -not $SkipWorker

    if ($startsBackendProcess) {
        Ensure-BackendEnvFile
    }

    if (-not $SkipInfra) {
        Start-Infra -IncludeProxy:$IncludeProxy
    }
    else {
        Write-WarningMessage "Skipping Docker infrastructure startup."
    }

    if ($startsBackendProcess) {
        Apply-BackendMigrations
    }

    if (-not $SkipFrontend) {
        Ensure-NodeDependencies -ProjectPath (Join-Path $Root "frontend") -Label "web frontend"
    }

    if (-not $SkipExtensionWatch -or $LaunchExtensionBrowser) {
        Ensure-NodeDependencies -ProjectPath (Join-Path $Root "browser-extension") -Label "browser extension"
    }

    if (-not $SkipApi) {
        Start-DevProcess -Title "Backend API" `
            -WorkingDirectory (Join-Path $Root "backend") `
            -Command '$env:ASPNETCORE_ENVIRONMENT="Development"; $env:DOTNET_ENVIRONMENT="Development"; $env:ASPNETCORE_URLS="http://localhost:5000"; dotnet watch run --project src/SentinelKnowledgebase.Api'
    }
    else {
        Write-WarningMessage "Skipping backend API."
    }

    if (-not $SkipWorker) {
        Start-DevProcess -Title "Backend Worker" `
            -WorkingDirectory (Join-Path $Root "backend") `
            -Command '$env:DOTNET_ENVIRONMENT="Development"; dotnet watch run --project src/SentinelKnowledgebase.Worker'
    }
    else {
        Write-WarningMessage "Skipping backend worker."
    }

    if (-not $SkipFrontend) {
        Start-DevProcess -Title "Web Frontend" `
            -WorkingDirectory (Join-Path $Root "frontend") `
            -Command "npm run start"
    }
    else {
        Write-WarningMessage "Skipping Angular frontend."
    }

    if (-not $SkipExtensionWatch) {
        Start-DevProcess -Title "Browser Extension Watch" `
            -WorkingDirectory (Join-Path $Root "browser-extension") `
            -Command "npm run watch"
    }
    else {
        Write-WarningMessage "Skipping browser extension watch build."
    }

    if ($LaunchExtensionBrowser) {
        Start-DevProcess -Title "Extension Browser" `
            -WorkingDirectory (Join-Path $Root "browser-extension") `
            -Command "node launch-browser.js"
    }

    Write-Host "`nDev environment started in separate terminals." -ForegroundColor Green
    Write-Host "Local URLs:"
    Write-Host "- Frontend: $FrontendUrl"
    Write-Host "- API: $ApiUrl"
    Write-Host "- OpenAPI: $OpenApiUrl"
    Write-Host "- Hangfire: $HangfireUrl"
    Write-Host "- Health: $HealthUrl"
    Write-Host "- PostgreSQL: localhost:$PostgresPort"
    if ($IncludeProxy -and -not $SkipInfra) {
        Write-Host "- Shared Proxy: https://localhost (if a local domain is routed)"
    }
    Write-Host "`nStarted processes:"
    if (-not $SkipApi) { Write-Host "- Backend API: dotnet watch run (http://localhost:5000)" }
    if (-not $SkipWorker) { Write-Host "- Backend Worker: dotnet watch run" }
    if (-not $SkipFrontend) { Write-Host "- Web Frontend: npm run start ($FrontendUrl)" }
    if (-not $SkipExtensionWatch) { Write-Host "- Browser Extension: npm run watch" }
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
        "Check" {
            Check-Environment
            Write-Host "`nEnvironment check completed successfully." -ForegroundColor Green
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
        "DevWithProxy" {
            Start-DevEnvironment -IncludeProxy
        }
    }
}
catch {
    Write-Host "`nScript FAILED: $_" -ForegroundColor Red
    exit 1
}
