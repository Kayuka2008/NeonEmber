using System;
using System.Collections.Generic;
namespace NeonEmber {
public static partial class Program {
    static bool journal;
    static bool codex;
    static int codexPage;
    static int journalPage, savedChapter;
    static float archiveUntil;
    static readonly string[] SchoolIds = {"ember","echo","forge"};
    static readonly Dictionary<int,Enemy> enemyLookup = new Dictionary<int,Enemy>();
    static Col SchoolColor(string id) { return id=="ember"?Gold:id=="echo"?Violet:Cyan; }
    static int UnlockedChapter() { return Math.Min(Lore.StoryCount-1,Math.Max(savedChapter,game==null?0:game.StoryChapter)); }
    static bool PrepareWorldScenario() {
        if(scenario=="boss_patterns"){
            // Capture actual attack planning from three independent encounters side by side.
            // This is a rendering fixture, not a claim that one boss casts all three at once.
            game.Time=20;game.X=640;game.Y=560;game.Ranks.Clear();
            System.Reflection.MethodInfo begin=typeof(Game).GetMethod("TryBeginAttack",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            for(int i=0;i<3;i++){
                Game encounter=new Game(143+i);encounter.Start(0,0);encounter.Time=20;encounter.X=300+i*340;encounter.Y=410;
                Enemy boss=new Enemy{Id=710+i,Kind="boss",X=encounter.X-165,Y=310,R=34,Hp=1800,MaxHp=2200,Damage=19,AttackSerial=i};encounter.Enemies.Add(boss);
                if(!(bool)begin.Invoke(encounter,new object[]{boss}))throw new InvalidOperationException("Boss shape fixture did not enter real windup: "+i);
                game.Enemies.Add(boss);foreach(Hazard hazard in encounter.Hazards)game.Hazards.Add(hazard);
            }
            game.Announcement="形状检视 / 三个独立前摇快照";game.AnnouncementTime=30;game.Events.Clear();return true;
        }
        if(scenario=="codex"){codex=true;codexPage=7;return true;}
        if(scenario=="encounter"||scenario=="elite"){
            game.Time=52;game.X=600;game.Y=390;game.Hp=83;game.Coins=97;game.Level=17;
            game.Ranks["bolt"]=3;game.Ranks["arc"]=2;game.Ranks["echo_decoy"]=1;game.Ranks["forge_ward"]=2;game.Ward=18;
            string[] encounterKinds={"relay","tank","repairer","prism","shooter","shooter","runner","bomber"};
            float[] encounterXs={890,795,976,786,916,830,391,390};float[] encounterYs={340,340,448,492,524,231,265,494};
            for(int i=0;i<encounterKinds.Length;i++)game.Enemies.Add(new Enemy{Id=501+i,Kind=encounterKinds[i],X=encounterXs[i],Y=encounterYs[i],R=i==0?20:i==1?22:15,Hp=i==0?190:90,MaxHp=i==0?240:120,Speed=80,Damage=14,Cooldown=1.5f,Elite=i==2||i==3,FacingX=-1,FacingY=0,RelayId=i>0?501:0,SquadClock=i==0?1.4f:0});
            for(int i=0;i<24;i++)game.Step(1f/60,0,1,false);
            game.FocusTargetId=scenario=="elite"?504:501;game.AnnouncementTime=0;game.Events.Clear();
            Enemy repairer=game.Enemies.Find(delegate(Enemy e){return e.Id==503;});if(repairer!=null){repairer.Channel=.8f;repairer.MaxChannel=1.4f;repairer.RepairTargetId=501;}
            game.Hazards.Add(new Hazard{OwnerId=501,Shape="line",X=400,Y=250,X2=760,Y2=538,R=17,Warning=.8f,Life=1.8f,Damage=15});
            game.Hazards.Add(new Hazard{OwnerId=501,Shape="circle",X=610,Y=536,R=48,Warning=.9f,Life=2,Damage=13});
            return true;
        }
        if(scenario=="transition"){
            game.Time=89.99f;game.Coins=200;game.Orbs.Add(new Orb{X=game.X,Y=game.Y,Value=game.NextXp});game.Step(1f/60,0,0,false);
            if(game.Mode!="level")throw new InvalidOperationException("Transition fixture did not reach level selection.");return true;
        }
        if(scenario=="upgrades"){
            game.Ranks["ember_brand"]=3;game.Ranks["echo_jammer"]=3;game.Ranks["forge_breaker"]=3;game.Level=12;game.Mode="level";game.Choices.Clear();
            game.Choices.Add(game.WorldUpgradeInfo("ember_brand"));game.Choices.Add(game.WorldUpgradeInfo("echo_jammer"));game.Choices.Add(game.WorldUpgradeInfo("forge_breaker"));game.Ranks["ember_brand"]=2;game.Ranks["echo_jammer"]=2;game.Ranks["forge_breaker"]=2;return true;
        }
        if(scenario=="archive"){game.StoryChapter=2;journal=true;journalPage=2;return true;}
        if(scenario=="schools"){
            game.Ranks["ember_brand"]=2;game.Ranks["ember_breach"]=2;game.Ranks["ember_cascade"]=2;game.Ranks["echo_jammer"]=2;game.Ranks["forge_ward"]=2;game.Ward=game.MaxWard;buildView=true;return true;
        }
        if(scenario!="tactics")return false;
        game.Time=35;game.Ranks["bolt"]=2;game.Ranks["echo_jammer"]=1;game.Ranks["forge_ward"]=1;game.Ward=14;
        string[] kinds={"relay","tank","shooter","shooter","runner","runner","bomber"};float[] xs={990,905,955,970,430,480,520};float[] ys={330,340,242,460,200,536,407};
        for(int i=0;i<kinds.Length;i++){game.Enemies.Add(new Enemy{Id=301+i,Kind=kinds[i],X=xs[i],Y=ys[i],R=i==0?20:i==1?22:13,Hp=800,MaxHp=800,Speed=i==1?72:88,Damage=12,Cooldown=i==6?0:.55f});}
        for(int i=0;i<40;i++)game.Step(1f/60,1,0,false);
        game.FocusTargetId=301;game.AnnouncementTime=0;game.Hp=100;game.Coins=62;game.Kills=77;
        return true;
    }
    static void Fit(string s,float x,float y,float max,float size,Col color) { if(s==null)return;float w=TW(s,size);T(s,x,y,w>max?size*max/w:size,color); }
    static bool ModalButton(string s,float x,float y,float w,float h,bool enabled) {
        bool hover=enabled&&Hit(x,y,w,h);Card(x,y,w,h,hover?Cyan:Line);TC(s,x+w/2,y+(h-17)/2,17,enabled?White:Muted.Alpha(.4f));return ConsumeClick(hover);
    }
    static void TargetInput() {
        if(R.IsKeyPressed(69)){
            Enemy best=null;float score=float.MaxValue;
            foreach(Enemy e in game.Enemies)if(e.Kind=="relay"&&e.Hp>0){float d=(e.X-game.X)*(e.X-game.X)+(e.Y-game.Y)*(e.Y-game.Y);if(d<score){score=d;best=e;}}
            if(best==null){foreach(Enemy e in game.Enemies)if(e.Hp>0){float d=(e.X-game.X)*(e.X-game.X)+(e.Y-game.Y)*(e.Y-game.Y);if(d<score){score=d;best=e;}}}
            game.FocusTargetId=best==null?0:game.FocusTargetId==best.Id?0:best.Id;
        }
        if(click&&my>104&&my<640){Enemy chosen=null;float distance=900;foreach(Enemy e in game.Enemies){float d=(e.X-mx)*(e.X-mx)+(e.Y-my)*(e.Y-my);if(e.Hp>0&&d<(e.R+14)*(e.R+14)&&d<distance){distance=d;chosen=e;}}game.FocusTargetId=chosen==null?0:chosen.Id;}
    }
    static void DrawTacticalGround() {
        // Readouts sit under all units, projectiles and hazards, so even a locked target
        // moving into its information card remains visible and clickable.
        if(game.Mode=="play"){DrawSquadReadout();DrawTargetReadout();}
        enemyLookup.Clear();foreach(Enemy e in game.Enemies)enemyLookup[e.Id]=e;
        foreach(Hazard h in game.Hazards)DrawHazard(h);
        foreach(Enemy e in game.Enemies){Enemy other;
            if(e.RelayId!=0&&enemyLookup.TryGetValue(e.RelayId,out other)&&other.Hp>0){bool important=e.Id==game.FocusTargetId||other.Id==game.FocusTargetId;DashedLine(e.X,e.Y,other.X,other.Y,important?2:1,Violet.Alpha(important?.5f:.2f));}
            if(e.Shielded&&e.GuardId!=0&&enemyLookup.TryGetValue(e.GuardId,out other)&&other.Hp>0)L(e.X,e.Y,other.X,other.Y,2,Gold.Alpha(.45f));
            if(e.Channel>0&&e.RepairTargetId!=0&&enemyLookup.TryGetValue(e.RepairTargetId,out other)&&other.Hp>0){L(e.X,e.Y,other.X,other.Y,6,Gold.Alpha(.1f));DashedLine(e.X,e.Y,other.X,other.Y,3,Gold.Alpha(.8f));}
        }
    }
    static void DrawTacticalDetails() {
        foreach(Enemy e in game.Enemies){
            if(e.Kind=="relay"){R.DrawPolyLinesEx(new V(e.X,e.Y),6,e.R+8,0,1.5f,Violet);EnemyTag("指挥",e.X,e.Y-e.R-28,Violet);}
            if(e.BurnTime>0){R.DrawRing(new V(e.X,e.Y),e.R+2,e.R+4,realTime*120,realTime*120+170,16,Gold.Alpha(.85f));}
            if(e.Shielded)R.DrawRing(new V(e.X,e.Y),e.R+5,e.R+7,0,360,24,Gold.Alpha(.7f));
            if(e.Elite){R.DrawPolyLinesEx(new V(e.X,e.Y),6,e.R+12,30,2,Gold);EnemyTag("精英",e.X,e.Y-e.R-30,Gold);}
            if(e.Channel>0){float ratio=1-e.Channel/Math.Max(.01f,e.MaxChannel);Box(e.X-24,e.Y+e.R+9,48,5,Line);Box(e.X-24,e.Y+e.R+9,48*Math.Max(0,Math.Min(1,ratio)),5,Gold);if(e.Id!=game.FocusTargetId)EnemyTag("修复引导",e.X,e.Y-e.R-28,Gold);}
            if(e.Stun>0){R.DrawPolyLinesEx(new V(e.X,e.Y),4,e.R+11,45,1,Violet);TC("失联",e.X,e.Y+e.R+9,11,Violet);}
            if(e.Id==game.FocusTargetId){float r=e.R+15;L(e.X-r,e.Y-r,e.X-r+8,e.Y-r,2,White);L(e.X-r,e.Y-r,e.X-r,e.Y-r+8,2,White);L(e.X+r,e.Y+r,e.X+r-8,e.Y+r,2,White);L(e.X+r,e.Y+r,e.X+r,e.Y+r-8,2,White);EnemyTag("锁定",e.X,e.Y+e.R+25,White);}
        }
        if(game.DecoyTime>0){R.DrawPolyLinesEx(new V(game.DecoyX,game.DecoyY),3,17,0,2,Violet.Alpha(.65f));R.DrawCircleLinesV(new V(game.DecoyX,game.DecoyY),29,Violet.Alpha(.4f));T("回声",game.DecoyX+24,game.DecoyY-8,12,Violet);}
        if(game.MaxWard>0&&game.Ward>0)R.DrawRing(new V(game.X,game.Y),27,29,-90,-90+360*game.Ward/game.MaxWard,32,Cyan.Alpha(.8f));
    }
    static void DrawTacticalHud() {
        if(game.Mode!="play")return;
        string readout=string.IsNullOrEmpty(game.MotionReadout)?"网络正在观察你的移动":game.MotionReadout;
        Fit(readout,41,608,720,16,game.CircleConfidence>.5f?Gold:Muted);
        T("E 锁节点  ·  点击锁定  ·  K 怪物图鉴",868,610,14,Muted);
        if(game.MaxWard>0)T("护盾 "+Math.Ceiling(game.Ward)+" / "+game.MaxWard,163,68,12,Cyan);
        if(realTime<archiveUntil){Card(40,200,355,70,Violet);T("记忆已解封  /  J 阅读",55,214,15,Violet);Fit(Lore.StoryTitle(UnlockedChapter()),55,241,320,15,White);}
    }
    static void DrawMenu() {
        Box(0,0,1280,720,BG.Alpha(.96f));T("N / E",62,36,19,Cyan);T("NEON EMBER / ARCHIVE BREACH",136,39,14,Muted);T("WINDOWS / v0.4",1010,39,14,Muted);
        T("未删之城",66,99,19,Gold);T("霓虹余烬",60,133,61,White);T("当记忆成为罪证，你负责让它抵达明天。",65,219,21,White);
        T("你是携带原始记忆的信使。校正网络开始联手追捕，而三个地下组织递来了各自的技术。",66,260,16,Muted);
        Emblem(1081,173,71,realTime*8,Violet);R.DrawCircleLinesV(new V(1081,173),97,Line);
        T("01 / 选择起始武器与技术",65,306,15,Muted);
        for(int i=0;i<3;i++){
            float x=65+i*387;Col color=SchoolColor(SchoolIds[i]);bool selected=weapon==i,hover=Hit(x,340,365,218)&&!journal&&!help&&!codex;
            Card(x,340,365,218,selected?color:hover?Muted:Line);if(selected)Box(x+1,360,3,42,color);
            T(Lore.SchoolName(SchoolIds[i]),x+23,358,15,color);T(WeaponNames[i],x+23,388,29,White);T(WeaponSub[i],x+23,432,15,color);Wrap(WeaponDesc[i],x+23,473,319,16,Muted);
            if(hover&&click)weapon=i;
        }
        if(Button(difficulty==0?"标准协议  ›":"高压协议  ›",65,586,203,48,false,true))difficulty=1-difficulty;
        if(Button("J  记忆档案",286,586,202,48,false,true)){journal=true;journalPage=UnlockedChapter();}
        T("最高生存 "+Clock(difficulty==0?bestTime:hardBest),519,602,16,White);
        if(Button("携带记忆，出发  ↗",884,582,316,57,true,true))Start();
        T("WASD 移动 / 空格冲刺 / 自动攻击 / E 优先拆除节点",65,667,14,Muted);T("Enter 开始 · K 图鉴 · H 指南 · F11 全屏",792,667,14,Muted);
        if(persistWarning!="")T(persistWarning,65,286,12,Gold);
    }
    static string UpgradeSchool(Upgrade u){return string.IsNullOrEmpty(u.School)?"":u.School;}
    static void DrawLevel() {
        Shade();T("RECONSTRUCTION / "+game.Level.ToString("00"),116,107,15,Cyan);T("选择技术，改写追捕规则",112,138,35,White);
        T("每系累计 6 点启动核心协议；跨系可产生联动。所有卡片可混搭。",115,193,16,Muted);
        for(int i=0;i<game.Choices.Count;i++){
            Upgrade u=game.Choices[i];float x=114+i*358;bool hover=Hit(x,251,336,302)&&!help&&!journal&&!buildView&&!codex;string school=UpgradeSchool(u);Col c=u.Name.Contains("进化")?Gold:school!=""?SchoolColor(school):Cyan;
            Card(x,251,336,302,hover?c:Line);T("0"+(i+1)+" / "+u.Tag,x+22,272,14,c);Fit(u.Name,x+22,311,290,25,White);
            Wrap(u.Description,x+22,358,288,16,White.Alpha(.86f));
            if(!string.IsNullOrEmpty(u.Flavor))Wrap(u.Flavor,x+22,461,288,13,Muted);
            T("按 "+(i+1)+" 选择  ↗",x+22,520,14,c);if(ConsumeClick(hover)){game.Choose(i);break;}
        }
        Fit(game.SchoolProgressText(),115,581,715,16,Muted);if(Button("R 刷新 / 剩余 "+game.Rerolls,879,577,286,46,false,game.Rerolls>0))game.Reroll();
    }
    static void DrawShop() {
        Shade();T("DEAD DROP / 地下接驳站",115,106,15,Cyan);T("他们也在等待这份记忆",111,143,36,White);T("修补匠「弥」：补好装甲，带上能破局的技术。网络不会永远失明。",115,200,17,Muted);
        T("合金  "+game.Coins,1000,152,22,Gold);
        for(int i=0;i<game.ShopOffers.Count;i++){
            Upgrade u=game.ShopOffers[i];float x=114+i*358;string school=UpgradeSchool(u);Col c=school==""?Cyan:SchoolColor(school);Card(x,259,336,285,Line);T(u.Tag,x+22,281,14,c);Fit(u.Name,x+22,320,290,25,White);Wrap(u.Description,x+22,363,290,16,Muted);
            if(!string.IsNullOrEmpty(u.Flavor))Wrap(u.Flavor,x+22,435,290,12,c.Alpha(.6f));
            if(Button(u.Price==0?"免费领取":"购买 / "+u.Price+" 合金",x+22,485,292,40,false,game.Coins>=u.Price)){game.Buy(i);break;}
        }
        Fit(game.SchoolProgressText(),115,586,730,15,Muted);if(Button("继续传递  ↗",880,574,286,51,true,true))game.LeaveShop();
    }
    static void DrawBuild() {
        Shade();Card(122,37,1036,648,Line);T("BUILD / 技术谱系 · 战场暂停",154,63,15,Cyan);T("你选择了怎样的反抗？",151,95,32,White);
        string[] weaponKeys={"bolt","arc","blade"};
        string[] summaries={"点燃目标 · 惩罚破绽 · 传播火种","留下残像 · 干扰指挥 · 多武器共鸣","充能护盾 · 冲刺破阵 · 拦截弹幕"};
        string detail="悬停技术条目或觉醒名称可阅读效果。武器等级不计入体系点数。";
        for(int i=0;i<3;i++){
            float x=154+i*326;string school=SchoolIds[i];Col c=SchoolColor(school);Card(x,150,310,282,Line);T(Lore.SchoolName(school),x+18,165,21,c);Fit(summaries[i],x+18,199,275,14,Muted);
            int rank=Rank(weaponKeys[i]);T(WeaponNames[i]+" / "+(rank==0?"未装备":rank>=5?"已进化":"Lv."+rank),x+18,232,17,White);
            int row=0;foreach(string id in Lore.UpgradeIds){Upgrade u=game.WorldUpgradeInfo(id);if(u.School!=school)continue;bool hover=Hit(x+14,261+row*31,280,29);string name=u.Name.Substring(0,u.Name.LastIndexOf(' '));Fit(name,x+18,267+row*31,219,16,hover?White:Muted);T(Rank(id)+" / 3",x+244,267+row*31,14,Rank(id)>0?White:Muted.Alpha(.45f));if(hover)detail=(Rank(id)>0?"当前效果 / ":"1 级预览 / ")+u.Description;row++;}
            T("投入 "+game.SchoolPoints(school)+" / 6",x+18,373,15,c);Fit((game.HasSchoolCapstone(school)?"已觉醒 / ":"6 点觉醒 / ")+Lore.CapstoneName(school),x+18,400,276,14,c);
            if(Hit(x+14,368,280,53))detail=Lore.CapstoneDescription(school);
        }
        Wrap(game.SynergyText(),156,445,965,14,Gold);
        string[] ids={"power","haste","vitality","speed","magnet","scholar","crit","guard","dash"};string[] names={"增伤","攻速","装甲","移速","拾取","经验","暴击","减伤","冲刺"};
        for(int i=0;i<9;i++)T(names[i]+" "+Rank(ids[i]),155+i*107,518,14,Muted);
        T("伤害 ×"+game.PowerMultiplier.ToString("F2")+"   /   频率 ×"+game.FireRateMultiplier.ToString("F2")+"   /   护盾 "+Math.Ceiling(game.Ward)+" / "+game.MaxWard,155,547,17,Cyan);
        Wrap(detail,155,578,965,15,Muted);T("B / ESC 返回",155,642,14,Muted);
        if(ModalButton("返回战场",862,623,260,42,!help&&!journal&&!codex))buildView=false;
    }
    static void DrawJournal() {
        Shade();Card(91,42,1098,634,Line);T("ARCHIVE / 已解封记忆",126,68,15,Violet);T("一座城，四份互相矛盾的证词",122,103,33,White);
        int unlocked=UnlockedChapter();for(int i=0;i<Lore.StoryCount;i++){
            float y=174+i*81;bool open=i<=unlocked,selected=i==journalPage;Card(124,y,280,66,selected?Violet:Line);T("0"+i+" / "+(open?"可读取":"尚未解密"),139,y+10,12,open?Violet:Muted.Alpha(.5f));Fit(open?Lore.StoryTitle(i):"击破第 "+i+" 位监察者",139,y+34,250,16,open?White:Muted.Alpha(.5f));if(open&&click&&Hit(124,y,280,66))journalPage=i;
        }
        T("记录会跨局保留。",125,526,14,Muted);Wrap("每局前 3 次击破监察者，依次获得证词。读完后继续无尽模式，将未删记忆传给更远的街区。",125,555,270,14,Muted);
        L(429,174,429,618,1,Line);T(Lore.StoryTitle(journalPage),465,179,26,White);Wrap(Lore.StoryBody(journalPage),465,233,675,18,Muted);
        if(ModalButton("关闭档案 / J",895,609,255,43,true))journal=false;
    }
    static void DrawHelp() {
        Shade();Card(142,42,996,636,Line);T("FIELD MANUAL / 反校正战术",177,67,15,Cyan);T("观察协作，再拆掉它",173,101,33,White);
        string[] labels={"WASD / 方向键","SPACE","E / 鼠标点击","1 / 2 / 3 · R","B / J / K / ESC"};string[] desc={"移动；武器自动攻击，无需持续点击。","冲刺突围；回声系可留下诱饵，铸卫系可冲击封锁。","E 优先锁定指挥节点；点怪指定目标，点空地解除。","选择升级 / 重掷；初始 3 次，击破监察者可补充。","构筑 / 剧情 / 怪物图鉴 / 暂停，阅读时战场暂停。"};
        for(int i=0;i<labels.Length;i++){T(labels[i],179,168+i*38,17,i==2?Gold:White);T(desc[i],416,168+i*38,16,Muted);}
        L(178,371,1102,371,1,Line);Wrap("紫色虚线：节点正在协调随从。准备 → 压制 → 截击 → 恢复，抓住空当。\n金色护甲：盾卫掩护或棱镜正面装甲；找侧面。金色光束：维护者修复引导。\n橙色虚线是蓄力预告，红色实线是生效危险；圆环中心留有安全区。\n三系各 3 项技术，每项 3 级；同系 6 点觉醒，两系各 2 点产生联动。",179,393,918,17,Muted);
        T("预警锁定后改变方向，先拆节点或守卫，再穿过恢复期形成的缺口。",179,532,17,Gold);T("有限预判 / 可读意图 / 协作有依赖 / 破局有收益",179,569,14,Cyan);
        if(ModalButton("明白了 / H",845,610,257,43,!journal&&!codex))help=false;
    }
    static readonly string[] CodexKinds={"chaser","runner","shooter","bomber","tank","relay","repairer","prism","boss"};
    static readonly string[] CodexRoles={"追踪 / 驱赶","截击 / 冲锋","压制 / 交叉火力","封路 / 预判落点","掩护 / 保护后排","指挥 / 调度编队","支援 / 引导修复","装甲 / 侧背弱点","监察 / 多形态攻势"};
    static readonly string[] CodexBehavior={
        "持续追踪并挤压空间。它们把你赶进其他单位的火线，数量增加后会压缩回旋余地。",
        "先移动到侧翼，再根据你最近的轨迹选择截击方向。蓄力开始后，冲锋方向不再跟随你变化。",
        "拉开射距并分散站位。编队压制阶段从不同方向开火，逼你提前交出冲刺。",
        "沿你可能继续走的路线布置封锁。反复绕圈会使落点更准确，区域生效后会短暂留在场上。",
        "走到你与节点、射手之间。只有站在合适的掩护位置，后排才会获得减伤。",
        "组织四段协作：先准备站位，再压制逼走位，接着让冲锋单位截击，最后进入恢复。",
        "为附近受伤同伴引导修复。金色连线指向修复对象，下方进度条代表本次引导；它不会复活死者。",
        "护甲正面朝向你，蓄力后沿朝向释放长线突刺。金色护甲弧显示防御方向；转向较慢，侧背是突破口。",
        "交替使用圆形封锁、长线切割和环形震荡；生命低于一半后改变攻击组合。蓄力时会显示真实伤害范围。"
    };
    static readonly string[] CodexTell={
        "菱形轮廓；密集追踪群正在压缩通路。",
        "三角尖端与虚线表示已锁定的冲锋方向。",
        "准星轮廓；射击虚线出现后，预判落点已锁定。",
        "八边形外壳；橙色警戒圆先出现，再变为红色危险区。",
        "厚重方盾；金色连线连向当前被保护的同伴。",
        "紫色六边形与指挥标签；紫色虚线连接受其调度的随从。",
        "十字形核心；金色修复光束与引导进度条。",
        "双菱形机体；金色前向弧表示护甲朝向。",
        "双层六边形；圆形避开内部、长线横向离开、环形可进入内圈。"
    };
    static readonly string[] CodexCounter={
        "不要一直沿同一方向绕圈；提前换边，把追踪群和远程单位分开。",
        "引出蓄力后横向改向；冲锋穿过之后，利用恢复时间反击。",
        "不要同时背对两条火线；借站位把它们引到同一侧，或冲刺离开夹角。",
        "看到封锁预告再改向；留出回头路线，别冲进刚生效的红区。",
        "绕过盾卫侧面、拉开它和后排的距离；也可先锁定盾卫将掩护拆除。",
        "E 优先锁节点。击破或干扰节点会打断整组进攻；恢复阶段是靠近它的机会。",
        "点击维护者优先攻击；单次直击伤害达到 5 可打断引导，让它暂时无法修复。",
        "诱导转身后冲刺绕侧；蓄力时侧背直击达到 8 可打断。背击伤害更高，干扰与恢复时护甲关闭。",
        "等预警锁定后换方向。找安全区、保留冲刺；监察者恢复时会受到额外伤害，抓住机会输出。"
    };
    static void CodexInput(){
        if(R.IsKeyPressed(262)||R.IsKeyPressed(264))codexPage=(codexPage+1)%CodexKinds.Length;
        if(R.IsKeyPressed(263)||R.IsKeyPressed(265))codexPage=(codexPage+CodexKinds.Length-1)%CodexKinds.Length;
    }
    static void DrawCodex(){
        Shade();Card(63,35,1154,650,Line);T("THREAT ARCHIVE / 战场已暂停",95,60,15,Cyan);T("看懂意图，再选反制",91,92,34,White);
        T("点击左侧单位 · 方向键切换",846,106,15,Muted);
        for(int i=0;i<CodexKinds.Length;i++){
            float y=155+i*49;bool selected=codexPage==i;Col c=EnemyRoleColor(CodexKinds[i]);Card(94,y,266,43,selected?c:Line);if(selected)Box(96,y+9,3,25,c);
            T((i+1).ToString("00"),111,y+12,15,c);T(Game.EnemyName(CodexKinds[i]),145,y+10,18,selected?White:Muted);
            if(ConsumeClick(Hit(94,y,266,43)))codexPage=i;
        }
        L(387,157,387,602,1,Line);
        string kind=CodexKinds[codexPage];Col color=EnemyRoleColor(kind);
        T(Game.EnemyName(kind),419,163,29,White);T(CodexRoles[codexPage],420,204,17,color);
        Enemy preview=new Enemy{Kind=kind,X=1105,Y=204,R=29,Hp=100,MaxHp=100,FacingX=-1,FacingY=0,Elite=kind=="prism"};DrawEnemyBody(preview,color);
        T("它在做什么",420,257,16,color);Wrap(CodexBehavior[codexPage],420,287,713,18,White);
        T("如何辨认",420,382,16,color);Wrap(CodexTell[codexPage],420,412,713,17,Muted);
        T("破解方式",420,494,16,Cyan);Wrap(CodexCounter[codexPage],420,524,713,18,White);
        T("橙色虚线：正在蓄力  /  红色实线：危险生效  /  青色与白色：你的武器",95,635,14,Muted);
        if(ModalButton("返回 / K",934,618,247,43,true))codex=false;
    }
    static Col EnemyRoleColor(string kind){return kind=="relay"?Violet:kind=="shooter"?new Col(243,124,189,255):kind=="boss"||kind=="tank"||kind=="prism"||kind=="repairer"?Gold:kind=="bomber"?new Col(255,153,88,255):Pink;}
    static void DrawEnemyBody(Enemy e,Col c){
        int sides=e.Kind=="runner"?3:e.Kind=="bomber"?8:e.Kind=="relay"||e.Kind=="boss"?6:4;
        float facing=(float)(Math.Atan2(e.FacingY,e.FacingX)*180/Math.PI);if(e.FacingX==0&&e.FacingY==0)facing=(float)(Math.Atan2(game.Y-e.Y,game.X-e.X)*180/Math.PI);
        if(e.Kind=="runner"&&e.AttackPhase==1)facing=(float)(Math.Atan2(e.TargetY-e.Y,e.TargetX-e.X)*180/Math.PI);
        if(e.Kind=="runner"&&e.AttackPhase==2)facing=(float)(Math.Atan2(e.VY,e.VX)*180/Math.PI);
        float rotation=e.Kind=="runner"?facing:e.Kind=="tank"||e.Kind=="repairer"?45:0;
        R.DrawPoly(new V(e.X,e.Y),sides,e.R+3,rotation,c.Alpha(.075f));R.DrawPoly(new V(e.X,e.Y),sides,e.R,rotation,c.Alpha(.18f));R.DrawPolyLinesEx(new V(e.X,e.Y),sides,e.R,rotation,e.Kind=="boss"||e.Elite?3:2,c);
        if(e.Kind=="relay"){R.DrawPolyLinesEx(new V(e.X,e.Y),6,e.R*.54f,0,2,c);R.DrawCircleV(new V(e.X,e.Y),3,White);}
        else if(e.Kind=="boss"){R.DrawPolyLinesEx(new V(e.X,e.Y),6,e.R*.57f,30,2,c);L(e.X-6,e.Y,e.X+6,e.Y,2,c);L(e.X,e.Y-6,e.X,e.Y+6,2,c);}
        else if(e.Kind=="repairer"){L(e.X-7,e.Y,e.X+7,e.Y,3,c);L(e.X,e.Y-7,e.X,e.Y+7,3,c);}
        else if(e.Kind=="prism"){R.DrawPolyLinesEx(new V(e.X,e.Y),4,e.R*.56f,0,2,c);if(e.JamTime<=0&&e.Stun<=0&&e.Recovery<=0)R.DrawRing(new V(e.X,e.Y),e.R+4,e.R+7,facing-60,facing+60,20,Gold);}
        else if(e.Kind=="tank"){float a=facing*(float)Math.PI/180,dx=(float)Math.Cos(a),dy=(float)Math.Sin(a);L(e.X+dx*e.R-dy*e.R*.7f,e.Y+dy*e.R+dx*e.R*.7f,e.X+dx*e.R+dy*e.R*.7f,e.Y+dy*e.R-dx*e.R*.7f,4,Gold);R.DrawPolyLinesEx(new V(e.X,e.Y),4,e.R*.45f,45,2,c);}
        else if(e.Kind=="shooter"){R.DrawCircleLinesV(new V(e.X,e.Y),e.R*.45f,c);L(e.X-e.R*.85f,e.Y,e.X+e.R*.85f,e.Y,1.6f,c);L(e.X,e.Y-e.R*.85f,e.X,e.Y+e.R*.85f,1.6f,c);}
        else if(e.Kind=="bomber"){R.DrawCircleLinesV(new V(e.X,e.Y),e.R*.5f,c);R.DrawCircleV(new V(e.X,e.Y),3,c);}
        else if(e.Kind=="runner"){float a=facing*(float)Math.PI/180;L(e.X-(float)Math.Cos(a)*4,e.Y-(float)Math.Sin(a)*4,e.X+(float)Math.Cos(a)*8,e.Y+(float)Math.Sin(a)*8,2,c);}
        else R.DrawCircleV(new V(e.X,e.Y),3,c);
    }
    static void EnemyTag(string label,float x,float y,Col c){float w=TW(label,13)+12;Box(x-w/2,y-2,w,20,BG.Alpha(.91f));TC(label,x,y,13,c);}
    static void DashedLine(float x,float y,float tx,float ty,float width,Col c){float dx=tx-x,dy=ty-y,len=(float)Math.Sqrt(dx*dx+dy*dy);if(len<1)return;for(float at=0;at<len;at+=15){float end=Math.Min(at+8,len);L(x+dx*at/len,y+dy*at/len,x+dx*end/len,y+dy*end/len,width,c);}}
    static void DashedCircle(float x,float y,float r,Col c){for(int i=0;i<24;i++){float a=i*(float)Math.PI/12,b=a+.12f;L(x+(float)Math.Cos(a)*r,y+(float)Math.Sin(a)*r,x+(float)Math.Cos(b)*r,y+(float)Math.Sin(b)*r,2,c);}}
    static void HazardCap(float x,float y,float radius,float from,bool warning,Col color){for(int i=0;i<24;i++){if(warning&&i%2!=0)continue;float a=(from+i*7.5f)*(float)Math.PI/180,b=(from+(i+1)*7.5f)*(float)Math.PI/180;L(x+(float)Math.Cos(a)*radius,y+(float)Math.Sin(a)*radius,x+(float)Math.Cos(b)*radius,y+(float)Math.Sin(b)*radius,2,color);}}
    static void DrawHazard(Hazard h){
        bool warning=h.Warning>0;Col c=warning?Gold:Pink;float alpha=warning?.08f:.21f;
        if(h.Shape=="line"){
            float dx=h.X2-h.X,dy=h.Y2-h.Y,len=Math.Max(1,(float)Math.Sqrt(dx*dx+dy*dy)),nx=-dy/len*h.R,ny=dx/len*h.R;
            L(h.X,h.Y,h.X2,h.Y2,h.R*2,c.Alpha(alpha));R.DrawCircleV(new V(h.X,h.Y),h.R,c.Alpha(alpha));R.DrawCircleV(new V(h.X2,h.Y2),h.R,c.Alpha(alpha));
            if(warning){DashedLine(h.X+nx,h.Y+ny,h.X2+nx,h.Y2+ny,2,c);DashedLine(h.X-nx,h.Y-ny,h.X2-nx,h.Y2-ny,2,c);}else{L(h.X+nx,h.Y+ny,h.X2+nx,h.Y2+ny,2,c);L(h.X-nx,h.Y-ny,h.X2-nx,h.Y2-ny,2,c);}
            float angle=(float)(Math.Atan2(dy,dx)*180/Math.PI);HazardCap(h.X,h.Y,h.R,angle+90,warning,c);HazardCap(h.X2,h.Y2,h.R,angle-90,warning,c);
            EnemyTag(warning?"切割蓄力":"切割生效",(h.X+h.X2)/2,(h.Y+h.Y2)/2-8,c);
        }else if(h.Shape=="ring"){
            R.DrawRing(new V(h.X,h.Y),h.InnerR,h.R,0,360,64,c.Alpha(alpha));
            if(warning){DashedCircle(h.X,h.Y,h.R,c);DashedCircle(h.X,h.Y,h.InnerR,c);}else{R.DrawCircleLinesV(new V(h.X,h.Y),h.R,c);R.DrawCircleLinesV(new V(h.X,h.Y),h.InnerR,c);}
            EnemyTag("本环安全区",h.X,h.Y-8,White);EnemyTag(warning?"环震蓄力":"环震生效",h.X,h.Y-h.R+12,c);
        }else{
            R.DrawCircleV(new V(h.X,h.Y),h.R,c.Alpha(alpha));if(warning)DashedCircle(h.X,h.Y,h.R,c);else R.DrawCircleLinesV(new V(h.X,h.Y),h.R,c);
            EnemyTag(warning?"封锁蓄力":"封锁生效",h.X,h.Y-8,c);
        }
    }
    static string PhaseCounter(string phase){if(phase=="准备")return "敌人正分散站位：找节点，预留侧向退路";if(phase=="压制")return "后排交叉开火：改向避弹，留一次冲刺";if(phase=="截击")return "侧翼正在收口：等冲锋锁定后横向穿出";if(phase=="恢复")return "协作暂歇：穿过缺口，集火节点或维护者";return "引出蓄力，再改向；点击敌人查看破解方式";}
    static void DrawSquadReadout(){
        bool linked=false;foreach(Enemy e in game.Enemies)if(e.Kind=="relay"&&e.Hp>0){linked=true;break;}
        float x=35,y=stats?196:112;Box(x,y,530,65,BG.Alpha(.93f));
        if(linked){string[] phases={"准备","压制","截击","恢复"};T("编队",x+12,y+10,14,Violet);for(int i=0;i<phases.Length;i++){bool active=game.SquadPhase==phases[i];Col c=active?(i==3?Cyan:Gold):Muted.Alpha(.5f);T(phases[i],x+75+i*92,y+9,16,c);if(i<3)T("›",x+137+i*92,y+10,16,Muted.Alpha(.45f));}T(game.SquadPhaseLeft.ToString("F1")+"s",x+470,y+11,14,Gold);Fit(PhaseCounter(game.SquadPhase),x+12,y+38,510,15,White);}
        else{T("战术提示",x+12,y+10,14,Cyan);Fit("引出预警 → 改变方向 → 抓恢复期反击",x+95,y+9,417,16,White);T("敌方蓄力为橙色虚线，红色区域已经生效",x+12,y+38,15,Muted);}
    }
    static void DrawTargetReadout(){
        Enemy target=null;foreach(Enemy e in game.Enemies)if(e.Id==game.FocusTargetId&&e.Hp>0){target=e;break;}
        if(target==null)return;float x=898,y=111,w=347;Col c=EnemyRoleColor(target.Kind);Card(x,y,w,150,c.Alpha(.65f));
        T((target.Elite?"精英 / ":"锁定 / ")+Game.EnemyName(target.Kind),x+14,y+11,20,c);
        float ratio=target.MaxHp<=0?0:Math.Max(0,Math.Min(1,target.Hp/target.MaxHp));Box(x+15,y+42,w-30,4,Line);Box(x+15,y+42,(w-30)*ratio,4,c);
        string intent=target.Stun>0?"失联僵直：现在可以进攻":target.JamTime>0?"受到干扰：协作已中断":target.Channel>0?"正在引导修复：攻击可打断":!string.IsNullOrEmpty(target.Intent)?target.Intent:target.Order;
        Fit("行动  "+intent,x+15,y+58,w-30,16,White);
        Wrap(Game.EnemyCounter(target.Kind),x+15,y+86,w-30,15,Muted);
        T("点击空地解除锁定 · K 查看机制",x+15,y+128,13,c);
    }
    static void DrawAttackWarning(Enemy e,Col c) {
        if(e.AttackPhase!=1||(e.Kind!="runner"&&e.Kind!="shooter"))return;
        float tx=e.TargetX,ty=e.TargetY;
        if(e.Kind=="runner"){
            float dx=tx-e.X,dy=ty-e.Y,len=Math.Max(1,(float)Math.Sqrt(dx*dx+dy*dy)),travel=Math.Max(.35f,Math.Min(.86f,len/520))*520;
            for(float at=0;at<travel;at+=15){float end=Math.Min(at+8,travel);L(ArenaClampX(e.X+dx/len*at,e.R),ArenaClampY(e.Y+dy/len*at,e.R),ArenaClampX(e.X+dx/len*end,e.R),ArenaClampY(e.Y+dy/len*end,e.R),3,Gold.Alpha(.85f));}
            tx=ArenaClampX(e.X+dx/len*travel,e.R);ty=ArenaClampY(e.Y+dy/len*travel,e.R);
        }else DashedLine(e.X,e.Y,tx,ty,2,Gold.Alpha(.85f));
        R.DrawCircleLinesV(new V(tx,ty),8,Gold.Alpha(.85f));
    }
    static float ArenaClampX(float x,float radius){return Math.Max(24+radius,Math.Min(1256-radius,x));}
    static float ArenaClampY(float y,float radius){return Math.Max(104+radius,Math.Min(640-radius,y));}
}
}
