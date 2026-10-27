using System;
using System.Collections.Generic;

namespace NeonEmber
{
    public partial class Enemy
    {
        // GuardId is reciprocal: guard -> protected unit, protected unit -> guard.
        public int RelayId, GuardId;
        public float Stun, Recovery, OrderX, OrderY;
        public bool Shielded, RelayBroken;
        public string Order = "追踪";
        public bool Elite;
        public string Role = "游猎", Intent = "追踪", BrainState = "准备";
        public float FacingX = 1, FacingY, Channel, MaxChannel, SquadClock;
        public int RepairTargetId, AttackSerial, BossStage = 1;
        public float PhaseLeft;
    }

    public class Hazard
    {
        public int OwnerId;
        public float X, Y, R, Warning, Life, Damage;
        public string Shape = "circle";
        public float X2, Y2, InnerR;
    }

    public partial class Game
    {
        public const int HazardLimit = 12, SpecialAttackLimit = 3;
        public float PlayerVX, PlayerVY;
        public int FocusTargetId;
        public string TacticName = "游猎 · 观察前摇后转向";
        public string MotionReadout = "轨迹采样中", SquadPhase = "游猎";
        public float CircleConfidence, ObservedTurnRate, ObservedDashAge = 99, PredictedX, PredictedY, SquadPhaseLeft;
        public List<Hazard> Hazards = new List<Hazard>();
        private readonly Dictionary<int, Enemy> tacticalIndex = new Dictionary<int, Enemy>();
        private float tacticClock, nextSquad, nextSpecial;
        private float lastObservedVX, lastObservedVY, turnHistory, turnSign, nextElite;

        public static string EnemyName(string kind)
        {
            switch (kind) {
                case "relay": return "指挥节点"; case "tank": return "护航盾卫";
                case "shooter": return "火力手"; case "runner": return "截击者";
                case "bomber": return "封路者"; case "repairer": return "维护者";
                case "prism": return "棱镜卫"; case "boss": return "监察者";
                default: return "追缉机";
            }
        }
        public static string EnemyCounter(string kind)
        {
            switch (kind) {
                case "relay": return "压制后接截击；优先锁定节点，击破可中断编队。";
                case "tank": return "必须站在盟友前方才能护航；从侧面绕开护面。";
                case "shooter": return "预判轨迹后锁定射线；看到蓄力再改变路线。";
                case "runner": return "等待火力压制后截击；保留冲刺躲第二波。";
                case "bomber": return "读取持续转向，封锁弧线前方；反向或切入内圈。";
                case "repairer": return "金色连线引导修复；直接命中或干扰可以打断，不会复活。";
                case "prism": return "护面转向较慢；正面减伤，侧背暴露。冲刺干扰可以破甲。";
                case "boss": return "圆区、切割线、环区交替；环内安全，攻击后有恢复弱点。";
                default: return "侧向逼近，切入空隙；连续绕圈会被后续编队识别。";
            }
        }

        private void ResetTactics()
        {
            PlayerVX = PlayerVY = 0; FocusTargetId = 0; tacticClock = nextSpecial = 0;
            nextSquad = 36; nextElite = 48; Hazards.Clear(); tacticalIndex.Clear();
            lastObservedVX = lastObservedVY = turnHistory = turnSign = CircleConfidence = ObservedTurnRate = 0;
            ObservedDashAge = 99; PredictedX = X; PredictedY = Y; SquadPhase = "游猎"; SquadPhaseLeft = 0; MotionReadout = "轨迹采样中";
            TacticName = "游猎 · 观察前摇后转向";
        }

        private void ObserveMovement(float dt, float previousX, float previousY)
        {
            // Only displacement already observed is available to enemy planning. Do not use input.
            float vx = (X - previousX) / dt, vy = (Y - previousY) / dt;
            float speed = (float)Math.Sqrt(vx * vx + vy * vy);
            ObservedDashAge = Math.Min(99, ObservedDashAge + dt);
            // A fast displacement is evidence that a dash happened. Never inspect dash input/cooldown.
            if (speed > 440) { ObservedDashAge = 0; turnHistory = Math.Max(0, turnHistory - dt * 2); }
            float previousSpeed = (float)Math.Sqrt(lastObservedVX * lastObservedVX + lastObservedVY * lastObservedVY);
            if (speed > 80 && speed < 440 && previousSpeed > 80 && ObservedDashAge > .22f)
            {
                float turn = (float)Math.Atan2(lastObservedVX * vy - lastObservedVY * vx, lastObservedVX * vx + lastObservedVY * vy) / dt;
                if (Math.Abs(turn) > .16f && Math.Abs(turn) < 3.2f)
                {
                    float sign = Math.Sign(turn);
                    if (sign != turnSign) { turnHistory = 0; CircleConfidence *= .2f; }
                    turnSign = sign; turnHistory = Math.Min(3, turnHistory + dt);
                    ObservedTurnRate += (turn - ObservedTurnRate) * Math.Min(1, dt * 5);
                }
                else
                {
                    turnHistory = Math.Max(0, turnHistory - dt * 2);
                    ObservedTurnRate *= Math.Max(0, 1 - dt * 5);
                    if (Math.Abs(turn) >= 3.2f) CircleConfidence *= .25f;
                }
            }
            else { turnHistory = Math.Max(0, turnHistory - dt); ObservedTurnRate *= Math.Max(0, 1 - dt * 3); }
            CircleConfidence += (Clamp((turnHistory - .35f) / 1.1f, 0, 1) - CircleConfidence) * Math.Min(1, dt * 3);
            lastObservedVX = vx; lastObservedVY = vy;
            if (speed > 300) { vx *= 300 / speed; vy *= 300 / speed; }
            float blend = Math.Min(1, dt * 12);
            PlayerVX += (vx - PlayerVX) * blend; PlayerVY += (vy - PlayerVY) * blend;
            PredictPosition(1.2f, out PredictedX, out PredictedY);
            MotionReadout = ObservedDashAge < .6f ? "检测到冲刺 · 截击者准备接力" : CircleConfidence > .55f ? "弧线轨迹已识别 · 变向或切入内圈" : "线性推演 · 蓄力后变向可欺骗瞄准";
        }

