param([string]$ApiUrl = 'http://localhost:5180')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $projectRoot
. (Join-Path $PSScriptRoot 'Docker-Sql.ps1')
Assert-DockerSql
$env:EXAM_API_URL = $ApiUrl
& dotnet run --project '.\Exam.Desktop\Exam.Desktop.csproj'
exit $LASTEXITCODE
