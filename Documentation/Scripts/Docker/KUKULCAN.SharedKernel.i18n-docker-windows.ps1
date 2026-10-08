# KUKULCAN.SharedKernel.i18n Docker deployment/update helper for Windows PowerShell.
# Uses the published Docker Hub image and never builds the image locally.

$ErrorActionPreference = 'Stop'

$IMAGE_REPOSITORY = 'jpardokukulcan/kukulcan-i18n'
$CONTAINER_NAME = 'kukulcan-i18n'

function Cleanup {
    $DB_PASSWORD = $null
    $JWT_SECRET = $null
    $CONNECTION_STRING = $null
    $DOCKER_SERVER = $null
    $LATEST_VERSION = $null
    $CURRENT_VERSION = $null
    $CURRENT_IMAGE = $null

    Remove-Variable DB_PASSWORD, JWT_SECRET, CONNECTION_STRING, DOCKER_SERVER,
        LATEST_VERSION, CURRENT_VERSION, CURRENT_IMAGE -ErrorAction SilentlyContinue

    Remove-Item Env:DB_PASSWORD, Env:JWT_SECRET, Env:CONNECTION_STRING -ErrorAction SilentlyContinue
}

function Fail([string]$Message) {
    throw $Message
}

function Prompt-Default([string]$Prompt, [string]$Default) {
    $value = Read-Host ('  ' + $Prompt + ' [' + $Default + ']')
    if ([string]::IsNullOrWhiteSpace($value)) { return $Default }
    return $value
}

function Prompt-Required([string]$Prompt) {
    $value = Read-Host ('  ' + $Prompt)
    if ([string]::IsNullOrWhiteSpace($value)) {
        Fail ($Prompt + ' cannot be empty.')
    }
    return $value
}

function Prompt-Password([string]$Prompt) {
    $secure = Read-Host ('  ' + $Prompt) -AsSecureString
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
        $secure.Dispose()
    }
}

function Prompt-YesNo([string]$Prompt, [string]$Default) {
    $value = Read-Host ('  ' + $Prompt + ' [' + $Default + ']')
    if ([string]::IsNullOrWhiteSpace($value)) { $value = $Default }

    switch ($value.ToUpperInvariant()) {
        'Y' { return 'true' }
        'YES' { return 'true' }
        'N' { return 'false' }
        'NO' { return 'false' }
        default { Fail 'Please answer Y or N.' }
    }
}

function Invoke-Docker([string[]]$Arguments) {
    & docker @script:DOCKER_ARGS @Arguments
    if ($LASTEXITCODE -ne 0) {
        Fail ('Docker command failed (exit code ' + $LASTEXITCODE + ').')
    }
}

function Get-DockerOutput([string[]]$Arguments) {
    $output = & docker @script:DOCKER_ARGS @Arguments
    if ($LASTEXITCODE -ne 0) {
        Fail ('Docker command failed (exit code ' + $LASTEXITCODE + ').')
    }
    return ($output -join [Environment]::NewLine).Trim()
}

function Test-SemVerGreater([string]$Left, [string]$Right) {
    $l = $Left.Split('.')
    $r = $Right.Split('.')

    if ([int]$l[0] -ne [int]$r[0]) {
        return ([int]$l[0] -gt [int]$r[0])
    }
    if ([int]$l[1] -ne [int]$r[1]) {
        return ([int]$l[1] -gt [int]$r[1])
    }
    return ([int]$l[2] -gt [int]$r[2])
}

function Get-LatestDockerHubVersion {
    $uri = "https://registry.hub.docker.com/v2/repositories/$script:IMAGE_REPOSITORY/tags?ordering=last_updated&page_size=100"

    try {
        $response = Invoke-RestMethod -Uri $uri -Method Get
    }
    catch {
        Fail ('Unable to query Docker Hub for ' + $script:IMAGE_REPOSITORY + '. ' + $_.Exception.Message)
    }

    $latest = $null
    foreach ($tag in $response.results) {
        if ($tag.name -match '^\d+\.\d+\.\d+$') {
            if ($null -eq $latest -or (Test-SemVerGreater $tag.name $latest)) {
                $latest = $tag.name
            }
        }
    }

    if ($null -eq $latest) {
        Fail 'No semantic version tag (x.y.z) was found on Docker Hub.'
    }

    return $latest
}