        private void PredictPosition(float seconds, out float tx, out float ty)
        {
            if (DecoyTime > 0) { tx = DecoyX; ty = DecoyY; return; }
            seconds = Clamp(seconds, 0, 2.6f);
            float omega = CircleConfidence > .35f ? ObservedTurnRate * CircleConfidence : 0;
            if (Math.Abs(omega) > .12f)
            {
                // Integrate the observed turn rate, rather than aiming along an impossible tangent.
                float angle = omega * seconds, sin = (float)Math.Sin(angle), cos = (float)Math.Cos(angle);
                tx = X + (PlayerVX * sin + PlayerVY * (cos - 1)) / omega;
                ty = Y + (PlayerVX * (1 - cos) + PlayerVY * sin) / omega;
            }
            else { tx = X + PlayerVX * seconds; ty = Y + PlayerVY * seconds; }
            tx = Clamp(tx, Left + 18, Right - 18); ty = Clamp(ty, Top + 18, Bottom - 18);
        }

        private Enemy FindEnemy(int id)
        {
            if (id <= 0) return null;
            Enemy found;
            if (tacticalIndex.TryGetValue(id, out found)) return found;
            for (int i = 0; i < Enemies.Count; i++) if (Enemies[i].Id == id) return Enemies[i];
            return null;
        }

        private void SpawnSquad()
        {
            if (Enemies.Count > EnemyLimit - 9) return;
            int relayCount = 0;
            for (int i = 0; i < Enemies.Count; i++) if (Enemies[i].Kind == "relay" && Enemies[i].Hp > 0) relayCount++;
            if (relayCount >= 3) return;
            Spawn("relay"); Enemy leader = Enemies[Enemies.Count - 1];
            string[] kinds = Time >= 90 ? new string[] { "tank", "shooter", "shooter", "runner", "runner", "bomber", "repairer", "prism" }
                : new string[] { "tank", "shooter", "shooter", "runner", "runner", "bomber" };
            for (int i = 0; i < kinds.Length; i++)
            {
                Spawn(kinds[i]); Enemy member = Enemies[Enemies.Count - 1];
                float angle = (float)(i * Math.PI * 2 / kinds.Length);
                member.X = Clamp(leader.X + (float)Math.Cos(angle) * 82, Left + member.R, Right - member.R);
                member.Y = Clamp(leader.Y + (float)Math.Sin(angle) * 82, Top + member.R, Bottom - member.R);
                member.RelayId = leader.Id;
                member.Cooldown += i * .28f;
                // Spawn graphics must reflect the actual squad position.
                AddEffect("spawn", member.X, member.Y, 0, 0, member.R + 8, .55f);
            }
            // A squad replaces part of the normal drip instead of adding unbounded density.
            spawnClock += 1.4f;
            tacticClock = 0;
        }

        private void UpdateSquadPhase(Enemy relay, float dt)
        {
            if (relay.Recovery > 0 || relay.JamTime > 0 || relay.Stun > 0)
            { relay.SquadClock = 0; relay.BrainState = "恢复"; relay.PhaseLeft = Math.Max(relay.Recovery, relay.JamTime); return; }
            if (relay.AttackPhase == -1) { relay.BrainState = "准备"; relay.PhaseLeft = 1.15f; return; }
            relay.SquadClock = (relay.SquadClock + dt) % 6.9f;
            float clock = relay.SquadClock;
            relay.BrainState = clock < 1.15f ? "准备" : clock < 3.05f ? "压制" : clock < 5.25f ? "截击" : "恢复";
            relay.PhaseLeft = (clock < 1.15f ? 1.15f : clock < 3.05f ? 3.05f : clock < 5.25f ? 5.25f : 6.9f) - clock;
            relay.Intent = relay.BrainState == "准备" ? "展开交叉火力" : relay.BrainState == "压制" ? "远程封锁路线" : relay.BrainState == "截击" ? "近战接力围堵" : "重组 · 节点暴露";
        }

