$ErrorActionPreference = 'Stop'

function Assert-DockerEngine {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw 'Chưa cài Docker Desktop.'
    }
    & docker info --format '{{.ServerVersion}}' 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw 'Docker engine chưa chạy. Mở Docker Desktop (Linux containers) rồi thử lại.'
    }
}

function Assert-DockerSql {
    Assert-DockerEngine
    $running = & docker ps --filter 'label=com.docker.compose.project=exam-team-local' --filter 'label=com.docker.compose.service=sql' --format '{{.ID}}'
    if ($LASTEXITCODE -ne 0 -or -not $running) {
        throw 'SQL Docker chưa chạy. Chạy .\scripts\Start-SqlDocker.ps1 trước.'
    }
}

function Get-DockerSqlPassword {
    $passwordPath = Join-Path (Split-Path $PSScriptRoot -Parent) '.local/docker-sql-password.txt'
    if ($env:EXAM_SQL_PASSWORD) { return $env:EXAM_SQL_PASSWORD }
    if (Test-Path -LiteralPath $passwordPath) {
        return (Get-Content -LiteralPath $passwordPath -Raw).TrimEnd("`r", "`n")
    }
    throw 'Chưa có mật khẩu SQL Docker. Chạy .\scripts\Start-SqlDocker.ps1 trước.'
}
