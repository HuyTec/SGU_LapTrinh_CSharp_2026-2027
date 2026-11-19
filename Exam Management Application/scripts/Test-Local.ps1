$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath (Split-Path $PSScriptRoot -Parent)
. (Join-Path $PSScriptRoot 'Docker-Sql.ps1')
Assert-DockerSql
# Every SQL test overrides InitialCatalog with a private ExamAppTests_<GUID> database.
$connection = [System.Data.Common.DbConnectionStringBuilder]::new()
$connection['Server'] = 'tcp:127.0.0.1,14333'
$connection['User ID'] = 'sa'
$connection['Password'] = Get-DockerSqlPassword
$connection['TrustServerCertificate'] = $true
$connection['Encrypt'] = $true
$env:EXAM_TEST_CONNECTION = $connection.get_ConnectionString()
& dotnet test '.\Exam.Tests\Exam.Tests.csproj' -m:1
exit $LASTEXITCODE