        private void UpdateTactics(float dt)
        {
            tacticalIndex.Clear();
            for (int i = 0; i < Enemies.Count; i++) tacticalIndex[Enemies[i].Id] = Enemies[i];
            for (int i = 0; i < Enemies.Count; i++)
            {
                Enemy e = Enemies[i];
                if (e.Kind == "relay" && e.Hp <= 0) DisruptRelay(e);
                e.Recovery = Math.Max(0, e.Recovery - dt);
                if (e.Kind == "relay" && e.Hp > 0) UpdateSquadPhase(e, dt);
                if (e.RelayId != 0)
                {
                    Enemy relay = FindEnemy(e.RelayId);
                    if (relay == null || relay.Hp <= 0 || relay.JamTime > 0 || relay.Stun > 0 || relay.Recovery > 0 || DistanceSquared(e.X, e.Y, relay.X, relay.Y) > 460 * 460) e.RelayId = 0;
                }
                e.Shielded = false;
            }
            tacticClock -= dt;
            if (tacticClock <= 0)
            {
                tacticClock = .22f;
                AssignSquads(); AssignGuards();
                for (int i = 0; i < Enemies.Count; i++) PlanPosition(Enemies[i]);
            }
            bool coordinated = false; Enemy visibleLeader = null; float leaderDistance = float.MaxValue;
            for (int i = 0; i < Enemies.Count; i++)
            {
                Enemy e = Enemies[i]; if (e.Hp <= 0) continue;
                e.Shielded = IsProtected(e);
                if (e.RelayId > 0) coordinated = true;
                if (e.Kind == "relay")
                {
                    float distance = DistanceSquared(X, Y, e.X, e.Y);
                    if (distance < leaderDistance) { leaderDistance = distance; visibleLeader = e; }
                }
                else if (e.RelayId > 0)
                {
                    Enemy leader = FindEnemy(e.RelayId); if (leader != null) { e.BrainState = leader.BrainState; e.PhaseLeft = leader.PhaseLeft; }
                }
            }
            Enemy focused = FindEnemy(FocusTargetId);
            if (focused == null || focused.Hp <= 0) FocusTargetId = 0;
            SquadPhase = visibleLeader != null ? visibleLeader.BrainState : "游猎";
            SquadPhaseLeft = visibleLeader != null ? visibleLeader.PhaseLeft : 0;
            TacticName = coordinated ? "中继合围 · " + SquadPhase + " · E 锁节点，切断下一轮接力" : "游猎 · " + MotionReadout;
        }

        private void AssignSquads()
        {
            Dictionary<int, int> assigned = new Dictionary<int, int>();
            for (int i = 0; i < Enemies.Count; i++)
            {
                Enemy e = Enemies[i]; e.RelayId = 0;
                if (e.Kind == "relay" || e.Kind == "boss" || e.Hp <= 0 || e.Recovery > 0 || e.JamTime > 0) continue;
                float best = 420 * 420;
                for (int j = 0; j < Enemies.Count; j++)
                {
                    Enemy relay = Enemies[j];
                    if (relay.Kind != "relay" || relay.Hp <= 0 || relay.AttackPhase == -1 || relay.JamTime > 0 || relay.Stun > 0 || relay.Recovery > 0) continue;
                    int members; if (assigned.TryGetValue(relay.Id, out members) && members >= 10) continue;
                    float d = DistanceSquared(e.X, e.Y, relay.X, relay.Y);
                    if (d < best) { best = d; e.RelayId = relay.Id; }
                }
                if (e.RelayId > 0) { int members; assigned.TryGetValue(e.RelayId, out members); assigned[e.RelayId] = members + 1; }
            }
        }

        private void AssignGuards()
        {
            for (int i = 0; i < Enemies.Count; i++) Enemies[i].GuardId = 0;
            for (int i = 0; i < Enemies.Count; i++)
            {
                Enemy guard = Enemies[i];
                if (guard.Kind != "tank" || guard.Hp <= 0 || guard.JamTime > 0 || guard.Stun > 0 || guard.Recovery > 0) continue;
                Enemy target = null; float best = 235 * 235;
                for (int j = 0; j < Enemies.Count; j++)
                {
                    Enemy e = Enemies[j];
                    if ((e.Kind != "shooter" && e.Kind != "relay" && e.Kind != "repairer" && e.Kind != "prism") || e.Hp <= 0 || e.GuardId != 0 || e.Stun > 0) continue;
                    if (guard.RelayId > 0 && e.RelayId != guard.RelayId && e.Id != guard.RelayId) continue;
                    float distance = DistanceSquared(guard.X, guard.Y, e.X, e.Y);
                    if (e.Kind == "relay") distance *= 1.3f;
                    if (e.Kind == "prism") distance *= .65f;
                    if (distance < best) { best = distance; target = e; }
                }
                if (target != null) { guard.GuardId = target.Id; target.GuardId = guard.Id; }
            }
        }

