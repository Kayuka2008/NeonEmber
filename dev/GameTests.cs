using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using NeonEmber;

public static class GameTests
{
    static List<string> lines = new List<string>();
    static int passed, failed;
    static string reportPath = "game-test-report.md";
    static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    static void Log(string message) { Console.WriteLine(message); lines.Add(message); }
    static void Test(string name, Action test)
    {
        try { test(); passed++; Log("PASS | " + name); }
        catch (Exception error) { failed++; Log("FAIL | " + name + " | " + error.Message); }
    }
    static Game NewGame(int weapon, int seed)
    {
        Game game = new Game(seed); game.Start(weapon, 0); return game;
    }
    static void Advance(Game game, float seconds, float dt)
    {
        int steps = (int)Math.Ceiling(seconds / dt);
        for (int i = 0; i < steps; i++) game.Step(dt, 0, 0, false);
    }
    static int CountBosses(Game game)
    {
        int count = 0; foreach (Enemy enemy in game.Enemies) if (enemy.Kind == "boss" && enemy.Hp > 0) count++; return count;
    }
    static void Validate(Game game)
    {
        Assert(Finite(game.X) && Finite(game.Y) && Finite(game.Hp) && Finite(game.MaxHp) && Finite(game.Time) && Finite(game.Xp) && Finite(game.NextXp), "non-finite player state");
        Assert(game.Coins >= 0, "negative balance");
        Assert(game.Enemies.Count <= Game.EnemyLimit, "enemy cap exceeded: " + game.Enemies.Count);
        Assert(game.Shots.Count <= Game.ShotLimit, "shot cap exceeded: " + game.Shots.Count);
        Assert(game.Orbs.Count <= Game.OrbLimit, "orb cap exceeded: " + game.Orbs.Count);
        Assert(game.Effects.Count <= Game.EffectLimit, "effect cap exceeded: " + game.Effects.Count);
        Assert(game.Hazards.Count <= Game.HazardLimit, "hazard cap exceeded: " + game.Hazards.Count);
        int activeAttacks = 0;
        foreach (Enemy enemy in game.Enemies) if (enemy.Hp > 0 && (enemy.AttackPhase == 1 || enemy.AttackPhase == 2)) activeAttacks++;
        Assert(activeAttacks <= Game.SpecialAttackLimit, "special attack cap exceeded: " + activeAttacks);
        Assert(Finite(game.Ward) && Finite(game.PlayerVX) && Finite(game.PlayerVY) && Finite(game.DecoyTime), "non-finite world/tactics state");
        Assert(Finite(game.CircleConfidence) && game.CircleConfidence >= 0 && game.CircleConfidence <= 1 && Finite(game.ObservedTurnRate) && Finite(game.ObservedDashAge) && Finite(game.PredictedX) && Finite(game.PredictedY) && Finite(game.SquadPhaseLeft), "non-finite or unbounded adaptive AI state");
        Assert(game.Ward >= 0 && game.Ward <= game.MaxWard + .01f, "shield outside capacity");
        foreach (Hazard hazard in game.Hazards) Assert(Finite(hazard.X) && Finite(hazard.Y) && Finite(hazard.R) && Finite(hazard.Life) && Finite(hazard.X2) && Finite(hazard.Y2) && Finite(hazard.InnerR), "non-finite hazard");
        Assert(CountBosses(game) <= 1, "multiple living bosses");
        foreach (Enemy e in game.Enemies) Assert(Finite(e.X) && Finite(e.Y) && Finite(e.Hp) && Finite(e.Speed) && Finite(e.FacingX) && Finite(e.FacingY) && Finite(e.Channel) && e.Channel >= 0, "non-finite enemy or negative repair channel");
        foreach (Shot shot in game.Shots) Assert(Finite(shot.X) && Finite(shot.Y) && Finite(shot.VX) && Finite(shot.VY) && Finite(shot.Life), "non-finite projectile");
        foreach (Orb orb in game.Orbs) Assert(Finite(orb.X) && Finite(orb.Y) && Finite(orb.Value), "non-finite orb");
    }
    static int SelectUpgrade(Game game, int build)
    {
        string preferred = build == 0 ? "bolt" : build == 1 ? "arc" : "blade";
        int selected = 0, best = -100;
        for (int i = 0; i < game.Choices.Count; i++)
        {
            Upgrade choice = game.Choices[i]; string id = (choice.Id ?? "").ToLowerInvariant(); int score = 0;
            if (id.Contains(preferred)) score += 8;
            if (id.Contains("damage") || id.Contains("power") || id.Contains("rate") || id.Contains("speed")) score += 5;
            if (id.Contains("xp") || id.Contains("magnet") || id.Contains("learn")) score += 4;
            if (game.Hp < game.MaxHp * 0.55f && (id.Contains("heal") || id.Contains("hp") || id.Contains("health"))) score += 12;
            if (score > best) { best = score; selected = i; }
        }
        return selected;
    }
    static void ResolveMenus(Game game, int build)
    {
        int safety = 0;
        while (game.Mode == "level" && safety++ < 300)
        {
            Assert(game.Choices.Count > 0, "empty upgrade menu"); game.Choose(SelectUpgrade(game, build));
        }
        Assert(safety < 300, "upgrade queue never drains");
        if (game.Mode == "shop")
        {
            for (int i = 0; i < game.ShopOffers.Count; i++)
            {
                if (game.ShopOffers[i].Price <= game.Coins) { game.Buy(i); break; }
            }
            game.LeaveShop();
        }
    }
    static void MoveBot(Game game, out float mx, out float my)
    {
        // The bot knows positions, not upcoming attack state. This is only a reproducible test trajectory.
        float targetX = 640 + (float)Math.Cos(game.Time * 0.16f) * 290;
        float targetY = 380 + (float)Math.Sin(game.Time * 0.16f) * 190;
        float nearest = 240 * 240;
        foreach (Orb orb in game.Orbs)
        {
            float dx = orb.X - game.X, dy = orb.Y - game.Y, distance = dx * dx + dy * dy;
            if (distance < nearest) { nearest = distance; targetX = orb.X; targetY = orb.Y; }
        }
        mx = targetX - game.X; my = targetY - game.Y;
        float length = (float)Math.Sqrt(mx * mx + my * my);
        if (length > 1) { mx /= length; my /= length; }
        foreach (Enemy enemy in game.Enemies)
        {
            float dx = game.X - enemy.X, dy = game.Y - enemy.Y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
            if (distance > 0.1f && distance < 125) { float force = 2.2f * (125 - distance) / 125; mx += dx / distance * force; my += dy / distance * force; }
        }
        foreach (Shot shot in game.Shots)
        {
            if (!shot.Hostile) continue;
            float dx = game.X - shot.X, dy = game.Y - shot.Y, distance = (float)Math.Sqrt(dx * dx + dy * dy);
            if (distance > 0.1f && distance < 75) { mx += dx / distance; my += dy / distance; }
        }
        if (game.X < 100) mx += 2; if (game.X > 1180) mx -= 2;
        if (game.Y < 100) my += 2; if (game.Y > 660) my -= 2;
        length = (float)Math.Sqrt(mx * mx + my * my); if (length > 1) { mx /= length; my /= length; }
    }
    static float ContactDamage(float dt)
    {
        Game game = NewGame(0, 33); game.Hp = game.MaxHp = 100000; game.Enemies.Clear();
        Enemy enemy = new Enemy { Id = 10000, Kind = "chaser", X = game.X, Y = game.Y, R = 18, Hp = 100000, MaxHp = 100000, Speed = 0, Damage = 10 };
        game.Enemies.Add(enemy);
        int steps = (int)Math.Round(10 / dt);
        for (int i = 0; i < steps; i++)
        {
            game.Enemies.Clear(); game.Enemies.Add(enemy); enemy.X = game.X; enemy.Y = game.Y;
            game.Shots.Clear(); game.Step(dt, 0, 0, false);
        }
        return 100000 - game.Hp;
    }
    static void RunBot(int weapon, int seed, float targetSeconds, bool immortal)
    {
        Game game = NewGame(weapon, seed); if (immortal) game.Hp = game.MaxHp = 100000;
        List<float> intervals = new List<float>(); float lastLevelTime = 0; int oldLevel = game.Level;
        int enemyPeak = 0, shotPeak = 0, orbPeak = 0, effectPeak = 0, hazardPeak = 0, specialPeak = 0, steps = 0;
        Dictionary<int,float> bossBorn = new Dictionary<int,float>(); HashSet<int> bossDone = new HashSet<int>(); Stopwatch stopwatch = Stopwatch.StartNew();
        while (game.Time < targetSeconds && game.Mode != "over" && steps < targetSeconds * 65 + 10000)
        {
            ResolveMenus(game, weapon);
            if (game.Mode != "play") break;
            float mx, my; MoveBot(game, out mx, out my);
            bool dash = false;
            foreach (Enemy enemy in game.Enemies) { float dx = enemy.X - game.X, dy = enemy.Y - game.Y; if (dx * dx + dy * dy < 75 * 75) { dash = true; break; } }
            game.Step(1f / 60, mx, my, dash);
            int activeAttacks = 0;
            foreach(Enemy e in game.Enemies) { if(e.Kind=="boss" && !bossBorn.ContainsKey(e.Id)) bossBorn[e.Id]=game.Time; if(e.Hp>0 && (e.AttackPhase==1 || e.AttackPhase==2)) activeAttacks++; }
            hazardPeak = Math.Max(hazardPeak, game.Hazards.Count); specialPeak = Math.Max(specialPeak, activeAttacks);
            foreach(KeyValuePair<int,float> boss in bossBorn) { if(bossDone.Contains(boss.Key)) continue; bool alive=false; foreach(Enemy e in game.Enemies) if(e.Id==boss.Key && e.Hp>0) {alive=true;break;} if(!alive) {bossDone.Add(boss.Key); Log(string.Format(CultureInfo.InvariantCulture,"BOSS | weapon={0} seed={1} spawned={2:F2}s lifetime={3:F2}s",weapon,seed,boss.Value,game.Time-boss.Value));} }
            if (game.Level != oldLevel)
            {
                intervals.Add(game.Time - lastLevelTime); lastLevelTime = game.Time; oldLevel = game.Level;
            }
            enemyPeak = Math.Max(enemyPeak, game.Enemies.Count); shotPeak = Math.Max(shotPeak, game.Shots.Count);
            orbPeak = Math.Max(orbPeak, game.Orbs.Count); effectPeak = Math.Max(effectPeak, game.Effects.Count);
            steps++; if (steps % 60 == 0) Validate(game);
        }
        stopwatch.Stop(); Validate(game);
        float total = 0, largest = 0; int inTarget = 0;
        foreach (float interval in intervals) { total += interval; largest = Math.Max(largest, interval); if (interval >= 15 && interval <= 25) inTarget++; }
        intervals.Sort(); float median = intervals.Count == 0 ? 0 : intervals[intervals.Count / 2];
        Log(string.Format(CultureInfo.InvariantCulture,
            "BOT | weapon={0} seed={1} immortal={2} time={3:F1}s level={4} kills={5} hp={6:F1} upgrades={7} interval_mean={8:F2}s median={9:F2}s max={10:F2}s target15to25={11}/{12} peaks(enemy/shot/orb/effect)={13}/{14}/{15}/{16} sim_steps={17} wall={18:F2}s",
            weapon, seed, immortal, game.Time, game.Level, game.Kills, game.Hp, game.TotalUpgrades, intervals.Count == 0 ? 0 : total / intervals.Count, median, largest, inTarget, intervals.Count, enemyPeak, shotPeak, orbPeak, effectPeak, steps, stopwatch.Elapsed.TotalSeconds));
        if (immortal) Assert(game.Time >= targetSeconds, "long-run stopped at " + game.Time + " in mode " + game.Mode);
        Log("TACTICAL | weapon=" + weapon + " seed=" + seed + " hazards_peak=" + hazardPeak + " special_attacks_peak=" + specialPeak);
    }
    public static int Main(string[] args)
    {
        if (args.Length > 0) reportPath = args[0]; bool runBots = args.Length < 2 || args[1] != "quick";
        Log("# Game simulation verification");
        Log("Independent C# core tests; no renderer, input device or human gameplay assertions. Bot timing is diagnostic, not evidence of human balance. Wall time measures simulation only, not rendered FPS.");
        Test("Start/reset and three loadouts", delegate {
            for (int weapon = 0; weapon < 3; weapon++) { Game game = NewGame(weapon, 1); Assert(game.Mode == "play" && game.Hp > 0 && game.Level == 1, "invalid initial state"); game.Enemies.Add(new Enemy { Id=10000, Kind="chaser", X=game.X+70, Y=game.Y, R=18, Hp=1, MaxHp=1, Speed=0, Damage=1 }); Advance(game, 5, 1f/60); Assert(game.Kills > 0, "loadout could not kill a nearby target in 5 seconds"); game.Start(weapon, 0); Assert(game.Time == 0 && game.Level == 1 && game.Kills == 0 && game.Coins == 0, "reset retained run state"); }
        });
        Test("Upgrade resumes simulation", delegate {
            Game game = NewGame(0, 1); game.Xp = game.NextXp; game.Step(1f/60,0,0,false); Assert(game.Mode == "level" && game.Choices.Count == 3, "upgrade did not offer three cards"); float time = game.Time; game.Choose(0); Assert(game.Mode == "play", "single upgrade did not resume"); game.Step(1f/60,1,0,false); Assert(game.Time > time, "time froze after upgrade");
        });
        Test("Overflow XP queues multiple levels and invalid choice is harmless", delegate {
            Game game = NewGame(0, 2); game.Xp = 500; game.Step(1f/60,0,0,false); Assert(game.Mode == "level", "no upgrade"); int level = game.Level; game.Choose(-1); game.Choose(999); Assert(game.Mode == "level" && game.Level == level, "invalid selection mutated upgrade"); float time = game.Time; ResolveMenus(game, 0); Assert(game.Level >= 4 && game.Mode == "play", "overflow levels were lost"); Assert(game.Xp >= 0 && game.Xp < game.NextXp, "XP remainder invalid"); Assert(game.Time == time, "selection advanced combat time");
        });
        Test("Death has priority over level up", delegate {
            Game game = NewGame(0, 3); game.Hp = 0; game.Xp = game.NextXp * 5; game.Step(1f/60,0,0,false); Assert(game.Mode == "over", "dead player opened another state: " + game.Mode);
        });
        Test("Lethal contact and XP pickup in same frame prioritizes death", delegate { Game game=NewGame(0,3); game.Hp=1; game.Enemies.Add(new Enemy {Id=10000,Kind="chaser",X=game.X,Y=game.Y,R=18,Hp=1000,MaxHp=1000,Speed=0,Damage=10}); game.Orbs.Add(new Orb {X=game.X,Y=game.Y,Value=game.NextXp*3}); game.Step(1f/60,0,0,false); Assert(game.Mode=="over" && game.Hp==0,"lethal frame opened " + game.Mode); });
        Test("Dash cooldown prevents retrigger and expires", delegate {
            Game game = NewGame(0, 3); game.Step(1f/60,1,0,true); float cooldown = game.DashCooldown; Assert(cooldown > 0 && game.DashTime > 0, "dash not activated"); game.Step(1f/60,1,0,true); Assert(game.DashCooldown < cooldown, "cooldown reset from repeated input"); game.Enemies.Clear(); Advance(game, cooldown + 0.1f, 1f/60); Assert(game.DashCooldown <= 0.01f, "cooldown never expired");
        });
        Test("Contact damage is substantial and approximately FPS independent", delegate {
            float a = ContactDamage(1f/30), b = ContactDamage(1f/60), c = ContactDamage(1f/144); Log("CONTACT | 30fps=" + a + " 60fps=" + b + " 144fps=" + c); Assert(Math.Min(a, Math.Min(b,c)) >= 80, "contact damage still scaled by dt"); Assert(Math.Max(a, Math.Max(b,c)) - Math.Min(a, Math.Min(b,c)) <= 20, "contact damage varies by >2 hits across FPS");
        });
        Test("AOE warning delays damage and fixed target can be dodged", delegate {
            for (int dodge=0;dodge<2;dodge++) { Game game=NewGame(0,20); Enemy bomber=new Enemy {Id=10000,Kind="bomber",X=100,Y=160,R=17,Hp=100000,MaxHp=100000,Damage=20,AttackPhase=1,Windup=0.6f,TargetX=game.X,TargetY=game.Y}; game.Enemies.Add(bomber); Advance(game,0.4f,0.01f); Assert(game.Hp==100,"AOE hurt before warning expired"); if(dodge==1) game.X+=200; Advance(game,0.3f,0.01f); Assert(dodge==1 ? game.Hp==100 : game.Hp<100,"AOE fixed target behavior incorrect"); }
        });
        Test("Shop balance, insufficient funds, and frozen combat clock", delegate {
            Game game = NewGame(0, 4); game.Time = 89.99f; game.Step(0.02f,0,0,false); Assert(game.Mode == "shop", "90 second shop missing"); float time = game.Time, cd = game.DashCooldown; game.Step(1,1,0,true); Assert(game.Time == time && game.DashCooldown == cd, "shop advanced combat"); Assert(game.ShopOffers.Count > 0, "shop has no offers");
            game.Hp=50; int freeCount=game.ShopOffers.Count; game.Buy(0); Assert(game.Hp==75 && game.Coins==0 && game.ShopOffers.Count==freeCount-1,"free heal incorrect or repeatable"); int paid = -1; for (int i=0;i<game.ShopOffers.Count;i++) if (game.ShopOffers[i].Price>0) {paid=i;break;} Assert(paid >= 0, "no paid item to test"); game.Coins = 0; game.Buy(paid); Assert(game.Coins == 0, "insufficient funds deducted"); int price = game.ShopOffers[paid].Price; game.Coins = price; game.Buy(paid); Assert(game.Coins >= 0 && game.Coins < price, "purchase did not charge correctly"); game.Buy(-1); game.Buy(999); game.LeaveShop(); Assert(game.Mode == "play", "shop did not resume");
        });
        Test("Boss schedule and no stacked bosses", delegate {
            Game game = NewGame(0, 7); game.Hp = game.MaxHp = 100000; game.Time = 179.99f; game.Step(0.02f,0,0,false); if (game.Mode=="shop") { game.LeaveShop(); game.Step(0.02f,0,0,false); } Assert(CountBosses(game)==1, "first boss missing"); foreach(Enemy enemy in game.Enemies) if(enemy.Kind=="boss") enemy.Hp=enemy.MaxHp=100000;
            game.Time=359.99f; game.Step(0.02f,0,0,false); if(game.Mode=="shop") {game.LeaveShop();game.Step(0.02f,0,0,false);} Assert(CountBosses(game)==1, "second boss stacked while first alive");
        });
        Test("Boss overlap defers shop until boss dies", delegate {
            Game game=NewGame(0,25); game.Hp=game.MaxHp=100000; game.Time=179.99f; game.Step(0.02f,0,0,false); Assert(game.Mode=="play" && CountBosses(game)==1,"shop interrupted boss arrival"); foreach(Enemy enemy in game.Enemies) if(enemy.Kind=="boss") enemy.Hp=0; game.Step(0.02f,0,0,false); Assert(game.Mode=="shop","deferred shop was lost after boss death"); game.LeaveShop(); Assert(game.Mode=="play","boss shop failed to resume");
        });
        Test("Orb capacity merges value rather than dropping reward", delegate { Game game=NewGame(0,19); for(int i=0;i<Game.OrbLimit;i++) game.Orbs.Add(new Orb {X=100,Y=150,Value=1}); game.Enemies.Add(new Enemy {Id=10000,Kind="chaser",X=200,Y=200,R=18,Hp=0,MaxHp=10}); game.Step(1f/60,0,0,false); float value=game.Xp; foreach(Orb orb in game.Orbs) value+=orb.Value; Assert(game.Orbs.Count==Game.OrbLimit && value>Game.OrbLimit,"orb cap discarded XP"); });
        Test("Upgrade card pool remains usable after extensive progression", delegate {
            Game game = NewGame(0, 8); for (int i=0;i<150;i++) { game.Xp = game.NextXp; game.Step(1f/60,0,0,false); Assert(game.Mode=="level" && game.Choices.Count==3,"depleted upgrade pool at iteration " + i); game.Choose(i%3); } Validate(game);
        });
        if (runBots) for (int weapon=0;weapon<3;weapon++) { int captured=weapon; Test("Normal-health diagnostic bot weapon " + weapon, delegate { RunBot(captured, 41+captured, 420, false); }); }
        if (runBots) Test("900-second long run finite values and entity limits", delegate { RunBot(1, 198, 900, true); });
        if (runBots) for (int sample=0;sample<9;sample++) { int captured=sample; Test("Final multiseed 900-second run " + sample, delegate { RunBot(captured%3, 210+captured, 900, true); }); }
        Log("RESULT | passed=" + passed + " failed=" + failed);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath))); File.WriteAllLines(reportPath, lines.ToArray(), new System.Text.UTF8Encoding(false));
        return failed == 0 ? 0 : 1;
    }
}





