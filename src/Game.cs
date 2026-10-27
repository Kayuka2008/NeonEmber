using System;
using System.Collections.Generic;

namespace NeonEmber
{
    public partial class Enemy
    {
        public int Id, AttackPhase;
        public string Kind = "chaser";
        public float X, Y, R, Hp, MaxHp, VX, VY, Timer, Windup, TargetX, TargetY;
        public float Cooldown, Speed, Damage, Life, ContactCooldown;
    }
    public class Shot
    {
        public float X, Y, VX, VY, R, Damage, Life;
        public bool Hostile;
        public string Kind = "bolt";
        public int Pierce, LastHit;
        public HashSet<int> HitIds;
    }
    public class Orb { public float X, Y, Value, R = 4; }
    public class Effect
    {
        public float X, Y, X2, Y2, R, Life, MaxLife;
        public string Kind;
    }
    public class Upgrade
    {
        public string Id, Name, Description, Tag, School, Flavor;
        public int Price;
    }
    public partial class Game
    {
        public const int EnemyLimit = 360, ShotLimit = 900, OrbLimit = 280, EffectLimit = 220;
        public string Mode = "menu", Phase = "蓄势", Announcement = "";
        public float X = 640, Y = 380, Hp = 100, MaxHp = 100, Time, Xp, NextXp = 18;
        public int Level = 1, Kills, Coins, Stage = 1, Difficulty, TotalUpgrades, BestCombo, Rerolls = 3;
        public float DashCooldown, DashTime, Invulnerable, AimX = 1, AimY, AnnouncementTime;
        public List<Enemy> Enemies = new List<Enemy>();
        public List<Shot> Shots = new List<Shot>();
        public List<Orb> Orbs = new List<Orb>();
        public List<Effect> Effects = new List<Effect>();
        public List<Upgrade> Choices = new List<Upgrade>(), ShopOffers = new List<Upgrade>();
        public Dictionary<string, int> Ranks = new Dictionary<string, int>();
        public List<string> Events = new List<string>();
        private Random random;
        public Game(int seed) { random = new Random(seed); }

        private const float Left = 24, Right = 1256, Top = 104, Bottom = 640;
        private const int GridSize = 64, GridColumns = 21, GridRows = 12;
        private readonly List<Enemy>[] grid = new List<Enemy>[GridColumns * GridRows];
        private readonly HashSet<int> chainHits = new HashSet<int>();
        private float spawnClock, boltClock, arcClock, bladeClock, bladeAngle, dashX = 1, dashY;
        private float nextShop = 90, nextBoss = 180, phaseClock, idleXpClock;
        private int nextId, pendingLevels, lastPhase = -1, bossCycle;
        private bool pendingShop;

        public int Rank(string id) { int value; return Ranks.TryGetValue(id, out value) ? value : 0; }
        public float BladeAngle { get { return bladeAngle; } }
        public float PowerMultiplier { get { return 1 + Rank("power") * 0.16f + Rank("overclock") * .08f; } }
        public float FireRateMultiplier { get { return 1 + Rank("haste") * 0.12f; } }
        public float PickupRadius { get { return 58 + Rank("magnet") * 24; } }

        public void Start(int weapon, int difficulty)
        {
            Mode = "play"; Difficulty = difficulty == 1 ? 1 : 0;
            X = 640; Y = 380; Hp = MaxHp = 100; Time = Xp = 0; NextXp = 18;
            Level = Stage = 1; Kills = Coins = TotalUpgrades = BestCombo = 0; Rerolls = 3;
            DashCooldown = DashTime = Invulnerable = 0; AimX = dashX = 1; AimY = dashY = 0;
            Enemies.Clear(); Shots.Clear(); Orbs.Clear(); Effects.Clear(); Choices.Clear();
            ShopOffers.Clear(); Events.Clear(); Ranks.Clear();
            Ranks[weapon == 1 ? "arc" : weapon == 2 ? "blade" : "bolt"] = 1;
            spawnClock = 0.6f; boltClock = arcClock = bladeClock = 0;
            bladeAngle = phaseClock = idleXpClock = 0; nextShop = 90; nextBoss = 180;
            nextId = pendingLevels = bossCycle = 0; lastPhase = -1; pendingShop = false;
            ResetTactics(); ResetWorld();
            Phase = "蓄势"; Say("无尽协议启动 · 保持移动", 4);
        }

