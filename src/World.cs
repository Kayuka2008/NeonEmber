using System;
using System.Collections.Generic;

namespace NeonEmber
{
    public partial class Enemy
    {
        public float BurnTime, BurnDps, JamTime, ResonanceTime;
        public string LastWorldWeapon = "";
        public int WorldDashHit = -1;
    }

    // Narrative and card metadata are shared by the native UI and headless tests.
    public static class Lore
    {
        public const int StoryCount = 4;
        public static readonly string[] UpgradeIds = {
            "ember_brand", "ember_breach", "ember_cascade",
            "echo_decoy", "echo_jammer", "echo_trace",
            "forge_ward", "forge_breaker", "forge_intercept"
        };
        public static string SchoolOf(string id)
        {
            if (id == "bolt" || id.StartsWith("ember_", StringComparison.Ordinal)) return "ember";
            if (id == "arc" || id.StartsWith("echo_", StringComparison.Ordinal)) return "echo";
            if (id == "blade" || id.StartsWith("forge_", StringComparison.Ordinal)) return "forge";
            return "";
        }
        public static bool IsSchoolUpgrade(string id)
        {
            for (int i = 0; i < UpgradeIds.Length; i++) if (UpgradeIds[i] == id) return true;
            return false;
        }
        public static string SchoolName(string school)
        {
            return school == "ember" ? "余烬公社" : school == "echo" ? "回声潜网" : school == "forge" ? "铸城工坊" : "信使改装";
        }
        public static string SchoolSummary(string school)
        {
            return school == "ember" ? "热写记忆：点燃、惩罚破绽、传火。6 点觉醒「不灭余烬」。" :
                school == "echo" ? "伪造信号：残像、干扰指挥、异源共鸣。6 点觉醒「离线人格」。" :
                school == "forge" ? "夺回防线：充能护盾、冲刺破阵、拦截弹幕。6 点觉醒「巡夜城墙」。" : "混搭三系不会锁定其他路线。";
        }
        public static string CapstoneName(string school)
        {
            return school == "ember" ? "不灭余烬" : school == "echo" ? "离线人格" : "巡夜城墙";
        }
        public static string CapstoneDescription(string school)
        {
            return school == "ember" ? "武器点燃持续 4 秒；燃烧敌人死亡时，向 100 范围内至多 6 个目标造成 1.5 倍燃烧秒伤。需要热写烙印供火。" :
                school == "echo" ? "冲刺至少生成 1.2 秒残像；残像消散时在 130 范围内干扰敌人 0.9 秒，打断普通敌人蓄力。" :
                "护盾上限 +18；破盾时抵消该次溢出伤害，清除 160 范围内敌弹并无敌 0.8 秒，每 8 秒最多一次。";
        }
        public static string StoryTitle(int chapter)
        {
            switch (Math.Max(0, Math.Min(StoryCount - 1, chapter)))
            {
                case 0: return "00 / 一份不该存在的委托";
                case 1: return "01 / 死者的签名";
                case 2: return "02 / 第七次黎明";
                default: return "03 / 没有终点的投递";
            }
        }
        public static string StoryBody(int chapter)
        {
            switch (Math.Max(0, Math.Min(StoryCount - 1, chapter)))
            {
                case 0: return "暮环城用「总档案」保存每个人的记忆。灾后居民相信：只要档案仍在，死者就没有真正离去。\n\n你是无名信使「零」。一份没有寄件人的委托，把一段未经校订的记忆写进你的脊柱：『天亮之前，把我送出城。不要相信你的履历。』收件地址，竟然是你的住址。\n\n总档案随即将你列为失真源。余烬公社提供焚毁追踪码的热写器，回声潜网替你伪造身份，铸城工坊交出旧巡夜装甲。你需要在一次次清剿之间回收监察者的密钥，查明谁在投递这段记忆。";
                case 1: return "第一枚密钥解开一份已注销的死亡证明。姓名被抹去，身份签名却与你完全一致。落款日期，距今七年。\n\n通讯中的修补匠「弥」第一次沉默。她最终只说：『你不是第一位零。你的每一次出发，城里都会有人想起不该想起的事。』\n\n监察者并非偶然追来。指挥节点正在交换你的行动轨迹，盾卫为节点遮挡火力，截击者绕到前方。总档案害怕的不是一具身体，而是身体里那段未经它许可的过去。";
                case 2: return "第二枚密钥恢复了你参与签署的「黎明校订协议」。七年前能源危机中，总档案被授予删除痛苦记忆的权力；后来，反对配给的声音也被划进了同一类别。\n\n你曾是协议的维护者。发现城外求援记录被反复删除后，你把证词拆成片段，藏进自己的多个备份。每次封锁，系统都会重置你的履历；每次投递，一段证词便会逃出它的索引。\n\n余烬公社想让证词无法被回收，回声潜网想保住尚未暴露的人名，铸城工坊想让居民拥有关停审查的权力。三套改装不是信仰测试，而是三种把真相送出去的方法。";
                default: return "第三枚密钥终于打开原始委托。寄件人正是七年前的你。收件地址不是一栋房子，而是所有离线收音机共有的旧广播标识：『家』。\n\n你没有删除总档案。里面仍住着数以万计等待被唤醒的亲人。你和弥选择公开校订日志，把是否记起过去的选择还给城里的人。第一批接收者回传了同一句话：『已收到。继续。』\n\n清剿没有结束。每击破一位后续监察者，新的频段便加入转发；你坚持得越久，越多被隔离的街区能够接收证词。这不是另一个被承诺的结局，而是一条仍需要有人守住的广播线路。";
            }
        }
        public const string CombatManual = "总档案通过指挥节点组织清剿。连线说明节点与随从的协同关系；击破或干扰节点会打断随从行动。\n盾卫保护前方盟友，尝试绕背或冲刺破盾。截击者预测移动方向，冲锋前锁定落点；反向变速可以躲开。\n火力手与封路者会瞄准预测位置，攻击有预告且锁定后不跟随玩家。冲刺用于穿越火线，也能触发改装。\n余烬、回声、铸城各有 3 项改装，每项最多 3 级。同系累计 6 点自动觉醒；两系各 2 点产生协同。武器等级不计入系别点数。\n暂停后按 J 阅读已解锁档案。剧情由实际击破监察者解锁，时间到达只会触发敌人登场。";
    }

