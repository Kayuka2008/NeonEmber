using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NeonEmber;

public static class TacticsTests
{
    static List<string> lines = new List<string>();
    static int passed, failed;
    static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    static bool Near(float a, float b) { return Math.Abs(a - b) < .025f; }
    static bool Finite(float n) { return !float.IsNaN(n) && !float.IsInfinity(n); }
    static void Log(string text) { lines.Add(text); Console.WriteLine(text); }
    static void Test(string name, Action run)
    {
        try { run(); passed++; Log("PASS | " + name); }
        catch (Exception e) { failed++; Log("FAIL | " + name + " | " + e.Message); }
    }
    static Game Fixture()
    {
        Game g = new Game(71); g.Start(0, 0); g.Ranks.Clear(); g.Ward = 0;
        g.Enemies.Clear(); g.Shots.Clear(); g.Orbs.Clear(); return g;
    }
    static Enemy Add(Game g, int id, string kind, float x, float y)
    {
        Enemy e = new Enemy { Id=id, Kind=kind, X=x, Y=y, R=kind=="tank" ? 22 : 13,
            Hp=10000, MaxHp=10000, Speed=0, Damage=20, Cooldown=1000 };
        g.Enemies.Add(e); return e;
    }
    static void Advance(Game g, float seconds, float mx, float my)
    {
        for (int i=0; i<(int)Math.Ceiling(seconds/.01f); i++) g.Step(.01f,mx,my,false);
    }
    static void Upgrade(Game g, string id, int times)
    {
        for (int i=0;i<times;i++) { g.Mode="level"; g.Choices.Clear(); g.Choices.Add(new Upgrade {Id=id}); g.Choose(0); }
    }
    static float Hit(Game g, Enemy e, float amount, string kind)
    {
        float before=e.Hp;
        g.Shots.Add(new Shot {X=e.X,Y=e.Y,R=4,Damage=amount,Life=1,Kind=kind});
        g.Step(.01f,0,0,false); return before-e.Hp;
    }
    static void Hostile(Game g, float damage, float dx)
    {
        g.Shots.Add(new Shot {X=g.X+dx,Y=g.Y,R=6,Damage=damage,Life=3,Kind="enemy",Hostile=true});
    }
    static bool HasWarning(Game g, int id)
    {
        foreach(Hazard h in g.Hazards) if(h.OwnerId==id && h.Warning>0) return true;
        return false;
    }
    static int Active(Game g)
    {
        int n=0; foreach(Enemy e in g.Enemies) if(e.Hp>0 && (e.AttackPhase==1||e.AttackPhase==2)) n++; return n;
    }
    static void Limits(Game g)
    {
        Assert(g.Enemies.Count<=Game.EnemyLimit && g.Shots.Count<=Game.ShotLimit && g.Orbs.Count<=Game.OrbLimit && g.Effects.Count<=Game.EffectLimit,"base entity cap exceeded");
        Assert(g.Hazards.Count<=Game.HazardLimit,"hazard cap exceeded");
        Assert(Active(g)<=Game.SpecialAttackLimit,"special attack concurrency exceeded");
        Assert(Finite(g.Ward)&&Finite(g.PlayerVX)&&Finite(g.PlayerVY)&&Finite(g.Hp),"invalid state");
        foreach(Hazard h in g.Hazards) {
            Assert(Finite(h.X)&&Finite(h.Y)&&Finite(h.R),"invalid hazard geometry");
            // v0.4 intentionally adds a ring with an open center and a finite-width beam.
            // Preserve bounded danger rather than applying the old circle-only radius bound.
            if(h.Shape=="ring")Assert(h.R<=180&&h.InnerR>=100&&h.InnerR<h.R,"ring lost its safe center or exceeded radius budget");
            else if(h.Shape=="line")Assert(Finite(h.X2)&&Finite(h.Y2)&&h.R<=28&&(h.X2-h.X)*(h.X2-h.X)+(h.Y2-h.Y)*(h.Y2-h.Y)<=460*460,"beam exceeded bounded width/length");
            else Assert(h.R<=96,"circle exceeds radius budget");
        }
        foreach(Enemy e in g.Enemies) Assert(Finite(e.BurnDps)&&Finite(e.Hp)&&Finite(e.JamTime),"non-finite status");
    }