        private void PlanPosition(Enemy e)
        {
            float tx = DecoyTime > 0 ? DecoyX : X, ty = DecoyTime > 0 ? DecoyY : Y;
            float dx = e.X - tx, dy = e.Y - ty, distance = (float)Math.Sqrt(dx * dx + dy * dy);
            float nx = dx / Math.Max(1, distance), ny = dy / Math.Max(1, distance);
            float side = (e.Id & 1) == 0 ? 1 : -1;
            bool linked = e.RelayId > 0 && e.Recovery <= 0;
            e.OrderX = tx; e.OrderY = ty; e.Order = "追踪";
            e.Role = e.Kind == "relay" ? "指挥" : e.Kind == "repairer" ? "续航" : e.Kind == "prism" ? "破阵" : e.Kind == "tank" ? "掩护" : e.Kind == "runner" ? "截击" : e.Kind == "shooter" ? "压制" : e.Kind == "bomber" ? "封路" : "驱赶";
            if (e.Kind == "tank" && e.GuardId > 0)
            {
                Enemy ally = FindEnemy(e.GuardId);
                if (ally != null)
                {
                    float ax = tx - ally.X, ay = ty - ally.Y, ad = (float)Math.Sqrt(ax * ax + ay * ay);
                    e.OrderX = ally.X + ax / Math.Max(ad, 1) * 62;
                    e.OrderY = ally.Y + ay / Math.Max(ad, 1) * 62;
                    e.Order = "掩护";
                }
            }
            else if (e.Kind == "repairer")
            {
                Enemy patient = FindRepairTarget(e);
                if (patient != null)
                {
                    float ax = patient.X - tx, ay = patient.Y - ty, ad = (float)Math.Sqrt(ax * ax + ay * ay);
                    e.OrderX = patient.X + ax / Math.Max(1, ad) * 75; e.OrderY = patient.Y + ay / Math.Max(1, ad) * 75;
                    e.Order = "搜寻伤员";
                }
                else { e.OrderX = tx + nx * 355 - ny * side * 70; e.OrderY = ty + ny * 355 + nx * side * 70; e.Order = "后方待命"; }
            }
            else if (e.Kind == "shooter" || e.Kind == "relay" || e.Kind == "bomber")
            {
                float orbit = e.Kind == "relay" ? 345 : e.Kind == "bomber" ? 325 : 300;
                Enemy leader = FindEnemy(e.RelayId);
                if (linked && leader != null)
                {
                    dx = leader.X - tx; dy = leader.Y - ty; distance = (float)Math.Sqrt(dx * dx + dy * dy);
                    nx = dx / Math.Max(distance, 1); ny = dy / Math.Max(distance, 1);
                }
                float lateral = linked && e.Kind == "shooter" ? side * 145 : side * 50;
                e.OrderX = tx + nx * orbit - ny * lateral; e.OrderY = ty + ny * orbit + nx * lateral;
                e.Order = e.Kind == "relay" ? "指挥" : e.Kind == "bomber" ? "封路" : linked ? "交叉火力" : "拉开距离";
            }
            else if ((e.Kind == "chaser" || e.Kind == "runner" || e.Kind == "prism") && distance > 75)
            {
                float predict = linked ? 1.15f : .55f;
                float lateral = linked ? 80 : 28;
                PredictPosition(predict, out e.OrderX, out e.OrderY);
                e.OrderX -= ny * side * lateral; e.OrderY += nx * side * lateral;
                e.Order = linked ? "侧翼截击" : "弧线切入";
                Enemy leader = FindEnemy(e.RelayId);
                if (linked && leader != null && (leader.BrainState == "准备" || leader.BrainState == "压制"))
                {
                    // Hold both lateral approach routes while ranged units force a response.
                    e.OrderX += nx * 145; e.OrderY += ny * 145; e.Order = "保留截击";
                }
            }
            if (e.Kind != "relay" && e.AttackPhase == 0 && e.Recovery <= 0 && e.Channel <= 0) e.Intent = e.Order;
            e.OrderX = Clamp(e.OrderX, Left + e.R, Right - e.R);
            e.OrderY = Clamp(e.OrderY, Top + e.R, Bottom - e.R);
        }

        private Enemy FindRepairTarget(Enemy healer)
        {
            Enemy target = null; float best = float.MaxValue;
            for (int i = 0; i < Enemies.Count; i++)
            {
                Enemy e = Enemies[i];
                if (e == healer || e.Kind == "boss" || e.Kind == "repairer" || e.Hp <= 0 || e.AttackPhase == -1 || e.Hp >= e.MaxHp * .86f) continue;
                float distance = DistanceSquared(e.X, e.Y, healer.X, healer.Y);
                if (distance > 260 * 260) continue;
                float score = distance * (e.Kind == "relay" ? .4f : e.Kind == "prism" ? .7f : 1) + (e.Hp / Math.Max(e.MaxHp, 1)) * 3000;
                if (score < best) { target = e; best = score; }
            }
            return target;
        }

        private void StopRepair(Enemy e)
        {
            e.Channel = 0; e.RepairTargetId = 0; e.AttackPhase = 0; e.Windup = 0;
            e.Recovery = Math.Max(e.Recovery, 1.8f); e.Cooldown = Math.Max(e.Cooldown, 3.1f);
            e.BrainState = "恢复"; e.Intent = "修复被打断";
        }

        private bool UpdateRepair(Enemy e, float dt)
        {
            if (e.Channel > 0)
            {
                Enemy patient = FindEnemy(e.RepairTargetId);
                if (e.JamTime > 0 || e.Stun > 0 || e.Recovery > 0 || patient == null || patient.Hp <= 0 || DistanceSquared(e.X, e.Y, patient.X, patient.Y) > 280 * 280)
                { StopRepair(e); return false; }
                e.Channel = Math.Max(0, e.Channel - dt); e.VX = e.VY = 0; e.BrainState = "引导"; e.Intent = "修复 " + EnemyName(patient.Kind);
                if (e.Channel <= 0)
                {
                    // A bounded pulse, never resurrection or permanent damage negation.
                    patient.Hp = Math.Min(patient.MaxHp, patient.Hp + Math.Min(patient.MaxHp * .18f, 35 + Math.Min(Time, 900) * .06f));
                    e.AttackSerial++;
                    AddEffect("repair", e.X, e.Y, patient.X, patient.Y, 12, .5f);
                    e.RepairTargetId = 0; e.Cooldown = 5.2f; e.Recovery = .65f;
                }
                return true;
            }
            if (e.Cooldown <= 0 && e.Recovery <= 0 && e.JamTime <= 0 && e.BurnTime <= 0 && Phase != "喘息")
            {
                Enemy patient = FindRepairTarget(e);
                if (patient != null && DistanceSquared(e.X, e.Y, patient.X, patient.Y) < 220 * 220)
                { e.RepairTargetId = patient.Id; e.Channel = e.MaxChannel = 1.6f; e.Intent = "引导修复 · 命中可打断"; return true; }
            }
            return false;
        }

