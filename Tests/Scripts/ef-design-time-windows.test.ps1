$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$migrationProject = 'Source/KUKULCAN.SharedKernel.i18n.Migrations.PostgreSql/KUKULCAN.SharedKernel.i18n.Migrations.PostgreSql.csproj'
$databaseConnection = 'Host=localhost;Port=5432;Database=DesignTimeTest;Username=postgre;Password=TestSecret123!'

Set-Location $repoRoot
Remove-Item Env:Jwt__SecretKey -ErrorAction SilentlyContinue
$env:KUKULCAN__DATABASE__PROVIDER = 'PostgresSql'
$env:KUKULCAN__DATABASE__CONNECTIONSTRING = $databaseConnection

& dotnet ef migrations list --project $migrationProject --startup-project $migrationProject --configuration Release
if ($LASTEXITCODE -ne 0) { throw 'EF Core design-time migration discovery failed without Jwt:SecretKey.' }

$content = Get-Content 'Documentation/Scripts/Configure-Database-Windows.ps1' -Raw
if ($content -notmatch '--startup-project \$migrationProject') { throw 'Windows database configuration script must use the migration project as startup project.' }

Write-Host 'Windows EF design-time migration test passed.'