    public static int Main(string[] args)
    {
        Log("# v0.4 tactical and world-system behavior verification");
        Log("Deterministic core simulation only. These fixtures verify actual movement, damage, interruption and state transitions; they do not establish human difficulty, rendered FPS, or guaranteed escape from every possible crowd/bullet arrangement.");
        Test("Observed movement predicts ahead; locked warning does not track reversal", delegate {
            Game g=Fixture(); g.X=360; Enemy bomber=Add(g,10001,"bomber",720,160);
            Advance(g,.65f,1,0); bomber.Cooldown=0; g.Step(.01f,1,0,false);
            Assert(bomber.AttackPhase==1 && HasWarning(g,bomber.Id),"bomber failed to advertise attack");
            float tx=bomber.TargetX,ty=bomber.TargetY;
            Assert(tx>g.X+120,"aim was not meaningfully ahead of observed movement");
            Advance(g,.30f,-1,0);
            Assert(Near(tx,bomber.TargetX)&&Near(ty,bomber.TargetY),"warning chased a direction change");
            Assert(Near(g.Hp,100),"warning dealt early damage");
        });
        Test("Counterfactual route: straight movement is hit; reversal avoids the same predicted blast", delegate {
            float straight=0,reactive=0;
            for(int route=0;route<2;route++) {
                Game g=Fixture(); g.X=360; Enemy bomber=Add(g,10001,"bomber",720,160);
                Advance(g,.65f,1,0); bomber.Cooldown=0; g.Step(.01f,1,0,false);
                Assert(bomber.AttackPhase==1,"attack missing");
                Advance(g,1.35f,route==0?1:-1,0);
                if(route==0) straight=g.Hp; else reactive=g.Hp;
            }
            Log("ROUTES | straight_hp="+straight.ToString("F2",CultureInfo.InvariantCulture)+" reversed_hp="+reactive.ToString("F2",CultureInfo.InvariantCulture));
            Assert(straight<100&&Near(reactive,100),"straight and reactive routes did not produce intended different outcomes");
        });
        Test("Relay assigns coordinated lateral fire positions and shooters move to different sides", delegate {
            Game g=Fixture(); Add(g,10000,"relay",960,380);
            Enemy left=Add(g,10001,"shooter",960,380),right=Add(g,10002,"shooter",960,380);
            left.Speed=right.Speed=90; g.Step(.01f,0,0,false);
            Assert(left.RelayId==10000&&right.RelayId==10000,"nearby shooters not coordinated");
            Advance(g,.5f,0,0);
            Assert(left.Y<365&&right.Y>395,"allies did not move into distinct fire lanes");
        });
        Test("Guard physically moves between shooter and player; mitigation ends outside lane or after death", delegate {
            Game g=Fixture(); Enemy shooter=Add(g,10001,"shooter",960,380),guard=Add(g,10002,"tank",950,460);
            guard.Speed=160; Advance(g,1.1f,0,0);
            Assert(guard.GuardId==shooter.Id&&shooter.GuardId==guard.Id,"guard did not pick ally");
            Assert(guard.X<shooter.X&&Math.Abs(guard.Y-shooter.Y)<44&&shooter.Shielded,"guard failed to occupy firing lane");
            float reduced=Hit(g,shooter,100,"bolt");
            guard.X=1160; guard.Y=560; guard.Speed=0;
            float exposed=Hit(g,shooter,100,"bolt");
            guard.X=shooter.X-62;guard.Y=shooter.Y;g.Step(.01f,0,0,false);guard.Hp=0;
            float deadGuard=Hit(g,shooter,100,"bolt");
            Assert(reduced<75&&Near(exposed,100)&&Near(deadGuard,100),"shield remained without physical/live guard: "+reduced+"/"+exposed+"/"+deadGuard);
        });
        Test("Killing relay cancels follower warning and interrupts the whole linked squad", delegate {
            Game g=Fixture(); Enemy relay=Add(g,10000,"relay",940,380),bomber=Add(g,10001,"bomber",890,320),runner=Add(g,10002,"runner",890,440);
            g.Step(.01f,0,0,false); bomber.Cooldown=0;
            // Coordinated units now wait for the suppression phase instead of spending attacks on spawn.
            for(int wait=0;wait<450&&bomber.AttackPhase!=1;wait++)g.Step(.01f,0,0,false);
            Assert(bomber.RelayId==relay.Id&&bomber.AttackPhase==1&&HasWarning(g,bomber.Id),"linked warning fixture missing");
            Hit(g,relay,20000,"bolt");
            Assert(bomber.RelayId==0&&runner.RelayId==0&&bomber.Stun>0&&runner.Stun>0,"relay death did not disrupt followers");
            Assert(bomber.AttackPhase==0&&!HasWarning(g,bomber.Id),"orphan warning survived relay death");
            Advance(g,.5f,0,0); Assert(Near(g.Hp,100),"cancelled attack still damaged player");
        });
        Test("Dense mixed enemies respect attack budget and retain uncovered arena space", delegate {
            Game g=Fixture();g.Hp=g.MaxHp=100000;
            string[] kinds={"runner","shooter","bomber","bomber"};
            for(int i=0;i<80;i++) { float a=i*(float)Math.PI*2/80; Enemy e=Add(g,10000+i,kinds[i%4],640+(float)Math.Cos(a)*290,380+(float)Math.Sin(a)*210);e.Cooldown=0; }
            int peak=0,hazardPeak=0;
            for(int i=0;i<1800;i++) {
                g.Step(.01f,0,0,false); if(g.Mode=="level")g.Choose(0);
                Limits(g);peak=Math.Max(peak,Active(g));hazardPeak=Math.Max(hazardPeak,g.Hazards.Count);
                bool uncovered=false;
                for(int gy=0;gy<5&&!uncovered;gy++)for(int gx=0;gx<10&&!uncovered;gx++) {
                    float px=80+gx*120,py=140+gy*110;bool covered=false;
                    foreach(Hazard h in g.Hazards) if(Game.HazardContains(h,px,py,13))covered=true;
                    if(!covered)uncovered=true;
                }
                Assert(uncovered,"AOE covered every sampled arena region");
            }
            Log("DENSE | active_peak="+peak+" hazards_peak="+hazardPeak);
            Assert(peak>=2&&hazardPeak>0,"stress fixture did not exercise overlapping attacks and hazards");
        });
        Test("Ember brand causes damage over time after the original weapon stops", delegate {
            Game g=Fixture();Upgrade(g,"ember_brand",1);Enemy e=Add(g,10000,"tank",900,380);
            float initial=Hit(g,e,10,"bolt");Assert(Near(initial,10)&&e.BurnTime>0,"weapon did not apply brand");
            float hp=e.Hp;Advance(g,.5f,0,0);Assert(hp-e.Hp>3.2f&&hp-e.Hp<3.8f,"burn did not deal expected continuing damage");
            e.BurnTime=0;hp=e.Hp;Advance(g,.3f,0,0);Assert(Near(hp,e.Hp),"damage continued without burn or weapons");
        });
        Test("Breathing phase stops fresh normal attacks but lets announced attacks finish", delegate {
            Game g=Fixture();Enemy fresh=Add(g,10000,"bomber",900,300);fresh.Cooldown=0;
            Enemy existing=Add(g,10001,"bomber",900,450);existing.AttackPhase=1;existing.Windup=.12f;existing.TargetX=g.X;existing.TargetY=g.Y;
            g.Time=25;Advance(g,.25f,0,0);
            Assert(fresh.AttackPhase==0&&!HasWarning(g,fresh.Id),"fresh normal warning opened during breathing phase");
            Assert(g.Hp<100&&existing.AttackPhase==0,"already announced attack was silently erased by phase change");
        });
        Test("Boss receives an attack slot despite a crowd of earlier-listed shooters", delegate {
            Game g=Fixture();for(int i=0;i<30;i++){Enemy e=Add(g,10000+i,"shooter",900,300+i*3);e.Cooldown=0;}
            Enemy boss=Add(g,10100,"boss",1000,380);boss.Cooldown=0;bool warned=false;
            for(int i=0;i<90;i++){g.Step(.01f,0,0,false);Limits(g);if(boss.AttackPhase==1)warned=true;}
            Assert(warned,"normal enemies starved ready boss of attack budget");
        });
        Test("Maximum entity occupancy survives spawn, death rewards and attack allocation", delegate {
            Game g=Fixture();g.Hp=g.MaxHp=100000;
            for(int i=0;i<Game.EnemyLimit;i++){Enemy e=Add(g,10000+i,i%7==0?"bomber":"shooter",80+(i%24)*47,130+(i/24)*31);e.Cooldown=0;}
            for(int i=0;i<Game.ShotLimit;i++)g.Shots.Add(new Shot{X=100,Y=130,R=4,Life=100,Damage=1,Hostile=true});
            for(int i=0;i<Game.OrbLimit;i++)g.Orbs.Add(new Orb{X=80,Y=130,Value=1});
            for(int i=0;i<Game.EffectLimit;i++)g.Effects.Add(new Effect{X=80,Y=130,Life=10,MaxLife=10,Kind="hit"});
            for(int i=0;i<Game.HazardLimit;i++)g.Hazards.Add(new Hazard{X=90,Y=140,R=30,Life=10,Damage=1});
            g.Time=179.99f;g.Step(.02f,0,0,false);Limits(g);
            bool boss=false;foreach(Enemy e in g.Enemies)if(e.Kind=="boss")boss=true;Assert(boss,"full capacity prevented scheduled boss");
            for(int i=0;i<30;i++)g.Enemies[i].Hp=0;
            g.Step(.01f,0,0,false);Limits(g);
            float xp=g.Xp;foreach(Orb orb in g.Orbs)xp+=orb.Value;Assert(xp>Game.OrbLimit,"death rewards lost at orb capacity");
            for(int i=0;i<120;i++){g.Step(.01f,0,0,false);Limits(g);}
        });
        Test("Echo dash interrupts a charged attack and its hazard", delegate {
            Game g=Fixture();Upgrade(g,"echo_jammer",1);Enemy e=Add(g,10000,"bomber",g.X+85,g.Y);e.Cooldown=0;
            g.Step(.01f,0,0,false);Assert(e.AttackPhase==1&&HasWarning(g,e.Id),"no warning to interrupt");
            g.Step(.01f,1,0,true);Assert(e.JamTime>0&&e.AttackPhase==0&&!HasWarning(g,e.Id),"dash jammer failed to cancel actual attack");
            Advance(g,.5f,0,0);Assert(Near(g.Hp,100),"interrupted attack hurt the player");
        });
        Test("Decoy redirects only new attack locks while existing target remains fixed", delegate {
            Game g=Fixture();Upgrade(g,"echo_decoy",1);Enemy e=Add(g,10000,"bomber",1000,380);
            g.Step(.01f,1,0,true);Advance(g,.22f,1,0);Assert(g.DecoyTime>0&&g.X>g.DecoyX+100,"decoy fixture failed");
            e.Cooldown=0;g.Step(.01f,1,0,false);Assert(e.AttackPhase==1&&Near(e.TargetX,g.DecoyX)&&Near(e.TargetY,g.DecoyY),"new attack ignored clone");
            float tx=e.TargetX;Advance(g,.25f,-1,0);Assert(Near(tx,e.TargetX),"clone changed already locked target");
        });
        Test("Forge ward absorbs real hostile hit, waits, then regenerates", delegate {
            Game g=Fixture();Upgrade(g,"forge_ward",1);Assert(Near(g.Ward,14),"ward not granted");
            Hostile(g,10,0);g.Step(.01f,0,0,false);Assert(Near(g.Hp,100)&&Near(g.Ward,4),"ward failed to absorb projectile");
            Advance(g,2,0,0);Assert(Near(g.Ward,4),"ward regenerated inside damage delay");
            Advance(g,2.5f,0,0);Assert(g.Ward>6&&g.Ward<=g.MaxWard,"ward failed to regenerate after safe interval");
        });
        Test("Forge dash hits once per target and disables guard protection", delegate {
            Game g=Fixture();Upgrade(g,"forge_breaker",1);Enemy guard=Add(g,10000,"tank",g.X+50,g.Y),shooter=Add(g,10001,"shooter",g.X+112,g.Y);
            g.Step(.01f,0,0,false);Assert(shooter.Shielded,"no guard protection before dash");
            float hp=guard.Hp;g.Step(.01f,1,0,true);Advance(g,.20f,1,0);
            Assert(Near(hp-guard.Hp,24)&&guard.JamTime>0,"dash damage was absent or repeated: "+(hp-guard.Hp));
            Assert(!shooter.Shielded,"jammed tank kept protecting ally");
        });
        Test("Interceptor removes nearby hostile projectile without consuming friendly shots", delegate {
            Game g=Fixture();Upgrade(g,"forge_intercept",1);Hostile(g,20,50);
            Shot friendly=new Shot {X=g.X,Y=g.Y+50,R=4,Damage=10,Life=2,Kind="bolt"};g.Shots.Add(friendly);
            g.Step(.01f,0,0,false);Assert(g.Shots.Count==1&&g.Shots[0]==friendly,"interceptor missed hostile shot or removed friendly one");
        });
        Test("Mixed-weapon trace rewards alternating actual hit sources", delegate {
            Game g=Fixture();Upgrade(g,"echo_trace",1);Enemy e=Add(g,10000,"tank",900,380);
            float a=Hit(g,e,100,"bolt"),b=Hit(g,e,100,"bolt"),c=Hit(g,e,100,"arc");
            Assert(Near(a,100)&&Near(b,100)&&Near(c,114),"trace did not distinguish weapon changes: "+a+"/"+b+"/"+c);
        });
        Test("School ranks cap at three and awaken at six independent of weapon rank", delegate {
            Game g=Fixture();Upgrade(g,"bolt",10);Assert(g.SchoolPoints("ember")==0,"weapon ranks counted as school points");
            Upgrade(g,"ember_brand",10);Upgrade(g,"ember_breach",2);Assert(g.Rank("ember_brand")==3&&!g.HasSchoolCapstone("ember"),"premature awakening or rank overflow");
            Upgrade(g,"ember_cascade",1);Assert(g.SchoolPoints("ember")==6&&g.HasSchoolCapstone("ember"),"six-point capstone not unlocked");
            foreach(string id in Lore.UpgradeIds)Upgrade(g,id,10);
            foreach(string school in new string[]{"ember","echo","forge"})Assert(g.SchoolPoints(school)==9,"school exceeds maximum ranks");
            Limits(g);
        });
        Test("Ember awakening and propagation damage nearby enemies after a burning death", delegate {
            Game g=Fixture();Upgrade(g,"ember_brand",3);Upgrade(g,"ember_breach",2);Upgrade(g,"ember_cascade",1);
            Enemy dying=Add(g,10000,"chaser",900,380),near=Add(g,10001,"tank",945,380),far=Add(g,10002,"tank",1100,380);
            Hit(g,dying,10,"bolt");Assert(dying.BurnTime>3.9f,"awakened burn duration missing");dying.Hp=1;
            float nh=near.Hp,fh=far.Hp;Hit(g,dying,10,"bolt");
            Assert(near.Hp<nh&&near.BurnTime>0&&Near(far.Hp,fh),"death burst failed nearby effect or leaked beyond radius");
        });
        Test("Echo awakening expiry pulse interrupts nearby fresh enemy", delegate {
            Game g=Fixture();Upgrade(g,"echo_decoy",3);Upgrade(g,"echo_jammer",3);
            float x=g.X;g.Step(.01f,1,0,true);Advance(g,2.02f,1,0);
            Enemy e=Add(g,10000,"shooter",x+40,380);e.Cooldown=0;g.Step(.01f,0,0,false);
            Advance(g,.12f,0,0);Assert(g.DecoyTime<=0&&e.JamTime>0&&e.AttackPhase==0,"expired awakened clone did not jam local enemy");
        });
        Test("Forge awakening safely absorbs shield-break overflow inside projectile iteration", delegate {
            Game g=Fixture();Upgrade(g,"forge_ward",3);Upgrade(g,"forge_breaker",3);
            Hostile(g,500,50);Hostile(g,500,0);Hostile(g,500,-50);g.Step(.01f,0,0,false);
            Assert(Near(g.Hp,100)&&Near(g.Ward,0)&&g.Invulnerable>=.75f,"shield-break capstone did not protect player");
            g.Step(.01f,0,0,false);foreach(Shot s in g.Shots)Assert(!s.Hostile||s.Life<=0||s.Damage<=0,"capstone left nearby dangerous projectile");
            Limits(g);
            Advance(g,.85f,0,0);g.Ward=1;Hostile(g,30,0);g.Step(.01f,0,0,false);
            Assert(g.Hp<80,"shield break incorrectly retriggered inside eight-second cooldown");
        });
        Test("Cross-school synergies change burn damage and recovery", delegate {
            Game a=Fixture(),b=Fixture();Upgrade(a,"ember_brand",2);Upgrade(b,"ember_brand",2);Upgrade(b,"echo_trace",2);
            Enemy ea=Add(a,10000,"tank",900,380),eb=Add(b,10000,"tank",900,380);
            Hit(a,ea,10,"bolt");Hit(b,eb,10,"bolt");ea.JamTime=eb.JamTime=1;
            float ha=ea.Hp,hb=eb.Hp;Advance(a,.2f,0,0);Advance(b,.2f,0,0);
            Assert((hb-eb.Hp)>(ha-ea.Hp)*1.4f,"ember/echo did not amplify burning jammed target");
            Upgrade(b,"forge_ward",2);b.Ward=0;eb.Hp=0;b.Step(.01f,0,0,false);Assert(b.Ward>=3,"burning kill did not restore ward");
            Upgrade(b,"echo_decoy",1);b.Ward=20;Hostile(b,10,0);b.Invulnerable=0;b.Step(.01f,0,0,false);float before=b.Ward;
            b.Step(.01f,1,0,true);Advance(b,.2f,1,0);Assert(b.Ward>before+1&&b.MaxWard==38,"echo/forge failed immediate clone recovery or capacity");
        });
        Test("Story unlocks only on actual boss deaths, caps chapter and resets with run", delegate {
            Game g=Fixture();g.Time=180;g.Step(.01f,0,0,false);Assert(g.StoryChapter==0&&g.CompletedBosses==0,"mere boss spawn unlocked story");
            g.Enemies.Clear();g.Mode="play";
            for(int i=1;i<=4;i++) {Enemy boss=Add(g,10000+i,"boss",900,380);boss.Hp=1;Hit(g,boss,10,"bolt");if(g.Mode=="shop")g.LeaveShop();Assert(g.CompletedBosses==i&&g.StoryChapter==Math.Min(3,i),"boss story milestone incorrect");}
            Upgrade(g,"echo_decoy",3);Upgrade(g,"forge_ward",3);g.DecoyTime=2;g.FocusTargetId=10001;
            g.Hazards.Add(new Hazard {OwnerId=10001,X=500,Y=380,R=64,Life=2});g.Start(1,0);
            Assert(g.CompletedBosses==0&&g.StoryChapter==0&&g.DecoyTime==0&&g.Ward==0&&g.Hazards.Count==0&&g.FocusTargetId==0,"new run retained combat/story effects");
            Assert(g.SchoolPoints("ember")==0&&g.SchoolPoints("forge")==0&&g.SchoolPoints("echo")==1,"new run retained ranks beyond chosen starter");
        });
        Test("Pause, level and shop freeze all damage, hazards, burn and cooldowns", delegate {
            foreach(string mode in new string[]{"pause","level","shop"}) {
                Game g=Fixture();Upgrade(g,"forge_ward",1);Enemy e=Add(g,10000,"bomber",g.X,g.Y);
                e.BurnTime=1;e.BurnDps=10;e.JamTime=1;e.AttackPhase=1;e.Windup=.01f;e.TargetX=g.X;e.TargetY=g.Y;
                g.Hazards.Add(new Hazard {OwnerId=e.Id,X=g.X,Y=g.Y,R=64,Life=1,Damage=100});Hostile(g,100,0);
                g.DecoyTime=1;g.DashCooldown=2;g.Mode=mode;
                Advance(g,1,1,1);Assert(g.Time==0&&g.Hp==100&&e.Hp==10000&&e.BurnTime==1&&e.JamTime==1&&g.DecoyTime==1&&g.Ward==14&&g.Hazards[0].Life==1&&g.DashCooldown==2,"simulation leaked while mode="+mode);
            }
        });
        Log("RESULT | passed="+passed+" failed="+failed);
        string output=args.Length>0?args[0]:"tactics-report.md";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
        File.WriteAllLines(output,lines.ToArray(),new System.Text.UTF8Encoding(false));
        return failed==0?0:1;
    }
}
