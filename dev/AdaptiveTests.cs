using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using NeonEmber;

// Compile this file against either frozen v0.3 core or current core for the same route probes.
// ADAPTIVE_V04 additionally enables mechanism acceptance tests requiring the new public state.
public static class AdaptiveTests
{
    static readonly List<string> lines = new List<string>();
    static int passed, failed;
    const float Dt = 1f / 60;
    static void Log(string value) { lines.Add(value); Console.WriteLine(value); }
    static void Assert(bool ok, string message) { if (!ok) throw new Exception(message); }
    static bool Near(float a,float b) { return Math.Abs(a-b)<.025f; }
    static void Test(string name,Action run) { try {run();passed++;Log("PASS | "+name);} catch(Exception e){failed++;Log("FAIL | "+name+" | "+e.Message);} }
    static void SetClock(Game g,string name,float value) { typeof(Game).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g,value); }
    static Game Fixture()
    {
        Game g=new Game(4401);g.Start(0,0);g.Ranks.Clear();g.Ward=0;g.Enemies.Clear();g.Shots.Clear();g.Orbs.Clear();g.NextXp=10000000;
        SetClock(g,"spawnClock",1000000);SetClock(g,"nextSquad",1000000);SetClock(g,"nextBoss",1000000);SetClock(g,"nextShop",1000000);
        return g;
    }
    static Enemy Add(Game g,int id,string kind,float x,float y,float hp)
    {
        Enemy e=new Enemy {Id=id,Kind=kind,X=x,Y=y,R=kind=="boss"?38:kind=="tank"?22:15,Hp=hp,MaxHp=hp,Speed=0,Damage=20,Cooldown=1000};g.Enemies.Add(e);return e;
    }
    static void Step(Game g,float seconds,float mx,float my) { for(int i=0;i<(int)Math.Ceiling(seconds/Dt);i++)g.Step(Dt,mx,my,false); }
    static float Hit(Game g,Enemy e,float amount) {float before=e.Hp;g.Shots.Add(new Shot {X=e.X,Y=e.Y,R=4,Damage=amount,Life=1,Kind="bolt"});g.Step(Dt,0,0,false);return before-e.Hp;}
    static void Circle(Game g,float elapsed,int direction,out float mx,out float my)
    {
        float rx=g.X-640,ry=g.Y-372,r=(float)Math.Sqrt(rx*rx+ry*ry);
        // Tangential travel at real player speed; slight radial steering maintains a 185px circle.
        mx=-ry/Math.Max(1,r)*direction-rx/Math.Max(1,r)*(r-185)*.025f;
        my=rx/Math.Max(1,r)*direction-ry/Math.Max(1,r)*(r-185)*.025f;
    }
    static void ApplyBuild(Game g,bool formed,bool awakened)
    {
        g.Ranks.Clear();g.Ranks["bolt"]=formed?5:2;g.Ranks["arc"]=formed?4:0;g.Ranks["blade"]=formed?4:0;
        g.Ranks["power"]=formed?4:0;g.Ranks["haste"]=formed?3:0;
        if(awakened){g.Ranks["ember_brand"]=3;g.Ranks["ember_breach"]=2;g.Ranks["ember_cascade"]=1;g.Ranks["echo_decoy"]=1;g.Ranks["echo_jammer"]=1;}
        // No health, armor, ward ranks, bought healing or unlimited HP. Awakened build can use decoy/jam.
    }
    static void NaturalRoute(int seed,bool formed,bool react,bool awakened=false,bool focusRelays=false)
    {
        Game g=new Game(seed);g.Start(0,0);ApplyBuild(g,formed,awakened);g.NextXp=10000000;g.X=825;g.Y=372;
        string buildLabel=awakened?"awakened":formed?"formed":"early",routeLabel=focusRelays?"reverse_dash_focus_relay":react?"warning_reverse_dash":"circle";
        float damage=0,lastTurn=-100;int direction=1,warnings=0,attacks=0,killedBeforeAttack=0,specialDeaths=0;
        var observed=new Dictionary<int,int>();var previousPhase=new Dictionary<int,int>();var wasSpecial=new HashSet<int>();
#if ADAPTIVE_V04
        int finishedAttacks=0,eliteBorn=0,eliteActed=0,eliteDeadBeforeAttack=0,recoveryFrames=0;
        var serials=new Dictionary<int,int>();var eliteIds=new HashSet<int>();var eliteCompleted=new HashSet<int>();
        var eliteKindBorn=new Dictionary<string,int>();var eliteKindActed=new Dictionary<string,int>();
#endif
        while(g.Time<300 && g.Mode!="over")
        {
            if(g.Mode=="shop")g.LeaveShop();
            if(g.Mode=="level"){g.Choose(0);g.NextXp=10000000;ApplyBuild(g,formed,awakened);}
            if(focusRelays){int target=0;float best=700*700;foreach(Enemy e in g.Enemies)if(e.Kind=="relay"&&e.Hp>0){float dx=e.X-g.X,dy=e.Y-g.Y,d=dx*dx+dy*dy;if(d<best){best=d;target=e.Id;}}g.FocusTargetId=target;}
            float mx,my;Circle(g,g.Time,direction,out mx,out my);bool dash=false;
            if(react && g.Time-lastTurn>1.5f)
            {
                bool threatened=false;
                foreach(Hazard h in g.Hazards){float dx=h.X-g.X,dy=h.Y-g.Y;if(h.Warning>0 && dx*dx+dy*dy<220*220)threatened=true;}
                foreach(Enemy e in g.Enemies){float dx=e.TargetX-g.X,dy=e.TargetY-g.Y;if(e.AttackPhase==1&&dx*dx+dy*dy<180*180)threatened=true;}
                if(threatened){direction=-direction;lastTurn=g.Time;Circle(g,g.Time,direction,out mx,out my);dash=true;}
            }
            float hp=g.Hp;g.Step(Dt,mx,my,dash);damage+=Math.Max(0,hp-g.Hp);
            var live=new HashSet<int>();
            foreach(Enemy e in g.Enemies)
            {
                if(e.Kind=="chaser"||e.Kind=="tank"||e.Kind=="relay")continue;
                live.Add(e.Id);wasSpecial.Add(e.Id);if(!observed.ContainsKey(e.Id))observed[e.Id]=0;
                int old;previousPhase.TryGetValue(e.Id,out old);
                if(e.AttackPhase==1&&old!=1){warnings++;observed[e.Id]++;}
                if(old==1&&e.AttackPhase!=1&&e.Hp>0)attacks++;
                previousPhase[e.Id]=e.AttackPhase;
#if ADAPTIVE_V04
                int prior;serials.TryGetValue(e.Id,out prior);finishedAttacks+=Math.Max(0,e.AttackSerial-prior);serials[e.Id]=e.AttackSerial;
                if(e.Elite){if(eliteIds.Add(e.Id)){eliteBorn++;if(!eliteKindBorn.ContainsKey(e.Kind))eliteKindBorn[e.Kind]=0;eliteKindBorn[e.Kind]++;}if(e.AttackSerial>0&&eliteCompleted.Add(e.Id)){eliteActed++;if(!eliteKindActed.ContainsKey(e.Kind))eliteKindActed[e.Kind]=0;eliteKindActed[e.Kind]++;}if(e.Recovery>0)recoveryFrames++;}
#endif
            }
            var gone=new List<int>();foreach(int id in wasSpecial)if(!live.Contains(id))gone.Add(id);
            foreach(int id in gone){specialDeaths++;if(observed[id]==0)killedBeforeAttack++;wasSpecial.Remove(id);
#if ADAPTIVE_V04
                if(eliteIds.Contains(id)&&!eliteCompleted.Contains(id))eliteDeadBeforeAttack++;
#endif
            }
        }
        Log(string.Format(CultureInfo.InvariantCulture,"ROUTE | seed={0} build={1} route={2} survival_s={3:F2} hp={4:F2} damage_taken={5:F2} kills={6} warnings={7} completed_attack_transitions={8} specials_removed={9} removed_before_warning={10}",seed,buildLabel,routeLabel,g.Time,g.Hp,damage,g.Kills,warnings,attacks,specialDeaths,killedBeforeAttack));
#if ADAPTIVE_V04
        Log("EXECUTION | seed="+seed+" build="+buildLabel+" route="+routeLabel+" completed_attacks="+finishedAttacks+" elites_seen="+eliteBorn+" elites_executed="+eliteActed+" elite_deaths_without_action="+eliteDeadBeforeAttack+" elite_recovery_frames="+recoveryFrames);
        foreach(KeyValuePair<string,int> pair in eliteKindBorn){int n;eliteKindActed.TryGetValue(pair.Key,out n);Log("ELITE_KIND | seed="+seed+" build="+buildLabel+" route="+routeLabel+" kind="+pair.Key+" seen="+pair.Value+" executed="+n);}
#endif
    }

