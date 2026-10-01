# KUKULCAN.SharedKernel.i18n local Docker deployment helper for Windows PowerShell.
# Run from the repository root unless KUKULCAN_I18N_REPO_ROOT is set.

$ErrorActionPreference = 'Stop'

function EnvValue([string]$Name, [string]$DefaultValue) {
    $Value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($Value)) { return $DefaultValue }
    return $Value
}

function Step([string]$Description) {
    $script:StepNumber++
    $Percent = [math]::Floor(($script:StepNumber * 100) / $script:TotalSteps)
    Write-Host ("[{0,3}%] {1}" -f $Percent, $Description)
}

function Fail([string]$Message, [int]$Code = 1) {
    Write-Host "[ERROR] $Message" -ForegroundColor Red
    exit $Code
}

function NativeStep([string]$Description, [string]$Command, [string[]]$Arguments) {
    Step $Description
    Write-Host ("      Command: {0} {1}" -f $Command, ($Arguments -join ' '))
    & $Command @Arguments
    $Code = $LASTEXITCODE
    if ($Code -ne 0) {
        Write-Host "[ERROR] Command failed (exit code $Code)." -ForegroundColor Red
        Write-Host '[ERROR] The command output above is the reported cause.' -ForegroundColor Red
        exit $Code
    }
}

$ImageName = EnvValue 'KUKULCAN_I18N_IMAGE_NAME' 'kukulcan-i18n:local'
$ContainerName = EnvValue 'KUKULCAN_I18N_CONTAINER_NAME' 'kukulcan-i18n'
$NetworkName = EnvValue 'KUKULCAN_I18N_NETWORK_NAME' 'kukulcan-local'
$DbContainer = EnvValue 'KUKULCAN_I18N_DB_CONTAINER' 'mypostgres'
$DbProvider = EnvValue 'KUKULCAN_I18N_DB_PROVIDER' 'PostgresSql'
$DbHost = EnvValue 'KUKULCAN_I18N_DB_HOST' 'mypostgres'
$DbPort = EnvValue 'KUKULCAN_I18N_DB_PORT' '5432'
$DbName = EnvValue 'KUKULCAN_I18N_DB_NAME' 'Atlas'
$DbUser = EnvValue 'KUKULCAN_I18N_DB_USER' 'postgres'
$HttpPort = EnvValue 'KUKULCAN_I18N_HTTP_PORT' '8080'
$AutoMigrate = EnvValue 'KUKULCAN_I18N_AUTO_MIGRATE' 'true'
$SeedData = EnvValue 'KUKULCAN_I18N_SEED_DATA' 'true'
$RepoRoot = EnvValue 'KUKULCAN_I18N_REPO_ROOT' (Get-Location).Path
$DbPassword = [Environment]::GetEnvironmentVariable('KUKULCAN_I18N_DB_PASSWORD')
$JwtSecret = [Environment]::GetEnvironmentVariable('KUKULCAN_I18N_JWT_SECRET')
$RedisConnection = [Environment]::GetEnvironmentVariable('KUKULCAN_I18N_REDIS_CONNECTION')
if ($null -eq $RedisConnection) { $RedisConnection = '' }
$JwtIssuer = 'ATLAS'
$JwtAudience = 'ATLAS.i18n'

$TotalSteps = 11
$StepNumber = 0

Step 'Checking Docker and required tools.'
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { Fail 'Docker CLI is not available in PATH.' }
& docker info *> $null
if ($LASTEXITCODE -ne 0) { Fail 'Docker Desktop/Docker Engine is not running or is not accessible.' }
Write-Host '      Docker is available.'

Step 'Checking the repository and Dockerfile.'
$Dockerfile = Join-Path $RepoRoot 'Dockerfile'
if (-not (Test-Path -LiteralPath $Dockerfile)) { Fail "Dockerfile not found under $RepoRoot." }
Write-Host "      Repository: $RepoRoot"

Step 'Preparing PostgreSQL password and JWT secret.'
if ([string]::IsNullOrWhiteSpace($DbPassword)) {
    $SecurePassword = Read-Host "      PostgreSQL password for $DbUser@$DbContainer" -AsSecureString
    $DbPassword = [System.Net.NetworkCredential]::new('', $SecurePassword).Password
}
if ([string]::IsNullOrWhiteSpace($DbPassword)) { Fail 'PostgreSQL password cannot be empty.' }

if ([string]::IsNullOrWhiteSpace($JwtSecret)) {
    $SecureSecret = Read-Host '      JWT secret (leave empty to generate a local development secret)' -AsSecureString
    $JwtSecret = [System.Net.NetworkCredential]::new('', $SecureSecret).Password
}
if ([string]::IsNullOrWhiteSpace($JwtSecret)) {
    $Bytes = New-Object byte[] 48
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($Bytes)
    $JwtSecret = [Convert]::ToBase64String($Bytes)
    Write-Host '      A temporary local JWT secret was generated.'
}
if ($JwtSecret.Length -lt 32) { Fail 'JWT secret must contain at least 32 characters.' }

Step "Checking PostgreSQL container $DbContainer."
& docker inspect $DbContainer *> $null
if ($LASTEXITCODE -ne 0) { Fail "PostgreSQL container $DbContainer does not exist." }
$DbRunning = (& docker inspect -f '{{.State.Running}}' $DbContainer).Trim()
if ($DbRunning -ne 'true') { Fail "PostgreSQL container $DbContainer is not running." }
Write-Host '      PostgreSQL container is running.'

