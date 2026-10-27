'use strict';
// Small local MCP client used by the build/verification workflow.
const {spawn}=require('node:child_process');
const readline=require('node:readline');
const path=require('node:path');
const child=spawn(process.execPath,[path.join(__dirname,'mcp-server.cjs')],{stdio:['pipe','pipe','inherit'],windowsHide:true});
const tool=process.argv[2]||'game_status';
const args=process.argv[3]?JSON.parse(process.argv[3]):{};
const timer=setTimeout(()=>{child.kill();process.exitCode=1;console.error('MCP request timed out');},90000);
function send(o){child.stdin.write(JSON.stringify({jsonrpc:'2.0',...o})+'\n');}
readline.createInterface({input:child.stdout,crlfDelay:Infinity}).on('line',line=>{
 const msg=JSON.parse(line);
 if(msg.id===1){send({method:'notifications/initialized'});send({id:2,method:'tools/call',params:{name:tool,arguments:args}});}
 if(msg.id===2){clearTimeout(timer);console.log(JSON.stringify(msg.result,null,2));child.stdin.end();if(msg.result.isError)process.exitCode=1;}
});
child.on('error',e=>{clearTimeout(timer);console.error(e);process.exitCode=1;});
send({id:1,method:'initialize',params:{protocolVersion:'2024-11-05',capabilities:{},clientInfo:{name:'neon-ember-local-check',version:'0.4.0'}}});
