const assert=require('node:assert/strict');
async function check(){
 const targets=await(await fetch('http://127.0.0.1:39223/json/list')).json();const t=targets.find(t=>t.type==='page'&&t.url==='app://-/index.html');if(!t)throw Error('No test main renderer');
 return new Promise((resolve,reject)=>{const ws=new WebSocket(t.webSocketDebuggerUrl);const timer=setTimeout(()=>{ws.close();reject(Error('Timed out'));},3000);
 ws.onopen=()=>ws.send(JSON.stringify({id:1,method:'Runtime.evaluate',params:{expression:'window.__codexUsageBadge?.status()',returnByValue:true}}));
 ws.onmessage=e=>{const m=JSON.parse(e.data);if(m.id!==1)return;clearTimeout(timer);ws.close();resolve(m.result?.result?.value);};ws.onerror=()=>{clearTimeout(timer);reject(Error('CDP failed'));};});
}
(async()=>{for(let i=0;i<60;i++){try{const v=await check();if(v?.placed&&v.rings?.every(r=>Number.isFinite(r.percent))){assert.equal(v.badgeCount,1);console.log(JSON.stringify({placed:true,badgeCount:v.badgeCount,mode:v.mode,validRemaining:true}));return;}}catch{}await new Promise(r=>setTimeout(r,1000));}throw Error('Card failed to display valid quota');})().catch(e=>{console.error(e.message);process.exitCode=1});
