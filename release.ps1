dotnet publish Flow.Launcher.Plugin.OneNote -c Release -r win-x64 -o Flow.Launcher.Plugin.OneNote/bin/Release/OneNote
#Compress-Archive -LiteralPath Flow.Launcher.Plugin.OneNote/bin/Release/win-x64/publish -DestinationPath Flow.Launcher.Plugin.OneNote/bin/OneNote.zip -Force

# Kill Flow Launcher
Do {  
    $ProcessesFound = Get-Process | Where-Object -Property ProcessName -EQ 'Flow.Launcher'
    If ($ProcessesFound) {
        Stop-Process -Name Flow.Launcher
        Start-Sleep 1
    }
} Until (!$ProcessesFound)


Remove-Item -Recurse $env:APPDATA\FlowLauncher\Plugins\OneNote\ -Force
Copy-Item -Recurse -LiteralPath Flow.Launcher.Plugin.OneNote/bin/Release/OneNote $env:APPDATA\FlowLauncher\Plugins\ -Force
Start-Process $env:LOCALAPPDATA\FlowLauncher\Flow.Launcher.exe