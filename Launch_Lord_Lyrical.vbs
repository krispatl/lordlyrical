Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
root = fso.GetParentFolderName(WScript.ScriptFullName)
If Not fso.FileExists(root & "\.venv\Scripts\styly-netsync-server.exe") Then
    result = shell.Run("powershell.exe -NoProfile -ExecutionPolicy Bypass -File " & Chr(34) & root & "\Setup_Launcher.ps1" & Chr(34), 1, True)
    If result <> 0 Then
        shell.Popup "Launcher setup failed. Run Setup_Launcher.ps1 in PowerShell to see the error.", 0, "Lord Lyrical", 16
        WScript.Quit result
    End If
End If
shell.Run Chr(34) & root & "\.venv\Scripts\pythonw.exe" & Chr(34) & " " & Chr(34) & root & "\Tools\show_launcher.py" & Chr(34), 0, False