#if ADAPTIVE_V04
    static void V04Tests()
    {
        Test("Circular history predicts a curved route; reversing after the locked tell avoids real damage",delegate {
            float[] hp=new float[2];float confidence=0;
            for(int route=0;route<2;route++)
            {
                Game g=Fixture();g.X=825;g.Y=372;
                for(int i=0;i<150;i++){float mx,my;Circle(g,g.Time,1,out mx,out my);g.Step(Dt,mx,my,false);}
                confidence=g.CircleConfidence;Enemy e=Add(g,10001,"bomber",640,372,10000);e.Cooldown=0;
                for(int i=0;i<120&&e.AttackPhase!=1;i++){float mx,my;Circle(g,g.Time,1,out mx,out my);g.Step(Dt,mx,my,false);}
                Assert(e.AttackPhase==1,"circle fixture never began warning");float tx=e.TargetX,ty=e.TargetY;
                for(int i=0;i<108;i++){float mx,my;Circle(g,g.Time,route==0?1:-1,out mx,out my);g.Step(Dt,mx,my,false);if(e.AttackPhase==1)Assert(Near(tx,e.TargetX)&&Near(ty,e.TargetY),"locked target tracked reversal");}
                hp[route]=g.Hp;
            }
            Log(string.Format(CultureInfo.InvariantCulture,"CIRCLE | confidence={0:F3} continued_hp={1:F2} reversed_hp={2:F2}",confidence,hp[0],hp[1]));
            Assert(confidence>.3f,"no usable circle inference from observed motion");Assert(hp[0]<100&&hp[1]>hp[0]+10,"curved prediction had no damage consequence or reversal offered no counterplay");
        });
        Test("Squad suppression precedes interception and breaking relay prevents announced damage",delegate {
            float[] hp=new float[2];
            for(int intervention=0;intervention<2;intervention++)
            {
                Game g=Fixture();Enemy relay=Add(g,10000,"relay",920,372,300),bomb=Add(g,10001,"bomber",880,300,10000),runner=Add(g,10002,"runner",900,450,10000);bomb.Cooldown=runner.Cooldown=0;
                float bombAt=-1,runnerAt=-1;
                for(int i=0;i<360&&bomb.AttackPhase!=1;i++){g.Step(Dt,0,0,false);if(runner.AttackPhase==1&&runnerAt<0)runnerAt=g.Time;}
                Assert(bomb.AttackPhase==1&&bomb.RelayId==relay.Id,"linked suppression never opened");bombAt=g.Time;
                Assert(runnerAt<0||runnerAt>=bombAt,"interceptor spent attack before suppression");
                if(intervention==1){Hit(g,relay,10000);Assert(bomb.Stun>0&&bomb.AttackPhase==0&&bomb.RelayId==0,"relay destruction did not cancel squad attack");}
                Step(g,1.45f,0,0);hp[intervention]=g.Hp;
            }
            Log("RELAY | ignored_hp="+hp[0]+" destroyed_hp="+hp[1]);Assert(hp[0]<100&&Near(hp[1],100),"target priority did not change actual incoming damage");
        });
        Test("Prism armor rewards flanking and interruption over firing at the front",delegate {
            float[] damage=new float[4];
            for(int side=0;side<4;side++)
            {
                Game g=Fixture();Enemy e=Add(g,10000,"prism",900,372,1000);e.FacingX=-1;e.FacingY=0;
                if(side==1){g.X=900;g.Y=150;}if(side==2)g.X=1150;if(side==3)e.JamTime=1;
                damage[side]=Hit(g,e,100);
            }
            Log("PRISM | front="+damage[0]+" side="+damage[1]+" rear="+damage[2]+" jammed="+damage[3]);
            Assert(damage[0]<60&&damage[1]>damage[0]*2&&damage[2]>damage[1]&&damage[3]>damage[0]*2,"directional armor lacks effective counterplay");
        });
        Test("Prism facing has a finite turn speed and cannot snap onto a crossing player",delegate {
            Game g=Fixture();Enemy e=Add(g,10000,"prism",900,372,1000);e.FacingX=-1;e.FacingY=0;g.X=1140;g.Step(Dt,0,0,false);
            Assert(e.FacingX<-.99f,"guard snapped 180 degrees in one frame");
            Assert(Math.Abs(e.FacingX*e.FacingX+e.FacingY*e.FacingY-1)<.05f,"facing lost normalization");
        });
        Test("Moving behind a winding prism cancels the attack; frontal fire does not",delegate {
            for(int flank=0;flank<2;flank++)
            {
                Game g=Fixture();Enemy e=Add(g,10000,"prism",850,372,1000);e.FacingX=-1;e.FacingY=0;e.Cooldown=0;g.Step(Dt,0,0,false);
                Assert(e.AttackPhase==1,"prism failed to advertise attack");
                if(flank==1)g.X=1060;Hit(g,e,20);
                if(flank==1)Assert(e.AttackPhase==0&&e.Recovery>0&&g.Hazards.Count==0,"back hit did not interrupt warning and open recovery");
                else {Assert(e.AttackPhase==1,"front hit cancelled the advertised attack");Step(g,1,0,0);Assert(e.AttackSerial>0,"frontally pressured prism failed to act");}
            }
        });
        Test("Repairer heals a wounded living ally; direct damage cancels its channel",delegate {
            float[] finalHp=new float[2];
            for(int interrupt=0;interrupt<2;interrupt++)
            {
                Game g=Fixture();Enemy ally=Add(g,10000,"tank",870,372,500),medic=Add(g,10001,"repairer",940,372,500);ally.Hp=100;medic.Cooldown=0;
                for(int i=0;i<180&&medic.Channel<=0;i++)g.Step(Dt,0,0,false);
                Assert(medic.Channel>0&&medic.RepairTargetId==ally.Id,"repairer never selected wounded ally");
                if(interrupt==1){Hit(g,medic,10);Assert(medic.Channel<=0&&medic.Recovery>1,"direct fire failed to interrupt repair");}
                Step(g,1.85f,0,0);finalHp[interrupt]=ally.Hp;
            }
            Log("REPAIR | ignored_ally_hp="+finalHp[0]+" interrupted_ally_hp="+finalHp[1]);Assert(finalHp[0]>130&&Near(finalHp[1],100),"repair/no-repair outcomes did not diverge");
        });
        Test("Boss cycles distinct warnings, exposes recovery, and unlocks a second phase",delegate {
            Game g=Fixture();g.X=825;g.Y=372;Enemy boss=Add(g,10000,"boss",940,372,10000);boss.Cooldown=0;boss.Damage=2;
            var shapes=new HashSet<string>();bool recovered=false;
            for(int i=0;i<1800;i++)
            {
                float mx,my;Circle(g,g.Time,1,out mx,out my);g.Step(Dt,mx,my,false);
                foreach(Hazard h in g.Hazards)if(h.OwnerId==boss.Id&&h.Warning>0)shapes.Add(h.Shape);
                recovered|=boss.Recovery>0;
            }
            Assert(shapes.Contains("circle")&&shapes.Contains("line")&&shapes.Contains("ring"),"boss never exercised all three warning shapes");Assert(recovered,"boss never exposed recovery");
            boss.Hp=boss.MaxHp*.4f;g.Step(Dt,0,0,false);Assert(boss.BossStage>=2,"half-health stage failed");
            boss.Recovery=0;float closed=Hit(g,boss,100);boss.Recovery=1;float opened=Hit(g,boss,100);
            Log("BOSS | closed_damage="+closed+" recovery_damage="+opened);Assert(opened>closed*1.25f,"recovery gives no damage opportunity");
        });
        Test("Ring center and line sidestep are genuine safe zones with real collision damage",delegate {
            Hazard ring=new Hazard {Shape="ring",X=640,Y=372,R=180,InnerR=100,Life=3,Damage=20};
            Assert(!Game.HazardContains(ring,640,372,11)&&Game.HazardContains(ring,780,372,11)&&!Game.HazardContains(ring,850,372,11),"ring geometry has no inside/outside counterplay");
            Hazard lane=new Hazard {Shape="line",X=420,Y=372,X2=860,Y2=372,R=28,Life=3,Damage=20};
            Assert(Game.HazardContains(lane,640,372,11)&&!Game.HazardContains(lane,640,445,11),"lane geometry ignored sidestep");
            foreach(Hazard h in new Hazard[]{ring,lane})for(int safe=0;safe<2;safe++)
            {
                Game g=Fixture();g.Hazards.Add(h);g.X=h.Shape=="ring"?(safe==1?640:780):640;g.Y=h.Shape=="ring"?372:(safe==1?445:372);g.Step(Dt,0,0,false);
                Assert(safe==1?Near(g.Hp,100):g.Hp<100,"rendered shape and damage geometry disagree");
            }
        });
        Test("Stationary player does not prevent boss ring startup when safe center is reachable",delegate {
            Game g=Fixture();Enemy boss=Add(g,10000,"boss",940,372,10000);boss.Cooldown=0;boss.AttackSerial=2;
            for(int i=0;i<120&&boss.AttackPhase!=1;i++)g.Step(Dt,0,0,false);
            Assert(boss.AttackPhase==1,"ring was starved by escape test despite safe center");bool ring=false;foreach(Hazard h in g.Hazards)if(h.OwnerId==boss.Id&&h.Shape=="ring")ring=true;Assert(ring,"third boss attack failed to create a ring");
        });
        Test("Pause freezes learned motion, squad phase and repair; restart clears tactical memory",delegate {
            Game g=Fixture();g.X=825;g.Y=372;for(int i=0;i<120;i++){float mx,my;Circle(g,g.Time,1,out mx,out my);g.Step(Dt,mx,my,false);}
            Enemy e=Add(g,10000,"repairer",900,372,1000);e.Channel=1;e.RepairTargetId=10001;
            float confidence=g.CircleConfidence,turn=g.ObservedTurnRate,phase=g.SquadPhaseLeft,time=g.Time;g.Mode="pause";Step(g,2,1,1);
            Assert(g.Time==time&&g.CircleConfidence==confidence&&g.ObservedTurnRate==turn&&g.SquadPhaseLeft==phase&&e.Channel==1,"new AI state advanced while paused");
            g.Start(0,0);Assert(g.Enemies.Count==0&&g.Hazards.Count==0&&Near(g.CircleConfidence,0)&&Near(g.ObservedTurnRate,0),"new run retained prior trajectory or enemies");
        });
    }
