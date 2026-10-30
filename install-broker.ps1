param([switch]$Test)
$ErrorActionPreference='Stop'
$pkg=Get-AppxPackage OpenAI.Codex|Select-Object -First 1
$profileDir=[Environment]::GetFolderPath('UserProfile')
$root=Join-Path $profileDir ('AppData/Local/Packages/'+$pkg.PackageFamilyName+'/LocalCache/Local/CodexUsageCardWindows')
if(!(Test-Path (Join-Path $root 'config.json'))){throw 'Install the native package first'}
foreach($pair in @(@('CodexUsageCardBroker-v3.exe','CodexUsageCardBroker-v3.exe'),@('CodexUsageCard-v2.exe','CodexUsageCardHost-v2.exe'))){
 $source=Join-Path $PSScriptRoot $pair[0];$dest=Join-Path $root $pair[1]
 if(!(Test-Path $dest) -or (Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $dest).Hash){Copy-Item -LiteralPath $source -Destination $dest -Force}
}
$stop=Join-Path $root $(if($Test){'stop-broker-test'}else{'stop-broker'})
Remove-Item -LiteralPath $stop -Force -ErrorAction SilentlyContinue
$svc=New-Object -ComObject Schedule.Service;$svc.Connect();$folder=$svc.GetFolder('\')
$name=if($Test){'CodexUsageCardNative-Test'}else{'CodexUsageCardNative-'+[Security.Principal.WindowsIdentity]::GetCurrent().User.Value}
try{$old=$folder.GetTask($name)}catch{$old=$null}
if($old -and $old.Definition.RegistrationInfo.Description -notlike 'Codex Usage Card*'){throw 'Task ownership mismatch'}
if($old -and $old.State -eq 4){$old.Stop(0)}
$d=$svc.NewTask(0);$d.RegistrationInfo.Description='Codex Usage Card native startup monitor v3';$d.Principal.UserId=[Security.Principal.WindowsIdentity]::GetCurrent().Name;$d.Principal.LogonType=3
$d.Settings.ExecutionTimeLimit='PT0S';$d.Settings.DisallowStartIfOnBatteries=$false;$d.Settings.StopIfGoingOnBatteries=$false;$d.Settings.MultipleInstances=2;$d.Settings.Hidden=$true
if(!$Test){$t=$d.Triggers.Create(9);$t.UserId=$d.Principal.UserId}
$a=$d.Actions.Create(0);$a.Path=Join-Path $root 'CodexUsageCardBroker-v3.exe';$a.WorkingDirectory=$root;if($Test){$a.Arguments='--test'}
[void]$folder.RegisterTaskDefinition($name,$d,6,$d.Principal.UserId,$null,3,$null);[void]$folder.GetTask($name).Run($null)
Write-Output ('Started native monitor task: '+$name)
