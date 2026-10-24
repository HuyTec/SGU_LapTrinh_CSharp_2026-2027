param([string]$SqlInstance = '.\SQLEXPRESS', [ValidatePattern('^ExamApp[A-Za-z0-9_]*$')][string]$Database = 'ExamAppDb')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$backupDirectory = Join-Path $projectRoot '.local\backups'
New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null
$backup = Join-Path $backupDirectory "$Database-verification.bak"
$restoreName = 'ExamAppRestore_' + [Guid]::NewGuid().ToString('N').Substring(0,12)
$dataPath = Join-Path $backupDirectory "$restoreName.mdf"
$logPath = Join-Path $backupDirectory "$restoreName.ldf"
function Invoke-CheckedSql([string]$Query) {
    $result = @(& sqlcmd -S $SqlInstance -E -C -b -h -1 -W -d master -Q $Query)
    if ($LASTEXITCODE -ne 0) { throw 'SQL backup/restore operation failed. Check SQL Server service access to .local/backups.' }
    return $result
}
function Sql-Literal([string]$Value) { return $Value.Replace("'", "''") }
$dataName = @(Invoke-CheckedSql "SET NOCOUNT ON; SELECT name FROM sys.master_files WHERE database_id=DB_ID(N'$Database') AND type=0;")
$logName = @(Invoke-CheckedSql "SET NOCOUNT ON; SELECT name FROM sys.master_files WHERE database_id=DB_ID(N'$Database') AND type=1;")
if ($dataName.Count -ne 1 -or $logName.Count -ne 1) { throw 'This demo verification expects exactly one data and one log file.' }
$sourceCount = @(Invoke-CheckedSql "SET NOCOUNT ON; SELECT COUNT_BIG(*) FROM [$Database].dbo.Users;")[0].Trim()
Invoke-CheckedSql "BACKUP DATABASE [$Database] TO DISK=N'$(Sql-Literal $backup)' WITH COPY_ONLY, INIT, CHECKSUM; RESTORE VERIFYONLY FROM DISK=N'$(Sql-Literal $backup)' WITH CHECKSUM;" | Out-Null
try {
    Invoke-CheckedSql "IF DB_ID(N'$restoreName') IS NOT NULL THROW 50000, 'Restore target already exists', 1; RESTORE DATABASE [$restoreName] FROM DISK=N'$(Sql-Literal $backup)' WITH MOVE N'$(Sql-Literal $dataName[0].Trim())' TO N'$(Sql-Literal $dataPath)', MOVE N'$(Sql-Literal $logName[0].Trim())' TO N'$(Sql-Literal $logPath)', CHECKSUM;" | Out-Null
    $restoredCount = @(Invoke-CheckedSql "SET NOCOUNT ON; SELECT COUNT_BIG(*) FROM [$restoreName].dbo.Users;")[0].Trim()
    if ($sourceCount -ne $restoredCount) { throw 'Restored user count does not match the backup source.' }
    $evidenceDirectory = Join-Path $projectRoot 'artifacts\integration'
    New-Item -ItemType Directory -Path $evidenceDirectory -Force | Out-Null
    @("Checked UTC: $([DateTime]::UtcNow.ToString('o'))", 'COPY_ONLY backup + checksum verification: PASS',
      "Restored dedicated database: $restoreName", "Source users: $sourceCount; restored users: $restoredCount",
      'Only the private restore target is removed; the source database is preserved.') |
        Set-Content -LiteralPath (Join-Path $evidenceDirectory 'backup-restore.txt') -Encoding utf8
    Write-Host "Backup, checksum, restore and row-count comparison: PASS ($sourceCount users)."
} finally {
    if (!$restoreName.StartsWith('ExamAppRestore_')) { throw 'Invalid private restore target.' }
    Invoke-CheckedSql "IF DB_ID(N'$restoreName') IS NOT NULL DROP DATABASE [$restoreName];" | Out-Null
}
