$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Docker-Sql.ps1')
Assert-DockerEngine
$projectRoot = Split-Path $PSScriptRoot -Parent
$passwordPath = Join-Path $projectRoot '.local/docker-sql-password.txt'
if (-not $env:EXAM_SQL_PASSWORD -and -not (Test-Path -LiteralPath $passwordPath)) {
    # For an existing volume, enter its current SA password; never reset or remove data.
    $securePassword = Read-Host 'Mật khẩu sa SQL Docker (volume cũ: nhập mật khẩu đã dùng; DB mới: chọn mật khẩu mạnh)' -AsSecureString
    $env:EXAM_SQL_PASSWORD = [System.Net.NetworkCredential]::new('', $securePassword).Password
}
$env:EXAM_SQL_PASSWORD = Get-DockerSqlPassword
if ([string]::IsNullOrWhiteSpace($env:EXAM_SQL_PASSWORD)) { throw 'Mật khẩu SQL không được để trống.' }
& docker compose -f (Join-Path $projectRoot 'team/docker/compose.sql.yml') up -d --wait --wait-timeout 150 sql
if ($LASTEXITCODE -ne 0) { throw 'SQL Docker chưa sẵn sàng. Kiểm tra Docker Desktop, log container và mật khẩu sa.' }
New-Item -ItemType Directory -Path (Split-Path $passwordPath -Parent) -Force | Out-Null
Set-Content -LiteralPath $passwordPath -Value $env:EXAM_SQL_PASSWORD -NoNewline -Encoding utf8
Write-Host 'SQL Docker đã sẵn sàng tại tcp:127.0.0.1,14333. Volume được giữ nguyên.'