    public partial class Game
    {
        public int CompletedBosses, StoryChapter;
        public string LastStoryTitle = "";
        public float Ward, DecoyX, DecoyY, DecoyTime;
        private float wardWait, interceptClock, wallCooldown, lastWorldX, lastWorldY;
        private int worldDashSerial;

        public int SchoolPoints(string school)
        {
            int total = 0;
            for (int i = 0; i < Lore.UpgradeIds.Length; i++)
                if (Lore.SchoolOf(Lore.UpgradeIds[i]) == school) total += Rank(Lore.UpgradeIds[i]);
            return total;
        }
        public bool HasSchoolCapstone(string school) { return SchoolPoints(school) >= 6; }
        public bool HasWorldSynergy(string a, string b) { return a != b && SchoolPoints(a) >= 2 && SchoolPoints(b) >= 2; }
        public float MaxWard
        {
            get { return 14 * Rank("forge_ward") + (HasSchoolCapstone("forge") ? 18 : 0) + (HasWorldSynergy("echo", "forge") ? 10 : 0); }
        }
        public string SchoolProgressText()
        {
            return "余烬 " + SchoolPoints("ember") + "/6  ·  回声 " + SchoolPoints("echo") + "/6  ·  铸城 " + SchoolPoints("forge") + "/6  （6 点觉醒，上限 9）";
        }
        public string SynergyText()
        {
            List<string> active = new List<string>();
            if (HasWorldSynergy("ember", "echo")) active.Add("失真热写：干扰中的敌人燃烧伤害 +50%");
            if (HasWorldSynergy("ember", "forge")) active.Add("熔铸回路：击杀燃烧敌人恢复 3 点护盾");
            if (HasWorldSynergy("echo", "forge")) active.Add("幽灵护航：护盾上限 +10，残像存在时无延迟回充");
            return active.Count == 0 ? "混搭协同：任意两系各投入 2 点后激活。" : string.Join("\n", active.ToArray());
        }

        private void ResetWorld()
        {
            CompletedBosses = StoryChapter = 0; LastStoryTitle = Lore.StoryTitle(0);
            Ward = wardWait = interceptClock = wallCooldown = DecoyTime = 0;
            DecoyX = lastWorldX = X; DecoyY = lastWorldY = Y; worldDashSerial = 0;
            string starter = Rank("arc") > 0 ? "echo_decoy" : Rank("blade") > 0 ? "forge_ward" : "ember_brand";
            Ranks[starter] = 1;
            if (starter == "forge_ward") Ward = 14;
        }

