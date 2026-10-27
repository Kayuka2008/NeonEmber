using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace NeonEmber {
public static partial class Program {
 static readonly Col BG=new Col(8,13,22,255),Panel=new Col(15,24,36,255),Line=new Col(41,61,76,255),White=new Col(231,245,246,255),Muted=new Col(126,149,163,255),Cyan=new Col(94,235,220,255),Gold=new Col(255,198,103,255),Pink=new Col(255,103,127,255),Violet=new Col(180,151,255,255);
 static readonly string[] WeaponNames={"裂隙针","链式信标","轨道棱"};
 static readonly string[] WeaponSub={"焚档 / 灼烧 · 击破","回声 / 诱饵 · 断链","铸卫 / 护盾 · 突围"};
 static readonly string[] WeaponDesc={"携带裂隙针与烙印技术。\n精准打击，趁敌人蓄力破阵。","携带链式信标与诱饵技术。\n冲刺留下回声，欺骗协同网络。","携带轨道棱与储能护盾。\n贴身游走，寻找封锁线的裂口。"};
 static Font font; static Target canvas; static Game game; static bool quit,paused,stats,reduced,help,hasAudio,smoke; static int weapon,difficulty,volume=2;
 static float realTime,accumulator,shake,flash,mx,my; static bool click; static string prevMode="menu"; static string persistWarning=""; static int frames; static float smokeCost; static float bestTime,hardBest; static int bestKills;
 static string scenario="";static Stopwatch renderWatch;static List<double> renderTimes=new List<double>();static bool buildView,dashQueued;
 static float canvasScale=1;static bool automatedRun;
 static string savePath; static float stepCost; static readonly Random visualRandom=new Random(918);
 static Dictionary<string,byte[]> textCache=new Dictionary<string,byte[]>();
 static Dictionary<string,Sound> sounds=new Dictionary<string,Sound>(); static Dictionary<string,float> soundAt=new Dictionary<string,float>();

 [STAThread] public static int Main(string[] args){
  try { Run(args); return 0; }
  catch(Exception ex){try{File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"crash.log"),ex.ToString());}catch{} if(!automatedRun)MessageBox.Show("游戏启动失败。请将整个文件夹解压后再运行。\n\n"+ex.Message,"霓虹余烬",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
 }
 static void Run(string[] args){
  smoke=Array.IndexOf(args,"--smoke")>=0; bool capture=Array.IndexOf(args,"--capture")>=0;int scenarioAt=Array.IndexOf(args,"--scenario");if(scenarioAt>=0&&args.Length>scenarioAt+1)scenario=args[scenarioAt+1];bool automated=smoke||capture||scenario!="";
  automatedRun=automated;
  savePath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"NeonEmber","profile.txt"); LoadProfile();
  var desktop=Screen.PrimaryScreen.WorkingArea;float initialScale=Math.Min(1.25f,Math.Min((desktop.Width-60)/1280f,(desktop.Height-70)/720f));initialScale=Math.Max(.75f,initialScale);
  R.SetTraceLogLevel(4); R.SetConfigFlags((uint)(4|32|8192|(automated?128:0))); R.InitWindow((int)(1280*initialScale),(int)(720*initialScale),R.U("霓虹余烬 / NEON EMBER — 猎网协议 v0.4")); R.SetWindowMinSize(960,540); R.SetExitKey(0); R.SetTargetFPS(automated?0:60);
  LoadFont(); EnsureCanvas(); InitSounds(); game=new Game(Environment.TickCount);
  if(capture)scenario="play";if(scenario!="")PrepareScenario();
  Stopwatch sw=new Stopwatch();
  while(!quit&&!R.WindowShouldClose()){
   float dt=Math.Min(.1f,Math.Max(0,R.GetFrameTime()));realTime+=dt;frames++;
   if(!automated)Input(); if(!automated&&!R.IsWindowFocused()&&game.Mode=="play")paused=true;
   if(smoke||scenario=="runtime") SmokeStep();
   else if(scenario==""&&!paused&&!help&&!buildView&&!journal&&!codex&&game.Mode=="play"){
    accumulator=Math.Min(accumulator+dt,.1f); dashQueued|=R.IsKeyPressed(32); float ix=(Down(68,262)?1:0)-(Down(65,263)?1:0),iy=(Down(83,264)?1:0)-(Down(87,265)?1:0);
    sw.Restart();while(accumulator>=1f/60){game.Step(1f/60,ix,iy,dashQueued);dashQueued=false;accumulator-=1f/60;if(game.Mode!="play"){accumulator=0;break;}}sw.Stop();stepCost=stepCost*.96f+(float)sw.Elapsed.TotalMilliseconds*.04f;
   }else{accumulator=0;dashQueued=false;}
   if(scenario=="transition"&&frames==1){mx=500;my=500;click=true;}
   ProcessEvents(); if(game.Mode=="over"&&prevMode!="over")Record();prevMode=game.Mode;
   shake=Math.Max(0,shake-dt*25);flash=Math.Max(0,flash-dt*2);
   EnsureCanvas();renderWatch=Stopwatch.StartNew();R.BeginTextureMode(canvas);R.ClearBackground(BG);R.BeginMode2D(new Camera2D{Zoom=canvasScale});DrawArena();if(game.Mode=="menu")DrawMenu();else{DrawTacticalGround();DrawWorld();DrawTacticalDetails();DrawHud();DrawTacticalHud();if(game.Mode=="level")DrawLevel();if(game.Mode=="shop")DrawShop();if(game.Mode=="over")DrawOver();if(paused&&game.Mode=="play")DrawPause();}if(buildView)DrawBuild();if(help)DrawHelp();if(journal)DrawJournal();if(codex)DrawCodex();R.EndMode2D();R.EndTextureMode();
   R.BeginDrawing();R.ClearBackground(new Col(3,6,10,255));float scale=Math.Min(R.GetScreenWidth()/1280f,R.GetScreenHeight()/720f);float ox=(R.GetScreenWidth()-1280*scale)/2,oy=(R.GetScreenHeight()-720*scale)/2;
   float sx=reduced?0:(float)(visualRandom.NextDouble()-.5)*shake*scale,sy=reduced?0:(float)(visualRandom.NextDouble()-.5)*shake*scale;
   R.DrawTexturePro(canvas.Texture,new Rect(0,0,canvas.Texture.W,-canvas.Texture.H),new Rect(ox+sx,oy+sy,1280*scale,720*scale),new V(0,0),0,White);R.EndDrawing();
   renderWatch.Stop();if(scenario=="stress"&&frames>30)renderTimes.Add(renderWatch.Elapsed.TotalMilliseconds);
   if(scenario=="transition"&&frames==1){if(game.Mode!="shop"||game.Coins!=200||game.TotalUpgrades!=1||game.ShopOffers.Count!=3)throw new InvalidOperationException("Upgrade click leaked into the next modal.");click=false;}
   if(scenario!=""&&frames==(scenario=="runtime"?2400:scenario=="stress"?240:4)){SaveCanvas(scenario+".png");SaveScenario();quit=true;}
   if(smoke&&frames==2)SaveCanvas("smoke-menu.png");
   if(smoke&&frames==240)SaveCanvas("smoke-play.png");
   if(smoke&&frames==500){File.WriteAllText("smoke-result.txt","Native GPU window/render/audio initialized.\nFrames: "+frames+"\nAverage Step (ms): "+(smokeCost/Math.Max(1,frames-3)).ToString("F3",CultureInfo.InvariantCulture)+"\nFont glyphs: "+font.GlyphCount+"\nAudio: "+hasAudio+"\nGame mode: "+game.Mode+"\n");quit=true;}
  }
  if(!automated){if(game.Mode!="menu")Record();SaveProfile();}foreach(Sound s in sounds.Values)R.UnloadSound(s);if(hasAudio)R.CloseAudioDevice();R.UnloadRenderTexture(canvas);R.UnloadFont(font);R.CloseWindow();
 }
 static void EnsureCanvas(){
  float desired=automatedRun?1.5f:Math.Max(1,Math.Min(2,Math.Min(R.GetRenderWidth()/1280f,R.GetRenderHeight()/720f)));
  int height=(int)Math.Round(720*desired/9)*9,width=height/9*16;if(canvas.Texture.W==width&&canvas.Texture.H==height)return;
  if(canvas.Id!=0)R.UnloadRenderTexture(canvas);canvas=R.LoadRenderTexture(width,height);if(canvas.Id==0)throw new InvalidOperationException("无法创建高清渲染画布。");canvasScale=height/720f;R.SetTextureFilter(canvas.Texture,1);
 }
 static bool Down(int a,int b){return R.IsKeyDown(a)||R.IsKeyDown(b);}
 static void Input(){
  float scale=Math.Min(R.GetScreenWidth()/1280f,R.GetScreenHeight()/720f);V p=R.GetMousePosition();mx=(p.X-(R.GetScreenWidth()-1280*scale)/2)/scale;my=(p.Y-(R.GetScreenHeight()-720*scale)/2)/scale;click=R.IsMouseButtonPressed(0);
  if(R.IsKeyPressed(300))R.ToggleBorderlessWindowed(); // F11
  if(R.IsKeyPressed(292))stats=!stats;if(R.IsKeyPressed(77)){volume=(volume+1)%3;UpdateVolume();SaveProfile();}
  if(R.IsKeyPressed(256)){if(codex)codex=false;else if(journal)journal=false;else if(help)help=false;else if(buildView)buildView=false;else if(game.Mode=="play")paused=!paused;}
  if(R.IsKeyPressed(72)){help=!help;journal=false;buildView=false;codex=false;}
  if(R.IsKeyPressed(66)&&game.Mode!="menu"){buildView=!buildView;help=false;journal=false;codex=false;}
  if(R.IsKeyPressed(74)){journal=!journal;help=false;buildView=false;codex=false;journalPage=Math.Min(journalPage,UnlockedChapter());}
  if(R.IsKeyPressed(75)){codex=!codex;help=false;buildView=false;journal=false;click=false;}
  if(codex)CodexInput();
  if(!help&&!buildView&&!journal&&!codex&&!paused&&game.Mode=="level"){bool selected=false;for(int i=0;i<3;i++)if(R.IsKeyPressed(49+i)){game.Choose(i);selected=true;click=false;break;}if(!selected&&R.IsKeyPressed(82)){game.Reroll();click=false;}}
  if(!help&&!journal&&!codex&&game.Mode=="menu"&&R.IsKeyPressed(257))Start();
  if(game.Mode=="play"&&!paused&&!help&&!buildView&&!journal&&!codex)TargetInput();
 }
 static void Start(){game.Start(weapon,difficulty);paused=false;help=false;buildView=false;journal=false;codex=false;accumulator=0;}
 static void SmokeStep(){if(frames==3)game.Start(0,0);if(frames>3){if(game.Mode=="level")game.Choose(0);if(game.Mode=="shop")game.LeaveShop();game.Hp=game.MaxHp;var sw=Stopwatch.StartNew();for(int i=0;i<6;i++)game.Step(1f/60,(float)Math.Cos(game.Time*.18),(float)Math.Sin(game.Time*.18),frames%130==0);sw.Stop();smokeCost+=(float)sw.Elapsed.TotalMilliseconds;}}
 static void SaveCanvas(string path){Img img=R.LoadImageFromTexture(canvas.Texture);R.ImageFlipVertical(ref img);if(!R.ExportImage(img,R.U(path)))throw new IOException("无法保存游戏场景截图");R.UnloadImage(img);}
 static void PrepareScenario(){
  game=new Game(143);if(scenario=="menu")return;game.Start(0,0);
  if(scenario=="runtime")return;
  if(PrepareWorldScenario())return;
  if(scenario=="level"){game.Ranks["bolt"]=4;game.Xp=game.NextXp;game.Step(1f/60,0,0,false);return;}
  if(scenario=="shop"){game.Time=89.99f;game.Step(1f/60,0,0,false);game.Coins=145;return;}
  if(scenario=="over"){game.Time=387;game.Level=24;game.Kills=685;game.Hp=0;game.Step(1f/60,0,0,false);return;}
  if(scenario=="pause"){paused=true;return;}if(scenario=="help"){help=true;return;}if(scenario=="build"){buildView=true;game.Ranks["bolt"]=5;game.Ranks["arc"]=3;game.Ranks["power"]=2;game.Ranks["guard"]=1;return;}
  // Deterministic fixtures exercise the actual renderer without driving OS input or touching saves.
  game.Time=scenario=="boss"?179.99f:117.4f;game.Step(1f/60,0,0,false);if(game.Mode=="shop")game.LeaveShop();game.Coins=138;game.Kills=218;game.Level=16;game.NextXp=196.5f;game.Hp=78;game.Ranks["bolt"]=3;game.Ranks["arc"]=2;game.Ranks["blade"]=3;stats=scenario=="stress";game.AnnouncementTime=0;
  Random fixture=new Random(99);string[] kinds={"chaser","runner","shooter","bomber","tank"};int count=scenario=="stress"?359:38;
  for(int i=0;i<count;i++){string k=kinds[i%5];float x=48+(float)fixture.NextDouble()*1180,y=135+(float)fixture.NextDouble()*472;if((x-640)*(x-640)+(y-380)*(y-380)<95*95)x+=190;Enemy e=new Enemy{Id=100+i,Kind=k,X=x,Y=y,R=k=="tank"?22:13,Hp=52,MaxHp=70,Speed=50,Damage=12};if(i%9==0){e.AttackPhase=1;e.Windup=.7f;e.TargetX=game.X+55;e.TargetY=game.Y+60;}game.Enemies.Add(e);}
  for(int i=0;i<(scenario=="stress"?900:34);i++){float a=(float)fixture.NextDouble()*6.28f;game.Shots.Add(new Shot{X=40+(float)fixture.NextDouble()*1200,Y=130+(float)fixture.NextDouble()*480,R=i%3==0?6:4,Hostile=i%3==0,VX=(float)Math.Cos(a)*330,VY=(float)Math.Sin(a)*330,Life=3,Damage=12});}
  for(int i=0;i<(scenario=="stress"?280:50);i++)game.Orbs.Add(new Orb{X=40+(float)fixture.NextDouble()*1200,Y=130+(float)fixture.NextDouble()*480,Value=8});
  for(int i=0;i<(scenario=="stress"?220:8);i++)game.Effects.Add(new Effect{X=40+(float)fixture.NextDouble()*1200,Y=130+(float)fixture.NextDouble()*480,X2=640,Y2=380,R=24,Life=.4f,MaxLife=.5f,Kind=i%3==0?"arc":"death"});
  game.Events.Clear();
 }
 static void SaveScenario(){
  renderTimes.Sort();double p95=renderTimes.Count>0?renderTimes[(int)((renderTimes.Count-1)*.95)]:0,avg=0;foreach(double t in renderTimes)avg+=t;avg/=Math.Max(1,renderTimes.Count);
  int elites=0,active=0;foreach(Enemy e in game.Enemies){if(e.Elite)elites++;if(e.AttackPhase==1||e.AttackPhase==2)active++;}
  var j=new StringBuilder("{\"version\":\"0.4\"");
  j.Append(",\"scenario\":").Append(JsonText(scenario)).Append(",\"mode\":").Append(JsonText(game.Mode));
  j.Append(",\"time\":").Append(game.Time.ToString(CultureInfo.InvariantCulture)).Append(",\"level\":").Append(game.Level).Append(",\"hp\":").Append(game.Hp.ToString(CultureInfo.InvariantCulture));
  j.Append(",\"coins\":").Append(game.Coins).Append(",\"upgrades\":").Append(game.TotalUpgrades).Append(",\"enemies\":").Append(game.Enemies.Count).Append(",\"eliteEnemies\":").Append(elites).Append(",\"activeSpecials\":").Append(active);
  j.Append(",\"frames\":").Append(frames).Append(",\"bossesDefeated\":").Append(game.CompletedBosses).Append(",\"runtimeStepBatchMeanMs\":").Append((smokeCost/Math.Max(1,frames-3)).ToString("F3",CultureInfo.InvariantCulture));
  j.Append(",\"shots\":").Append(game.Shots.Count).Append(",\"orbs\":").Append(game.Orbs.Count).Append(",\"effects\":").Append(game.Effects.Count).Append(",\"hazards\":").Append(game.Hazards.Count);
  j.Append(",\"squadPhase\":").Append(JsonText(game.SquadPhase)).Append(",\"circleConfidence\":").Append(game.CircleConfidence.ToString(CultureInfo.InvariantCulture));
  j.Append(",\"canvasWidth\":").Append(canvas.Texture.W).Append(",\"canvasHeight\":").Append(canvas.Texture.H).Append(",\"fontGlyphs\":").Append(font.GlyphCount);
  j.Append(",\"renderMeanMs\":").Append(avg.ToString("F3",CultureInfo.InvariantCulture)).Append(",\"renderP95Ms\":").Append(p95.ToString("F3",CultureInfo.InvariantCulture)).Append(",\"renderSamples\":").Append(renderTimes.Count).Append(",\"audioInitialized\":").Append(hasAudio?"true":"false").Append("}");
  File.WriteAllText(scenario+".json",j.ToString(),Encoding.UTF8);
 }
 static string JsonText(string s){return "\""+(s??"").Replace("\\","\\\\").Replace("\"","\\\"").Replace("\n","\\n").Replace("\r","")+"\"";}

 static void LoadFont(){
  HashSet<int> chars=new HashSet<int>();for(int i=32;i<127;i++)chars.Add(i);string all="霓虹余烬未删之城";
  using(Stream s=Assembly.GetExecutingAssembly().GetManifestResourceStream("glyphs.txt")){if(s!=null)using(StreamReader sr=new StreamReader(s,Encoding.UTF8))all+=sr.ReadToEnd();}
  foreach(char ch in all)if(!char.IsControl(ch)&&!char.IsSurrogate(ch))chars.Add(ch);int[] cps=new int[chars.Count];chars.CopyTo(cps);
  string fonts=Environment.GetFolderPath(Environment.SpecialFolder.Fonts);string path=Path.Combine(fonts,"msyh.ttc");if(!File.Exists(path))path=Path.Combine(fonts,"simhei.ttf");if(!File.Exists(path))path=Path.Combine(fonts,"arial.ttf");
  byte[] data=ExtractTtc(File.ReadAllBytes(path));font=R.LoadFontFromMemory(R.U(".ttf"),data,data.Length,72,cps,cps.Length);if(font.Texture.Id==0||font.GlyphCount<=224)throw new Exception("无法加载系统中文字体。");R.SetTextureFilter(font.Texture,1);
 }
 static int BE(byte[] b,int o){return (b[o]<<24)|(b[o+1]<<16)|(b[o+2]<<8)|b[o+3];}
 static void PutBE(byte[] b,int o,int v){b[o]=(byte)(v>>24);b[o+1]=(byte)(v>>16);b[o+2]=(byte)(v>>8);b[o+3]=(byte)v;}
 // Windows Chinese fonts are TTC collections. stb_truetype expects an SFNT at byte zero.
 // Rebuild the first face in memory, retaining table bytes and relocating their offsets.
 static byte[] ExtractTtc(byte[] b){if(b.Length<16||b[0]!='t'||b[1]!='t'||b[2]!='c'||b[3]!='f')return b;int off=BE(b,12),n=(b[off+4]<<8)|b[off+5],head=12+16*n,total=head;for(int i=0;i<n;i++)total+=(BE(b,off+12+i*16+12)+3)&~3;byte[] result=new byte[total];Buffer.BlockCopy(b,off,result,0,head);int dest=head;for(int i=0;i<n;i++){int rec=12+i*16,src=BE(b,off+rec+8),len=BE(b,off+rec+12);Buffer.BlockCopy(b,src,result,dest,len);PutBE(result,rec+8,dest);dest+=(len+3)&~3;}return result;}
 static byte[] Enc(string s){byte[] b;if(!textCache.TryGetValue(s,out b)){if(textCache.Count>1024)textCache.Clear();b=R.U(s);textCache[s]=b;}return b;}
 static void T(string s,float x,float y,float size,Col c){R.DrawTextEx(font,Enc(s),new V(x,y),size,1,c);}
 static float TW(string s,float size){return R.MeasureTextEx(font,Enc(s),size,1).X;}
 static void TC(string s,float x,float y,float size,Col c){T(s,x-TW(s,size)/2,y,size,c);}
 static void Wrap(string s,float x,float y,float max,float size,Col c){string line="";foreach(char ch in s){if(ch=='\n'){T(line,x,y,size,c);y+=size*1.55f;line="";}else{string n=line+ch;if(TW(n,size)>max&&line.Length>0&&"。，；：！？、）】".IndexOf(ch)<0){T(line,x,y,size,c);y+=size*1.55f;line=ch.ToString();}else line=n;}}if(line.Length>0)T(line,x,y,size,c);}
 static void Box(float x,float y,float w,float h,Col color){R.DrawRectangleRec(new Rect(x,y,w,h),color);}
 static void L(float x,float y,float x2,float y2,float w,Col c){R.DrawLineEx(new V(x,y),new V(x2,y2),w,c);}
 static void Card(float x,float y,float w,float h,Col border){R.DrawRectangleRounded(new Rect(x,y,w,h),.07f,6,Panel);R.DrawRectangleRoundedLinesEx(new Rect(x,y,w,h),.07f,6,1,border);}
 static bool Hit(float x,float y,float w,float h){return mx>=x&&mx<=x+w&&my>=y&&my<=y+h;}
 static bool ConsumeClick(bool enabled){if(!enabled||!click)return false;click=false;return true;}
 static bool Button(string text,float x,float y,float w,float h,bool primary,bool enabled){bool hover=Hit(x,y,w,h)&&enabled&&!help&&!buildView&&!journal&&!codex;Col c=primary?Cyan:Line;R.DrawRectangleRounded(new Rect(x,y,w,h),.15f,6,(hover?c.Alpha(.9f):c).Alpha(enabled?1:.3f));TC(text,x+w/2,y+(h-18)/2,18,primary?BG:White.Alpha(enabled?1:.4f));return ConsumeClick(hover);}
 static string Clock(float t){return ((int)t/60).ToString("00")+":"+((int)t%60).ToString("00");}
 static int Rank(string k){int n;return game.Ranks.TryGetValue(k,out n)?n:0;}

 static void DrawArena(){
  for(int x=32;x<1280;x+=48)L(x,102,x,641,1,new Col(20,31,42,255));for(int y=113;y<642;y+=48)L(24,y,1256,y,1,new Col(20,31,42,255));
  for(int i=0;i<56;i++){float x=(i*237+31)%1248+16,y=(i*113+89)%505+119;R.DrawCircleV(new V(x,y),i%4==0?1.6f:1,new Col(57,86,96,130));}
  L(24,103,1256,103,1,Line);L(24,641,1256,641,1,Line);L(24,103,24,641,1,Line);L(1256,103,1256,641,1,Line);
  for(int i=0;i<4;i++){float x=i%2==0?24:1256,y=i<2?103:641,dx=i%2==0?1:-1,dy=i<2?1:-1;L(x,y,x+24*dx,y,2,Cyan.Alpha(.45f));L(x,y,x,y+24*dy,2,Cyan.Alpha(.45f));}
 }
 static void Emblem(float x,float y,float radius,float spin,Col c){R.DrawPolyLinesEx(new V(x,y),6,radius,spin,2,c.Alpha(.4f));R.DrawPolyLinesEx(new V(x,y),3,radius*.7f,-spin,3,c);R.DrawCircleV(new V(x,y),radius*.16f,c);}
 static Col EnemyColor(string kind){return EnemyRoleColor(kind);}
 static void DrawWorld(){
  for(int i=0;i<game.Orbs.Count;i++){Orb o=game.Orbs[i];R.DrawPoly(new V(o.X,o.Y),4,o.R,0,Cyan.Alpha(.2f));R.DrawPoly(new V(o.X,o.Y),4,Math.Max(2.5f,o.R*.55f),0,Cyan);}
  for(int i=0;i<game.Effects.Count;i++){
   Effect e=game.Effects[i];float a=e.MaxLife>0?e.Life/e.MaxLife:1;Col c=e.Kind=="aoeWarn"||e.Kind=="aoeBlast"?Pink:e.Kind=="death"||e.Kind=="repair"?Gold:Cyan;
   if(e.Kind=="arc"){L(e.X,e.Y,(e.X+e.X2)/2+8,(e.Y+e.Y2)/2-10,3,c.Alpha(a));L((e.X+e.X2)/2+8,(e.Y+e.Y2)/2-10,e.X2,e.Y2,3,c.Alpha(a));}
   else if(e.Kind=="aoeWarn"){R.DrawCircleV(new V(e.X,e.Y),e.R,c.Alpha(.08f));R.DrawCircleLinesV(new V(e.X,e.Y),e.R,c.Alpha(.7f));}
   else if(e.Kind=="dash") {L(e.X,e.Y,e.X2,e.Y2,8,c.Alpha(a*.35f));}
   else{R.DrawCircleLinesV(new V(e.X,e.Y),Math.Max(1,e.R*(1.2f-a*.4f)),c.Alpha(a));if(e.Kind=="death"&&!reduced)for(int j=0;j<5;j++){float an=j*1.2566f;R.DrawCircleV(new V(e.X+(float)Math.Cos(an)*e.R*(1-a),e.Y+(float)Math.Sin(an)*e.R*(1-a)),2,c.Alpha(a));}}
  }
  foreach(Enemy e in game.Enemies){
   Col c=EnemyColor(e.Kind);DrawAttackWarning(e,c);
   DrawEnemyBody(e,c);
   if(e.Hp<e.MaxHp){Box(e.X-e.R,e.Y-e.R-8,e.R*2,3,Line);Box(e.X-e.R,e.Y-e.R-8,e.R*2*Math.Max(0,e.Hp/e.MaxHp),3,c);}
  }
  foreach(Shot s in game.Shots){Col c=s.Hostile?Pink:Cyan;L(s.X-s.VX*.014f,s.Y-s.VY*.014f,s.X,s.Y,s.Hostile?3:2,c.Alpha(.45f));R.DrawCircleV(new V(s.X,s.Y),s.R,c);if(s.Hostile)R.DrawCircleLinesV(new V(s.X,s.Y),s.R+3,c.Alpha(.35f));}
  int blade=Rank("blade");if(blade>0){int count=1+blade;for(int i=0;i<count;i++){float a=game.BladeAngle+i*(float)Math.PI*2/count;float rad=63+blade*6;V p=new V(game.X+(float)Math.Cos(a)*rad,game.Y+(float)Math.Sin(a)*rad);R.DrawPoly(p,3,9,game.Time*260+i*60,Cyan);}}
  float angle=(float)(Math.Atan2(game.AimY,game.AimX)*180/Math.PI);float alpha=game.Invulnerable>0&&((int)(realTime*15)%2==0)?.38f:1;
  R.DrawCircleV(new V(game.X,game.Y),26,Cyan.Alpha(.06f));R.DrawPoly(new V(game.X,game.Y),3,16,angle,White.Alpha(alpha));R.DrawPoly(new V(game.X,game.Y),3,10,angle,Cyan.Alpha(alpha));
  if(game.DashTime>0)R.DrawCircleLinesV(new V(game.X,game.Y),23,Cyan);if(Rank("guard")>0)R.DrawCircleLinesV(new V(game.X,game.Y),22,new Col(112,159,232,90));
  if(game.AnnouncementTime>0){float a=Math.Min(1,game.AnnouncementTime);Card(416,119,448,42,Line.Alpha(a));TC(game.Announcement,640,130,18,Gold.Alpha(a));}
  if(flash>0&&!reduced){Box(0,102,12,540,Pink.Alpha(flash));Box(1268,102,12,540,Pink.Alpha(flash));}
 }
 static void DrawHud(){
  Box(0,0,1280,102,BG);Box(0,643,1280,77,BG);T("N / E",27,20,20,Cyan);T("未删之城",27,53,14,Muted);
  T("装甲  "+Math.Ceiling(game.Hp)+" / "+game.MaxHp,161,18,17,White);Box(162,48,224,7,Line);Box(162,48,224*Math.Max(0,game.Hp/game.MaxHp),7,game.Hp<game.MaxHp*.3f?Pink:Cyan);T("Lv. "+game.Level,423,18,17,White);T("能量  "+(int)game.Xp+" / "+(int)game.NextXp,512,21,13,Muted);Box(424,49,246,6,Line);Box(424,49,246*Math.Min(1,game.Xp/game.NextXp),6,Violet);
  T(game.Phase,708,17,18,Gold);T("击破 "+game.Kills+"    合金 "+game.Coins,708,48,15,Muted);TC(Clock(game.Time),1170,15,32,White);TC("危险等级 "+game.Stage,1170,55,13,Pink);
  float cyc=game.Time%30/30;Box(24,86,1232,2,Line);Box(24,86,1232*cyc,2,game.Time%30>=17&&game.Time%30<25?Pink:Cyan);
  string[] keys={"bolt","arc","blade"};for(int i=0;i<3;i++){float x=28+i*152;int n=Rank(keys[i]);T(WeaponNames[i],x,655,15,n>0?White:Muted.Alpha(.4f));for(int j=0;j<5;j++)Box(x+j*20,681,14,4,j<n?(i==0?Cyan:i==1?Violet:Gold):Line);if(n>=5)T("进化",x+102,676,12,Gold);}
  T(game.DashCooldown<=0?"SPACE  冲刺就绪":"冲刺冷却  "+game.DashCooldown.ToString("F1")+"s",510,656,17,game.DashCooldown<=0?Cyan:Muted);T("B 构筑 · J 档案 · K 图鉴 · ESC 暂停",510,683,14,Muted);
  Enemy boss=null;foreach(Enemy e in game.Enemies)if(e.Kind=="boss"&&e.Hp>0){boss=e;break;}
  T(boss!=null?"监察者交战中 / 击破后补给":"补给 "+Math.Ceiling(90-game.Time%90)+"s   /   BOSS "+Math.Ceiling(180-game.Time%180)+"s",929,657,14,boss!=null?Gold:Muted);T("M 音效 "+(volume==0?"关闭":volume==1?"轻柔":"标准")+"  ·  F3 性能",967,684,12,Muted);
  if(boss!=null){Card(889,10,207,67,Line);T("监察者 / 阶段 "+boss.BossStage,901,18,15,Gold);Fit(Math.Ceiling(boss.Hp)+" / "+Math.Ceiling(boss.MaxHp),901,41,182,13,Muted);Box(901,64,182,5,Line);Box(901,64,182*Math.Max(0,boss.Hp/boss.MaxHp),5,Gold);}
  if(stats){Card(27,114,298,73,Line);T("FPS "+R.GetFPS()+"  /  逻辑 "+stepCost.ToString("F2")+" ms",41,125,15,Cyan);T("敌人 "+game.Enemies.Count+"  弹丸 "+game.Shots.Count+"  特效 "+game.Effects.Count,41,151,13,Muted);}
 }
 static void Shade(){Box(0,0,1280,720,new Col(3,8,14,226));}
 static void DrawPause(){Shade();Card(371,111,538,485,Line);T("暂停 / PAUSED",407,144,15,Cyan);T("稍作喘息",404,182,38,White);T("回到战场后，时间与敌人才会继续。",408,240,17,Muted);
  if(Button("继续战斗",408,291,462,50,true,true))paused=false;if(Button("音效："+(volume==0?"关闭":volume==1?"轻柔":"标准"),408,357,218,42,false,true)){volume=(volume+1)%3;UpdateVolume();SaveProfile();}if(Button(reduced?"屏幕震动：关闭":"屏幕震动：开启",643,357,227,42,false,true)){reduced=!reduced;SaveProfile();}
  if(Button("操作指南",408,418,462,42,false,true))help=true;if(Button("结束本局，返回主菜单",408,479,462,42,false,true)){Record();game.Mode="menu";paused=false;}
  TC("ESC 继续  ·  F11 全屏  ·  关闭窗口退出",640,553,14,Muted);
 }
 static void DrawOver(){Shade();T("SIGNAL LOST / 本轮结束",115,115,16,Pink);T("余烬仍在。再突破一次。",110,159,42,White);T("存活时间",115,264,17,Muted);T(Clock(game.Time),110,299,62,Cyan);T("击破",500,264,17,Muted);T(game.Kills.ToString(),496,307,44,White);T("构筑等级",867,264,17,Muted);T("Lv. "+game.Level,863,307,44,Gold);
  Card(114,419,1051,85,Line);T("本地最高   "+Clock(difficulty==0?bestTime:hardBest),139,436,22,White);T("持续走位收集能量；保留冲刺穿过红色预警；组合多种攻击方式。",139,472,15,Muted);
  if(Button("返回主菜单",115,558,267,52,false,true))game.Mode="menu";if(Button("再来一局  ↗",843,558,322,52,true,true))Start();
 }
 static void ProcessEvents(){if(game.StoryChapter>savedChapter&&scenario==""&&!smoke){savedChapter=game.StoryChapter;archiveUntil=realTime+7;SaveProfile();}foreach(string ev in game.Events){Play(ev);if(ev=="hurt"){shake=reduced?0:8;flash=.7f;}if(ev=="boss"||ev=="evolve")shake=reduced?0:5;}game.Events.Clear();}
 static void InitSounds(){try{R.InitAudioDevice();hasAudio=R.IsAudioDeviceReady();if(!hasAudio)return;sounds["shoot"]=Tone(700,140,.045f,.13f);sounds["hit"]=Tone(160,60,.05f,.13f);sounds["hurt"]=Tone(100,45,.18f,.32f);sounds["dash"]=Tone(140,900,.15f,.18f);sounds["pickup"]=Tone(950,1600,.065f,.11f);sounds["level"]=Tone(420,1050,.45f,.22f);sounds["evolve"]=Tone(280,1400,.65f,.24f);sounds["boss"]=Tone(160,55,.8f,.3f);sounds["buy"]=Tone(650,1050,.16f,.2f);sounds["death"]=Tone(330,40,.8f,.22f);UpdateVolume();}catch{hasAudio=false;}}
 static Sound Tone(float from,float to,float duration,float gain){int count=(int)(22050*duration);short[] samples=new short[count];double phase=0;for(int i=0;i<count;i++){double t=i/(double)count;phase+=2*Math.PI*(from+(to-from)*t)/22050;double env=Math.Min(1,t*25)*Math.Pow(1-t,1.7);samples[i]=(short)(Math.Sin(phase)*env*gain*short.MaxValue);}IntPtr data=Marshal.AllocHGlobal(count*2);Marshal.Copy(samples,0,data,count);Sound s=R.LoadSoundFromWave(new Wave{Frames=(uint)count,Rate=22050,Size=16,Channels=1,Data=data});Marshal.FreeHGlobal(data);return s;}
 static void UpdateVolume(){foreach(Sound s in sounds.Values)R.SetSoundVolume(s,volume==0?0:volume==1?.25f:.65f);}
 static void Play(string ev){if(!hasAudio||volume==0||smoke||scenario!="")return;Sound s;float at;if(sounds.TryGetValue(ev,out s)){float limit=ev=="pickup"?.09f:ev=="hit"?.07f:.02f;if(!soundAt.TryGetValue(ev,out at)||realTime-at>limit){R.PlaySound(s);soundAt[ev]=realTime;}}}
 static void LoadProfile(){try{if(!File.Exists(savePath))return;foreach(string l in File.ReadAllLines(savePath)){string[] s=l.Split('=');if(s.Length!=2)continue;float n;if(!float.TryParse(s[1],NumberStyles.Float,CultureInfo.InvariantCulture,out n)||float.IsNaN(n)||float.IsInfinity(n))continue;if(s[0]=="best")bestTime=Math.Max(0,n);if(s[0]=="hardBest")hardBest=Math.Max(0,n);if(s[0]=="kills")bestKills=(int)Math.Max(0,n);if(s[0]=="volume")volume=Math.Max(0,Math.Min(2,(int)n));if(s[0]=="reduced")reduced=n==1;if(s[0]=="chapter")savedChapter=Math.Max(0,Math.Min(3,(int)n));}}catch{persistWarning="暂时无法读取本地纪录，本局仍可正常游玩。";}}
 static void Record(){if(smoke||scenario!="")return;if(difficulty==0)bestTime=Math.Max(bestTime,game.Time);else hardBest=Math.Max(hardBest,game.Time);bestKills=Math.Max(bestKills,game.Kills);SaveProfile();}
 static void SaveProfile(){if(smoke||scenario!="")return;try{Directory.CreateDirectory(Path.GetDirectoryName(savePath));string temp=savePath+".tmp";File.WriteAllText(temp,"best="+bestTime.ToString(CultureInfo.InvariantCulture)+"\nhardBest="+hardBest.ToString(CultureInfo.InvariantCulture)+"\nkills="+bestKills+"\nvolume="+volume+"\nreduced="+(reduced?1:0)+"\nchapter="+savedChapter,Encoding.UTF8);if(File.Exists(savePath))File.Replace(temp,savePath,null);else File.Move(temp,savePath);}catch{persistWarning="无法写入本地纪录，本局仍可正常游玩。";}}
}
}





