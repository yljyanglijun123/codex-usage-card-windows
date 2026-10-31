param([string]$Package,[string]$OutputRoot)
$ErrorActionPreference='Stop'
$installer=Join-Path $Package 'install.ps1'
New-Item -ItemType Directory -Force $OutputRoot|Out-Null
function FailCase([string]$Name,[string]$Expected,[scriptblock]$Setup){
 $target=Join-Path $OutputRoot $Name
 & {
   param($target,$Expected,$Setup,$installer,$Name)
   & $Setup
   $caught=$false
   try { & $installer -InstallRoot $target -NoShortcuts } catch {
    if($_.Exception.Message -notlike "*$Expected*"){throw "Unexpected error for ${Name}: $($_.Exception.Message)"};$caught=$true
   }
   if(!$caught){throw "Expected failure did not occur: $Name"}
   if(Test-Path -LiteralPath $target){throw "Failure left a partial installation: $Name"}
   Write-Output "PASS $Name"
 } $target $Expected $Setup $installer $Name
}
FailCase 'missing-codex' 'Codex Store application is required' {
 function global:Get-AppxPackage { [CmdletBinding()]param([string]$Name) return $null }
}
Remove-Item Function:/Get-AppxPackage
FailCase 'missing-cli' 'Codex CLI is required' {
 function global:Test-Path {
  [CmdletBinding()]param([string]$LiteralPath,[string]$Path,[string]$PathType)
  $p=if($LiteralPath){$LiteralPath}else{$Path}
  if($p -match 'codex\.exe$|OpenAI[/\\]Codex[/\\]bin$'){return $false}
  Microsoft.PowerShell.Management\Test-Path @PSBoundParameters
 }
}
Remove-Item Function:/Test-Path
FailCase 'missing-node' 'Node.js 24+ required' {
 function global:Test-Path {
  [CmdletBinding()]param([string]$LiteralPath,[string]$Path,[string]$PathType)
  $p=if($LiteralPath){$LiteralPath}else{$Path}
  if($p -match 'node\.exe$|cua_node$'){return $false}
  Microsoft.PowerShell.Management\Test-Path @PSBoundParameters
 }
}
Remove-Item Function:/Test-Path
FailCase 'incomplete-package' 'Missing agent.cjs' {
 function global:Test-Path {
  [CmdletBinding()]param([string]$LiteralPath,[string]$Path,[string]$PathType)
  $p=if($LiteralPath){$LiteralPath}else{$Path}
  if($p -match 'agent\.cjs$'){return $false}
  Microsoft.PowerShell.Management\Test-Path @PSBoundParameters
 }
}
Remove-Item Function:/Test-Path
$foreign=Join-Path $OutputRoot 'foreign-install';New-Item -ItemType Directory -Force $foreign|Out-Null
[IO.File]::WriteAllText((Join-Path $foreign 'config.json'),'{"owner":"another-application"}')
$before=(Get-FileHash (Join-Path $foreign 'config.json')).Hash
$caught=$false
try{& $installer -InstallRoot $foreign -NoShortcuts}catch{if($_.Exception.Message -notmatch 'ownership mismatch'){throw};$caught=$true}
if(!$caught -or (Get-FileHash (Join-Path $foreign 'config.json')).Hash -ne $before){throw 'Foreign installation was modified'}
Write-Output 'PASS foreign directory rejected without changes'
$target=Join-Path $OutputRoot 'fresh install 中文'
& $installer -InstallRoot $target -NoShortcuts
foreach($file in @('CodexUsageCard.exe','CodexUsageCardMonitor-v1.exe','agent.cjs','LICENSE','config.json')){if(!(Test-Path -LiteralPath (Join-Path $target $file))){throw "Missing installed file: $file"}}
$p=Start-Process -FilePath (Join-Path $target 'CodexUsageCard.exe') -ArgumentList '--resolve' -WindowStyle Hidden -PassThru
if(!$p.WaitForExit(30000) -or $p.ExitCode -ne 0){throw 'Fresh installed launcher failed resolution'}
Write-Output 'PASS fresh install from archive and native dependency resolution'
& $installer -InstallRoot $target -NoShortcuts
Write-Output 'PASS repeat install'
& $installer -InstallRoot $target -NoShortcuts -CheckOnly
Write-Output 'PASS read-only preflight'
