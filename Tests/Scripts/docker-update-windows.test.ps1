$ErrorActionPreference = 'Stop'

$script = "Documentation/Scripts/Docker/KUKULCAN.SharedKernel.i18n-docker-windows.ps1"

if (-not (Test-Path $script)) {
    throw "Required script not found: $script"
}

$errors = $null
$tokens = $null
$content = Get-Content $script -Raw
[System.Management.Automation.Language.Parser]::ParseInput(
    $content,
    [ref]$tokens,
    [ref]$errors
) | Out-Null

if ($errors.Count -gt 0) {
    throw "PowerShell syntax errors were found."
}

$required = @(
    'kukulcan-i18n',
    'jpardokukulcan/kukulcan-i18n',
    'Docker Hub',
    'docker pull',
    'docker inspect',
    'docker run',
    'PostgreSQL',
    'DB_PASSWORD',
    'DB_USER',
    'JWT_SECRET',
    'openssl',
    '/health/live',
    '/health/ready'
)

foreach ($value in $required) {
    if ($content -notmatch [regex]::Escape($value)) {
        if ($value -eq 'docker pull' -and $content.Contains("Invoke-Docker @('pull'")) { continue }
        if ($value -eq 'docker inspect' -and $content.Contains("Get-DockerOutput @('inspect'")) { continue }
        if ($value -eq 'docker run' -and $content.Contains("'run', '--detach'")) { continue }
        throw "Required Docker update behavior is missing: $value"
    }
}

if ($content -match '(?im)Read-Host[^\r\n]*JWT[ _-]?(secret|password)') {
    throw "The JWT secret must be generated automatically, not requested interactively."
}

if ($content -notmatch '(?im)(Remove-Item|Clear-Item)[^\r\n]*Env:') {
    throw "Environment variables must be explicitly removed during cleanup."
}

Write-Host "Windows Docker update script contract tests passed."