Step "Ensuring Docker network $NetworkName."
& docker network inspect $NetworkName *> $null
if ($LASTEXITCODE -ne 0) {
    NativeStep "Creating Docker network $NetworkName." 'docker' @('network', 'create', $NetworkName)
} else {
    Write-Host '      Network already exists.'
}

$NetworkContainers = (& docker network inspect $NetworkName --format '{{range .Containers}}{{.Name}}{{printf "\n"}}{{end}}') -split [Environment]::NewLine |
    ForEach-Object { $_.Trim() }

if ($NetworkContainers -contains $DbContainer) {
    Write-Host '      PostgreSQL is already attached to the network.'
} else {
    NativeStep "Connecting PostgreSQL to $NetworkName." 'docker' @('network', 'connect', $NetworkName, $DbContainer)
}

Step "Checking PostgreSQL readiness and database $DbName."
& docker exec $DbContainer pg_isready -U $DbUser
if ($LASTEXITCODE -ne 0) { Fail "PostgreSQL readiness command failed (exit code $LASTEXITCODE)." $LASTEXITCODE }

$DbExists = (& docker exec $DbContainer psql -U $DbUser -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$DbName';").Trim()
if ($LASTEXITCODE -ne 0) { Fail "Database existence command failed (exit code $LASTEXITCODE)." $LASTEXITCODE }
if ($DbExists -ne '1') { Fail "Database $DbName was not found. Create it before starting the API." }

NativeStep "Building Docker image $ImageName." 'docker' @('build', '--tag', $ImageName, $RepoRoot)

Step "Removing existing container $ContainerName, if present."
& docker container inspect $ContainerName *> $null
if ($LASTEXITCODE -eq 0) {
    NativeStep "Removing container $ContainerName." 'docker' @('rm', '--force', $ContainerName)
} else {
    Write-Host '      No existing container found.'
}

$ConnectionString = "Host=$DbHost;Port=$DbPort;Database=$DbName;Username=$DbUser;Password=$DbPassword"

Step "Starting container $ContainerName."
Write-Host "      Image: $ImageName"
Write-Host "      Network: $NetworkName"
Write-Host ("      API port: {0}:8080" -f $HttpPort)
Write-Host ("      Database: {0} on {1}:{2}" -f $DbName, $DbHost, $DbPort)

$RunArguments = @(
    'run', '--detach',
    '--name', $ContainerName,
    '--network', $NetworkName,
    '--publish', ($HttpPort + ':8080'),
    '--env', 'ASPNETCORE_HTTP_PORTS=8080',
    '--env', ('Kukulcan__Database__Provider=' + $DbProvider),
    '--env', ('Kukulcan__Database__ConnectionString=' + $ConnectionString),
    '--env', ('Kukulcan__Database__Migration__AutoMigrateOnStartup=' + $AutoMigrate),
    '--env', ('Kukulcan__Database__Migration__SeedDataOnStartup=' + $SeedData),
    '--env', ('Jwt__SecretKey=' + $JwtSecret),
    '--env', ('Jwt__Issuer=' + $JwtIssuer),
    '--env', ('Jwt__Audience=' + $JwtAudience),
    '--env', ('ConnectionStrings__Redis=' + $RedisConnection),
    $ImageName
)

& docker @RunArguments *> $null
if ($LASTEXITCODE -ne 0) {
    $Code = $LASTEXITCODE
    Write-Host "[ERROR] docker run failed (exit code $Code)." -ForegroundColor Red
    Write-Host '[ERROR] Container logs:'
    & docker logs $ContainerName
    exit $Code
}

Step 'Waiting for liveness endpoint.'
$LiveUrl = 'http://127.0.0.1:' + $HttpPort + '/health/live'
$LiveOk = $false
for ($Attempt = 1; $Attempt -le 30; $Attempt++) {
    try {
        Invoke-WebRequest -Uri $LiveUrl -UseBasicParsing -TimeoutSec 2 | Out-Null
        $LiveOk = $true
        Write-Host "      Liveness is UP (attempt $Attempt/30)."
        break
    } catch {
        Write-Host "      Waiting... ($Attempt/30)"
        Start-Sleep -Seconds 2
    }
}
if (-not $LiveOk) {
    Write-Host '[ERROR] /health/live did not become available.' -ForegroundColor Red
    Write-Host '[ERROR] Container logs:'
    & docker logs $ContainerName
    exit 1
}

Step 'Waiting for readiness endpoint (PostgreSQL).'
$ReadyUrl = 'http://127.0.0.1:' + $HttpPort + '/health/ready'
$ReadyOk = $false
for ($Attempt = 1; $Attempt -le 30; $Attempt++) {
    try {
        Invoke-WebRequest -Uri $ReadyUrl -UseBasicParsing -TimeoutSec 2 | Out-Null
        $ReadyOk = $true
        Write-Host "      Readiness is UP (attempt $Attempt/30)."
        break
    } catch {
        Write-Host "      Waiting... ($Attempt/30)"
        Start-Sleep -Seconds 2
    }
}
if (-not $ReadyOk) {
    Write-Host '[ERROR] /health/ready did not become available.' -ForegroundColor Red
    Write-Host '[ERROR] Container logs:'
    & docker logs $ContainerName
    exit 1
}

Write-Host '[100%] Deployment completed successfully.'
Write-Host ("      API:       http://127.0.0.1:{0}" -f $HttpPort)
Write-Host "      Liveness:  $LiveUrl"
Write-Host "      Readiness: $ReadyUrl"
Write-Host "      Logs:      docker logs -f $ContainerName"
