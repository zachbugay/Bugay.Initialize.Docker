#Requires -Version 7.0
param (
  [switch]$Build
)

function Register-BugayInitializeDockerTask {
  [CmdletBinding()]
  param (
    [string]$Description,
    [string]$TaskName,
    [string]$BinPath
  )

  $action = New-ScheduledTaskAction -Execute $BinPath
  $trigger = New-ScheduledTaskTrigger -AtLogOn -User $env:USERNAME
  $principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -LogonType S4U -RunLevel Limited
  $settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -ExecutionTimeLimit ([TimeSpan]::Zero) `
    -RestartCount 3 `
    -RestartInterval (New-TimeSpan -Minutes 1) `
    -Hidden

  Write-Host "Registering and starting the scheduled task..."
  Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger `
    -Principal $principal -Settings $settings -Description $Description

  Start-ScheduledTask -TaskName $TaskName
}

function Install-BugayInitializeDocker {
  [CmdletBinding()]
  param (
    [switch]$Build
  )
  if ($Build) {
    Write-Host "Building the binary..."
    dotnet clean
    dotnet publish -c release -r win-arm64
  }
  
  $installDirectory = [System.IO.Path]::Combine($Env:LOCALAPPDATA, 'bugay-docker-installer', 'bin')
  if ($false -eq (Test-Path $installDirectory -ErrorAction SilentlyContinue))
  {
    New-Item -Type Directory -Path $installDirectory
  }
  
  Write-Host "Installing the binary..."
  $installPath = [System.IO.Path]::Combine($installDirectory, 'Bugay.Initialize.Docker.exe')
  $exePath = '.\src\bin\release\net10.0\win-arm64\publish\Bugay.Initialize.Docker.exe'
  
  if ([System.IO.File]::Exists($installPath)) {
    Remove-Item $installPath
  }

  Move-Item -Path $exePath -Destination $installPath
  
  $desc = 'Starts WSL in the background so the Host can communicate with the Docker daemon.'
  $taskName = "Bugay.Initialize.Docker"
  $existingTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue

  if ($null -ne $existingTask) {
    Write-Host 'Removing existing scheduled task...'
    Stop-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
  }

  Register-BugayInitializeDockerTask -Description $desc -TaskName $taskName -BinPath $installPath
}

Install-BugayInitializeDocker -Build:$Build