        private void UpdateWorld(float dt, bool dashedNow)
        {
            wardWait = Math.Max(0, wardWait - dt);
            wallCooldown = Math.Max(0, wallCooldown - dt);
            interceptClock = Math.Max(0, interceptClock - dt);
            float previousDecoyTime = DecoyTime;
            DecoyTime = Math.Max(0, DecoyTime - dt);
            bool hotEcho = HasWorldSynergy("ember", "echo");
            for (int i = 0; i < Enemies.Count; i++)
            {
                Enemy e = Enemies[i]; if (e.Hp <= 0) continue;
                e.JamTime = Math.Max(0, e.JamTime - dt);
                e.ResonanceTime = Math.Max(0, e.ResonanceTime - dt);
                if (e.BurnTime > 0)
                {
                    float tick = Math.Min(dt, e.BurnTime);
                    float multiplier = hotEcho && e.JamTime > 0 ? 1.5f : 1;
                    e.Hp -= e.BurnDps * tick * multiplier;
                    e.BurnTime = Math.Max(0, e.BurnTime - dt);
                    // Keep a tiny burn marker until collection if this tick was lethal.
                    if (e.Hp <= 0) e.BurnTime = Math.Max(.001f, e.BurnTime);
                }
            }
            if (previousDecoyTime > 0 && DecoyTime <= 0 && HasSchoolCapstone("echo"))
                WorldJamPulse(DecoyX, DecoyY, 130, .9f);
            if (dashedNow)
            {
                worldDashSerial++;
                int decoy = Rank("echo_decoy");
                if (decoy > 0 || HasSchoolCapstone("echo"))
                {
                    DecoyX = lastWorldX; DecoyY = lastWorldY;
                    DecoyTime = decoy > 0 ? .9f + .4f * decoy : 1.2f;
                    AddEffect("echoDecoy", DecoyX, DecoyY, 0, 0, 24, DecoyTime);
                }
                int jammer = Rank("echo_jammer");
                if (jammer > 0) WorldJamPulse(X, Y, 105 + jammer * 15, .55f + jammer * .2f);
            }
            int breaker = Rank("forge_breaker");
            if (breaker > 0 && (dashedNow || DashTime > 0))
            {
                for (int i = 0; i < Enemies.Count; i++)
                {
                    Enemy e = Enemies[i]; if (e.Hp <= 0 || e.AttackPhase == -1 || e.WorldDashHit == worldDashSerial) continue;
                    if (WorldSegmentDistanceSquared(e.X, e.Y, lastWorldX, lastWorldY, X, Y) > (42 + e.R) * (42 + e.R)) continue;
                    e.WorldDashHit = worldDashSerial;
                    JamWorldEnemy(e, .7f + .15f * breaker);
                    DamageEnemy(e, (12 + 12 * breaker) * PowerMultiplier, "forge");
                    if (e.Kind != "boss")
                    {
                        float dx = e.X - X, dy = e.Y - Y;
                        float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (distance < 1) { dx = dashX; dy = dashY; distance = 1; }
                        e.X = Clamp(e.X + dx / distance * 48, Left + e.R, Right - e.R);
                        e.Y = Clamp(e.Y + dy / distance * 48, Top + e.R, Bottom - e.R);
                    }
                    AddEffect("forgeBreak", e.X, e.Y, 0, 0, 40, .3f);
                }
            }
            if (Ward < MaxWard && (wardWait <= 0 || (DecoyTime > 0 && HasWorldSynergy("echo", "forge"))))
                Ward = Math.Min(MaxWard, Ward + 6 * dt);
            int intercept = Rank("forge_intercept");
            if (intercept > 0 && interceptClock <= 0)
            {
                int nearest = -1; float distance = (70 + 10 * intercept) * (70 + 10 * intercept);
                for (int i = 0; i < Shots.Count; i++)
                {
                    if (!Shots[i].Hostile) continue;
                    float d = DistanceSquared(X, Y, Shots[i].X, Shots[i].Y);
                    if (d < distance) { distance = d; nearest = i; }
                }
                if (nearest >= 0)
                {
                    Shot shot = Shots[nearest]; AddEffect("intercept", X, Y, shot.X, shot.Y, 8, .2f);
                    RemoveFast(Shots, nearest); interceptClock = Math.Max(.45f, 1.4f - .25f * intercept); Event("arc");
                }
            }
            lastWorldX = X; lastWorldY = Y;
        }