        public void Step(float dt, float mx, float my, bool dash)
        {
            if (Mode != "play") return;
            if (Hp <= 0) { Die(); return; }
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0) return;
            dt = Math.Min(dt, 0.05f);
            if (float.IsNaN(mx) || float.IsInfinity(mx)) mx = 0;
            if (float.IsNaN(my) || float.IsInfinity(my)) my = 0;
            ResolveXp();
            if (pendingLevels > 0) { ContinueState(); return; }
            Time += dt; Stage = 1 + (int)(Time / 90);
            DashCooldown = Math.Max(0, DashCooldown - dt);
            Invulnerable = Math.Max(0, Invulnerable - dt);
            AnnouncementTime = Math.Max(0, AnnouncementTime - dt);
            float length = (float)Math.Sqrt(mx * mx + my * my);
            if (length > 1) { mx /= length; my /= length; }
            if (length > 0.05f) { dashX = mx / Math.Max(0.001f, length > 1 ? 1 : length); dashY = my / Math.Max(0.001f, length > 1 ? 1 : length); }
            float previousX = X, previousY = Y;
            bool dashedNow = dash && DashCooldown <= 0;
            if (dashedNow)
            {
                DashTime = 0.20f; DashCooldown = Math.Max(1.5f, 4.4f - Rank("dash") * 0.42f);
                Invulnerable = Math.Max(Invulnerable, 0.30f); Event("dash");
                AddEffect("dash", X, Y, X - dashX * 40, Y - dashY * 40, 24, 0.3f);
            }
            if (DashTime > 0)
            {
                float dashStep = Math.Min(DashTime, dt); DashTime = Math.Max(0, DashTime - dt);
                X += dashX * 760 * dashStep; Y += dashY * 760 * dashStep;
            }
            else { float speed = 220 + Rank("speed") * 16; X += mx * speed * dt; Y += my * speed * dt; }
            X = Clamp(X, Left + 13, Right - 13); Y = Clamp(Y, Top + 13, Bottom - 13);
            ObserveMovement(dt, previousX, previousY);
            UpdateWorld(dt, dashedNow);
            UpdateDirector(dt);
            UpdateTactics(dt);
            UpdateEnemies(dt);
            UpdateHazards(dt);
            if (Mode == "over") return;
            BuildGrid();
            UpdateWeapons(dt);
            UpdateShots(dt);
            if (Mode == "over") return;
            CollectDead();
            UpdateOrbs(dt);
            for (int i = Effects.Count - 1; i >= 0; --i) { Effects[i].Life -= dt; if (Effects[i].Life <= 0) RemoveFast(Effects, i); }
            // Ambient trickle prevents an unlucky scattered drop field from creating a dead progression period.
            idleXpClock += dt;
            if (idleXpClock >= 12) { idleXpClock -= 12; GainXp(2 + Time / 120); }
            ContinueState();
        }

        private void UpdateDirector(float dt)
        {
            phaseClock = Time % 30;
            int phase = phaseClock < 18 ? 0 : phaseClock < 25 ? 1 : 2;
            Phase = phase == 0 ? "游猎" : phase == 1 ? "怪潮" : "喘息";
            if (phase != lastPhase)
            {
                lastPhase = phase;
                if (phase == 1) { Say("怪潮来袭 · 寻找突破口", 2.8f); Event("wave"); }
                else if (phase == 2) { Say("短暂喘息 · 回收经验", 2.5f); }
            }
            if (Time >= nextShop) { nextShop += 90; pendingShop = true; }
            if (Time >= nextBoss)
            {
                nextBoss += 180; bossCycle++;
                bool exists = false;
                for (int i = 0; i < Enemies.Count; i++) if (Enemies[i].Kind == "boss" && Enemies[i].Hp > 0) exists = true;
                if (!exists) { Spawn("boss"); Say("警报 · 监察者降临", 4); Event("boss"); }
            }
            float pressure = 1 + Math.Min(Time / 125, 5.8f);
            float phaseRate = phase == 0 ? 1.1f : phase == 1 ? 2.8f : 0.20f;
            float rate = pressure * phaseRate * (Difficulty == 1 ? 1.25f : 1);
            if (Time >= nextSquad && phase != 2)
            {
                nextSquad = Time + (Time >= 180 ? 21 : 30); SpawnSquad();
                Say("中继编队 · 击破信标可打断合围", 3.5f); Event("wave");
            }
            if (Time >= nextElite && phase != 2)
            {
                nextElite = Time + (Time >= 180 ? 24 : 40);
                int elites = 0;
                for (int i = 0; i < Enemies.Count; i++) if (Enemies[i].Elite && Enemies[i].Hp > 0) elites++;
                if (elites < 8) { Spawn("prism"); if (Time >= 90) Spawn("repairer"); }
            }
            spawnClock -= dt;
            int budget = 6;
            while (spawnClock <= 0 && budget-- > 0)
            {
                spawnClock += 1 / rate;
                double roll = random.NextDouble();
                string kind = "chaser";
                if (Time > 18 && roll < .18) kind = "runner";
                else if (Time > 35 && roll < .34) kind = "shooter";
                else if (Time > 55 && roll < .46) kind = "bomber";
                else if (Time > 25 && roll > .89) kind = "tank";
                Spawn(kind);
            }
        }

