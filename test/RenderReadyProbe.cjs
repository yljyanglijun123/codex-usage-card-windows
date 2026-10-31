async function probe(){
 const targets=await(await fetch('http://127.0.0.1:39223/json/list')).json();
 const target=targets.find(t=>t.type==='page'&&t.url==='app://-/index.html');
 if(!target)throw Error('No test renderer');
 return new Promise((resolve,reject)=>{const ws=new WebSocket(target.webSocketDebuggerUrl);const timer=setTimeout(()=>{ws.close();reject(Error('Renderer probe timed out'));},3000);
 ws.onopen=()=>ws.send(JSON.stringify({id:1,method:'Runtime.evaluate',params:{expression:'({complete:document.readyState==="complete",controls:document.querySelectorAll("button,textarea,[role=button]").length,textChars:document.body?.innerText.length??0})',returnByValue:true}}));
 ws.onmessage=e=>{const m=JSON.parse(e.data);if(m.id!==1)return;clearTimeout(timer);ws.close();resolve(m.result?.result?.value);};ws.onerror=()=>{clearTimeout(timer);reject(Error('Renderer probe failed'));};});
}
(async()=>{for(let i=0;i<60;i++){try{const p=await probe();if(p?.complete&&p.controls>0&&p.textChars>60){console.log(JSON.stringify({rendererReady:true,hasControls:true,notEmpty:true}));return;}}catch{}await new Promise(r=>setTimeout(r,500));}throw Error('Main renderer remained empty or incomplete');})().catch(e=>{console.error(e.message);process.exitCode=1});