        private void UpdateFacing(Enemy e, float dt)
        {
            if (e.AttackPhase == 1 || e.AttackPhase == 2 || e.Stun > 0) return;
            float angle = (float)Math.Atan2(e.FacingY, e.FacingX), target = (float)Math.Atan2(Y - e.Y, X - e.X);
            float difference = (float)Math.Atan2(Math.Sin(target - angle), Math.Cos(target - angle));
            angle += Clamp(difference, -1.65f * dt, 1.65f * dt);
            e.FacingX = (float)Math.Cos(angle); e.FacingY = (float)Math.Sin(angle);
        }

        private float ApplyTacticalDamage(Enemy e, float damage, float sourceX, float sourceY)
        {
            if (e.Kind == "repairer" && e.Channel > 0 && damage >= 5) StopRepair(e);
            if (e.Kind == "boss" && e.Recovery > 0) damage *= 1.35f;
            if (e.Kind == "prism" && e.JamTime <= 0 && e.Stun <= 0 && e.Recovery <= 0)
            {
                float dx = sourceX - e.X, dy = sourceY - e.Y, length = (float)Math.Sqrt(dx * dx + dy * dy);
                float dot = (dx * e.FacingX + dy * e.FacingY) / Math.Max(.01f, length);
                if (dot > .5f) damage *= .35f;
                else
                {
                    damage *= dot < -.35f ? 1.45f : 1.10f;
                    if (damage >= 8 && e.AttackPhase == 1)
                    { e.AttackPhase = 0; e.Windup = 0; e.Recovery = 1.1f; e.Cooldown = 2.7f; CancelEnemyWarning(e.Id); e.Intent = "护面被击穿"; }
                }
            }
            return damage;
        }

        private bool IsProtected(Enemy e)
        {
            if (e.Hp <= 0 || e.GuardId <= 0 || (e.Kind != "shooter" && e.Kind != "relay" && e.Kind != "repairer" && e.Kind != "prism")) return false;
            Enemy guard = FindEnemy(e.GuardId);
            if (guard == null || guard.Kind != "tank" || guard.Hp <= 0 || guard.Stun > 0 || guard.JamTime > 0 || guard.Recovery > 0 || guard.GuardId != e.Id) return false;
            float dx = X - e.X, dy = Y - e.Y, length = (float)Math.Sqrt(dx * dx + dy * dy);
            float gx = guard.X - e.X, gy = guard.Y - e.Y;
            float forward = (gx * dx + gy * dy) / Math.Max(1, length);
            float lateral = Math.Abs(gx * dy - gy * dx) / Math.Max(1, length);
            // Shield works only while the guard physically occupies the firing lane.
            return forward > 12 && forward < length && lateral < 44 && gx * gx + gy * gy < 125 * 125;
        }

        public void CancelEnemyWarning(int id)
        {
            for (int i = Hazards.Count - 1; i >= 0; i--) if (Hazards[i].OwnerId == id && Hazards[i].Warning > 0) RemoveFast(Hazards, i);
        }

        public void DisruptRelay(Enemy relay)
        {
            if (relay == null || relay.Kind != "relay" || relay.RelayBroken) return;
            if (relay.Hp <= 0) relay.RelayBroken = true;
            relay.Recovery = Math.Max(relay.Recovery, 3.2f);
            int count = 0;
            for (int i = 0; i < Enemies.Count; i++)
            {
                Enemy e = Enemies[i];
                if (e.Hp <= 0 || e.Kind == "boss" || (e.RelayId != relay.Id && e.GuardId != relay.Id)) continue;
                e.RelayId = e.GuardId = 0; e.Shielded = false;
                e.Channel = 0; e.RepairTargetId = 0;
                e.Stun = Math.Max(e.Stun, 1.4f); e.Recovery = Math.Max(e.Recovery, 3.2f);
                e.AttackPhase = 0; e.Windup = 0; e.Cooldown = Math.Max(e.Cooldown, 3.0f);
                e.VX = e.VY = 0; e.Order = "链路中断"; CancelEnemyWarning(e.Id); count++;
            }
            if (count > 0)
            {
                AddEffect("disrupt", relay.X, relay.Y, 0, 0, 150, .65f);
                Say("指挥链中断 · 编队失序，趁机突破", 2.8f); Event("break");
            }
            tacticClock = 0;
        }