        private void Spawn(string kind)
        {
            if (Enemies.Count >= EnemyLimit)
            {
                if (kind != "boss") return;
                int remove = -1;
                for (int i = 0; i < Enemies.Count; i++) if (Enemies[i].Kind != "boss") { remove = i; break; }
                if (remove < 0) return;
                RemoveFast(Enemies, remove);
            }
            float sx = 40, sy = 120;
            for (int tries = 0; tries < 12; tries++)
            {
                int edge = random.Next(4);
                sx = edge == 0 ? Left + 18 : edge == 1 ? Right - 18 : Left + 18 + (float)random.NextDouble() * (Right - Left - 36);
                sy = edge == 2 ? Top + 18 : edge == 3 ? Bottom - 18 : Top + 18 + (float)random.NextDouble() * (Bottom - Top - 36);
                if (DistanceSquared(sx, sy, X, Y) > 260 * 260) break;
            }
            // Bounded scaling: late pressure comes from additional roles and attack patterns.
            float scaling = Math.Min(16, (float)Math.Pow(1 + Time / 130, 1.38)) * (Difficulty == 1 ? 1.2f : 1);
            float radius = kind == "tank" ? 22 : kind == "prism" ? 24 : kind == "boss" ? 38 : kind == "relay" ? 19 : kind == "repairer" ? 17 : kind == "runner" ? 10 : kind == "bomber" ? 17 : 13;
            float health = kind == "boss" ? 1600 * (1 + Math.Min(bossCycle, 8) * 0.55f) : kind == "prism" ? 85 : kind == "repairer" ? 48 : kind == "tank" ? 56 : kind == "relay" ? 62 : kind == "bomber" ? 24 : kind == "shooter" ? 19 : kind == "runner" ? 12 : 18;
            float speed = kind == "runner" ? 112 : kind == "tank" ? 86 : kind == "prism" ? 112 : kind == "repairer" ? 102 : kind == "boss" ? 60 : kind == "relay" ? 98 : kind == "shooter" ? 88 : kind == "bomber" ? 83 : 78;
            Enemy enemy = new Enemy();
            enemy.Id = ++nextId; enemy.Kind = kind; enemy.X = sx; enemy.Y = sy; enemy.R = radius;
            enemy.Hp = enemy.MaxHp = health * (kind == "boss" ? Math.Min(3.2f, (float)Math.Pow(1 + Time / 600, 0.6)) : scaling);
            enemy.Speed = speed * (1 + Math.Min(Time / 1800, 0.32f));
            enemy.Damage = (kind == "tank" || kind == "boss" || kind == "prism" ? 20 : 12) * (1 + Math.Min(Time / 650, 2.5f)) * (Difficulty == 1 ? 1.2f : 1);
            enemy.Elite = kind == "prism" || kind == "repairer";
            float facing = (float)Math.Atan2(Y - sy, X - sx); enemy.FacingX = (float)Math.Cos(facing); enemy.FacingY = (float)Math.Sin(facing);
            enemy.Cooldown = 0.7f + (float)random.NextDouble() * 1.5f;
            if (enemy.Elite) enemy.Cooldown = .12f;
            enemy.Timer = 0.55f; enemy.AttackPhase = -1;
            Enemies.Add(enemy); AddEffect("spawn", sx, sy, 0, 0, radius + 8, 0.55f);
        }

        private void UpdateEnemies(float dt) { UpdateTacticalEnemies(dt); }

        private void BuildGrid()
        {
            for (int i = 0; i < grid.Length; i++) if (grid[i] != null) grid[i].Clear();
            for (int i = 0; i < Enemies.Count; i++)
            {
                Enemy e = Enemies[i]; if (e.Hp <= 0 || e.AttackPhase == -1) continue;
                int cell = Cell(e.X, e.Y); if (grid[cell] == null) grid[cell] = new List<Enemy>(8); grid[cell].Add(e);
            }
        }

        private Enemy Nearest(float x, float y, float radius, HashSet<int> exclude)
        {
            Enemy best = null; float bestDistance = radius * radius;
            Enemy focused = FindEnemy(FocusTargetId);
            if (focused != null && focused.Hp > 0 && focused.AttackPhase != -1 && (exclude == null || !exclude.Contains(focused.Id)) && DistanceSquared(x, y, focused.X, focused.Y) < bestDistance) return focused;
            int minX = Math.Max(0, (int)((x - radius) / GridSize)), maxX = Math.Min(GridColumns - 1, (int)((x + radius) / GridSize));
            int minY = Math.Max(0, (int)((y - radius) / GridSize)), maxY = Math.Min(GridRows - 1, (int)((y + radius) / GridSize));
            for (int gy = minY; gy <= maxY; gy++) for (int gx = minX; gx <= maxX; gx++)
            {
                List<Enemy> bucket = grid[gy * GridColumns + gx]; if (bucket == null) continue;
                for (int i = 0; i < bucket.Count; i++)
                {
                    Enemy e = bucket[i]; if (e.Hp <= 0 || (exclude != null && exclude.Contains(e.Id))) continue;
                    float distance = DistanceSquared(x, y, e.X, e.Y);
                    if (distance < bestDistance) { bestDistance = distance; best = e; }
                }
            }
            return best;
        }

