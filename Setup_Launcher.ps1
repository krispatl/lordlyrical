$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$pythonExe = Join-Path $projectRoot '.venv\Scripts\python.exe'
$uvCommand = Get-Command uv -ErrorAction SilentlyContinue
$uvExe = if ($uvCommand) { $uvCommand.Source } else { Join-Path $env:USERPROFILE '.local\bin\uv.exe' }
if (!(Test-Path -LiteralPath $pythonExe)) {
    if (Test-Path -LiteralPath $uvExe) {
        & $uvExe venv --seed --python 3.11 (Join-Path $projectRoot '.venv')
    } else {
        $pythonCommand = Get-Command py -ErrorAction SilentlyContinue
        $pythonArgs = @('-3.11')
        if (!$pythonCommand) { $pythonCommand = Get-Command python -ErrorAction SilentlyContinue; $pythonArgs = @() }
        if (!$pythonCommand) { throw 'Install Python 3.11 (including the Windows py launcher), or uv, and run Setup_Launcher.ps1 again.' }
        & $pythonCommand.Source @pythonArgs -c 'import sys; sys.exit(0 if sys.version_info >= (3,11) else 1)'
        if ($LASTEXITCODE -ne 0) { throw 'Python 3.11 or newer is required. Install it or use uv.' }
        & $pythonCommand.Source @pythonArgs -m venv (Join-Path $projectRoot '.venv')
    }
    if ($LASTEXITCODE -ne 0) { throw 'Launcher Python environment creation failed.' }
}
if (Test-Path -LiteralPath $uvExe) {
    & $uvExe pip install --python $pythonExe styly-netsync-server==0.17.4 pip
} else {
    & $pythonExe -m pip install styly-netsync-server==0.17.4
}
if ($LASTEXITCODE -ne 0) { throw 'Launcher dependency installation failed. Check your internet connection and run setup again.' }
Write-Host 'Launcher ready. Double-click Launch_Lord_Lyrical.vbs.'