try {
    Write-Host '========================================================'
    Write-Host ' KUKULCAN.SharedKernel.I18N Docker Deployment'
    Write-Host '========================================================'

    Write-Host ''
    Write-Host 'Docker server'
    $DOCKER_SERVER = Prompt-Default 'Docker server (local or tcp://host:port)' 'local'

    $DOCKER_ARGS = @()
    $HEALTH_HOST = '127.0.0.1'

    if ($DOCKER_SERVER -in @('', 'local', 'localhost')) {
        $DOCKER_SERVER = 'local'
    }
    elseif ($DOCKER_SERVER -match '^tcp://([^/:]+):([0-9]+)$') {
        $DOCKER_ARGS = @('--host', $DOCKER_SERVER)
        $HEALTH_HOST = $Matches[1]
    }
    else {
        Fail "Use 'local' or a Docker endpoint in the form tcp://host:port."
    }

    Write-Host ''
    Write-Host 'Docker'
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        Fail 'Docker CLI is not available in PATH.'
    }

    $null = & docker @DOCKER_ARGS info 2>$null
    if ($LASTEXITCODE -ne 0) {
        Fail 'Docker server is not accessible.'
    }
    Write-Host '  Docker connection: OK'

    Write-Host ''
    Write-Host 'Container'
    $CONTAINER_EXISTS = $false
    $CURRENT_IMAGE = $null
    $CURRENT_VERSION = $null

    $null = & docker @DOCKER_ARGS container inspect $CONTAINER_NAME 2>$null
    if ($LASTEXITCODE -eq 0) {
        $CONTAINER_EXISTS = $true
        $CURRENT_IMAGE = Get-DockerOutput @('inspect', '-f', '{{.Config.Image}}', $CONTAINER_NAME)
        if ($CURRENT_IMAGE -match ':([0-9]+\.[0-9]+\.[0-9]+)$') {
            $CURRENT_VERSION = $Matches[1]
            Write-Host ('  Container: ' + $CONTAINER_NAME)
            Write-Host ('  Current image: ' + $CURRENT_IMAGE)
            Write-Host ('  Current version: ' + $CURRENT_VERSION)
        }
        else {
            Write-Host ('  Container: ' + $CONTAINER_NAME)
            Write-Host ('  Current image: ' + $CURRENT_IMAGE)
            Write-Host '  Current version: unknown'
        }
    }
    else {
        Write-Host ('  Container ' + $CONTAINER_NAME + ' does not exist.')
    }

    Write-Host ''
    Write-Host 'Docker Hub'
    $LATEST_VERSION = Get-LatestDockerHubVersion
    Write-Host ('  Latest version: ' + $LATEST_VERSION)

    if (-not $CONTAINER_EXISTS) {
        $ACTION = 'create'
    }
    elseif ([string]::IsNullOrWhiteSpace($CURRENT_VERSION)) {
        $ACTION = 'update'
    }
    elseif (Test-SemVerGreater $LATEST_VERSION $CURRENT_VERSION) {
        $ACTION = 'update'
    }
    else {
        $ACTION = 'none'
    }

    if ($ACTION -eq 'none') {
        Write-Host ''
        Write-Host 'Update'
        Write-Host ('  Current version: ' + $CURRENT_VERSION)
        Write-Host ('  Latest version:  ' + $LATEST_VERSION)
        Write-Host '  No update required.'

        $running = Get-DockerOutput @('inspect', '-f', '{{.State.Running}}', $CONTAINER_NAME)
        if ($running -eq 'true') {
            Write-Host '  Container is already running.'
        }
        else {
            Write-Host '  Container is stopped; it has not been started because no update is required.'
        }
        return
    }

    Write-Host ''
    Write-Host 'Database (PostgreSQL)'
    $DB_HOST = Prompt-Default 'PostgreSQL host' 'mypostgres'
    $DB_PORT = Prompt-Default 'PostgreSQL port' '5432'
    $DB_NAME = Prompt-Default 'PostgreSQL database' 'Atlas'
    $DB_USER = Prompt-Required 'PostgreSQL user'
    $DB_PASSWORD = Prompt-Password 'PostgreSQL password'

    Write-Host ''
    Write-Host 'Docker configuration'
    $NETWORK_NAME = Prompt-Default 'Docker network' 'kukulcan-local'
    $HTTP_PORT = Prompt-Default 'HTTP port' '8080'
    $AUTO_MIGRATE = Prompt-YesNo 'Enable automatic migrations' 'Y'
    $SEED_DATA = Prompt-YesNo 'Enable seed data' 'Y'
    $REDIS_CONNECTION = Prompt-Default 'Redis connection (empty to disable)' ''

    Write-Host ''
    Write-Host 'Application security'
    $JWT_ISSUER = Prompt-Default 'JWT issuer' 'ATLAS'
    $JWT_AUDIENCE = Prompt-Default 'JWT audience' 'ATLAS.i18n'

    Write-Host '  Generating JWT secret...'
    # Linux/macOS use: JWT_SECRET="$(openssl rand -base64 48 | tr -d '\n')"
    if (Get-Command openssl -ErrorAction SilentlyContinue) {
        $JWT_SECRET = ((& openssl rand -base64 48) -join '').Trim()
    }
    else {
        $bytes = New-Object byte[] 48
        [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
        $JWT_SECRET = [Convert]::ToBase64String($bytes)
    }

    if ($JWT_SECRET.Length -lt 32) {
        Fail 'Generated JWT secret is too short.'
    }

    $ConnectionPassword = $DB_PASSWORD.Replace('"', '""')
    $ConnectionUser = $DB_USER.Replace('"', '""')
    $ConnectionHost = $DB_HOST.Replace('"', '""')
    $ConnectionPort = $DB_PORT.Replace('"', '""')
    $ConnectionDatabase = $DB_NAME.Replace('"', '""')
    $Quote = [char]34
    $CONNECTION_STRING = 'Host=' + $Quote + $ConnectionHost + $Quote +
        ';Port=' + $Quote + $ConnectionPort + $Quote +
        ';Database=' + $Quote + $ConnectionDatabase + $Quote +
        ';Username=' + $Quote + $ConnectionUser + $Quote +
        ';Password=' + $Quote + $ConnectionPassword + $Quote

    Write-Host ''
    Write-Host 'Docker network'
    $null = & docker @DOCKER_ARGS network inspect $NETWORK_NAME 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host ('  Network ' + $NETWORK_NAME + ' already exists.')
    }
    else {
        Invoke-Docker @('network', 'create', $NETWORK_NAME)
        Write-Host ('  Network ' + $NETWORK_NAME + ' created.')
    }

    Write-Host ''
    Write-Host 'Image'
    $TARGET_IMAGE = $IMAGE_REPOSITORY + ':' + $LATEST_VERSION
    Invoke-Docker @('pull', $TARGET_IMAGE)
    Write-Host ('  Pulled ' + $TARGET_IMAGE)

    Write-Host ''
    Write-Host 'Container'
    if ($ACTION -eq 'update') {
        $oldVersion = if ([string]::IsNullOrWhiteSpace($CURRENT_VERSION)) { 'unknown' } else { $CURRENT_VERSION }
        Write-Host ('  Updating ' + $CONTAINER_NAME + ': ' + $oldVersion + ' -> ' + $LATEST_VERSION)
        Invoke-Docker @('rm', '--force', $CONTAINER_NAME)
    }
    else {
        Write-Host ('  Creating ' + $CONTAINER_NAME + ' at version ' + $LATEST_VERSION)
    }

    $runArguments = @(
        'run', '--detach',
        '--name', $CONTAINER_NAME,
        '--network', $NETWORK_NAME,
        '--publish', ($HTTP_PORT + ':8080'),
        '--env', 'ASPNETCORE_HTTP_PORTS=8080',
        '--env', 'Kukulcan__Database__Provider=PostgresSql',
        '--env', ('Kukulcan__Database__ConnectionString=' + $CONNECTION_STRING),
        '--env', ('Kukulcan__Database__Migration__AutoMigrateOnStartup=' + $AUTO_MIGRATE),
        '--env', ('Kukulcan__Database__Migration__SeedDataOnStartup=' + $SEED_DATA),
        '--env', ('Jwt__SecretKey=' + $JWT_SECRET),
        '--env', ('Jwt__Issuer=' + $JWT_ISSUER),
        '--env', ('Jwt__Audience=' + $JWT_AUDIENCE),
        '--env', ('ConnectionStrings__Redis=' + $REDIS_CONNECTION),
        $TARGET_IMAGE
    )

    $null = & docker @DOCKER_ARGS @runArguments
    if ($LASTEXITCODE -ne 0) {
        Fail ('Unable to start container ' + $CONTAINER_NAME + '.')
    }

    Write-Host '  Container started.'

    Write-Host ''
    Write-Host 'Health'
    $LIVE_URL = 'http://' + $HEALTH_HOST + ':' + $HTTP_PORT + '/health/live'
    $READY_URL = 'http://' + $HEALTH_HOST + ':' + $HTTP_PORT + '/health/ready'

    $liveOk = $false
    for ($attempt = 1; $attempt -le 30; $attempt++) {
        try {
            Invoke-WebRequest -Uri $LIVE_URL -UseBasicParsing -TimeoutSec 2 | Out-Null
            $liveOk = $true
            break
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }

    if (-not $liveOk) {
        Fail ('/health/live did not become available. Check: docker logs ' + $CONTAINER_NAME)
    }

    $readyOk = $false
    for ($attempt = 1; $attempt -le 30; $attempt++) {
        try {
            Invoke-WebRequest -Uri $READY_URL -UseBasicParsing -TimeoutSec 2 | Out-Null
            $readyOk = $true
            break
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }

    if (-not $readyOk) {
        Fail ('/health/ready did not become available. Check: docker logs ' + $CONTAINER_NAME)
    }

    Write-Host '  /health/live  -> Healthy'
    Write-Host '  /health/ready -> Healthy'
    Write-Host '========================================================'
    Write-Host (' KUKULCAN.SharedKernel.I18N ' + $LATEST_VERSION)
    if ($ACTION -eq 'update') {
        Write-Host ' Status: Updated successfully'
    }
    else {
        Write-Host ' Status: Created successfully'
    }
    Write-Host '========================================================'
}
catch {
    Write-Host ('[ERROR] ' + $_.Exception.Message) -ForegroundColor Red
    exit 1
}
finally {
    Cleanup
}