        private void UpdateWeapons(float dt)
        {
            boltClock -= dt; arcClock -= dt; bladeClock -= dt; bladeAngle += dt * (2.2f + Rank("haste") * .12f);
            Enemy aim = Nearest(X, Y, 700, null);
            if (aim != null)
            {
                float dx = aim.X - X, dy = aim.Y - Y, d = (float)Math.Sqrt(dx * dx + dy * dy);
                AimX = dx / Math.Max(d, .001f); AimY = dy / Math.Max(d, .001f);
            }
            int bolt = Rank("bolt");
            if (bolt > 0 && boltClock <= 0 && aim != null)
            {
                boltClock = .62f / FireRateMultiplier / (1 + bolt * .08f);
                int count = bolt >= 5 ? 5 : bolt >= 3 ? 3 : 1;
                float damage = (20 + bolt * 6) * PowerMultiplier;
                float angle = (float)Math.Atan2(AimY, AimX);
                for (int j = 0; j < count; j++) AddShot(X + AimX * 16, Y + AimY * 16, angle + (j - (count - 1) / 2f) * .115f, 680, damage, false, bolt >= 5 ? 3 : bolt >= 2 ? 1 : 0, bolt >= 5 ? "nova" : "bolt");
                Event("shoot");
            }
            int arc = Rank("arc");
            if (arc > 0 && arcClock <= 0)
            {
                Enemy target = Nearest(X, Y, 330 + arc * 24, null);
                if (target != null)
                {
                    arcClock = 1.2f / FireRateMultiplier; chainHits.Clear();
                    int chains = arc >= 5 ? 9 : 2 + arc;
                    float sx = X, sy = Y;
                    for (int j = 0; j < chains && target != null; j++)
                    {
                        chainHits.Add(target.Id); AddEffect("arc", sx, sy, target.X, target.Y, arc >= 5 ? 5 : 3, .16f);
                        DamageEnemy(target, (19 + arc * 7) * PowerMultiplier * (arc >= 5 ? 1 : (float)Math.Pow(.9, j)), "arc");
                        sx = target.X; sy = target.Y; target = Nearest(sx, sy, 170 + arc * 13, chainHits);
                    }
                    Event("arc");
                }
            }
            int blade = Rank("blade");
            if (blade > 0 && bladeClock <= 0)
            {
                bladeClock = .14f / FireRateMultiplier;
                int count = blade >= 5 ? 6 : 1 + blade;
                float orbit = 63 + blade * 6;
                for (int j = 0; j < count; j++)
                {
                    float angle = bladeAngle + j * (float)Math.PI * 2 / count;
                    float bx = X + (float)Math.Cos(angle) * orbit, by = Y + (float)Math.Sin(angle) * orbit;
                    DamageArea(bx, by, blade >= 5 ? 27 : 20, (8 + blade * 3) * PowerMultiplier, null);
                    if (blade >= 5)
                    {
                        for (int k = Shots.Count - 1; k >= 0; k--)
                            if (Shots[k].Hostile && DistanceSquared(bx, by, Shots[k].X, Shots[k].Y) < 29 * 29) RemoveFast(Shots, k);
                    }
                }
            }
        }

        private void DamageArea(float x, float y, float radius, float damage, Enemy excluded)
        {
            int minX = Math.Max(0, (int)((x - radius - 40) / GridSize)), maxX = Math.Min(GridColumns - 1, (int)((x + radius + 40) / GridSize));
            int minY = Math.Max(0, (int)((y - radius - 40) / GridSize)), maxY = Math.Min(GridRows - 1, (int)((y + radius + 40) / GridSize));
            for (int gy = minY; gy <= maxY; gy++) for (int gx = minX; gx <= maxX; gx++)
            {
                List<Enemy> bucket = grid[gy * GridColumns + gx]; if (bucket == null) continue;
                for (int i = 0; i < bucket.Count; i++)
                {
                    Enemy e = bucket[i]; if (e == excluded || e.Hp <= 0) continue;
                    if (DistanceSquared(x, y, e.X, e.Y) < (radius + e.R) * (radius + e.R)) DamageEnemy(e, damage, "blade", x, y);
                }
            }
        }

        private void AddShot(float x, float y, float angle, float speed, float damage, bool hostile, int pierce, string kind)
        {
            if (Shots.Count >= ShotLimit) return;
            Shot s = new Shot(); s.X = x; s.Y = y; s.VX = (float)Math.Cos(angle) * speed; s.VY = (float)Math.Sin(angle) * speed;
            s.R = hostile ? 6 : kind == "nova" ? 5 : 4; s.Damage = damage; s.Hostile = hostile;
            s.Pierce = pierce; s.Kind = kind; s.Life = hostile ? 6 : 2;
            Shots.Add(s);
        }

