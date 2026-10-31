using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Web.Script.Serialization;
using System.Runtime.InteropServices;
class Broker {
 delegate bool EnumProc(IntPtr h,IntPtr l);
 [DllImport("user32.dll")]static extern bool EnumWindows(EnumProc c,IntPtr l);
 [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint p);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr h,StringBuilder b,int n);
 [StructLayout(LayoutKind.Sequential)]struct Rect{public int L,T,R,B;}
 [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr h,out Rect r);
 [StructLayout(LayoutKind.Sequential)]struct Input{public uint Size,Tick;}
 [DllImport("user32.dll")]static extern bool GetLastInputInfo(ref Input i);
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct Entry{public uint Size,Usage,Id;public UIntPtr Heap;public uint Module,Threads,Parent;public int Priority;public uint Flags;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)]public string Name;}
 [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr CreateToolhelp32Snapshot(uint flags,uint id);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern bool Process32First(IntPtr h,ref Entry e);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern bool Process32Next(IntPtr h,ref Entry e);
 [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr h);
 static List<int> Candidates(string app){var entries=new List<Entry>();var ids=new HashSet<uint>();IntPtr h=CreateToolhelp32Snapshot(2,0);if(h==new IntPtr(-1))return new List<int>();try{var e=new Entry();e.Size=(uint)Marshal.SizeOf(e);if(Process32First(h,ref e))do{if(String.Equals(e.Name,Path.GetFileName(app),StringComparison.OrdinalIgnoreCase)){entries.Add(e);ids.Add(e.Id);}}while(Process32Next(h,ref e));}finally{CloseHandle(h);}var found=new List<int>();foreach(var e in entries)if(!ids.Contains(e.Parent))found.Add((int)e.Id);return found;}
 static uint InputTick(){var i=new Input();i.Size=(uint)Marshal.SizeOf(i);GetLastInputInfo(ref i);return i.Tick;}
 static bool Visible(int id){bool yes=false;EnumWindows(delegate(IntPtr h,IntPtr l){uint p;Rect r;GetWindowThreadProcessId(h,out p);var title=new StringBuilder(100);GetWindowText(h,title,100);if(p==id&&title.Length>0&&IsWindowVisible(h)&&GetWindowRect(h,out r)&&r.R-r.L>200&&r.B-r.T>200)yes=true;return true;},IntPtr.Zero);return yes;}
 static bool HostRunning(bool test){using(var m=new Mutex(false,"Local\\CodexUsageCardAgentHost"+(test?"Test":""))){bool free;try{free=m.WaitOne(0,false);}catch(AbandonedMutexException){free=true;}if(free)m.ReleaseMutex();return !free;}}
 static void StartHost(string root,bool test,bool attach){var p=new ProcessStartInfo(Path.Combine(root,"CodexUsageCardHost-v6.exe"),(test?"--test ":"")+(attach?"--attach":""));p.UseShellExecute=false;p.CreateNoWindow=true;using(var child=Process.Start(p)){} }
 static int Main(string[] args){
  string root=Path.GetDirectoryName(typeof(Broker).Assembly.Location);bool test=Array.IndexOf(args,"--test")>=0;
  string log=Path.Combine(root,test?"broker-test.log":"broker.log");
  Action<string> record=s=>File.AppendAllText(log,s+Environment.NewLine);
  using(var mutex=new Mutex(false,"Local\\CodexUsageCardBroker"+(test?"Test":""))){
   bool first;try{first=mutex.WaitOne(0,false);}catch(AbandonedMutexException){first=true;}if(!first)return 0;
   ManagementEventWatcher watcher=null;var wake=new AutoResetEvent(false);
   try{
    var json=new JavaScriptSerializer();var config=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(root,"config.json"),Encoding.UTF8));
    if(Convert.ToString(config["owner"])!="codex-usage-card-windows-v1")throw new Exception("Installation ownership mismatch");
    string app=Convert.ToString(config["app"]),profile=Path.Combine(root,"test-profile");
    var queue=new ConcurrentQueue<int>();var seen=new HashSet<string>();var debug=new Dictionary<int,DateTime>();var knownFallback=new HashSet<int>();
    string processName=Path.GetFileNameWithoutExtension(app);bool events=false;
    try{
     watcher=new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace WHERE ProcessName='"+Path.GetFileName(app)+"'"));
     watcher.EventArrived+=delegate(object sender,EventArrivedEventArgs e){
      try{int parent=Convert.ToInt32(e.NewEvent["ParentProcessID"]);try{using(var p=Process.GetProcessById(parent)){if(String.Equals(p.ProcessName,processName,StringComparison.OrdinalIgnoreCase))return;}}catch(ArgumentException){}
       queue.Enqueue(Convert.ToInt32(e.NewEvent["ProcessID"]));wake.Set();
      }catch{} };
     watcher.Start();events=true;record("process-start-events enabled");
    }catch(Exception e){record("event-source unavailable; 500ms fallback "+e.GetType().Name);if(watcher!=null){watcher.Dispose();watcher=null;}}
    Action<int,bool> inspect=(id,baseline)=>{
     try{using(var process=Process.GetProcessById(id)){
      if(process.Handle==IntPtr.Zero)return;string key=id+":"+process.StartTime.ToUniversalTime().Ticks;if(!seen.Add(key))return;
      using(var p=new ManagementObject("Win32_Process.Handle='"+id+"'")){
       p.Get();string exe=Convert.ToString(p["ExecutablePath"]),cmd=Convert.ToString(p["CommandLine"]);
       if(!String.Equals(exe,app,StringComparison.OrdinalIgnoreCase)||cmd.Contains(" --type="))return;
       if(test&&!cmd.Contains(profile))return;if(!test&&cmd.Contains("--user-data-dir="))return;
       if(cmd.Contains("--remote-debugging-port="+(test?39223:39222))){debug[id]=DateTime.MinValue;return;}
       record("passive-no-connection "+id);return;
      }
     }}catch(ManagementException){}catch(InvalidOperationException){}catch(ArgumentException){}catch(System.ComponentModel.Win32Exception){}
    };
    record("v6-passive-started "+Process.GetCurrentProcess().Id+" "+DateTime.UtcNow.ToString("o"));
    foreach(int id in Candidates(app))inspect(id,true);
    File.WriteAllText(Path.Combine(root,test?"broker-test-ready.json":"broker-ready.json"),json.Serialize(new{process=Process.GetCurrentProcess().Id,events=events,readyAt=DateTime.UtcNow.ToString("o")}));record("ready");
    DateTime refresh=DateTime.MinValue,scan=DateTime.MinValue;bool refreshing=false;
    while(!File.Exists(Path.Combine(root,test?"stop-broker-test":"stop-broker"))){
     if(DateTime.UtcNow>=refresh&&!refreshing){refresh=DateTime.UtcNow.AddSeconds(60);refreshing=true;ThreadPool.QueueUserWorkItem(delegate{try{string updated=Resolver.SelectApp(Resolver.RegisteredRoot(),app);Interlocked.Exchange(ref app,updated);}catch{}finally{refreshing=false;}});}
     if(!events){var current=new HashSet<int>();bool added=false;foreach(var p in Process.GetProcessesByName(processName))using(p){current.Add(p.Id);if(!knownFallback.Contains(p.Id))added=true;}if(added)foreach(int candidate in Candidates(app))queue.Enqueue(candidate);knownFallback=current;}
     int next;while(queue.TryDequeue(out next))inspect(next,false);
     if(DateTime.UtcNow>=scan){scan=DateTime.UtcNow.AddSeconds(3);
      foreach(int id in new List<int>(debug.Keys)){
       try{using(var p=Process.GetProcessById(id)){if(p.HasExited){debug.Remove(id);continue;}}}catch(ArgumentException){debug.Remove(id);continue;}
       if(!HostRunning(test)&&Visible(id)&&(DateTime.UtcNow-debug[id]).TotalSeconds>=15){StartHost(root,test,true);debug[id]=DateTime.UtcNow;record("passive-attach "+id);}
      }
      // Discard exited process identities; a new start always carries its own creation time.
      seen.RemoveWhere(key=>{int id=Int32.Parse(key.Split(':')[0]);try{using(var p=Process.GetProcessById(id)){return p.HasExited;}}catch(ArgumentException){return true;}});
     }
     wake.WaitOne(500);
    }
    record("stopped");return 0;
   }catch(Exception e){record(e.ToString());return 1;}
   finally{if(watcher!=null){try{watcher.Stop();}catch{}watcher.Dispose();}wake.Dispose();}
  }
 }
}
