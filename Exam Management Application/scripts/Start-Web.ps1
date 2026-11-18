param([int]$Port = 5180)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $projectRoot
. (Join-Path $PSScriptRoot 'Docker-Sql.ps1')
Assert-DockerSql
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = "http://localhost:$Port"
& dotnet run --project '.\Exam.Web\Exam.Web.csproj' --no-launch-profile
exit $LASTEXITCODE