        private void UpdateShots(float dt)
        {
            for (int i = Shots.Count - 1; i >= 0; --i)
            {
                Shot s = Shots[i]; s.X += s.VX * dt; s.Y += s.VY * dt; s.Life -= dt;
                bool remove = s.Life <= 0 || s.X < Left - 20 || s.X > Right + 20 || s.Y < Top - 20 || s.Y > Bottom + 20;
                if (!remove && s.Hostile)
                {
                    if (DistanceSquared(s.X, s.Y, X, Y) < (s.R + 11) * (s.R + 11)) { Hurt(s.Damage); remove = true; }
                }
                else if (!remove)
                {
                    int minX = Math.Max(0, (int)((s.X - 45) / GridSize)), maxX = Math.Min(GridColumns - 1, (int)((s.X + 45) / GridSize));
                    int minY = Math.Max(0, (int)((s.Y - 45) / GridSize)), maxY = Math.Min(GridRows - 1, (int)((s.Y + 45) / GridSize));
                    for (int gy = minY; gy <= maxY && !remove; gy++) for (int gx = minX; gx <= maxX && !remove; gx++)
                    {
                        List<Enemy> bucket = grid[gy * GridColumns + gx]; if (bucket == null) continue;
                        for (int j = 0; j < bucket.Count && !remove; j++)
                        {
                            Enemy e = bucket[j]; if (e.Hp <= 0 || (s.HitIds != null && s.HitIds.Contains(e.Id))) continue;
                            if (DistanceSquared(s.X, s.Y, e.X, e.Y) < (s.R + e.R) * (s.R + e.R))
                            {
                                DamageEnemy(e, s.Damage, s.Kind, Math.Abs(s.VX) + Math.Abs(s.VY) > .01f ? e.X - s.VX : X, Math.Abs(s.VX) + Math.Abs(s.VY) > .01f ? e.Y - s.VY : Y);
                                if (s.Pierce <= 0) remove = true;
                                else { s.Pierce--; if (s.HitIds == null) s.HitIds = new HashSet<int>(); s.HitIds.Add(e.Id); }
                            }
                        }
                    }
                }
                if (remove) RemoveFast(Shots, i);
                if (Mode == "over") return;
            }
        }

        private void DamageEnemy(Enemy e, float damage)
        { DamageEnemy(e, damage, "bolt"); }

        private void DamageEnemy(Enemy e, float damage, string weapon)
        { DamageEnemy(e, damage, weapon, X, Y); }

        private void DamageEnemy(Enemy e, float damage, string weapon, float sourceX, float sourceY)
        {
            if (e.Hp <= 0) return;
            damage = ApplyWorldDamage(e, damage, weapon);
            if (random.NextDouble() < Rank("crit") * .065f) damage *= 2;
            if (IsProtected(e)) damage *= .52f;
            damage = ApplyTacticalDamage(e, damage, sourceX, sourceY);
            e.Hp -= damage; AddEffect("hit", e.X, e.Y, 0, 0, e.R * .5f + 5, .10f); Event("hit");
            if (e.Hp <= 0 && e.Kind == "relay") DisruptRelay(e);
        }

        private void CollectDead()
        {
            for (int i = Enemies.Count - 1; i >= 0; --i)
            {
                Enemy e = Enemies[i]; if (e.Hp > 0) continue;
                if (e.Kind == "relay") DisruptRelay(e);
                CancelEnemyWarning(e.Id);
                if (FocusTargetId == e.Id) FocusTargetId = 0;
                Kills++; Coins += e.Kind == "boss" ? 60 : e.Kind == "relay" ? 8 : e.Elite ? 6 : e.Kind == "tank" ? 3 : 1;
                float value = (e.Kind == "boss" ? 42 : e.Kind == "relay" ? 8 : e.Elite ? 5 : e.Kind == "tank" ? 3 : e.Kind == "bomber" ? 2 : 1) * (1 + Time / 110);
                if (e.Kind == "boss")
                {
                    Rerolls = Math.Min(9, Rerolls + 1); Say("监察者击破 · +60 零件 · +1 重掷", 4); Event("bosskill");
                    Hp = Math.Min(MaxHp, Hp + MaxHp * .2f);
                }
                OnWorldEnemyDeath(e);
                AddOrb(e.X, e.Y, value); AddEffect("death", e.X, e.Y, 0, 0, e.R + 15, .3f); RemoveFast(Enemies, i);
            }
        }

