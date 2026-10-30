using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Runtime.InteropServices;
class Broker {
 delegate bool EnumProc(IntPtr h,IntPtr l);
 [DllImport("user32.dll")]static extern bool EnumWindows(EnumProc c,IntPtr l);
 [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint p);
 [StructLayout(LayoutKind.Sequential)]struct Rect{public int L,T,R,B;}
 [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr h,out Rect r);
 [StructLayout(LayoutKind.Sequential)]struct Input{public uint Size,Tick;}
 [DllImport("user32.dll")]static extern bool GetLastInputInfo(ref Input i);
 static uint Idle(){var i=new Input();i.Size=(uint)Marshal.SizeOf(i);GetLastInputInfo(ref i);return unchecked((uint)Environment.TickCount-i.Tick);}
 static bool Visible(int id){bool yes=false;EnumWindows(delegate(IntPtr h,IntPtr l){uint p;Rect r;GetWindowThreadProcessId(h,out p);if(p==id&&IsWindowVisible(h)&&GetWindowRect(h,out r)&&r.R-r.L>200&&r.B-r.T>200)yes=true;return true;},IntPtr.Zero);return yes;}
 static int Main(string[] args){
  string root=Path.GetDirectoryName(typeof(Broker).Assembly.Location);bool test=args.Length>0&&args[0]=="--test";
  string log=Path.Combine(root,test?"broker-test.log":"broker.log");
  bool created;using(var mutex=new Mutex(false,"Local\\CodexUsageCardBroker"+(test?"Test":""))){
   try{created=mutex.WaitOne(0,false);}catch(AbandonedMutexException){created=true;}
   if(!created)return 0;
   try{
    var c=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(root,"config.json"),Encoding.UTF8));
    string app=(string)c["app"],profile=Path.Combine(root,"test-profile");
    var seen=new HashSet<int>();var visibility=new Dictionary<int,bool>();bool baseline=true;DateTime attempted=DateTime.MinValue;
    File.AppendAllText(log,"started "+Process.GetCurrentProcess().Id+" "+DateTime.UtcNow.ToString("o")+Environment.NewLine);
    while(!File.Exists(Path.Combine(root,test?"stop-broker-test":"stop-broker"))){
     using(var q=new ManagementObjectSearcher("SELECT ProcessId,ExecutablePath,CommandLine,CreationDate FROM Win32_Process WHERE Name='ChatGPT.exe' OR Name='Codex.exe'"))
     foreach(ManagementObject p in q.Get()){
      string exe=Convert.ToString(p["ExecutablePath"]),cmd=Convert.ToString(p["CommandLine"]);
      if(!String.Equals(exe,app,StringComparison.OrdinalIgnoreCase)||cmd.Contains(" --type="))continue;
      if(test&&!cmd.Contains(profile))continue;if(!test&&cmd.Contains("--user-data-dir="))continue;
      int id=Convert.ToInt32(p["ProcessId"]);bool fresh=seen.Add(id),visible=Visible(id),previous=false;visibility.TryGetValue(id,out previous);visibility[id]=visible;if(baseline)continue;
      if(!fresh){if(visible&&!previous&&cmd.Contains("--remote-debugging-port=")){
        var reopen=new ProcessStartInfo(Path.Combine(root,"CodexUsageCardHost-v2.exe"),test?"--test":"");reopen.UseShellExecute=false;reopen.CreateNoWindow=true;Process.Start(reopen);File.AppendAllText(log,"window-reopened "+id+Environment.NewLine);
       }continue;}
      if(cmd.Contains("--remote-debugging-port="))continue;
      double age=(DateTime.UtcNow-ManagementDateTimeConverter.ToDateTime(Convert.ToString(p["CreationDate"])).ToUniversalTime()).TotalMilliseconds;
      if(age<0||age>1500||Idle()<age||Visible(id)||(DateTime.UtcNow-attempted).TotalSeconds<120){File.AppendAllText(log,"skipped "+id+" age="+age+Environment.NewLine);continue;}
      attempted=DateTime.UtcNow;File.AppendAllText(log,"restart-new-unshown "+id+Environment.NewLine);
      // Revalidate the exact fresh process immediately before the single startup handoff.
      using(var verify=new ManagementObject("Win32_Process.Handle='"+id+"'")){
       verify.Get();if(!String.Equals(Convert.ToString(verify["ExecutablePath"]),app,StringComparison.OrdinalIgnoreCase)||Convert.ToString(verify["CommandLine"])!=cmd||Visible(id)||Idle()<age)continue;
       var proc=Process.GetProcessById(id);proc.Kill();proc.WaitForExit(5000);
      }
      var start=new ProcessStartInfo(Path.Combine(root,"CodexUsageCardHost-v2.exe"),test?"--test":"");start.UseShellExecute=false;start.CreateNoWindow=true;Process.Start(start);
     }
     baseline=false;Thread.Sleep(100);
    }
    File.AppendAllText(log,"stopped"+Environment.NewLine);return 0;
   }catch(Exception e){File.AppendAllText(log,e.ToString()+Environment.NewLine);return 1;}
  }
 }
}
