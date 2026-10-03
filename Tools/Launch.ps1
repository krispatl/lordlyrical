param([ValidateSet('Server','Stop','Simulator','BuildPico')][string]$Mode='Server',[switch]$NoBrowser)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $projectRoot
New-Item -ItemType Directory -Path Logs -Force | Out-Null
if($Mode -eq 'Stop') {
    $ownedProcesses=Get-CimInstance Win32_Process | Where-Object {$_.Name -match '^(python|styly-netsync-server)\.exe$' -and $_.CommandLine -like "*$projectRoot*" -and ($_.CommandLine -like '*show_authority.py*' -or $_.CommandLine -like '*styly-netsync-server*')}
    $ownedProcesses | ForEach-Object {Stop-Process -Id $_.ProcessId -ErrorAction SilentlyContinue}
    Write-Host 'Stopped this project venue server and authority.';exit
}
if($Mode -eq 'BuildPico') {
    $editorVersion=((Get-Content ProjectSettings/ProjectVersion.txt | Select-String '^m_EditorVersion:').Line -split ': ')[1]
    $editorExe=Join-Path $env:ProgramFiles "Unity\Hub\Editor\$editorVersion\Editor\Unity.exe"
    if(!(Test-Path -LiteralPath $editorExe)){throw "Unity $editorVersion is required. Install it through Unity Hub with Android Build Support."}
    $buildProcess=Start-Process -FilePath $editorExe -ArgumentList @('-batchmode','-quit','-buildTarget','Android','-projectPath',('"'+$projectRoot+'"'),'-executeMethod','LordLyrical.Editor.ShowBuilder.BuildPico','-logFile',('"'+(Join-Path $projectRoot 'Logs\PICO-build.log')+'"')) -WindowStyle Hidden -Wait -PassThru
    if($buildProcess.ExitCode -ne 0){throw 'Build failed. See Logs/PICO-build.log. Close this project in Unity before a batch build.'}
    Write-Host 'APK: Builds/LordLyrical-PICO.apk';exit
}
$uvCommand=Get-Command uv -ErrorAction SilentlyContinue
$uvExe=if($uvCommand){$uvCommand.Source}else{Join-Path $env:USERPROFILE '.local\bin\uv.exe'}
if(!(Test-Path -LiteralPath $uvExe)){throw 'Install uv from https://docs.astral.sh/uv/getting-started/installation/ and run again.'}
if(!(Test-Path .venv/Scripts/python.exe)){& $uvExe venv --python 3.11 .venv;if($LASTEXITCODE){throw 'Python environment setup failed'}}
& $uvExe pip install --python .venv/Scripts/python.exe styly-netsync-server==0.17.4
if($LASTEXITCODE){throw 'NetSync installation failed'}
if($Mode -eq 'Simulator') { & .venv/Scripts/styly-netsync-simulator.exe --clients 10 --room official-load-test --transform-send-rate 20;exit }
try {$existing=Invoke-RestMethod http://127.0.0.1:8810/status -TimeoutSec 2} catch {$existing=$null}
if($existing -and $existing.sdk -eq '0.17.4'){if(!$NoBrowser){Start-Process http://127.0.0.1:8810};Write-Host 'Existing venue authority is available.';exit}
$serverExe=Join-Path $projectRoot '.venv\Scripts\styly-netsync-server.exe'
$configText=Get-Content -LiteralPath 'Assets/LordLyrical/ScriptableObjects/Experience.asset' -Raw
$configuredRoom=[regex]::Match($configText,'(?m)^  roomId: (.+)$').Groups[1].Value.Trim()
$configuredMode=[regex]::Match($configText,'(?m)^  showMode: (.+)$').Groups[1].Value.Trim()
$configuredCountdown=[regex]::Match($configText,'(?m)^  countdownDuration: (.+)$').Groups[1].Value.Trim()
if(!$configuredRoom -or $configuredMode -notin @('Adaptive','FullCast') -or !$configuredCountdown){throw 'Invalid room, mode or countdown in Experience.asset'}
$serverProcess=Start-Process -FilePath $serverExe -ArgumentList @('--server-discovery-port','49999','--log-level-console','WARNING') -WindowStyle Hidden -RedirectStandardOutput 'Logs/server.log' -RedirectStandardError 'Logs/server-errors.log' -PassThru
$authorityProcess=Start-Process -FilePath (Join-Path $projectRoot '.venv\Scripts\python.exe') -ArgumentList @(('"'+(Join-Path $projectRoot 'Tools\show_authority.py')+'"'),'--room',('"'+$configuredRoom+'"'),'--mode',$configuredMode,'--countdown',$configuredCountdown) -WorkingDirectory $projectRoot -WindowStyle Hidden -RedirectStandardOutput 'Logs/authority.log' -RedirectStandardError 'Logs/authority-errors.log' -PassThru
@{server=$serverProcess.Id;authority=$authorityProcess.Id} | ConvertTo-Json | Set-Content Logs/venue-processes.json
$ready=$false
for($attempt=0;$attempt -lt 30;$attempt++){
    Start-Sleep -Milliseconds 500
    try{$startedStatus=Invoke-RestMethod http://127.0.0.1:8810/status -TimeoutSec 1;if($startedStatus.connected -and $startedStatus.sdk -eq '0.17.4'){$ready=$true;break}}catch{}
}
if(!$ready){throw 'Venue authority did not connect. Inspect Logs/server-errors.log and Logs/authority-errors.log'}
if(!$NoBrowser){Start-Process http://127.0.0.1:8810}
Write-Host 'Venue server started. Operator: http://127.0.0.1:8810. Close the server only between sessions.'
