param([string]$SqlInstance = '.\SQLEXPRESS')
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath (Split-Path $PSScriptRoot -Parent)
# Every SQL test overrides InitialCatalog with a private ExamAppTests_<GUID> database.
$env:EXAM_TEST_CONNECTION = "Server=$SqlInstance;Integrated Security=True;TrustServerCertificate=True;Encrypt=True"
& dotnet test '.\Exam.Tests\Exam.Tests.csproj' -m:1
exit $LASTEXITCODE