        private void UpdateTacticalEnemies(float dt)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                Enemy e = Enemies[i]; if (e.Hp <= 0) continue;
                e.Life += dt;
                if (e.AttackPhase == -1) { e.Timer -= dt; if (e.Timer <= 0) { e.AttackPhase = 0; PlanPosition(e); } continue; }
                UpdateFacing(e, dt);
                if (e.Kind == "boss")
                {
                    e.BossStage = e.Hp < e.MaxHp * .5f ? 2 : 1;
                    e.BrainState = e.Recovery > 0 ? "恢复" : e.AttackPhase == 1 ? "蓄力" : "追猎";
                }
                if (e.Kind == "repairer" && e.Channel > 0 && (e.Stun > 0 || e.JamTime > 0 || e.Recovery > 0)) StopRepair(e);
                if (e.Stun > 0) { e.Stun = Math.Max(0, e.Stun - dt); e.VX = e.VY = 0; continue; }
                e.Cooldown -= dt;
                if (e.Kind == "repairer" && UpdateRepair(e, dt)) continue;
                if (e.AttackPhase == 1)
                {
                    e.VX = e.VY = 0; e.Windup -= dt;
                    if (e.Windup <= 0) FinishAttack(e);
                }
                else if (e.AttackPhase == 2)
                {
                    e.X += e.VX * dt; e.Y += e.VY * dt; e.Timer -= dt;
                    if (e.Timer <= 0) { e.AttackPhase = 0; e.Cooldown = 2.8f; e.Recovery = Math.Max(e.Recovery, .75f); e.Intent = "截击落空 · 暴露"; }
                }
                else
                {
                    float distance = DistanceSquared(X, Y, e.X, e.Y);
                    bool special = e.Kind == "runner" || e.Kind == "shooter" || e.Kind == "bomber" || e.Kind == "boss" || e.Kind == "prism";
                    float range = e.Kind == "runner" ? 450 : e.Kind == "prism" ? 440 : e.Kind == "boss" ? 1100 : 630;
                    if (special && e.Cooldown <= 0 && e.JamTime <= 0 && e.Recovery <= 0 && distance < range * range && TryBeginAttack(e)) { }
                    else
                    {
                        float dx = e.OrderX - e.X, dy = e.OrderY - e.Y, d = (float)Math.Sqrt(dx * dx + dy * dy);
                        float speed = e.Speed * (e.JamTime > 0 ? .5f : 1);
                        float travel = Math.Min(d, speed * dt);
                        if (e.Recovery > 0 && e.Kind == "runner") travel *= .35f;
                        if (e.Recovery > 0 && (e.Kind == "boss" || e.Kind == "prism")) travel *= .18f;
                        e.VX = d > 6 ? dx / Math.Max(d, 1) * travel / dt : 0;
                        e.VY = d > 6 ? dy / Math.Max(d, 1) * travel / dt : 0;
                        e.X += e.VX * dt; e.Y += e.VY * dt;
                    }
                }
                e.X = Clamp(e.X, Left + e.R, Right - e.R); e.Y = Clamp(e.Y, Top + e.R, Bottom - e.R);
                if (DistanceSquared(X, Y, e.X, e.Y) < (e.R + 11) * (e.R + 11)) Hurt(e.Damage);
                if (Mode == "over") return;
            }
        }

        private bool TryBeginAttack(Enemy e)
        {
            if (Phase == "喘息" && e.Kind != "boss") return false;
            Enemy leader = FindEnemy(e.RelayId);
            if (leader != null)
            {
                if ((e.Kind == "shooter" || e.Kind == "bomber") && leader.BrainState != "压制") return false;
                if ((e.Kind == "runner" || e.Kind == "prism") && leader.BrainState != "截击") return false;
            }
            if (Time < nextSpecial) return false;
            int active = 0;
            bool bossWaiting = false;
            for (int i = 0; i < Enemies.Count; i++)
            {
                Enemy other = Enemies[i]; if (other.Hp <= 0) continue;
                if (other.AttackPhase == 1 || other.AttackPhase == 2) active++;
                if (other.Kind == "boss" && other.AttackPhase == 0 && other.Cooldown <= 0) bossWaiting = true;
            }
            if (active >= SpecialAttackLimit) return false;
            if (e.Kind != "boss" && bossWaiting && active >= SpecialAttackLimit - 1) return false;
            bool area = e.Kind == "bomber" || e.Kind == "boss" || e.Kind == "prism";
            if (area && Hazards.Count >= 5) return false;
            float tx = DecoyTime > 0 ? DecoyX : X, ty = DecoyTime > 0 ? DecoyY : Y;
            float lead = e.Kind == "runner" ? 1.10f : e.Kind == "shooter" ? 1.70f : e.Kind == "prism" ? .80f : e.Kind == "boss" ? 1.10f : 1.20f;
            if (e.Kind == "shooter" && DecoyTime <= 0)
            {
                // Solve an intercept using the observed velocity and a fixed 0.9s firing tell.
                float px = X + PlayerVX * .9f - e.X, py = Y + PlayerVY * .9f - e.Y;
                float a = PlayerVX * PlayerVX + PlayerVY * PlayerVY - 255 * 255;
                float b = 2 * (px * PlayerVX + py * PlayerVY), c = px * px + py * py;
                float discriminant = b * b - 4 * a * c;
                if (discriminant >= 0 && Math.Abs(a) > .01f)
                {
                    float t1 = (-b - (float)Math.Sqrt(discriminant)) / (2 * a), t2 = (-b + (float)Math.Sqrt(discriminant)) / (2 * a);
                    float travel = t1 > 0 ? t1 : t2 > 0 ? t2 : .8f;
                    lead = Math.Min(2.6f, .9f + travel);
                }
            }
            if (e.RelayId > 0 && e.Kind != "shooter") lead += .05f;
            if (e.Kind == "runner" && ObservedDashAge > .20f && ObservedDashAge < 1.05f) lead = .9f;
            if (e.Kind == "boss" && e.AttackSerial % 3 == 2) lead = .35f;
            PredictPosition(lead, out tx, out ty);
            float radius = e.Kind == "boss" ? 96 : 64;
            tx = Clamp(tx, Left + 18, Right - 18); ty = Clamp(ty, Top + 18, Bottom - 18);
            float windup = e.Kind == "runner" ? .78f : e.Kind == "shooter" ? .9f : e.Kind == "prism" ? .85f : e.Kind == "boss" ? 1.15f : 1.25f;
            List<Hazard> proposed = area ? AttackHazards(e, tx, ty, windup) : null;
            if (!LeavesEscape(e, tx, ty, area ? radius : 22, proposed)) { e.Cooldown = .28f; return false; }
            e.AttackPhase = 1; e.TargetX = tx; e.TargetY = ty;
            e.Windup = windup;
            e.Intent = e.Kind == "boss" ? (e.AttackSerial % 3 == 0 ? "审判圆区" : e.AttackSerial % 3 == 1 ? "棱线切割" : "回声环震") : e.Kind == "prism" ? "护面突刺 · 侧背可打断" : e.Kind == "runner" && ObservedDashAge < 1.05f ? "冲刺后接力" : "锁定 · 不再追踪";
            if (e.Kind == "boss") e.BrainState = "蓄力";
            e.VX = e.VY = 0;
            // Stagger the starts: no frame can suddenly open every attack lane.
            nextSpecial = Time + .30f;
            if (area)
            {
                for (int i = 0; i < proposed.Count && Hazards.Count < HazardLimit; i++) Hazards.Add(proposed[i]);
            }
            Event("warning"); return true;
        }

        private List<Hazard> AttackHazards(Enemy e, float tx, float ty, float warning)
        {
            List<Hazard> result = new List<Hazard>(2);
            Hazard h = new Hazard { OwnerId = e.Id, X = tx, Y = ty, R = e.Kind == "boss" ? 96 : 64,
                Warning = warning, Life = e.Kind == "boss" ? 1.0f : 2.0f, Damage = e.Damage * .80f };
            if (e.Kind == "prism")
            {
                h.Shape = "line"; h.X = e.X; h.Y = e.Y;
                h.X2 = Clamp(e.X + e.FacingX * 420, Left + 18, Right - 18);
                h.Y2 = Clamp(e.Y + e.FacingY * 420, Top + 18, Bottom - 18);
                h.R = 28; h.Life = .28f;
            }
            else if (e.Kind == "boss" && e.AttackSerial % 3 == 1)
            {
                float dx = PlayerVX, dy = PlayerVY, distance = (float)Math.Sqrt(dx * dx + dy * dy);
                if (distance < 30) { dx = tx - e.X; dy = ty - e.Y; distance = (float)Math.Sqrt(dx * dx + dy * dy); }
                dx /= Math.Max(1, distance); dy /= Math.Max(1, distance);
                if (Math.Abs(dx) + Math.Abs(dy) < .01f) dx = 1;
                h.Shape = "line"; h.R = e.BossStage >= 2 ? 24 : 28; h.Life = .8f;
                h.X = Clamp(tx - dy * 220, Left + 18, Right - 18); h.Y = Clamp(ty + dx * 220, Top + 18, Bottom - 18);
                h.X2 = Clamp(tx + dy * 220, Left + 18, Right - 18); h.Y2 = Clamp(ty - dx * 220, Top + 18, Bottom - 18);
                if (e.BossStage >= 2)
                {
                    Hazard second = new Hazard { OwnerId = e.Id, Shape = "line", Warning = warning, Life = h.Life, Damage = h.Damage, R = 24,
                        X = Clamp(h.X + dx * 130, Left + 18, Right - 18), Y = Clamp(h.Y + dy * 130, Top + 18, Bottom - 18),
                        X2 = Clamp(h.X2 + dx * 130, Left + 18, Right - 18), Y2 = Clamp(h.Y2 + dy * 130, Top + 18, Bottom - 18) };
                    result.Add(second);
                }
            }
            else if (e.Kind == "boss" && e.AttackSerial % 3 == 2)
            { h.Shape = "ring"; h.R = 180; h.InnerR = 100; h.Life = .9f; }
            result.Add(h); return result;
        }

        public static bool HazardContains(Hazard h, float px, float py, float padding)
        {
            if (h.Shape == "line")
                return WorldSegmentDistanceSquared(px, py, h.X, h.Y, h.X2, h.Y2) < (h.R + padding) * (h.R + padding);
            float distance = DistanceSquared(px, py, h.X, h.Y);
            if (h.Shape == "ring") return distance < (h.R + padding) * (h.R + padding) && distance > Math.Max(0, h.InnerR - padding) * Math.Max(0, h.InnerR - padding);
            return distance < (h.R + padding) * (h.R + padding);
        }

        private bool LeavesEscape(Enemy attacker, float tx, float ty, float radius, List<Hazard> proposed)
        {
            // Reserve at least one walkable direction. A warning is not yet a blocking floor:
            // allow crossing it before detonation, but always require a safe destination.
            // This is a bounded eight-sector fairness check, not navigation over the whole arena.
            bool proposedArea = proposed != null;
            float proposedWarning = proposedArea && proposed.Count > 0 ? proposed[0].Warning : 0;
            // Staying inside a ring or taking a short step is also a valid response.
            for (int route = 0; route < 3; route++) for (int sector = 0; sector < (route == 0 ? 1 : 8); sector++)
            {
                float travel = route == 0 ? 0 : route == 1 ? 80 : 175;
                float angle = sector * (float)Math.PI / 4;
                float ex = X + (float)Math.Cos(angle) * travel, ey = Y + (float)Math.Sin(angle) * travel;
                if (ex < Left + 18 || ex > Right - 18 || ey < Top + 18 || ey > Bottom - 18) continue;
                bool safe = true;
                for (int sample = 1; sample <= 2 && safe; sample++)
                {
                    float px = X + (ex - X) * sample / 2, py = Y + (ey - Y) * sample / 2;
                    float arrive = travel * sample / 2 / (220 + Rank("speed") * 16);
                    if (!proposedArea && AttackCovers(attacker, tx, ty, radius, px, py)) { safe = false; break; }
                    if (proposedArea && (sample == 2 || proposedWarning <= arrive))
                        for (int j = 0; j < proposed.Count && safe; j++) if (HazardContains(proposed[j], px, py, 18)) safe = false;
                    for (int j = 0; j < Hazards.Count && safe; j++)
                        if ((sample == 2 || Hazards[j].Warning <= arrive) && HazardContains(Hazards[j], px, py, 18)) safe = false;
                    for (int j = 0; j < Enemies.Count && safe; j++)
                    {
                        Enemy e = Enemies[j];
                        if (e.Hp <= 0 || e.AttackPhase != 1 || e.Kind == "boss" || e.Kind == "bomber" || e.Kind == "prism") continue;
                        if (AttackCovers(e, e.TargetX, e.TargetY, 24, px, py)) safe = false;
                    }
                }
                if (safe) return true;
            }
            return false;
        }

        private bool AttackCovers(Enemy e, float tx, float ty, float radius, float px, float py)
        {
            if (e.Kind == "bomber" || e.Kind == "boss") return DistanceSquared(px, py, tx, ty) < (radius + 18) * (radius + 18);
            float dx = tx - e.X, dy = ty - e.Y, d2 = dx * dx + dy * dy;
            float t = Clamp(((px - e.X) * dx + (py - e.Y) * dy) / Math.Max(1, d2), 0, 1.4f);
            return DistanceSquared(px, py, e.X + dx * t, e.Y + dy * t) < (radius + 15) * (radius + 15);
        }

        private void FinishAttack(Enemy e)
        {
            e.AttackSerial++;
            if (e.Kind == "runner")
            {
                float dx = e.TargetX - e.X, dy = e.TargetY - e.Y, d = (float)Math.Sqrt(dx * dx + dy * dy);
                e.VX = dx / Math.Max(1, d) * 520; e.VY = dy / Math.Max(1, d) * 520;
                e.AttackPhase = 2; e.Timer = Clamp(d / 520, .35f, .86f);
            }
            else if (e.Kind == "shooter")
            {
                float angle = (float)Math.Atan2(e.TargetY - e.Y, e.TargetX - e.X);
                int count = Time > 180 ? 3 : 1;
                for (int j = 0; j < count; j++) AddShot(e.X, e.Y, angle + (j - (count - 1) / 2f) * .18f, 255, e.Damage, true, 0, "enemy");
                e.AttackPhase = 0; e.Cooldown = e.RelayId > 0 ? 2.4f : 2.9f; e.Recovery = .32f;
            }
            else
            {
                bool resolvedHazard = false;
                for (int i = 0; i < Hazards.Count; i++)
                {
                    Hazard h = Hazards[i]; if (h.OwnerId != e.Id || h.Warning <= 0) continue;
                    resolvedHazard = true;
                    h.Warning = 0;
                    if (h.Shape == "circle") AddEffect("aoeBlast", h.X, h.Y, 0, 0, h.R, .42f);
                    if (HazardContains(h, X, Y, 10)) Hurt(e.Damage * 1.25f);
                }
                // Public scenario fixtures can supply a legacy circular windup without a Hazard.
                if (!resolvedHazard && (e.Kind == "bomber" || (e.Kind == "boss" && (e.AttackSerial - 1) % 3 == 0)))
                {
                    float radius = e.Kind == "boss" ? 96 : 64;
                    AddEffect("aoeBlast", e.TargetX, e.TargetY, 0, 0, radius, .42f);
                    if (DistanceSquared(X, Y, e.TargetX, e.TargetY) < (radius + 10) * (radius + 10)) Hurt(e.Damage * 1.25f);
                }
                if (e.Kind == "boss")
                {
                    // Every distinct attack ends in a visible punish window; no simultaneous bullet ring.
                    e.Recovery = e.BossStage >= 2 ? 1.45f : 1.8f; e.BrainState = "恢复"; e.Intent = "核心暴露 · 受到伤害 +35%";
                }
                if (e.Kind == "prism") { e.Recovery = 1.25f; e.Intent = "护甲过热 · 反击窗口"; }
                e.AttackPhase = 0; e.Cooldown = e.Kind == "boss" ? (e.BossStage >= 2 ? 2.55f : 3.0f) : e.Kind == "prism" ? 3.5f : 3.8f;
            }
        }

        private void UpdateHazards(float dt)
        {
            for (int i = Hazards.Count - 1; i >= 0; i--)
            {
                Hazard h = Hazards[i];
                if (h.Warning > 0)
                {
                    Enemy owner = FindEnemy(h.OwnerId);
                    if (owner == null || owner.Hp <= 0 || owner.AttackPhase != 1) { RemoveFast(Hazards, i); continue; }
                    h.Warning = Math.Max(.001f, owner.Windup); continue;
                }
                h.Life -= dt;
                if (h.Life <= 0) { RemoveFast(Hazards, i); continue; }
                if (HazardContains(h, X, Y, 8)) Hurt(h.Damage);
                if (Mode == "over") return;
            }
        }
    }
}
