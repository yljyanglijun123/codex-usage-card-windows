using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public static class AppIdentityProbe {
 [StructLayout(LayoutKind.Sequential)]struct Key {public Guid Format;public uint Id;}
 [StructLayout(LayoutKind.Explicit,Size=24)]struct Value {[FieldOffset(0)]public ushort Type;[FieldOffset(8)]public IntPtr Pointer;}
 [ComImport,Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface Store{[PreserveSig]int GetCount(out uint count);[PreserveSig]int GetAt(uint index,out Key key);[PreserveSig]int GetValue(ref Key key,out Value value);[PreserveSig]int SetValue(ref Key key,ref Value value);[PreserveSig]int Commit();}
 [DllImport("shell32.dll")]static extern int SHGetPropertyStoreForWindow(IntPtr h,ref Guid iid,out Store store);
 [DllImport("ole32.dll")]static extern int PropVariantClear(ref Value value);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern int GetApplicationUserModelId(IntPtr process,ref uint length,StringBuilder value);
 delegate bool Callback(IntPtr h,IntPtr l);
 [DllImport("user32.dll")]static extern bool EnumWindows(Callback c,IntPtr l);
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint id);
 [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr h);
 public static string[] Read(int id){var values=new List<string>();using(var p=System.Diagnostics.Process.GetProcessById(id)){uint n=0;int status=GetApplicationUserModelId(p.Handle,ref n,null);if(n>0){var b=new StringBuilder((int)n);status=GetApplicationUserModelId(p.Handle,ref n,b);values.Add("ProcessAppId="+b+" status="+status);}else values.Add("ProcessAppId status="+status);}
 EnumWindows(delegate(IntPtr h,IntPtr l){uint pid;GetWindowThreadProcessId(h,out pid);if(pid==id&&IsWindowVisible(h)){Store store;Guid iid=typeof(Store).GUID;if(SHGetPropertyStoreForWindow(h,ref iid,out store)==0){try{var key=new Key{Format=new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),Id=5};Value v;if(store.GetValue(ref key,out v)==0){values.Add("Window="+h+" AppId="+(v.Type==31?Marshal.PtrToStringUni(v.Pointer):"<unset>"));PropVariantClear(ref v);}}finally{Marshal.ReleaseComObject(store);}}}return true;},IntPtr.Zero);return values.ToArray();}
}
