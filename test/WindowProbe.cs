using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public static class WindowProbe {
 public class Window { public long Handle; public string Class; public bool Visible; public bool Responding; public int Width; public int Height; public int TitleLength; }
 delegate bool Callback(IntPtr h,IntPtr l);
 [StructLayout(LayoutKind.Sequential)] struct Rect {public int L,T,R,B;}
 [DllImport("user32.dll")] static extern bool EnumWindows(Callback c,IntPtr l);
 [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h,out uint id);
 [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h,out Rect r);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h,StringBuilder s,int n);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr h,StringBuilder s,int n);
 [DllImport("user32.dll",SetLastError=true)] static extern IntPtr SendMessageTimeout(IntPtr h,uint m,IntPtr w,IntPtr l,uint flags,uint timeout,out IntPtr result);
 public static Window[] Get(int id){var list=new List<Window>();EnumWindows(delegate(IntPtr h,IntPtr l){uint p;GetWindowThreadProcessId(h,out p);if(p==id){Rect r;GetWindowRect(h,out r);var t=new StringBuilder(500);GetWindowText(h,t,500);var c=new StringBuilder(500);GetClassName(h,c,500);IntPtr v;bool responding=SendMessageTimeout(h,0,IntPtr.Zero,IntPtr.Zero,2,1000,out v)!=IntPtr.Zero;list.Add(new Window{Handle=h.ToInt64(),Class=c.ToString(),Visible=IsWindowVisible(h),Responding=responding,Width=r.R-r.L,Height=r.B-r.T,TitleLength=t.Length});}return true;},IntPtr.Zero);return list.ToArray();}
}
