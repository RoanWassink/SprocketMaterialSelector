param([Parameter(Mandatory=$true)][string]$GameDirectory)
$ErrorActionPreference='Stop'
if(Get-Process -Name Sprocket -ErrorAction SilentlyContinue){throw 'Close Sprocket before installing.'}
$gameRoot=[IO.Path]::GetFullPath($GameDirectory).TrimEnd('\')
if(!(Test-Path -LiteralPath (Join-Path $gameRoot 'BepInEx\core\BepInEx.Core.dll'))){throw 'Choose the Sprocket folder with a working BepInEx IL2CPP loader.'}
$fileRoot=Join-Path $PSScriptRoot 'files'
$plans=[Collections.Generic.List[object]]::new()
function Destination([string]$relative){
 $resolved=[IO.Path]::GetFullPath((Join-Path $gameRoot $relative))
 if(!$resolved.StartsWith($gameRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid package path.'}
 return $resolved
}
function AddPlan([string]$relative,[byte[]]$bytes){$plans.Add([pscustomobject]@{Relative=$relative;Target=(Destination $relative);Bytes=$bytes})}
function ReadJson([string]$path){Get-Content -LiteralPath $path -Raw | ConvertFrom-Json}
# Preflight both catalogues before writing any file. Existing recipe/settings values win.
foreach($name in @('sprocket.armour.responses.json','sprocket.era.bindings.json')){
 $relative='BepInEx\config\'+$name;$target=Destination $relative
 $defaults=ReadJson (Join-Path $PSScriptRoot ('defaults\'+$name))
 if(!(Test-Path -LiteralPath $target)){AddPlan $relative ([IO.File]::ReadAllBytes((Join-Path $PSScriptRoot ('defaults\'+$name))));continue}
 $existing=ReadJson $target
 if($existing.schemaVersion -ne 1){throw "Unsupported schema in $name; existing file preserved."}
 $changed=$false
 if($name -eq 'sprocket.armour.responses.json'){
  if($existing.responses -isnot [Array]){throw 'Response catalogue must contain an array.'}
  $seen=@{};foreach($r in $existing.responses){if(!$r.responseId -or $seen.ContainsKey([string]$r.responseId)){throw 'Invalid or duplicate response ID.'};$seen[[string]$r.responseId]=$true}
  foreach($r in $defaults.responses){if(!$seen.ContainsKey([string]$r.responseId)){$existing.responses=@($existing.responses)+@($r);$seen[[string]$r.responseId]=$true;$changed=$true}}
 }else{
  if($existing.matchMode -ne 'componentId' -or $existing.bindings -isnot [Array]){throw 'Unsupported ERA binding format.'}
  $roles=@{};foreach($b in $existing.bindings){foreach($role in @($b.cassetteComponentId,$b.mountComponentId)){if(!$role -or $roles.ContainsKey([string]$role)){throw 'Invalid or duplicate ERA role.'};$roles[[string]$role]=$b}}
  foreach($b in $defaults.bindings){
   $found=@($b.cassetteComponentId,$b.mountComponentId | Where-Object {$roles.ContainsKey([string]$_)})
   if($found.Count){
    if($found.Count -ne 2){throw 'Partial ERA binding conflict; existing files preserved.'}
    foreach($role in $found){$old=$roles[[string]$role];foreach($field in @('cassetteComponentId','mountComponentId','materialId','responseId','kind')){if($old.$field -cne $b.$field){throw "Conflicting ERA binding $role; existing files preserved."}}}
   }else{$existing.bindings=@($existing.bindings)+@($b);$roles[[string]$b.cassetteComponentId]=$b;$roles[[string]$b.mountComponentId]=$b;$changed=$true}
  }
 }
 if($changed){AddPlan $relative ([Text.UTF8Encoding]::new($false).GetBytes(($existing | ConvertTo-Json -Depth 100)))}
}
$history=ReadJson (Join-Path $PSScriptRoot 'KNOWN-AUTHORED-HASHES.json')
foreach($file in Get-ChildItem -LiteralPath $fileRoot -Recurse -File){
 $relative=$file.FullName.Substring($fileRoot.Length+1);$target=Destination $relative
 if(Test-Path -LiteralPath $target){
  $before=(Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
  $after=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
  if($before -eq $after){continue}
  $known=$history.PSObject.Properties[$relative.Replace('\','/')]
  if($relative -ne 'BepInEx\plugins\SprocketMaterialSelector.dll' -and (!$known -or $before -notin $known.Value)){
   Write-Warning "Preserved customized or unrecognized file: $relative";continue
  }
 }
 AddPlan $relative ([IO.File]::ReadAllBytes($file.FullName))
}
if(!$plans.Count){Write-Host 'Already up to date; existing settings preserved.';return}
$backup=Destination ('BepInEx\material-selector-backups\'+[DateTime]::Now.ToString('yyyyMMdd-HHmmss-fffffff'))
foreach($plan in $plans){
 if(Test-Path -LiteralPath $plan.Target){$saved=Join-Path $backup $plan.Relative;[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($saved))|Out-Null;[IO.File]::Copy($plan.Target,$saved,$false)}
 [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($plan.Target))|Out-Null
 [IO.File]::WriteAllBytes($plan.Target,$plan.Bytes)
 $actual=[IO.File]::ReadAllBytes($plan.Target)
 if([Convert]::ToBase64String($actual) -cne [Convert]::ToBase64String($plan.Bytes)){throw "Verification failed: $($plan.Relative)"}
}
Write-Host "Installed $($plans.Count) files. Existing recipes, enable state and custom files preserved. Backup: $backup"