        private void AddOrb(float x, float y, float value)
        {
            if (Orbs.Count >= OrbLimit)
            {
                int nearest = 0; float distance = float.MaxValue;
                for (int i = 0; i < Orbs.Count; i++) { float d = DistanceSquared(x, y, Orbs[i].X, Orbs[i].Y); if (d < distance) { distance = d; nearest = i; } }
                Orbs[nearest].Value += value; Orbs[nearest].R = Math.Min(9, 4 + (float)Math.Sqrt(Orbs[nearest].Value) * .6f); return;
            }
            Orb orb = new Orb(); orb.X = x; orb.Y = y; orb.Value = value; Orbs.Add(orb);
        }

        private void UpdateOrbs(float dt)
        {
            float radius = PickupRadius + (Phase == "喘息" ? 90 : 0);
            for (int i = Orbs.Count - 1; i >= 0; --i)
            {
                Orb o = Orbs[i]; float dx = X - o.X, dy = Y - o.Y, d2 = dx * dx + dy * dy;
                if (d2 < 22 * 22) { GainXp(o.Value); RemoveFast(Orbs, i); Event("pickup"); }
                else if (d2 < radius * radius)
                {
                    float d = (float)Math.Sqrt(d2), speed = 340 + (radius - d) * 3;
                    float step = Math.Min(d, speed * dt); o.X += dx / d * step; o.Y += dy / d * step;
                }
            }
        }

        private void Hurt(float damage)
        {
            if (Invulnerable > 0 || Mode != "play") return;
            Hp = Math.Max(0, Hp - AbsorbWorldDamage(damage * (1 - Math.Min(.42f, Rank("guard") * .06f))));
            Invulnerable = Math.Max(Invulnerable, .6f); AddEffect("hurt", X, Y, 0, 0, 30, .24f); Event("hurt");
            if (Hp <= 0) Die();
        }

        private void Die() { Hp = 0; Mode = "over"; Choices.Clear(); pendingLevels = 0; Event("death"); }
        private void GainXp(float value) { Xp += value * (1 + Rank("scholar") * .18f); ResolveXp(); }
        private void ResolveXp()
        {
            if (float.IsNaN(Xp) || float.IsInfinity(Xp)) Xp = 0;
            int budget = 200;
            while (Xp >= NextXp && budget-- > 0)
            {
                Xp -= NextXp; Level++; pendingLevels++;
                float growth = Level - 1, mature = Math.Max(0, growth - 8);
                NextXp = 18 + growth * 7 + mature * mature * 1.5f;
            }
        }

        private void ContinueState()
        {
            if (Mode == "over" || Hp <= 0) { if (Mode != "over") Die(); return; }
            if (pendingLevels > 0)
            {
                Mode = "level"; RollChoices(); Event("level");
            }
            else if (pendingShop && !HasLivingBoss()) { OpenShop(); }
            else Mode = "play";
        }

        public void Choose(int index)
        {
            if (Mode != "level" || index < 0 || index >= Choices.Count || Hp <= 0) { if (Hp <= 0 && Mode != "menu") Die(); return; }
            Apply(Choices[index].Id); pendingLevels = Math.Max(0, pendingLevels - 1); Choices.Clear(); ContinueState();
        }

        public void Reroll()
        {
            if (Mode != "level" || Rerolls <= 0 || Hp <= 0) return;
            Rerolls--; RollChoices(); Event("reroll");
        }

        public void Buy(int index)
        {
            if (Mode != "shop" || Hp <= 0 || index < 0 || index >= ShopOffers.Count) return;
            Upgrade offer = ShopOffers[index]; if (offer.Price < 0 || Coins < offer.Price) return;
            Coins -= offer.Price; Apply(offer.Id); ShopOffers.RemoveAt(index); Event("buy");
        }

        public void LeaveShop()
        {
            if (Mode != "shop") return;
            ShopOffers.Clear(); Say("补给完成 · 继续突围", 2.5f); ContinueState();
        }

        private void OpenShop()
        {
            pendingShop = false; Mode = "shop"; ShopOffers.Clear();
            Upgrade repair = MakeUpgrade("supply"); repair.Price = 0; ShopOffers.Add(repair);
            List<string> ids = AvailableUpgrades();
            for (int i = 0; i < 2 && ids.Count > 0; i++)
            {
                int pick = random.Next(ids.Count); Upgrade offer = MakeUpgrade(ids[pick]);
                offer.Price = 38 + Rank(offer.Id) * 16 + Stage * 3; ShopOffers.Add(offer); ids.RemoveAt(pick);
            }
            Say("补给窗口 · 战场已暂停", 4); Event("shop");
        }