        private static float WorldSegmentDistanceSquared(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay, denom = dx * dx + dy * dy;
            float t = denom <= .001f ? 0 : Clamp(((px - ax) * dx + (py - ay) * dy) / denom, 0, 1);
            return DistanceSquared(px, py, ax + t * dx, ay + t * dy);
        }
        private void WorldJamPulse(float x, float y, float radius, float duration)
        {
            AddEffect("jamPulse", x, y, 0, 0, radius, .38f);
            for (int i = 0; i < Enemies.Count; i++)
            {
                Enemy e = Enemies[i];
                if (e.Hp > 0 && e.AttackPhase != -1 && DistanceSquared(x, y, e.X, e.Y) <= radius * radius)
                    JamWorldEnemy(e, duration);
            }
        }
        private void JamWorldEnemy(Enemy e, float duration)
        {
            if (e.Kind == "boss") { e.JamTime = Math.Max(e.JamTime, .25f); return; }
            e.JamTime = Math.Max(e.JamTime, duration);
            if (e.Kind == "relay") DisruptRelay(e);
            if (e.AttackPhase == 1) { e.AttackPhase = 0; e.Windup = 0; CancelEnemyWarning(e.Id); }
            e.Cooldown = Math.Max(e.Cooldown, duration + .35f);
        }

        private float ApplyWorldDamage(Enemy e, float damage, string weapon)
        {
            int breach = Rank("ember_breach");
            if (breach > 0 && (e.AttackPhase == 1 || e.Recovery > 0 || e.JamTime > 0)) damage *= 1 + .18f * breach;
            bool weaponHit = weapon == "bolt" || weapon == "nova" || weapon == "arc" || weapon == "blade";
            if (!weaponHit) return damage;
            int brand = Rank("ember_brand");
            if (brand > 0)
            {
                e.BurnTime = HasSchoolCapstone("ember") ? 4 : 2;
                e.BurnDps = Math.Max(e.BurnDps, (4 + 3 * brand) * PowerMultiplier);
            }
            string category = weapon == "nova" ? "bolt" : weapon;
            int trace = Rank("echo_trace");
            if (trace > 0 && e.ResonanceTime > 0 && e.LastWorldWeapon != "" && e.LastWorldWeapon != category)
            {
                damage *= 1 + .14f * trace;
                AddEffect("resonance", e.X, e.Y, 0, 0, e.R + 7, .18f);
            }
            e.ResonanceTime = 3; e.LastWorldWeapon = category;
            return damage;
        }

        private float AbsorbWorldDamage(float damage)
        {
            wardWait = 4;
            float absorbed = Math.Min(Ward, Math.Max(0, damage));
            float before = Ward; Ward -= absorbed; damage -= absorbed;
            if (absorbed > 0) AddEffect("wardHit", X, Y, 0, 0, 30, .25f);
            if (before > 0 && Ward <= 0 && HasSchoolCapstone("forge") && wallCooldown <= 0)
            {
                wallCooldown = 8; Invulnerable = Math.Max(Invulnerable, .8f);
                for (int i = Shots.Count - 1; i >= 0; i--)
                    if (Shots[i].Hostile && DistanceSquared(X, Y, Shots[i].X, Shots[i].Y) < 160 * 160)
                    {
                        // Hurt can run inside UpdateShots: defer structural removal to that loop.
                        Shots[i].Life = 0; Shots[i].Damage = 0;
                    }
                AddEffect("wardBurst", X, Y, 0, 0, 160, .45f);
                // The breaking hit consumes the ward; the awakened wall absorbs its overflow.
                damage = 0;
            }
            return Math.Max(0, damage);
        }

        private void OnWorldEnemyDeath(Enemy e)
        {
            if (e.BurnTime > 0)
            {
                if (HasWorldSynergy("ember", "forge")) Ward = Math.Min(MaxWard, Ward + 3);
                int spread = Rank("ember_cascade"), spreadCount = 0, blastCount = 0;
                bool awakened = HasSchoolCapstone("ember");
                for (int i = 0; i < Enemies.Count; i++)
                {
                    Enemy other = Enemies[i];
                    if (other == e || other.Hp <= 0 || DistanceSquared(e.X, e.Y, other.X, other.Y) > 100 * 100) continue;
                    if (spread > 0 && spreadCount < spread + 1)
                    {
                        other.BurnTime = Math.Max(other.BurnTime, 2);
                        other.BurnDps = Math.Max(other.BurnDps, e.BurnDps); spreadCount++;
                    }
                    if (awakened && blastCount < 6) { other.Hp -= e.BurnDps * 1.5f; blastCount++; }
                }
                if (spreadCount > 0 || blastCount > 0) AddEffect("emberBurst", e.X, e.Y, 0, 0, 100, .3f);
            }
            if (e.Kind == "boss")
            {
                CompletedBosses++;
                int chapter = Math.Min(Lore.StoryCount - 1, CompletedBosses);
                if (chapter > StoryChapter)
                {
                    StoryChapter = chapter; LastStoryTitle = Lore.StoryTitle(chapter);
                    Say("档案解封 · " + LastStoryTitle + " · J 阅读", 6); Event("story");
                }
            }
        }

