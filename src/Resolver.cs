using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
public static class Resolver {
 public static string SelectApp(string registeredRoot,string saved){
  if(!String.IsNullOrEmpty(registeredRoot))foreach(string name in new[]{"ChatGPT.exe","Codex.exe"}){
   string p=Path.Combine(registeredRoot,"app",name);if(File.Exists(p))return p;
  }
  if(!String.IsNullOrEmpty(saved)&&File.Exists(saved))return saved;
  throw new FileNotFoundException("Codex is not installed or its registered desktop executable is unavailable.");
 }
 static string Run(string exe,string args,int timeout){
  var pi=new ProcessStartInfo(exe,args);pi.UseShellExecute=false;pi.CreateNoWindow=true;pi.RedirectStandardOutput=true;pi.RedirectStandardError=true;
  pi.EnvironmentVariables["USERPROFILE"]=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
  pi.EnvironmentVariables["LOCALAPPDATA"]=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"AppData","Local");
  using(var p=Process.Start(pi)){
   var output=p.StandardOutput.ReadToEndAsync();var error=p.StandardError.ReadToEndAsync();
   if(!p.WaitForExit(timeout)){p.Kill();throw new TimeoutException("Path discovery timed out.");}
   if(p.ExitCode!=0)throw new InvalidOperationException("Path discovery command failed.");
   return output.Result.Trim();
  }
 }
 public static string RegisteredRoot(){
  string code="$ErrorActionPreference='Stop';$p=Get-AppxPackage -Name OpenAI.Codex|Sort-Object -Property @{Expression={[version]$_.Version}} -Descending|Select-Object -First 1;if(!$p){exit 2};[Console]::Out.Write([Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes([string]$p.InstallLocation)))";
  string encoded=Convert.ToBase64String(Encoding.Unicode.GetBytes(code));
  string shell=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell","v1.0","powershell.exe");
  return Encoding.UTF8.GetString(Convert.FromBase64String(Run(shell,"-NoProfile -NonInteractive -EncodedCommand "+encoded,15000)));
 }
 public static string ResolveCli(string app,string local,string saved){
  string root=Path.GetDirectoryName(app);
  var candidates=new List<string>{Path.Combine(root,"resources","codex.exe"),Path.Combine(root,"resources","codex-cli","bin","codex.exe"),Path.Combine(root,"resources","codex-cli","codex.exe")};
  string cache=Path.Combine(local,"OpenAI","Codex","bin");
  if(Directory.Exists(cache)){
   // The unversioned managed entry is authoritative when the client supplies it.
   candidates.Add(Path.Combine(cache,"codex.exe"));
   candidates.AddRange(Directory.GetDirectories(cache).Select(d=>Path.Combine(d,"codex.exe")).Where(File.Exists).OrderByDescending(File.GetLastWriteTimeUtc));
  }
  candidates.Add(saved);
  string found=candidates.FirstOrDefault(p=>!String.IsNullOrEmpty(p)&&File.Exists(p));
  if(found==null)throw new FileNotFoundException("Codex CLI is unavailable. Repair or update Codex, then try again.");return found;
 }
 public static string SelectNode(IEnumerable<string> candidates,Func<string,int> probe){
  foreach(string p in candidates.Where(p=>!String.IsNullOrEmpty(p)).Distinct(StringComparer.OrdinalIgnoreCase)){
   if(!File.Exists(p))continue;try{if(probe(p)>=24)return p;}catch{}
  }
  throw new FileNotFoundException("Node.js 24+ is required. Install Node.js or repair the Codex runtime.");
 }
 public static string ResolveNode(string profile,string local,string saved){
  var paths=new List<string>();
  foreach(string dir in (Environment.GetEnvironmentVariable("PATH")??"").Split(Path.PathSeparator))try{paths.Add(Path.Combine(dir.Trim('"'),"node.exe"));}catch{}
  paths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"nodejs","node.exe"));
  paths.Add(Path.Combine(profile,".cache","codex-runtimes","codex-primary-runtime","dependencies","node","bin","node.exe"));
  string runtime=Path.Combine(local,"OpenAI","Codex","runtimes","cua_node");
  if(Directory.Exists(runtime))paths.AddRange(Directory.GetDirectories(runtime).OrderByDescending(Directory.GetLastWriteTimeUtc).Select(d=>Path.Combine(d,"bin","node.exe")));
  paths.Add(saved);
  return SelectNode(paths,p=>Int32.Parse(Run(p,"-p parseInt(process.versions.node)",4000)));
 }
 public static Dictionary<string,object> Refresh(Dictionary<string,object> current){
  var c=new Dictionary<string,object>(current);string registered=null;
  try{registered=RegisteredRoot();}catch(Exception e){c["discoveryWarning"]=e.Message;}
  Func<string,string> old=k=>c.ContainsKey(k)?Convert.ToString(c[k]):null;
  string profile=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),local=Path.Combine(profile,"AppData","Local");
  c["app"]=SelectApp(registered,old("app"));c["cli"]=ResolveCli((string)c["app"],local,old("cli"));c["node"]=ResolveNode(profile,local,old("node"));
  c["resolutionSource"]=registered==null?"saved-valid-fallback":"registered-package";
  c["version"]="0.10.1-windows.1";c["resolvedAt"]=DateTime.UtcNow.ToString("o");return c;
 }
}
