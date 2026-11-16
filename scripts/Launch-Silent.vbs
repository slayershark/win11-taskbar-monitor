Dim WshShell, fso, scriptDir, runnerPath, psExe
Set WshShell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
scriptDir = fso.GetParentFolderName(WScript.ScriptFullName)

If WScript.Arguments.Count > 0 Then
    runnerPath = WScript.Arguments(0)
Else
    runnerPath = scriptDir & "\Run-WinTaskbarMonitor.ps1"
End If

psExe = WshShell.ExpandEnvironmentStrings("%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe")
If Not fso.FileExists(psExe) Then
    psExe = "powershell.exe"
End If

WshShell.Run """" & psExe & """ -WindowStyle Hidden -ExecutionPolicy Bypass -File """ & runnerPath & """", 0, False