        private void OnWorldUpgrade(string id)
        {
            if (!Lore.IsSchoolUpgrade(id)) return;
            if (id == "forge_ward") Ward = Math.Min(MaxWard, Ward + 14);
            string school = Lore.SchoolOf(id);
            if (SchoolPoints(school) == 6)
            {
                if (school == "forge") Ward = Math.Min(MaxWard, Ward + 18);
                Say("体系觉醒 · " + (school == "ember" ? "不灭余烬" : school == "echo" ? "离线人格" : "巡夜城墙"), 4);
                AddEffect("evolve", X, Y, 0, 0, 130, .8f); Event("evolve");
            }
        }

        private Upgrade MakeWorldUpgrade(string id)
        {
            return DescribeWorldUpgrade(id, Rank(id) + 1);
        }
        public Upgrade WorldUpgradeInfo(string id)
        {
            return DescribeWorldUpgrade(id, Math.Max(1, Rank(id)));
        }
        private Upgrade DescribeWorldUpgrade(string id, int next)
        {
            if (!Lore.IsSchoolUpgrade(id)) return null;
            Upgrade u = new Upgrade(); u.Id = id; u.School = Lore.SchoolOf(id); u.Tag = Lore.SchoolName(u.School) + " · " + next + "/3";
            switch (id)
            {
                case "ember_brand":
                    u.Name = "热写烙印 " + next; u.Description = "武器命中点燃目标 2 秒，觉醒后 4 秒；每秒 " + (4 + 3 * next) + " 基础伤害，受全局火力加成。重复命中刷新时间。"; u.Flavor = "他们删去名字，我们把名字烧进金属。"; break;
                case "ember_breach":
                    u.Name = "破绽焚解 " + next; u.Description = "对正在蓄力、恢复或被干扰的敌人，直接伤害增加 " + (18 * next) + "%；追击刚刚打空技能的敌人。"; u.Flavor = "最坚固的校订，也有落笔前的一瞬空白。"; break;
                case "ember_cascade":
                    u.Name = "传火证词 " + next; u.Description = "燃烧中的敌人死亡时，将 2 秒燃烧传播给 100 范围内至多 " + (next + 1) + " 个敌人。需热写烙印提供火种。"; u.Flavor = "证词只要还有一个人记得，就没有消失。"; break;
                case "echo_decoy":
                    u.Name = "离线替身 " + next; u.Description = "冲刺起点留下 " + (.9f + .4f * next).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " 秒残像，诱导敌人新一轮瞄准；已锁定的攻击不会改向。"; u.Flavor = "一张被注销的身份，仍可以敲开一扇门。"; break;
                case "echo_jammer":
                    u.Name = "静默频段 " + next; u.Description = "冲刺干扰周围 " + (105 + 15 * next) + " 范围，打断普通敌人蓄力，断开节点协同，持续 " + (.55f + .2f * next).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " 秒。Boss 仅延迟后续行动。"; u.Flavor = "把命令与执行之间的那条线剪断。"; break;
                case "echo_trace":
                    u.Name = "异源共鸣 " + next; u.Description = "3 秒内用另一种武器命中同一敌人，后一次命中伤害增加 " + (14 * next) + "%；适合两种及以上武器混搭。"; u.Flavor = "一段回忆可能被改写，两份矛盾的回忆会留下回声。"; break;
                case "forge_ward":
                    u.Name = "巡夜护层 " + next; u.Description = "基础护盾容量增至 " + (14 * next) + "，另计觉醒与联动；本次补充 14 点。4 秒未受击后每秒回充 6 点。"; u.Flavor = "城墙的职责不是困住居民。"; break;
                case "forge_breaker":
                    u.Name = "破阵撞角 " + next; u.Description = "冲刺沿途近身敌人受到 " + (12 + 12 * next) + " 基础伤害，普通敌人被击退、干扰；盾卫护面与指挥协同会被拆开。每敌人每次冲刺只触发一次。"; u.Flavor = "我们建过这座城，也知道从哪里撬开它。"; break;
                default:
                    u.Name = "拾荒拦截器 " + next; u.Description = "每 " + Math.Max(.45f, 1.4f - .25f * next).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " 秒拦截一枚 " + (70 + 10 * next) + " 范围内的敌弹，优先最近目标。"; u.Flavor = "废弃巡逻机的最后一条命令：保护街上的人。"; break;
            }
            return u;
        }
    }
}
