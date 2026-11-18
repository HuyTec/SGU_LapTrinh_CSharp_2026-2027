param(
    [ValidatePattern('^ExamApp[A-Za-z0-9_]*$')][string]$Database = 'ExamAppDocker',
    [switch]$RunTests
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $projectRoot
& (Join-Path $PSScriptRoot 'Start-SqlDocker.ps1')
$webProject = Join-Path $projectRoot 'Exam.Web\Exam.Web.csproj'
$localDirectory = Join-Path $projectRoot '.local'
New-Item -ItemType Directory -Path $localDirectory -Force | Out-Null
$credentialPath = Join-Path $localDirectory 'demo-credentials.txt'
if (Test-Path -LiteralPath $credentialPath) {
    $demoPassword = (Get-Content -LiteralPath $credentialPath | Where-Object { $_ -like 'password=*' } | Select-Object -First 1).Substring(9)
} else {
    $demoPassword = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(18)) + 'Aa1!'
    @('Local demo only - do not commit or share this file.', 'users=admin,lecturer,lecturer2,student,student2', "password=$demoPassword") | Set-Content -LiteralPath $credentialPath -Encoding utf8
}
$keyPath = Join-Path $localDirectory 'jwt-key.txt'
if (Test-Path -LiteralPath $keyPath) { $signingKey = (Get-Content -LiteralPath $keyPath -Raw).Trim() }
else {
    $signingKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
    Set-Content -LiteralPath $keyPath -Value $signingKey -Encoding utf8
}
$sqlConnection = [System.Data.Common.DbConnectionStringBuilder]::new()
$sqlConnection['Server'] = 'tcp:127.0.0.1,14333'
$sqlConnection['Database'] = $Database
$sqlConnection['User ID'] = 'sa'
$sqlConnection['Password'] = $env:EXAM_SQL_PASSWORD
$sqlConnection['TrustServerCertificate'] = $true
$sqlConnection['Encrypt'] = $true
$connection = $sqlConnection.get_ConnectionString()
foreach ($setting in @(
    @('ConnectionStrings:ExamDb', $connection), @('Jwt:Key', $signingKey),
    @('Jwt:Issuer', 'Exam.Web'), @('Jwt:Audience', 'Exam.Desktop'), @('Seed:Password', $demoPassword)
)) {
    & dotnet user-secrets set $setting[0] $setting[1] --project $webProject | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Cannot save local setting $($setting[0])." }
}
# Keep this invocation consistent even if the shell previously selected SQLEXPRESS.
$env:ConnectionStrings__ExamDb = $connection
$env:Jwt__Key = $signingKey
$env:ASPNETCORE_ENVIRONMENT = 'Development'
& dotnet tool restore
if ($LASTEXITCODE -ne 0) { throw 'Tool restore failed.' }
& dotnet restore '.\Exam Management Application.slnx'
if ($LASTEXITCODE -ne 0) { throw 'Package restore failed.' }
& dotnet build '.\Exam Management Application.slnx' --no-restore -m:1
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
& dotnet ef database update --project '.\Exam.Data' --startup-project '.\Exam.Web' --no-build
if ($LASTEXITCODE -ne 0) { throw 'Database migration failed.' }
& dotnet run --project $webProject --no-build --no-launch-profile -- --seed
if ($LASTEXITCODE -ne 0) { throw 'Demo seeding failed.' }
if ($RunTests) {
    $env:EXAM_TEST_CONNECTION = $connection
    & dotnet test '.\Exam.Tests\Exam.Tests.csproj' --no-build -m:1
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}
Write-Host "Ready. Credentials are stored only at: $credentialPath"
Write-Host 'Start Web: .\scripts\Start-Web.ps1'
Write-Host 'Start Desktop in another terminal: .\scripts\Start-Desktop.ps1'