#endif
    public static int Main(string[] args)
    {
        string report=args.Length>0?args[0]:"adaptive-report.md";string mode=args.Length>1?args[1]:"all";
        Log("# Adaptive encounter verification");
        Log("Fixed seed core simulation. Natural routes use real 100 HP, no health/armor/ward ranks, no purchases, fixed ranks, and no level growth. Formed=bolt5/arc4/blade4/power4/haste3. Awakened additionally uses ember_brand3/ember_breach2/ember_cascade1/echo_decoy1/echo_jammer1. Damage includes losses before any boss reward healing. Route results are diagnostics, not human balance or rendered performance claims.");
        if(mode!="mechanisms")for(int seed=511;seed<=513;seed++)for(int build=0;build<2;build++)for(int route=0;route<2;route++)NaturalRoute(seed,build==1,route==1);
        if(mode!="mechanisms")for(int seed=511;seed<=513;seed++){NaturalRoute(seed,true,false,true);NaturalRoute(seed,true,true,true);NaturalRoute(seed,true,true,true,true);}
#if ADAPTIVE_V04
        if(mode!="routes")V04Tests();
#endif
        Log("RESULT | passed="+passed+" failed="+failed);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(report)));File.WriteAllLines(report,lines.ToArray(),new System.Text.UTF8Encoding(false));return failed==0?0:1;
    }
}
