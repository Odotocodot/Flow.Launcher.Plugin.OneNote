$FolderName = 'Flow.Launcher.Plugin.OneNote'
$PluginJson = Get-Content .\$FolderName\plugin.json -Raw | ConvertFrom-Json

$Name = $PluginJson.Name 
$Version = $PluginJson.Version
$ActionKeyword = $PluginJson.ActionKeyword

if (!$Name) {
    Write-Host 'Invalid Name'
    Exit
}

$Choices =  @('&Yes', '&No')

$Choice1 = $Host.UI.PromptForChoice('Name Check', "Is the plugin name valid: $($Name)?", $Choices, 0)
if ($Choice1 -eq 1) {
    Write-Host "Invalid Name Cancelling Release"
    Exit
} 

$Choice2 = $Host.UI.PromptForChoice('Create Zip', 'Do you want to create a zip file?', $Choices, 1)

$Choice3 = $Host.UI.PromptForChoice('Auto Show', 'Do you want to automatically show the plugin?', $Choices, 0)

$FullName = $Name + '-' + $Version

dotnet publish -c Release -r win-x64 --property:PublishDir=.\bin\Release\$FullName --no-self-contained

if ($Choice2 -eq 0) {
    Write-Host "Creating Zip at: .\$FolderName\bin\$FullName.zip"
    Compress-Archive -LiteralPath .\$FolderName\bin\Release\$FullName -DestinationPath .\$FolderName\bin\"$FullName.zip" -Force
} 

Do {
    $Flow = Get-Process | Where-Object -Property ProcessName -eq 'Flow.Launcher'
    if ($Flow) {
        Stop-Process $Flow
        Start-Sleep 1
    }
} Until (!$Flow)

$Folders = Get-ChildItem -Path $env:APPDATA\FlowLauncher\Plugins\ | Where-Object { $_ -Match "$Name-\d.\d.\d" }
foreach ($Folder in $Folders) {
    Remove-Item -Recurse $env:APPDATA\FlowLauncher\Plugins\$Folder\ -Force -ErrorAction Stop
}

Copy-Item -Recurse -LiteralPath ./$FolderName/bin/Release/$FullName $env:APPDATA\FlowLauncher\Plugins\ -Force
$Flow = Start-Process $env:LOCALAPPDATA\FlowLauncher\Flow.Launcher.exe -PassThru

#Do {} While ($Flow.WaitForInputIdle(5000) -ne $true)
$null = $Flow.WaitForInputIdle(5000)

# while ($Flow.MainWindowTitle -eq 0) 
# {
#     Start-Sleep -Milliseconds 1000
# }

if ($Choice3 -eq 0) {
    $wshell = New-Object -ComObject wscript.shell;
    $wshell.AppActivate('Flow.Launcher')
    Start-Sleep 3

    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.SendKeys]::SendWait("% ")
    [System.Windows.Forms.SendKeys]::SendWait($ActionKeyword)
} 
