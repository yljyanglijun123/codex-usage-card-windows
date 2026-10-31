param([switch]$Test,[string]$InstallRoot,[switch]$Disable)
$ErrorActionPreference='Stop'
$profileDir=[Environment]::GetFolderPath('UserProfile')
if($InstallRoot){$root=[IO.Path]::GetFullPath($InstallRoot)}else{$pkg=Get-AppxPackage OpenAI.Codex|Select-Object -First 1;$root=Join-Path $profileDir ('AppData/Local/Packages/'+$pkg.PackageFamilyName+'/LocalCache/Local/CodexUsageCardWindows')}
if($InstallRoot -and !$Test){throw 'Custom listener directories are supported only for isolated tests'}
Add-Type -AssemblyName System.Web.Extensions
$json=New-Object System.Web.Script.Serialization.JavaScriptSerializer
$cfg=$json.DeserializeObject([IO.File]::ReadAllText((Join-Path $root 'config.json')))
if($cfg.owner -ne 'codex-usage-card-windows-v1'){throw 'Installation ownership mismatch'}
$svc=New-Object -ComObject Schedule.Service;$svc.Connect();$folder=$svc.GetFolder('\')
$name=if($Test){'CodexUsageCardNative-Test'}else{'CodexUsageCardNative-'+[Security.Principal.WindowsIdentity]::GetCurrent().User.Value}
try{$old=$folder.GetTask($name)}catch{$old=$null}
if($old -and $old.Definition.RegistrationInfo.Description -notlike 'Codex Usage Card native startup monitor*'){throw 'Task ownership mismatch'}
$stop=Join-Path $root $(if($Test){'stop-broker-test'}else{'stop-broker'})
if($old -and $old.State -eq 4){
 [IO.File]::WriteAllText($stop,'')
 $stopDeadline=[DateTime]::UtcNow.AddSeconds(10)
 while($old.State -eq 4 -and [DateTime]::UtcNow -lt $stopDeadline){Start-Sleep -Milliseconds 100}
 if($old.State -eq 4 -and !$Disable){throw 'Previous task is still ending. Save work and fully exit Codex before updating the listener.'}
}
if($Disable){[IO.File]::WriteAllText($stop,'');if($old){$old.Enabled=$false};Write-Output 'Original-icon listener disabled';return}
foreach($pair in @(@('CodexUsageCardBroker-v6.exe','CodexUsageCardBroker-v6.exe'),@('CodexUsageCard.exe','CodexUsageCardHost-v6.exe'),@('CodexUsageCard.exe','CodexUsageCardMonitor-v1.exe'))){
 $source=Join-Path $PSScriptRoot $pair[0];$dest=Join-Path $root $pair[1]
 if(!(Test-Path -LiteralPath $dest) -or (Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $dest).Hash){Copy-Item -LiteralPath $source -Destination $dest -Force}
}
Remove-Item -LiteralPath $stop -Force -ErrorAction SilentlyContinue
$ready=Join-Path $root $(if($Test){'broker-test-ready.json'}else{'broker-ready.json'})
Remove-Item -LiteralPath $ready -Force -ErrorAction SilentlyContinue
$d=$svc.NewTask(0);$d.RegistrationInfo.Description='Codex Usage Card native startup monitor v6';$d.Principal.UserId=[Security.Principal.WindowsIdentity]::GetCurrent().Name;$d.Principal.LogonType=3
$d.Settings.ExecutionTimeLimit='PT0S';$d.Settings.DisallowStartIfOnBatteries=$false;$d.Settings.StopIfGoingOnBatteries=$false;$d.Settings.MultipleInstances=2;$d.Settings.Hidden=$true
$d.Settings.RestartCount=3;$d.Settings.RestartInterval='PT1M'
if(!$Test){$t=$d.Triggers.Create(9);$t.UserId=$d.Principal.UserId}
$a=$d.Actions.Create(0);$a.Path=Join-Path $root 'CodexUsageCardBroker-v6.exe';$a.WorkingDirectory=$root;if($Test){$a.Arguments='--test'}
[void]$folder.RegisterTaskDefinition($name,$d,6,$d.Principal.UserId,$null,3,$null);[void]$folder.GetTask($name).Run($null)
$deadline=[DateTime]::UtcNow.AddSeconds(20)
while(!(Test-Path -LiteralPath $ready) -and [DateTime]::UtcNow -lt $deadline){Start-Sleep -Milliseconds 100}
if(!(Test-Path -LiteralPath $ready)){throw 'Listener task did not report ready; inspect broker.log'}
Write-Output ('Started native monitor task: '+$name)
