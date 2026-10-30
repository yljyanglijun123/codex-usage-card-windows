param([switch]$OriginalIconMonitor,[string]$InstallRoot,[switch]$NoShortcuts,[switch]$CheckOnly)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Web.Extensions
$pkg=Get-AppxPackage OpenAI.Codex|Sort-Object Version -Descending|Select-Object -First 1
if(!$pkg){throw 'Codex Store application is required. Install Codex and sign in first.'}
if($InstallRoot -and $OriginalIconMonitor){throw 'OriginalIconMonitor requires the default installation directory.'}
$homeDir=if($InstallRoot){[IO.Path]::GetFullPath($InstallRoot)}else{Join-Path ([Environment]::GetFolderPath('UserProfile')) ('AppData/Local/Packages/'+$pkg.PackageFamilyName+'/LocalCache/Local/CodexUsageCardWindows')}
$json=New-Object System.Web.Script.Serialization.JavaScriptSerializer
if(Test-Path (Join-Path $homeDir 'config.json')){
 $old=$json.DeserializeObject([IO.File]::ReadAllText((Join-Path $homeDir 'config.json')))
 if($old.owner -ne 'codex-usage-card-windows-v1'){throw 'Installation directory ownership mismatch'}
}
$app=Join-Path $pkg.InstallLocation 'app/ChatGPT.exe'
if(!(Test-Path -LiteralPath $app)){$app=Join-Path $pkg.InstallLocation 'app/Codex.exe'}
if(!(Test-Path -LiteralPath $app)){throw 'The registered Codex desktop executable is missing. Repair Codex.'}
$cliPaths=@((Join-Path $pkg.InstallLocation 'app/resources/codex.exe'),(Join-Path $pkg.InstallLocation 'app/resources/codex-cli/bin/codex.exe'),(Join-Path $pkg.InstallLocation 'app/resources/codex-cli/codex.exe'))
$managed=Join-Path $env:LOCALAPPDATA 'OpenAI/Codex/bin'
if(Test-Path -LiteralPath $managed){$cliPaths+=@(Get-ChildItem -LiteralPath $managed -Filter codex.exe -Recurse -File|Sort-Object LastWriteTime -Descending|Select-Object -ExpandProperty FullName)}
$cliPath=$cliPaths|Where-Object {Test-Path -LiteralPath $_ -PathType Leaf}|Select-Object -First 1
if(!$cliPath){throw 'Codex CLI is required. Open Codex once or repair its installation.'}
$candidates=@((Get-Command node.exe -ErrorAction SilentlyContinue|Select-Object -ExpandProperty Source),"$env:ProgramFiles/nodejs/node.exe","$env:USERPROFILE/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe")
$runtime=Join-Path $env:LOCALAPPDATA 'OpenAI/Codex/runtimes/cua_node'
if(Test-Path $runtime){$candidates+=@(Get-ChildItem $runtime -Filter node.exe -Recurse -File|Select-Object -ExpandProperty FullName)}
$node=$null
foreach($p in $candidates){if($p -and (Test-Path -LiteralPath $p)){try{$v=& $p -p 'parseInt(process.versions.node)' 2>$null;$major=0;if($LASTEXITCODE -eq 0 -and [int]::TryParse([string]$v,[ref]$major) -and $major -ge 24){$node=$p;break}}catch{continue}}}
if(!$node){throw 'Node.js 24+ required'}
foreach($file in @('CodexUsageCard.exe','agent.cjs','LICENSE')){if(!(Test-Path (Join-Path $PSScriptRoot $file))){throw "Missing $file"}}
if($OriginalIconMonitor){foreach($file in @('install-broker.ps1','CodexUsageCardBroker-v3.exe','CodexUsageCard-v2.exe')){if(!(Test-Path (Join-Path $PSScriptRoot $file))){throw "Missing $file"}}}
if($CheckOnly){Write-Host 'Preflight passed: Codex, CLI, Node 24+, and package files are available. No files or shortcuts changed.';return}
New-Item -ItemType Directory -Force $homeDir|Out-Null
Copy-Item (Join-Path $PSScriptRoot 'CodexUsageCard.exe') (Join-Path $homeDir 'CodexUsageCard.exe') -Force
Copy-Item (Join-Path $PSScriptRoot 'agent.cjs') (Join-Path $homeDir 'agent.cjs') -Force
Copy-Item (Join-Path $PSScriptRoot 'LICENSE') (Join-Path $homeDir 'LICENSE') -Force
$cfg=New-Object 'System.Collections.Generic.Dictionary[string,string]'
$cfg.Add('owner','codex-usage-card-windows-v1');$cfg.Add('version','0.9.6-windows.1');$cfg.Add('node',[string]$node);$cfg.Add('app',[string]$app);$cfg.Add('cli',[string]$cliPath)
[IO.File]::WriteAllText((Join-Path $homeDir 'config.json'),$json.Serialize($cfg),[Text.UTF8Encoding]::new($false))
if(!$InstallRoot){
$svc=New-Object -ComObject Schedule.Service;$svc.Connect();$folder=$svc.GetFolder('\');$taskName='CodexUsageCardWindows-'+[Security.Principal.WindowsIdentity]::GetCurrent().User.Value
try{$task=$folder.GetTask($taskName)}catch{$task=$null}
if($task -and $task.Definition.RegistrationInfo.Description -eq 'Codex Usage Card Windows owned task v1'){if($task.State -eq 4){$task.Stop(0)};$folder.DeleteTask($taskName,0)}
}
Remove-Item (Join-Path $homeDir 'launch-on-exit.json') -Force -ErrorAction SilentlyContinue
if(!$NoShortcuts){$shell=New-Object -ComObject WScript.Shell
foreach($dir in @([Environment]::GetFolderPath('Desktop'),[Environment]::GetFolderPath('Programs'))){
 $link=$shell.CreateShortcut((Join-Path $dir 'Codex Usage Card.lnk'));$link.TargetPath=Join-Path $homeDir 'CodexUsageCard.exe';$link.WorkingDirectory=$homeDir;$link.IconLocation=(Join-Path $homeDir 'CodexUsageCard.exe')+',0';$link.Description='Codex with automatic usage rings';$link.Save()
}
}
Write-Host 'Installed native launcher. Pin Codex Usage Card to the taskbar instead of the original Store shortcut.'

if($OriginalIconMonitor){& (Join-Path $PSScriptRoot 'install-broker.ps1')}
