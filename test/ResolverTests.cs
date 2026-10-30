using System;
using System.IO;
class ResolverTests {
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("PASS "+label);}
 static void Main(){string root=Path.Combine(Path.GetTempPath(),"codex-resolver-"+Guid.NewGuid());Directory.CreateDirectory(root);
  try{
   string old=Path.Combine(root,"old.exe"),package=Path.Combine(root,"new-package");File.WriteAllText(old,"");Directory.CreateDirectory(Path.Combine(package,"app"));string app=Path.Combine(package,"app","ChatGPT.exe");File.WriteAllText(app,"");
   Check(Resolver.SelectApp(package,old)==app,"registered new version wins even when old path still exists");File.Delete(old);
   Check(Resolver.SelectApp(package,old)==app,"deleted old app path recovers automatically");Check(Resolver.SelectApp(null,app)==app,"valid fallback if package discovery unavailable");
   bool failed=false;try{Resolver.SelectApp(null,old);}catch(FileNotFoundException){failed=true;}Check(failed,"missing installation gives explicit failure");
   string a=Path.Combine(root,"node-old.exe"),b=Path.Combine(root,"node-new.exe");File.WriteAllText(a,"");File.WriteAllText(b,"");
   Check(Resolver.SelectNode(new[]{a,b},p=>p==a?22:24)==b,"Node below 24 is skipped");Check(Resolver.SelectNode(new[]{old,b},p=>24)==b,"deleted cached Node is skipped");
   Check(Resolver.SelectNode(new[]{a,b},p=>{if(p==a)throw new Exception();return 24;})==b,"broken Node does not prevent runtime fallback");
   string cache=Path.Combine(root,"OpenAI","Codex","bin");Directory.CreateDirectory(Path.Combine(cache,"new"));string cli=Path.Combine(cache,"new","codex.exe");File.WriteAllText(cli,"");
   Check(Resolver.ResolveCli(app,root,old)==cli,"versioned managed CLI replaces missing saved CLI");
   Directory.CreateDirectory(Path.Combine(package,"app","resources"));string bundled=Path.Combine(package,"app","resources","codex.exe");File.WriteAllText(bundled,"");Check(Resolver.ResolveCli(app,root,cli)==bundled,"current app bundled CLI wins over managed cache");
  }finally{Directory.Delete(root,true);}
 }
}
