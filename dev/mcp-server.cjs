'use strict';
// Game-scoped MCP stdio server. No npm packages, HTTP listener or arbitrary command tool.
const fs = require('node:fs');
const path = require('node:path');
const {spawnSync} = require('node:child_process');
const readline = require('node:readline');
const root = path.resolve(__dirname, '..');
const reports = path.join(__dirname, 'reports');
const compiler = path.join(process.env.WINDIR || 'C:\\Windows', 'Microsoft.NET', 'Framework64', 'v4.0.30319', 'csc.exe');
const scenarios = ['menu','play','level','shop','boss','pause','help','build','over','stress','tactics','archive','schools','upgrades','transition','encounter','codex','elite','boss_patterns','runtime'];
fs.mkdirSync(reports, {recursive:true});
function run(exe,args,cwd=root,timeout=60000) {
  const r=spawnSync(exe,args,{cwd,timeout,windowsHide:true,encoding:'utf8',maxBuffer:4*1024*1024});
  if(r.error)throw r.error;
  if(r.status!==0)throw Error(`${path.basename(exe)} exited ${r.status}\n${r.stdout}\n${r.stderr}`);
  return r.stdout.trim();
}
function build() {
  const files=['Native.cs','Game.cs','Tactics.cs','World.cs','Program.cs','WorldUi.cs'].map(f=>path.join(root,'src',f));
  const glyphs=path.join(reports,'glyphs.txt');
  fs.writeFileSync(glyphs,files.map(f=>fs.readFileSync(f,'utf8')).join('\n'),'utf8');
  const output=run(compiler,['/nologo','/target:winexe','/platform:x64','/optimize+',`/out:${path.join(root,'NeonEmber.exe')}`,`/win32manifest:${path.join(root,'src','app.manifest')}`,'/r:System.Windows.Forms.dll','/r:System.Drawing.dll',`/resource:${glyphs},glyphs.txt`,...files]);
  return {executable:path.join(root,'NeonEmber.exe'),bytes:fs.statSync(path.join(root,'NeonEmber.exe')).size,compilerOutput:output};
}
function test() {
  const out=path.join(reports,'GameTests.exe'),report=path.join(reports,'simulation.md');
  run(compiler,['/nologo','/target:exe','/platform:x64','/optimize+',`/out:${out}`,...['Game.cs','Tactics.cs','World.cs'].map(f=>path.join(root,'src',f)),path.join(__dirname,'GameTests.cs')]);
  const stdout=run(out,[report],root);
  return {report,summary:stdout,content:fs.readFileSync(report,'utf8')};
}
function tacticsTest() {
  const out=path.join(reports,'TacticsTests.exe'),report=path.join(reports,'tactics.md');
  run(compiler,['/nologo','/target:exe','/platform:x64','/optimize+',`/out:${out}`,...['Game.cs','Tactics.cs','World.cs'].map(f=>path.join(root,'src',f)),path.join(__dirname,'TacticsTests.cs')]);
  const summary=run(out,[report],root);
  return {report,summary};
}
function scenario(name) {
  if(!scenarios.includes(name))throw Error('Unknown scene. Use an advertised enum value.');
  run(path.join(root,'NeonEmber.exe'),['--scenario',name],reports,45000);
  return {state:JSON.parse(fs.readFileSync(path.join(reports,name+'.json'),'utf8').replace(/^\uFEFF/,'')),screenshot:path.join(reports,name+'.png')};
}
function adaptiveTest() {
  const out=path.join(reports,'AdaptiveTests.exe'),report=path.join(reports,'adaptive.md');
  run(compiler,['/nologo','/target:exe','/platform:x64','/optimize+','/define:ADAPTIVE_V04',`/out:${out}`,...['Game.cs','Tactics.cs','World.cs'].map(f=>path.join(root,'src',f)),path.join(__dirname,'AdaptiveTests.cs')]);
  return {report,summary:run(out,[report],root)};
}
const definitions=[
 {name:'game_build',description:'Compile this game into its Windows x64 client. Writes only the game build and development artifacts.',inputSchema:{type:'object',properties:{},additionalProperties:false},annotations:{readOnlyHint:false,destructiveHint:false,idempotentHint:true,openWorldHint:false}},
 {name:'game_test',description:'Compile and run deterministic simulation regression tests, damage tests, and 15-minute seeded bot runs. Does not launch or manipulate user UI.',inputSchema:{type:'object',properties:{},additionalProperties:false},annotations:{readOnlyHint:false,destructiveHint:false,idempotentHint:true,openWorldHint:false}},
 {name:'game_tactics_test',description:'Verify predictive aim, cooperating squads, disruption, school mechanics and story unlocks through the game simulation.',inputSchema:{type:'object',properties:{},additionalProperties:false},annotations:{readOnlyHint:false,destructiveHint:false,idempotentHint:true,openWorldHint:false}},
 {name:'game_adaptive_test',description:'Verify observed-path prediction, staged cooperation, elite counterplay, boss shapes, and seeded fixed-build route diagnostics.',inputSchema:{type:'object',properties:{},additionalProperties:false},annotations:{readOnlyHint:false,destructiveHint:false,idempotentHint:true,openWorldHint:false}},
 {name:'game_scene',description:'Render an isolated deterministic scene in the real native renderer; export its framebuffer PNG and JSON state. Stress reports render submission/present wall time, not a hardware-wide FPS guarantee. Does not change saved records.',inputSchema:{type:'object',properties:{scene:{type:'string',enum:scenarios}},required:['scene'],additionalProperties:false},annotations:{readOnlyHint:false,destructiveHint:false,idempotentHint:true,openWorldHint:false}},
 {name:'game_status',description:'Inspect this game build and latest development reports. Does not inspect other applications.',inputSchema:{type:'object',properties:{},additionalProperties:false},annotations:{readOnlyHint:true,destructiveHint:false,idempotentHint:true,openWorldHint:false}}
];
function result(value){return {content:[{type:'text',text:JSON.stringify(value,null,2)}]};}
function call(name,args={}){
 if(name==='game_build')return result(build());
 if(name==='game_test')return result(test());
 if(name==='game_tactics_test')return result(tacticsTest());
 if(name==='game_adaptive_test')return result(adaptiveTest());
 if(name==='game_scene')return result(scenario(args.scene));
 if(name==='game_status')return result({root,buildExists:fs.existsSync(path.join(root,'NeonEmber.exe')),reports:fs.readdirSync(reports).filter(f=>/\.(md|png|json)$/.test(f))});
 throw Error('Unknown tool');
}
function dispatch(req){
 const {method,id}=req;
 if(method==='notifications/initialized'||method==='notifications/cancelled')return;
 let value;
 if(method==='initialize')value={protocolVersion:'2024-11-05',capabilities:{tools:{listChanged:false}},serverInfo:{name:'neon-ember-dev',version:'0.4.0'}};
 else if(method==='ping')value={};
 else if(method==='tools/list')value={tools:definitions};
 else if(method==='tools/call'){try{value=call(req.params.name,req.params.arguments);}catch(e){value={isError:true,content:[{type:'text',text:e.message}]};}}
 else {if(id!==undefined)process.stdout.write(JSON.stringify({jsonrpc:'2.0',id,error:{code:-32601,message:'Method not found'}})+'\n');return;}
 if(id!==undefined)process.stdout.write(JSON.stringify({jsonrpc:'2.0',id,result:value})+'\n');
}
// MCP transport: one JSON-RPC message per line. stdout is reserved for protocol traffic.
readline.createInterface({input:process.stdin,crlfDelay:Infinity}).on('line',line=>{try{dispatch(JSON.parse(line));}catch(e){process.stdout.write(JSON.stringify({jsonrpc:'2.0',id:null,error:{code:-32700,message:'Invalid JSON-RPC message'}})+'\n');}});
