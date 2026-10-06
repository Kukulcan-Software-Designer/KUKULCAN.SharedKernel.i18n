$ErrorActionPreference = 'Stop'

$envFile = Join-Path $HOME '.config/kukulcan/database.env'
$dotnetToolsPath = Join-Path $HOME '.dotnet/tools'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$apiProject = 'Source/KUKULCAN.SharedKernel.i18n.API/KUKULCAN.SharedKernel.i18n.API.csproj'

function Read-MaskedPassword {
    $password = [System.Text.StringBuilder]::new()
    Write-Host -NoNewline 'Password: '

    while ($true) {
        $key = [Console]::ReadKey($true)

        if ($key.Key -eq [ConsoleKey]::Enter) {
            break
        }

        if ($key.Key -eq [ConsoleKey]::Backspace) {
            if ($password.Length -gt 0) {
                [void]$password.Remove($password.Length - 1, 1)
                Write-Host -NoNewline ([char]8 + ' ' + [char]8)
            }
            continue
        }

        if (-not [char]::IsControl($key.KeyChar)) {
            [void]$password.Append($key.KeyChar)
            Write-Host -NoNewline '*'
        }
    }

    Write-Host
    return $password.ToString()
}

function Escape-ConnectionValue([string]$Value) {
    if ($null -eq $Value) {
        return ''
    }

    return $Value.Replace('\', '\\').Replace('"', '""')
}

function Ensure-EfTool {
    if (Test-Path $dotnetToolsPath) {
        $env:PATH = "$dotnetToolsPath;$env:PATH"
    }

    $efVersion = (& dotnet ef --version 2>$null)
    if ($LASTEXITCODE -ne 0 -or $efVersion -notmatch '^10.') {
        & dotnet tool update --global dotnet-ef --version 10.*
        if ($LASTEXITCODE -ne 0) {
            & dotnet tool install --global dotnet-ef --version 10.*
        }
    }

    $env:PATH = "$dotnetToolsPath;$env:PATH"
}

while ($true) {
    Write-Host 'Seleccione el gestor de base de datos:'
    Write-Host '1. SQL Server'
    Write-Host '2. PostgreSQL'
    Write-Host '3. MySQL'
    Write-Host '0. Salir sin registrar las variables'
    $option = Read-Host 'Opción'

    switch ($option) {
        '1' {
            $provider = 'SqlServer'
            $port = 1433
            $migrationProject = 'Source/KUKULCAN.SharedKernel.i18n.Migrations.SqlServer/KUKULCAN.SharedKernel.i18n.Migrations.SqlServer.csproj'
            break
        }
        '2' {
            $provider = 'PostgresSql'
            $port = 5432
            $migrationProject = 'Source/KUKULCAN.SharedKernel.i18n.Migrations.PostgreSql/KUKULCAN.SharedKernel.i18n.Migrations.PostgreSql.csproj'
            break
        }
        '3' {
            $provider = 'MySql'
            $port = 3306
            $migrationProject = 'Source/KUKULCAN.SharedKernel.i18n.Migrations.MySql/KUKULCAN.SharedKernel.i18n.Migrations.MySql.csproj'
            break
        }
        '0' {
            Write-Host 'Salir sin registrar las variables'
            exit 0
        }
        default {
            Write-Host 'Opción no válida. Inténtelo de nuevo.'
            continue
        }
    }

    break
}

$database = Read-Host 'Database name'
$user = Read-Host 'User'
$password = Read-MaskedPassword

$databaseEscaped = Escape-ConnectionValue $database
$userEscaped = Escape-ConnectionValue $user
$passwordEscaped = Escape-ConnectionValue $password

switch ($provider) {
    'SqlServer' {
        $connectionString = "Server=localhost,$port;Database=$databaseEscaped;User Id=$userEscaped;Password=$passwordEscaped;TrustServerCertificate=True"
    }
    'PostgresSql' {
        $connectionString = "Host=localhost;Port=$port;Database=$databaseEscaped;Username=$userEscaped;Password=$passwordEscaped"
    }
    'MySql' {
        $connectionString = "Server=localhost;Port=$port;Database=$databaseEscaped;User=$userEscaped;Password=$passwordEscaped"
    }
}

$directory = Split-Path -Parent $envFile
New-Item -ItemType Directory -Path $directory -Force | Out-Null

@"
KUKULCAN__DATABASE__PROVIDER=$provider
KUKULCAN__DATABASE__CONNECTIONSTRING=$connectionString
"@ | Set-Content -Path $envFile -Encoding utf8NoBOM

[Environment]::SetEnvironmentVariable('KUKULCAN__DATABASE__PROVIDER', $provider, 'User')
[Environment]::SetEnvironmentVariable('KUKULCAN__DATABASE__CONNECTIONSTRING', $connectionString, 'User')

$env:KUKULCAN__DATABASE__PROVIDER = $provider
$env:KUKULCAN__DATABASE__CONNECTIONSTRING = $connectionString

Set-Location $repoRoot
Ensure-EfTool

Write-Host "Ejecutando migraciones EF Core para $provider..."
& dotnet ef database update --project $migrationProject --startup-project $apiProject --configuration Release

if ($LASTEXITCODE -ne 0) {
    throw "EF Core database migration failed for provider $provider."
}

Write-Host 'Base de datos configurada y migraciones aplicadas correctamente.'
Write-Host "Provider: $provider"
Write-Host 'Host: localhost'
Write-Host "Port: $port"
Write-Host 'La configuración persistirá para futuras sesiones de PowerShell.'
