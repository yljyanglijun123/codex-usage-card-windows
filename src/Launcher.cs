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
 static void Trace(string message){try{File.AppendAllText(Path.Combine(Path.GetDirectoryName(typeof(Launcher).Assembly.Location),"activation.log"),DateTime.UtcNow.ToString("o")+" pid="+Process.GetCurrentProcess().Id+" "+message+Environment.NewLine);}catch{}}
 delegate bool EnumProc(IntPtr w,IntPtr l);
 [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb,IntPtr l);
 [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr w,out uint pid);
 [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr w);
 [StructLayout(LayoutKind.Sequential)] struct Rect {public int L,T,R,B;}
 [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr w,out Rect r);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr w,StringBuilder b,int n);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr w,StringBuilder b,int n);
 [DllImport("user32.dll")] static extern bool IsIconic(IntPtr w);
 [DllImport("user32.dll")] static extern bool ShowWindowAsync(IntPtr w,int n);
 [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr w);
 static bool Visible(int pid){bool visible=false;EnumWindows(delegate(IntPtr w,IntPtr l){uint p;Rect r;GetWindowThreadProcessId(w,out p);var b=new StringBuilder(500);GetWindowText(w,b,500);if(p==pid&&b.Length>0&&IsWindowVisible(w)&&GetWindowRect(w,out r)&&r.R-r.L>200&&r.B-r.T>200)visible=true;return true;},IntPtr.Zero);return visible;}
 // Never unhide arbitrary titled windows: Electron also owns hidden dialogs and IME hosts.
 // A second application launch uses Electron's own single-instance activation to reopen from tray.
 static bool FocusVisible(int pid){IntPtr best=IntPtr.Zero;long area=0;EnumWindows(delegate(IntPtr w,IntPtr l){uint p;Rect r;GetWindowThreadProcessId(w,out p);var b=new StringBuilder(500);var cls=new StringBuilder(100);GetWindowText(w,b,500);GetClassName(w,cls,100);if(p==pid&&b.Length>0&&cls.ToString()=="Chrome_WidgetWin_1"&&IsWindowVisible(w)&&GetWindowRect(w,out r)&&r.R-r.L>200&&r.B-r.T>200){long size=(long)(r.R-r.L)*(r.B-r.T);if(size>area){best=w;area=size;}}return true;},IntPtr.Zero);if(best==IntPtr.Zero)return false;if(IsIconic(best))ShowWindowAsync(best,9);SetForegroundWindow(best);return true;}
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 static bool Foreground(int id){uint p;GetWindowThreadProcessId(GetForegroundWindow(),out p);return p==id;}
 static int ActivateDesktop(int port,string profile){string arguments="--remote-debugging-address=127.0.0.1 --remote-debugging-port="+port+(profile==null?"":" --user-data-dir="+Q(profile));return NativeActivation.Launch("OpenAI.Codex_2p2nqsd0c76g0!App",arguments);}
 static Process FindDesktop(string app,string profile,bool test,int port,HashSet<int> disconnected){
  using(var search=new ManagementObjectSearcher("SELECT ProcessId,ExecutablePath,CommandLine FROM Win32_Process WHERE Name='ChatGPT.exe' OR Name='Codex.exe'"))
  foreach(ManagementObject p in search.Get()){
   string exe=Convert.ToString(p["ExecutablePath"]),cmd=Convert.ToString(p["CommandLine"]);
   if(!String.Equals(exe,app,StringComparison.OrdinalIgnoreCase)||cmd.Contains(" --type=")||(test&&!cmd.Contains(profile))||(!test&&cmd.Contains("--user-data-dir=")))continue;
   try{var candidate=Process.GetProcessById(Convert.ToInt32(p["ProcessId"]));if(candidate.HasExited){candidate.Dispose();continue;}if(!cmd.Contains("--remote-debugging-port="+port))disconnected.Add(candidate.Id);return candidate;}catch(ArgumentException){}
  }
  return null;
 }
 static string EntryPath(string root){string host=Path.Combine(root,"CodexUsageCardHost-v6.exe");return File.Exists(host)?host:Path.Combine(root,"CodexUsageCard.exe");}
 static void StartMonitor(string root,bool test){string file=Path.Combine(root,"CodexUsageCardMonitor-v1.exe");var p=new ProcessStartInfo(file,"--monitor --attach"+(test?" --test":""));p.UseShellExecute=false;p.CreateNoWindow=true;using(var monitor=Process.Start(p)){}Trace("monitor-dispatched");}
 static string Q(string s){return "\""+s.Replace("\"","\\\"")+"\"";}
 static int Main(string[] args){
  string root=Path.GetDirectoryName(typeof(Launcher).Assembly.Location);Trace("entry attach="+(Array.IndexOf(args,"--attach")>=0));
  try{
   if(args.Length==2&&args[0]=="--windows"){int id=int.Parse(args[1]);var lines=new List<string>();EnumWindows(delegate(IntPtr w,IntPtr l){uint p;Rect r;GetWindowThreadProcessId(w,out p);if(p==id){GetWindowRect(w,out r);var b=new StringBuilder(500);GetWindowText(w,b,500);lines.Add(w+" "+IsWindowVisible(w)+" "+(r.R-r.L)+"x"+(r.B-r.T)+" "+b);}return true;},IntPtr.Zero);File.WriteAllLines(Path.Combine(root,"windows.txt"),lines);return 0;}
   bool test=Array.IndexOf(args,"--test")>=0;
   bool lifecycle=Array.IndexOf(args,"--monitor")>=0;
   bool attach=lifecycle||Array.IndexOf(args,"--attach")>=0;
   if(attach&&!lifecycle){StartMonitor(root,test);return 0;}
   // A background observer must never activate a window or steal focus.
   if(attach){using(var existing=new Mutex(false,"Local\\CodexUsageCardAgentHost"+(test?"Test":""))){bool free;try{free=existing.WaitOne(0,false);}catch(AbandonedMutexException){free=true;}if(!free)return 0;existing.ReleaseMutex();}}
   // Coalesce repeated clicks before path probing, config writes, or native activation.
   using(var activation=new Mutex(false,"Local\\CodexUsageCardActivation"+(test?"Test":""))){
   bool activating;try{activating=activation.WaitOne(0,false);}catch(AbandonedMutexException){activating=true;}
   if(!activating){Trace("activation-mutex-busy");return 0;}
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
   string appId=test?"CodexUsageCard.IsolatedTests":(c.ContainsKey("appUserModelId")?Convert.ToString(c["appUserModelId"]):"OpenAI.Codex_2p2nqsd0c76g0!App");
   bool first;instance=new Mutex(false,"Local\\CodexUsageCardAgentHost"+(test?"Test":""));try{first=instance.WaitOne(0,false);}catch(AbandonedMutexException){first=true;}
   int port=test?39223:39222;
   string profile=test?Path.Combine(root,"test-profile"):null;
   var disconnected=new HashSet<int>();
   Process desktop=null;
   for(int attempt=0;attempt<4;attempt++){
    desktop=FindDesktop(app,profile,test,port,disconnected);
    if(attach){if(desktop==null||desktop.HasExited||!Visible(desktop.Id))return 0;break;}
    bool restoring=desktop!=null&&!desktop.HasExited;
    if(desktop==null||desktop.HasExited){Trace("native-cold-start attempt="+attempt);int launched=ActivateDesktop(port,profile);Trace("activation-returned="+launched);desktop=null;}
    else if(!FocusVisible(desktop.Id)){Trace("native-restore app="+desktop.Id+" attempt="+attempt);ActivateDesktop(port,profile);}
    var openDeadline=DateTime.UtcNow.AddSeconds(restoring?2:8);
    bool stable=false;
    while(DateTime.UtcNow<openDeadline){
     if(desktop==null)desktop=FindDesktop(app,profile,test,port,disconnected);
     if(desktop!=null){
      if(desktop.HasExited){Trace("startup-process-exited app="+desktop.Id);desktop.Dispose();desktop=null;break;}
      if(Visible(desktop.Id)){Thread.Sleep(400);if(!desktop.HasExited&&Visible(desktop.Id)){stable=true;break;}}
     }
     Thread.Sleep(150);
    }
    if(stable)break;
    Trace("activation-retry attempt="+attempt);desktop=null;Thread.Sleep(250);
   }
   if(desktop==null||desktop.HasExited||!Visible(desktop.Id)){if(attach)return 0;throw new Exception("Codex did not complete activation after bounded retries. See activation.log.");}
   Trace("window-visible app="+desktop.Id+" foreground="+Foreground(desktop.Id));
   if(!attach){FocusVisible(desktop.Id);Thread.Sleep(100);Trace("foreground-after-focus="+Foreground(desktop.Id));}
   activation.ReleaseMutex();
   if(disconnected.Contains(desktop.Id)){File.WriteAllText(Path.Combine(root,"connection-status.json"),j.Serialize(new{connected=false,reason="existing-native-session",app=desktop.Id}));return 0;}
   if(!lifecycle){if(first)instance.ReleaseMutex();StartMonitor(root,test);Trace("entry-complete");return 0;}
   if(!first){try{first=instance.WaitOne(5000,false);}catch(AbandonedMutexException){first=true;}if(!first){Trace("existing-monitor-active");return 0;}}
   string stop=Path.Combine(root,"stop-agent-"+Process.GetCurrentProcess().Id);
   string agent=Path.Combine(root,"agent.cjs");
   var payload=j.Serialize(new {agent=agent,app=app,cli=cli,port=port,stop=stop});
   string b64=Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
   string code="const c=JSON.parse(Buffer.from('"+b64+"','base64').toString('utf8'));process.argv=[process.execPath,c.agent,'--app',c.app,'--codex-bin',c.cli,'--port',String(c.port),'--stop-file',c.stop];require(c.agent).main().catch(e=>{console.error(e.message);process.exitCode=1;});";
   var pi=new ProcessStartInfo(node,"-e "+Q(code));pi.UseShellExecute=false;pi.CreateNoWindow=true;pi.RedirectStandardError=true;
   pi.EnvironmentVariables["LOCALAPPDATA"]=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
   pi.EnvironmentVariables["APPDATA"]=Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
   pi.EnvironmentVariables["USERPROFILE"]=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
   // Drain stderr so a long-running agent cannot block on a full redirected pipe.
   pi.RedirectStandardOutput=true;
   Process worker=null;int hiddenTicks=0;DateTime retryAgent=DateTime.MinValue;
   try{
    while(!desktop.HasExited){
     bool visible=Visible(desktop.Id);
     if(visible){
      TaskbarIdentity.Apply(desktop.Id,appId,EntryPath(root));
      hiddenTicks=0;
      if(worker==null&&DateTime.UtcNow>=retryAgent){
       File.Delete(stop);
       worker=Process.Start(pi);worker.OutputDataReceived+=delegate{};worker.ErrorDataReceived+=delegate{};worker.BeginOutputReadLine();worker.BeginErrorReadLine();
       File.WriteAllText(Path.Combine(root,test?"test-session.json":"session.json"),j.Serialize(new{launcher=Process.GetCurrentProcess().Id,app=desktop.Id,agent=worker.Id,port=port,startedAt=DateTime.UtcNow.ToString("o")}));
      }
     }else if(++hiddenTicks>=3&&worker!=null){
      StopWorker(worker,stop);worker.Dispose();worker=null;
      File.WriteAllText(Path.Combine(root,test?"test-exited.json":"exited.json"),j.Serialize(new{appExited=desktop.HasExited,windowClosed=true,agentExited=true,monitorWaiting=true,at=DateTime.UtcNow.ToString("o")}));
     }
     if(worker!=null&&worker.HasExited){worker.Dispose();worker=null;retryAgent=DateTime.UtcNow.AddSeconds(5);}
     Thread.Sleep(500);
    }
   }finally{if(worker!=null){StopWorker(worker,stop);worker.Dispose();}}
   File.WriteAllText(Path.Combine(root,test?"test-exited.json":"exited.json"),j.Serialize(new{appExited=true,windowClosed=true,agentExited=true,monitorWaiting=false,at=DateTime.UtcNow.ToString("o")}));
   return 0;
   }
  }catch(Exception e){Trace("error="+e.GetType().Name+" "+e.Message);File.WriteAllText(Path.Combine(root,"launcher-error.txt"),e.ToString());if(Array.IndexOf(args,"--attach")<0&&(args.Length==0||args[0]!="--resolve"))MessageBox.Show(e.Message,"Codex Usage Card",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
 }
 static void StopWorker(Process worker,string stop){File.WriteAllText(stop,"");if(!worker.WaitForExit(10000))throw new Exception("Usage agent did not exit in time. Check diagnostics.");File.Delete(stop);}
}