        private void RollChoices()
        {
            Choices.Clear(); List<string> ids = AvailableUpgrades();
            // Every draft keeps a weapon route available until all three routes are evolved.
            List<string> weapons = new List<string>();
            string[] weaponIds = { "bolt", "arc", "blade" };
            for (int i = 0; i < weaponIds.Length; i++)
            {
                string weapon = weaponIds[i]; if (Rank(weapon) >= 5) continue;
                weapons.Add(weapon);
                if (Rank(weapon) > 0) { weapons.Add(weapon); weapons.Add(weapon); }
            }
            if (weapons.Count > 0)
            {
                string picked = weapons[random.Next(weapons.Count)]; Choices.Add(MakeUpgrade(picked)); ids.Remove(picked);
            }
            // Keep a world-linked build decision in each draft, weighted toward prior investment.
            List<string> schoolChoices = new List<string>();
            for (int i = 0; i < ids.Count; i++)
            {
                if (!Lore.IsSchoolUpgrade(ids[i])) continue;
                schoolChoices.Add(ids[i]);
                if (SchoolPoints(Lore.SchoolOf(ids[i])) > 0) { schoolChoices.Add(ids[i]); schoolChoices.Add(ids[i]); }
            }
            if (schoolChoices.Count > 0 && Choices.Count < 3)
            {
                string picked = schoolChoices[random.Next(schoolChoices.Count)]; Choices.Add(MakeUpgrade(picked)); ids.Remove(picked);
            }
            while (Choices.Count < 3 && ids.Count > 0)
            {
                int pick = random.Next(ids.Count); Choices.Add(MakeUpgrade(ids[pick])); ids.RemoveAt(pick);
            }
        }

        private List<string> AvailableUpgrades()
        {
            string[] all = { "bolt", "arc", "blade", "power", "haste", "vitality", "speed", "magnet", "scholar", "crit", "guard", "dash" };
            List<string> result = new List<string>();
            for (int i = 0; i < all.Length; i++) if (Rank(all[i]) < Cap(all[i])) result.Add(all[i]);
            for (int i = 0; i < Lore.UpgradeIds.Length; i++)
            {
                string id = Lore.UpgradeIds[i];
                if (id == "ember_cascade" && Rank("ember_brand") == 0) continue;
                if (id == "echo_trace" && (Rank("bolt") > 0 ? 1 : 0) + (Rank("arc") > 0 ? 1 : 0) + (Rank("blade") > 0 ? 1 : 0) < 2) continue;
                if (Rank(id) < Cap(id)) result.Add(id);
            }
            // At full build, level rewards continue to offer distinct useful infinite-run decisions.
            if (result.Count < 3) result.Add("overclock");
            if (result.Count < 3) result.Add("reserve");
            if (result.Count < 3) result.Add("repair");
            return result;
        }

        private static int Cap(string id)
        {
            if (Lore.IsSchoolUpgrade(id)) return 3;
            if (id == "bolt" || id == "arc" || id == "blade") return 5;
            if (id == "power" || id == "haste" || id == "vitality") return 8;
            if (id == "speed" || id == "scholar" || id == "crit") return 6;
            if (id == "magnet" || id == "dash") return 5;
            return 7;
        }

