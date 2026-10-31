using System;
using System.Runtime.InteropServices;
public static class NativeActivation {
 [ComImport,Guid("2e941141-7f97-4756-ba1d-9decde894a3d"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface Manager {
  [PreserveSig]int ActivateApplication([MarshalAs(UnmanagedType.LPWStr)]string app,[MarshalAs(UnmanagedType.LPWStr)]string args,uint options,out uint pid);
  [PreserveSig]int ActivateForFile(IntPtr items,string verb,out uint pid);
  [PreserveSig]int ActivateForProtocol(IntPtr items,out uint pid);
 }
 [DllImport("ole32.dll")]static extern int CoAllowSetForegroundWindow([MarshalAs(UnmanagedType.IUnknown)]object server,IntPtr reserved);
 [DllImport("ole32.dll")]static extern int CoCreateInstance(ref Guid cls,IntPtr outer,uint context,ref Guid iid,[MarshalAs(UnmanagedType.Interface)]out Manager manager);
 public static int Launch(string id,string args){Guid cls=new Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C"),iid=typeof(Manager).GUID;Manager manager;Marshal.ThrowExceptionForHR(CoCreateInstance(ref cls,IntPtr.Zero,4,ref iid,out manager));try{CoAllowSetForegroundWindow(manager,IntPtr.Zero);uint pid;Marshal.ThrowExceptionForHR(manager.ActivateApplication(id,args,2,out pid));return (int)pid;}finally{Marshal.ReleaseComObject(manager);}}
}