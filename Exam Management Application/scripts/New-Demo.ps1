param([string]$BaseUrl = 'http://localhost:5180', [ValidateRange(2,120)][int]$Minutes = 5)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$credentialPath = Join-Path $projectRoot '.local\demo-credentials.txt'
if (!(Test-Path -LiteralPath $credentialPath)) { throw 'Run Initialize-Local.ps1 first.' }
$demoPassword = (Get-Content -LiteralPath $credentialPath | Where-Object { $_ -like 'password=*' } | Select-Object -First 1).Substring(9)
function Invoke-DemoApi([string]$Path, [object]$Payload, [string]$Token) {
    $headers = @{}
    if ($Token) { $headers.Authorization = "Bearer $Token" }
    if ($null -eq $Payload) { return Invoke-RestMethod -Uri "$BaseUrl$Path" -Headers $headers }
    return Invoke-RestMethod -Uri "$BaseUrl$Path" -Method Post -Headers $headers -ContentType 'application/json; charset=utf-8' -Body ($Payload | ConvertTo-Json -Depth 12)
}
$admin = Invoke-DemoApi '/api/auth/login' @{ userName='admin'; password=$demoPassword } ''
$officer = Invoke-DemoApi '/api/auth/login' @{ userName='officer'; password=$demoPassword } ''
$catalog = Invoke-DemoApi '/api/admin/catalog' $null $officer.token
$paperField = ($catalog.resources | Where-Object key -eq 'sessions').fields | Where-Object name -eq 'examId'
if (!$paperField.choices.Count) { throw 'No Ready exam available; create and confirm an exam in Desktop first.' }
$paper = [int]$paperField.choices[0].value
$suffix = [DateTime]::UtcNow.ToString('MMddHHmmss') + (Get-Random -Minimum 100 -Maximum 999)
$studentName = "demo_$suffix"
$student = Invoke-DemoApi '/api/admin/users' @{ values=@{ userName=$studentName; fullName="Thí sinh demo $suffix"; studentCode="DEMO$suffix"; password=$demoPassword; roles=@('Student'); isActive=$true } } $admin.token
$nowUtc = [DateTime]::UtcNow
$session = Invoke-DemoApi '/api/admin/sessions' @{ values=@{ examId=$paper; title="Demo $suffix"; opensAt=$nowUtc.AddSeconds(-10).ToString('o'); closesAt=$nowUtc.AddMinutes($Minutes).ToString('o'); durationMinutes=$Minutes; shuffleQuestions=$true; shuffleOptions=$true; reviewPolicy='CorrectAnswers' } } $officer.token
Invoke-DemoApi '/api/admin/candidates' @{ values=@{ sessionId=$session.id; studentId=$student.id } } $officer.token | Out-Null
Invoke-DemoApi "/api/admin/sessions/$($session.id)/action" @{ action='activate' } $officer.token | Out-Null
Invoke-DemoApi '/api/auth/logout' @{} $admin.token | Out-Null
Invoke-DemoApi '/api/auth/logout' @{} $officer.token | Out-Null
$details = @{ studentUserName=$studentName; sessionId=$session.id; closesAt=$nowUtc.AddMinutes($Minutes).ToString('o'); passwordFile='.local/demo-credentials.txt' }
$details | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $projectRoot '.local\latest-demo.json') -Encoding utf8
Write-Host "Student: $studentName"
Write-Host "Session: $($session.id); closes after $Minutes minutes."
Write-Host "Password: read the password= line in $credentialPath"
Write-Host "Web: $BaseUrl/Account/Login"
Write-Host 'After the session closes, log into Desktop as officer and publish its result.'
