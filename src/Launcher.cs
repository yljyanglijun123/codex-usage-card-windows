using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Runtime.InteropServices;
class Launcher {
 static Mutex instance;
 delegate bool EnumProc(IntPtr w,IntPtr l);
 [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb,IntPtr l);
 [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr w,out uint pid);
 [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr w);
 [StructLayout(LayoutKind.Sequential)] struct Rect {public int L,T,R,B;}
 [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr w,out Rect r);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr w,StringBuilder b,int n);
 [DllImport("user32.dll")] static extern bool ShowWindowAsync(IntPtr w,int n);
 [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr w);
 static bool Visible(int pid){bool visible=false;EnumWindows(delegate(IntPtr w,IntPtr l){uint p;Rect r;GetWindowThreadProcessId(w,out p);var b=new StringBuilder(500);GetWindowText(w,b,500);if(p==pid&&b.Length>0&&IsWindowVisible(w)&&GetWindowRect(w,out r)&&r.R-r.L>200&&r.B-r.T>200)visible=true;return true;},IntPtr.Zero);return visible;}
 static void Show(int pid){EnumWindows(delegate(IntPtr w,IntPtr l){uint p;GetWindowThreadProcessId(w,out p);var b=new StringBuilder(500);GetWindowText(w,b,500);if(p==pid&&b.Length>0){ShowWindowAsync(w,9);SetForegroundWindow(w);}return true;},IntPtr.Zero);}
 static string Q(string s){return "\""+s.Replace("\"","\\\"")+"\"";}
 static int Main(string[] args){
  string root=Path.GetDirectoryName(typeof(Launcher).Assembly.Location);
  try{
   if(args.Length==2&&args[0]=="--windows"){int id=int.Parse(args[1]);var lines=new List<string>();EnumWindows(delegate(IntPtr w,IntPtr l){uint p;Rect r;GetWindowThreadProcessId(w,out p);if(p==id){GetWindowRect(w,out r);var b=new StringBuilder(500);GetWindowText(w,b,500);lines.Add(w+" "+IsWindowVisible(w)+" "+(r.R-r.L)+"x"+(r.B-r.T)+" "+b);}return true;},IntPtr.Zero);File.WriteAllLines(Path.Combine(root,"windows.txt"),lines);return 0;}
   var j=new JavaScriptSerializer();
   var c=j.Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(root,"config.json"),Encoding.UTF8));
   c=Resolver.Refresh(c);
   string configPath=Path.Combine(root,"config.json"),temp=configPath+"."+Process.GetCurrentProcess().Id+".tmp";
   File.WriteAllText(temp,j.Serialize(c),new UTF8Encoding(false));
   try{File.Replace(temp,configPath,null);}catch(IOException){
    // Some redirected Windows directories reject ReplaceFile. Preserve a backup first.
    File.Copy(configPath,configPath+".bak",true);File.Copy(temp,configPath,true);File.Delete(temp);
   }
   if(args.Length>0&&args[0]=="--resolve"){
    File.WriteAllText(Path.Combine(root,"resolution.json"),j.Serialize(c),new UTF8Encoding(false));return 0;
   }
   string app=(string)c["app"],node=(string)c["node"],cli=(string)c["cli"];
   bool test=args.Length>0 && args[0]=="--test";
   bool first;instance=new Mutex(false,"Local\\CodexUsageCardAgentHost"+(test?"Test":""));try{first=instance.WaitOne(0,false);}catch(AbandonedMutexException){first=true;}
   int port=test?39223:39222;
   string profile=test?Path.Combine(root,"test-profile"):null;
   var running=new List<int>();
   using(var search=new ManagementObjectSearcher("SELECT ProcessId,ExecutablePath,CommandLine FROM Win32_Process WHERE Name='ChatGPT.exe' OR Name='Codex.exe'"))
   foreach(ManagementObject p in search.Get()){
    string exe=Convert.ToString(p["ExecutablePath"]),cmd=Convert.ToString(p["CommandLine"]);
    if(String.Equals(exe,app,StringComparison.OrdinalIgnoreCase)&&!cmd.Contains(" --type=")&&(!test||cmd.Contains(profile))&& (test||!cmd.Contains("--user-data-dir="))) {
     if(!cmd.Contains("--remote-debugging-port="+port))throw new Exception("Codex is already open without the usage-card connection. Exit it fully, then open the Codex shortcut installed by Usage Card.");
     running.Add(Convert.ToInt32(p["ProcessId"]));
    }
   }
   Process desktop;
   if(running.Count>0){desktop=Process.GetProcessById(running[0]);Show(desktop.Id);}
   else{
    var start=new ProcessStartInfo(app,"--remote-debugging-address=127.0.0.1 --remote-debugging-port="+port+(test?" --user-data-dir="+Q(profile):""));
    start.UseShellExecute=false;
    desktop=Process.Start(start);
   }
   if(!first)return 0;
   string stop=Path.Combine(root,"stop-agent-"+Process.GetCurrentProcess().Id);
   string agent=Path.Combine(root,"agent.cjs");
   var payload=j.Serialize(new {agent=agent,app=app,cli=cli,port=port,stop=stop});
   string b64=Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
   string code="const c=JSON.parse(Buffer.from('"+b64+"','base64').toString('utf8'));process.argv=[process.execPath,c.agent,'--app',c.app,'--codex-bin',c.cli,'--port',String(c.port),'--stop-file',c.stop];require(c.agent).main().catch(e=>{console.error(e.message);process.exitCode=1;});";
   var pi=new ProcessStartInfo(node,"-e "+Q(code));pi.UseShellExecute=false;pi.CreateNoWindow=true;pi.RedirectStandardError=true;
   pi.EnvironmentVariables["LOCALAPPDATA"]=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
   pi.EnvironmentVariables["APPDATA"]=Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
   pi.EnvironmentVariables["USERPROFILE"]=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
   var worker=Process.Start(pi);
   File.WriteAllText(Path.Combine(root,test?"test-session.json":"session.json"),j.Serialize(new{launcher=Process.GetCurrentProcess().Id,app=desktop.Id,agent=worker.Id,port=port,startedAt=DateTime.UtcNow.ToString("o")}));
   bool seen=false;int hiddenTicks=0;
   while(!desktop.HasExited){bool visible=Visible(desktop.Id);if(visible){seen=true;hiddenTicks=0;}else if(seen&&++hiddenTicks>=3)break;Thread.Sleep(1000);}
   File.WriteAllText(stop,"");
   if(!worker.WaitForExit(10000))throw new Exception("Usage agent did not exit in time. Check diagnostics.");
   File.Delete(stop);
   File.WriteAllText(Path.Combine(root,test?"test-exited.json":"exited.json"),j.Serialize(new{appExited=desktop.HasExited,windowClosed=true,agentExited=true,at=DateTime.UtcNow.ToString("o")}));
   return 0;
  }catch(Exception e){File.WriteAllText(Path.Combine(root,"launcher-error.txt"),e.ToString());if(args.Length==0||args[0]!="--resolve")MessageBox.Show(e.Message,"Codex Usage Card",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
 }
}
