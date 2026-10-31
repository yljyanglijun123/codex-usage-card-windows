param([string]$Root)
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'PackageIdentityProbe.cs')
Add-Type -Path (Join-Path $PSScriptRoot 'WindowProbe.cs')
$cfg=Get-Content -Encoding UTF8 -LiteralPath (Join-Path $Root 'config.json')|ConvertFrom-Json
$testScripts=$PSScriptRoot
$results=@()
for($cycle=1;$cycle -le 3;$cycle++){
 $started=[DateTime]::UtcNow
 $entry=Start-Process -FilePath (Join-Path $Root 'CodexUsageCard.exe') -ArgumentList '--test' -WindowStyle Hidden -PassThru
 $session=$null
 for($i=0;$i -lt 200;$i++){try{$s=Get-Content -Encoding UTF8 -LiteralPath (Join-Path $Root 'test-session.json')|ConvertFrom-Json;if(([DateTimeOffset]$s.startedAt).UtcDateTime -ge $started -and (Get-Process -Id $s.app -ErrorAction SilentlyContinue)){$session=$s;break}}catch{};Start-Sleep -Milliseconds 200}
 if(!$session){throw "Single activation failed at cycle $cycle"}
 if(!$entry.WaitForExit(2000)){throw "Foreground launch entry remained resident"}
 $identity=[AppIdentityProbe]::Read($session.app)
 if(!($identity -contains 'ProcessAppId=OpenAI.Codex_2p2nqsd0c76g0!App status=0')){throw 'Store process identity missing'}
 & $cfg.node (Join-Path $testScripts 'RenderReadyProbe.cjs');if($LASTEXITCODE){throw 'Blank or incomplete page'}
 & $cfg.node (Join-Path $testScripts 'QuotaProbe.cjs');if($LASTEXITCODE){throw 'Quota not valid'}
 $windows=@([WindowProbe]::Get($session.app)|Where-Object {$_.Visible -and $_.TitleLength -gt 0 -and $_.Width -gt 200})
 if($windows.Count -ne 1 -or !$windows[0].Responding){throw 'Invalid visible main window'}
 $results+=[pscustomobject]@{cycle=$cycle;singleActivation=$true;storeIdentity=$true;rendererReady=$true;quotaValid=$true;visibleMainWindows=1}
 Write-Output "PASS native first activation $cycle"
 $p=Get-CimInstance Win32_Process -Filter "ProcessId=$($session.app)"
 if($p.CommandLine.Contains((Join-Path $Root 'test-profile'))){Invoke-CimMethod -InputObject $p -MethodName Terminate|Out-Null}
 for($i=0;$i -lt 100;$i++){if(!(Get-Process -Id $session.launcher -ErrorAction SilentlyContinue)){break};Start-Sleep -Milliseconds 100}
 if(Get-Process -Id $session.launcher -ErrorAction SilentlyContinue){throw 'Test host did not exit'}
}
$results|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $Root 'first-click-verification.json') -Encoding utf8
