using System;
using System.Text;
using System.Runtime.InteropServices;
public static class TaskbarIdentity {
 [StructLayout(LayoutKind.Sequential)]struct Key{public Guid Format;public uint Id;}
 [StructLayout(LayoutKind.Explicit,Size=24)]struct Value{[FieldOffset(0)]public ushort Type;[FieldOffset(8)]public IntPtr Pointer;}
 [ComImport,Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface Store{
  [PreserveSig]int GetCount(out uint count);[PreserveSig]int GetAt(uint index,out Key key);
  [PreserveSig]int GetValue(ref Key key,out Value value);[PreserveSig]int SetValue(ref Key key,ref Value value);[PreserveSig]int Commit();
 }
 [DllImport("shell32.dll")]static extern int SHGetPropertyStoreForWindow(IntPtr window,ref Guid iid,out Store store);
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)]static extern int SHGetPropertyStoreFromParsingName(string path,IntPtr binding,uint flags,ref Guid iid,out Store store);
 [DllImport("ole32.dll")]static extern int PropVariantClear(ref Value value);
 delegate bool Callback(IntPtr h,IntPtr l);
 [DllImport("user32.dll")]static extern bool EnumWindows(Callback callback,IntPtr l);
 [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint id);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr h,StringBuilder b,int count);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr h,StringBuilder b,int count);
 static Key AppIdKey(){return new Key{Format=new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),Id=5};}
 public static string Read(IntPtr window){Store store;Guid iid=typeof(Store).GUID;Marshal.ThrowExceptionForHR(SHGetPropertyStoreForWindow(window,ref iid,out store));try{var key=AppIdKey();Value value;Marshal.ThrowExceptionForHR(store.GetValue(ref key,out value));try{return value.Type==31?Marshal.PtrToStringUni(value.Pointer):null;}finally{PropVariantClear(ref value);}}finally{Marshal.ReleaseComObject(store);}}
 public static void Set(IntPtr window,string appId){Store store;Guid iid=typeof(Store).GUID;Marshal.ThrowExceptionForHR(SHGetPropertyStoreForWindow(window,ref iid,out store));try{var key=AppIdKey();var value=new Value();if(appId!=null){value.Type=31;value.Pointer=Marshal.StringToCoTaskMemUni(appId);}try{Marshal.ThrowExceptionForHR(store.SetValue(ref key,ref value));}finally{PropVariantClear(ref value);}}finally{Marshal.ReleaseComObject(store);}}
 static string ReadString(Store store,uint id){var key=new Key{Format=AppIdKey().Format,Id=id};Value value;Marshal.ThrowExceptionForHR(store.GetValue(ref key,out value));try{return value.Type==31?Marshal.PtrToStringUni(value.Pointer):null;}finally{PropVariantClear(ref value);}}
 static void WriteString(Store store,uint id,string text){var key=new Key{Format=AppIdKey().Format,Id=id};var value=new Value{Type=31,Pointer=Marshal.StringToCoTaskMemUni(text)};try{Marshal.ThrowExceptionForHR(store.SetValue(ref key,ref value));}finally{PropVariantClear(ref value);}}
 public static bool Relaunch(IntPtr window,string launcher){Store store;Guid iid=typeof(Store).GUID;Marshal.ThrowExceptionForHR(SHGetPropertyStoreForWindow(window,ref iid,out store));try{string command="\""+launcher+"\"";if(ReadString(store,2)==command&&ReadString(store,4)=="Codex")return false;WriteString(store,2,command);WriteString(store,4,"Codex");WriteString(store,3,launcher+",0");return true;}finally{Marshal.ReleaseComObject(store);}}
 public static string RelaunchCommand(IntPtr window){Store store;Guid iid=typeof(Store).GUID;Marshal.ThrowExceptionForHR(SHGetPropertyStoreForWindow(window,ref iid,out store));try{return ReadString(store,2);}finally{Marshal.ReleaseComObject(store);}}
 public static void SetShortcut(string path,string appId){Store store;Guid iid=typeof(Store).GUID;Marshal.ThrowExceptionForHR(SHGetPropertyStoreFromParsingName(path,IntPtr.Zero,2,ref iid,out store));try{var key=AppIdKey();var value=new Value{Type=31,Pointer=Marshal.StringToCoTaskMemUni(appId)};try{Marshal.ThrowExceptionForHR(store.SetValue(ref key,ref value));Marshal.ThrowExceptionForHR(store.Commit());}finally{PropVariantClear(ref value);}}finally{Marshal.ReleaseComObject(store);}}
 public static int Apply(int processId,string appId){return Apply(processId,appId,null);}
 public static int Apply(int processId,string appId,string launcher){int changed=0;EnumWindows(delegate(IntPtr h,IntPtr l){uint id;GetWindowThreadProcessId(h,out id);if(id!=processId||!IsWindowVisible(h))return true;var cls=new StringBuilder(100);var title=new StringBuilder(100);GetClassName(h,cls,100);GetWindowText(h,title,100);if(cls.ToString()!="Chrome_WidgetWin_1"||title.Length==0)return true;try{bool relaunch=launcher!=null&&Relaunch(h,launcher);if(Read(h)!=appId||relaunch){Set(h,appId);changed++;}}catch(COMException){}return true;},IntPtr.Zero);return changed;}
}