        private Upgrade MakeUpgrade(string id)
        {
            Upgrade worldUpgrade = MakeWorldUpgrade(id); if (worldUpgrade != null) return worldUpgrade;
            Upgrade u = new Upgrade(); u.Id = id; int rank = Rank(id); u.Tag = "信使改装"; u.School = Lore.SchoolOf(id); u.Flavor = "弥从暮环城的旧零件里，为这次投递留下的新办法。";
            switch (id)
            {
                case "bolt": u.Name = rank == 4 ? "焚档 · 星火齐鸣" : "焚档 · 裂隙针 " + (rank + 1); u.Tag = "余烬公社 / 弹道"; u.Flavor = "把删去的名字，刻进监察者的外壳。"; u.Description = rank == 0 ? "获得自动瞄准的直线裂隙针。可混搭；武器等级不计入觉醒点数。" : rank == 4 ? "进化为五重弹幕，每枚穿透 3 个目标。" : "提高弹伤和射速；2 级穿透，3 级三重齐射。"; break;
                case "arc": u.Name = rank == 4 ? "回声 · 万城同频" : "回声 · 链式信标 " + (rank + 1); u.Tag = "回声潜网 / 连锁"; u.Flavor = "只要还有一座街区回答，广播就没有结束。"; u.Description = rank == 0 ? "获得周期连锁电弧。可混搭；武器等级不计入觉醒点数。" : rank == 4 ? "连锁 9 个目标，移除后续跳跃伤害衰减。" : "提高电弧伤害、射程与连锁数量。"; break;
                case "blade": u.Name = rank == 4 ? "铸卫 · 不落城墙" : "铸卫 · 轨道棱 " + (rank + 1); u.Tag = "铸城工坊 / 环刃"; u.Flavor = "巡夜人的城墙，现在跟着信使移动。"; u.Description = rank == 0 ? "获得两枚近身旋转环刃。可混搭；武器等级不计入觉醒点数。" : rank == 4 ? "六枚大型环刃，可拦截碰到的敌方子弹。" : "增加环刃数量、伤害与旋转半径。"; break;
                case "power": u.Name = "超载核心 " + (rank + 1); u.Description = "所有武器伤害增加 16%（按基础伤害相加）。"; u.Tag = "火力"; break;
                case "haste": u.Name = "高频谐振 " + (rank + 1); u.Description = "所有武器攻击频率增加 12%，环刃转速提高。"; u.Tag = "攻速"; break;
                case "vitality": u.Name = "复合装甲 " + (rank + 1); u.Description = "生命上限 +20，并立即恢复 30 点生命。"; u.Tag = "生存"; break;
                case "speed": u.Name = "轻量推进 " + (rank + 1); u.Description = "移动速度 +16，便于绕开冲锋和范围攻击。"; u.Tag = "机动"; break;
                case "magnet": u.Name = "磁场扩展 " + (rank + 1); u.Description = "经验吸取范围 +24。喘息阶段另有额外吸取范围。"; u.Tag = "收集"; break;
                case "scholar": u.Name = "经验回路 " + (rank + 1); u.Description = "获得的经验增加 18%，与其他经验加成相加。"; u.Tag = "成长"; break;
                case "crit": u.Name = "弱点解析 " + (rank + 1); u.Description = "全武器暴击率 +6.5%；暴击造成双倍伤害。"; u.Tag = "暴击"; break;
                case "guard": u.Name = "缓冲护盾 " + (rank + 1); u.Description = "受到伤害减少 6%，最高减伤 42%。"; u.Tag = "生存"; break;
                case "dash": u.Name = "相位冷却 " + (rank + 1); u.Description = "冲刺冷却减少 0.42 秒；冲刺时短暂无敌。"; u.Tag = "机动"; break;
                case "overclock": u.Name = "极限超载"; u.Description = "满构筑奖励：全武器基础增伤再提高 8%。可重复获得。"; u.Tag = "无尽成长"; break;
                case "reserve": u.Name = "备用能量"; u.Description = "满构筑奖励：生命上限 +10，并恢复 20 点。"; u.Tag = "无尽成长"; break;
                case "supply": u.Name = "免费医疗补给"; u.Description = "恢复最大生命值的 25%，每次补给窗口限领取一次。"; u.Tag = "免费补给"; break;
                default: u.Name = "紧急修复"; u.Description = "立即恢复最大生命值的 45%。"; u.Tag = "恢复"; break;
            }
            return u;
        }

        private void Apply(string id)
        {
            if (id == "repair" || id == "supply") { Hp = Math.Min(MaxHp, Hp + MaxHp * (id == "supply" ? .25f : .45f)); AddEffect("heal", X, Y, 0, 0, 40, .5f); return; }
            if (id == "overclock") { Ranks[id] = Rank(id) + 1; TotalUpgrades++; return; }
            if (id == "reserve") { MaxHp += 10; Hp = Math.Min(MaxHp, Hp + 20); TotalUpgrades++; return; }
            int rank = Rank(id); if (rank >= Cap(id)) return;
            Ranks[id] = rank + 1; TotalUpgrades++;
            OnWorldUpgrade(id);
            if (id == "vitality") { MaxHp += 20; Hp = Math.Min(MaxHp, Hp + 30); }
            if ((id == "bolt" || id == "arc" || id == "blade") && rank == 4)
            {
                AddEffect("evolve", X, Y, 0, 0, 130, .8f); Say("武器进化 · " + MakeUpgrade(id).Tag, 4); Event("evolve");
            }
            else Event("upgrade");
        }

        private void AddEffect(string kind, float x, float y, float x2, float y2, float radius, float life)
        {
            if (Effects.Count >= EffectLimit) return;
            Effect e = new Effect(); e.Kind = kind; e.X = x; e.Y = y; e.X2 = x2; e.Y2 = y2;
            e.R = radius; e.Life = e.MaxLife = life; Effects.Add(e);
        }
        private void Say(string message, float duration) { Announcement = message; AnnouncementTime = duration; }
        private bool HasLivingBoss() { for (int i = 0; i < Enemies.Count; i++) if (Enemies[i].Kind == "boss" && Enemies[i].Hp > 0) return true; return false; }
        private void Event(string name) { if (Events.Count < 48 && !Events.Contains(name)) Events.Add(name); }
        private static float Clamp(float value, float min, float max) { return Math.Max(min, Math.Min(max, value)); }
        private static float DistanceSquared(float ax, float ay, float bx, float by) { float x = ax - bx, y = ay - by; return x * x + y * y; }
        private static int Cell(float x, float y) { return Math.Max(0, Math.Min(GridRows - 1, (int)(y / GridSize))) * GridColumns + Math.Max(0, Math.Min(GridColumns - 1, (int)(x / GridSize))); }
        private static void RemoveFast<T>(List<T> list, int index) { int last = list.Count - 1; list[index] = list[last]; list.RemoveAt(last); }
    }
}

