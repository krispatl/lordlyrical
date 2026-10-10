$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$pythonExe = Join-Path $projectRoot '.venv\Scripts\python.exe'
if (!(Test-Path -LiteralPath $pythonExe)) {
    $uvCommand = Get-Command uv -ErrorAction SilentlyContinue
    $uvExe = if ($uvCommand) { $uvCommand.Source } else { Join-Path $env:USERPROFILE '.local\bin\uv.exe' }
    if (Test-Path -LiteralPath $uvExe) {
        & $uvExe venv --python 3.11 (Join-Path $projectRoot '.venv')
    } else {
        $pythonCommand = Get-Command py -ErrorAction SilentlyContinue
        $pythonArgs = @('-3.11')
        if (!$pythonCommand) { $pythonCommand = Get-Command python -ErrorAction SilentlyContinue; $pythonArgs = @() }
        if (!$pythonCommand) { throw 'Install Python 3.11 (including the Windows py launcher), or uv, and run Setup_Launcher.ps1 again.' }
        & $pythonCommand.Source @pythonArgs -c 'import sys; assert sys.version_info >= (3,11), "Python 3.11 or newer is required"'
        if ($LASTEXITCODE -ne 0) { throw 'Python 3.11 or newer is required. Install it or use uv.' }
        & $pythonCommand.Source @pythonArgs -m venv (Join-Path $projectRoot '.venv')
    }
    if ($LASTEXITCODE -ne 0) { throw 'Launcher Python environment creation failed.' }
}
& $pythonExe -c 'import importlib.metadata; assert importlib.metadata.version("styly-netsync-server") == "0.17.4"' 2>$null
if ($LASTEXITCODE -ne 0) {
    & $pythonExe -m pip install styly-netsync-server==0.17.4
    if ($LASTEXITCODE -ne 0) { throw 'Launcher dependency installation failed. Check your internet connection and run setup again.' }
}
Write-Host 'Launcher ready. Double-click Launch_Lord_Lyrical.vbs.'
