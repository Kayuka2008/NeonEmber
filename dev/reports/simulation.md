# Game simulation verification
Independent C# core tests; no renderer, input device or human gameplay assertions. Bot timing is diagnostic, not evidence of human balance. Wall time measures simulation only, not rendered FPS.
PASS | Start/reset and three loadouts
PASS | Upgrade resumes simulation
PASS | Overflow XP queues multiple levels and invalid choice is harmless
PASS | Death has priority over level up
PASS | Lethal contact and XP pickup in same frame prioritizes death
PASS | Dash cooldown prevents retrigger and expires
CONTACT | 30fps=170 60fps=170 144fps=170
PASS | Contact damage is substantial and approximately FPS independent
PASS | AOE warning delays damage and fixed target can be dodged
PASS | Shop balance, insufficient funds, and frozen combat clock
PASS | Boss schedule and no stacked bosses
PASS | Boss overlap defers shop until boss dies
PASS | Orb capacity merges value rather than dropping reward
PASS | Upgrade card pool remains usable after extensive progression
BOT | weapon=0 seed=41 immortal=False time=137.7s level=12 kills=290 hp=0.0 upgrades=11 interval_mean=11.92s median=10.48s max=29.87s target15to25=1/11 peaks(enemy/shot/orb/effect)=19/19/77/19 sim_steps=8261 wall=0.03s
TACTICAL | weapon=0 seed=41 hazards_peak=2 special_attacks_peak=3
PASS | Normal-health diagnostic bot weapon 0
BOT | weapon=1 seed=42 immortal=False time=138.5s level=13 kills=290 hp=0.0 upgrades=12 interval_mean=11.50s median=9.49s max=22.32s target15to25=3/12 peaks(enemy/shot/orb/effect)=26/11/47/31 sim_steps=8312 wall=0.03s
TACTICAL | weapon=1 seed=42 hazards_peak=3 special_attacks_peak=3
PASS | Normal-health diagnostic bot weapon 1
BOT | weapon=2 seed=43 immortal=False time=165.7s level=15 kills=371 hp=0.0 upgrades=14 interval_mean=11.29s median=11.95s max=21.90s target15to25=1/14 peaks(enemy/shot/orb/effect)=35/6/16/31 sim_steps=9943 wall=0.04s
TACTICAL | weapon=2 seed=43 hazards_peak=3 special_attacks_peak=3
PASS | Normal-health diagnostic bot weapon 2
BOSS | weapon=1 seed=198 spawned=180.00s lifetime=5.93s
BOSS | weapon=1 seed=198 spawned=360.01s lifetime=9.48s
BOSS | weapon=1 seed=198 spawned=540.01s lifetime=12.23s
BOSS | weapon=1 seed=198 spawned=720.00s lifetime=19.81s
BOT | weapon=1 seed=198 immortal=True time=900.0s level=52 kills=5324 hp=99957.6 upgrades=51 interval_mean=17.61s median=18.63s max=27.18s target15to25=33/51 peaks(enemy/shot/orb/effect)=136/27/34/77 sim_steps=54011 wall=0.52s
TACTICAL | weapon=1 seed=198 hazards_peak=5 special_attacks_peak=3
PASS | 900-second long run finite values and entity limits
BOSS | weapon=0 seed=210 spawned=180.00s lifetime=9.73s
BOSS | weapon=0 seed=210 spawned=360.01s lifetime=9.35s
BOSS | weapon=0 seed=210 spawned=540.01s lifetime=8.16s
BOSS | weapon=0 seed=210 spawned=720.00s lifetime=25.39s
BOT | weapon=0 seed=210 immortal=True time=900.0s level=52 kills=5356 hp=99992.8 upgrades=51 interval_mean=17.56s median=18.05s max=26.66s target15to25=27/51 peaks(enemy/shot/orb/effect)=115/26/52/75 sim_steps=54011 wall=0.43s
TACTICAL | weapon=0 seed=210 hazards_peak=5 special_attacks_peak=3
PASS | Final multiseed 900-second run 0
BOSS | weapon=1 seed=211 spawned=180.00s lifetime=18.63s
BOSS | weapon=1 seed=211 spawned=360.01s lifetime=5.47s
BOSS | weapon=1 seed=211 spawned=540.01s lifetime=39.04s
BOSS | weapon=1 seed=211 spawned=720.00s lifetime=21.01s
BOT | weapon=1 seed=211 immortal=True time=900.0s level=51 kills=5322 hp=99904.7 upgrades=50 interval_mean=17.47s median=19.81s max=26.18s target15to25=30/50 peaks(enemy/shot/orb/effect)=133/27/34/67 sim_steps=54011 wall=0.54s
TACTICAL | weapon=1 seed=211 hazards_peak=5 special_attacks_peak=3
PASS | Final multiseed 900-second run 1
BOSS | weapon=2 seed=212 spawned=180.00s lifetime=19.96s
BOSS | weapon=2 seed=212 spawned=360.01s lifetime=26.21s
BOSS | weapon=2 seed=212 spawned=540.01s lifetime=9.66s
BOSS | weapon=2 seed=212 spawned=720.00s lifetime=9.03s
BOT | weapon=2 seed=212 immortal=True time=900.0s level=52 kills=5356 hp=99987.7 upgrades=51 interval_mean=17.54s median=18.52s max=27.68s target15to25=28/51 peaks(enemy/shot/orb/effect)=116/25/40/71 sim_steps=54011 wall=0.46s
TACTICAL | weapon=2 seed=212 hazards_peak=5 special_attacks_peak=3
PASS | Final multiseed 900-second run 2
BOSS | weapon=0 seed=213 spawned=180.00s lifetime=15.98s
BOSS | weapon=0 seed=213 spawned=360.01s lifetime=18.78s
BOSS | weapon=0 seed=213 spawned=540.01s lifetime=11.58s
BOSS | weapon=0 seed=213 spawned=720.00s lifetime=21.86s
BOT | weapon=0 seed=213 immortal=True time=900.0s level=52 kills=5346 hp=99857.9 upgrades=51 interval_mean=17.51s median=18.86s max=26.33s target15to25=31/51 peaks(enemy/shot/orb/effect)=110/28/57/69 sim_steps=54011 wall=0.42s
TACTICAL | weapon=0 seed=213 hazards_peak=5 special_attacks_peak=3
PASS | Final multiseed 900-second run 3
BOSS | weapon=1 seed=214 spawned=180.00s lifetime=9.11s
BOSS | weapon=1 seed=214 spawned=360.01s lifetime=13.16s
BOSS | weapon=1 seed=214 spawned=540.01s lifetime=21.99s
BOSS | weapon=1 seed=214 spawned=720.00s lifetime=30.16s
BOT | weapon=1 seed=214 immortal=True time=900.0s level=52 kills=5343 hp=99998.4 upgrades=51 interval_mean=17.61s median=19.26s max=27.34s target15to25=31/51 peaks(enemy/shot/orb/effect)=141/31/40/70 sim_steps=54011 wall=0.69s
TACTICAL | weapon=1 seed=214 hazards_peak=5 special_attacks_peak=3
PASS | Final multiseed 900-second run 4
BOSS | weapon=2 seed=215 spawned=180.00s lifetime=5.13s
BOSS | weapon=2 seed=215 spawned=360.01s lifetime=21.41s
BOSS | weapon=2 seed=215 spawned=540.01s lifetime=24.28s
BOSS | weapon=2 seed=215 spawned=720.00s lifetime=25.23s
BOT | weapon=2 seed=215 immortal=True time=900.0s level=52 kills=5342 hp=100000.0 upgrades=51 interval_mean=17.60s median=18.36s max=26.64s target15to25=31/51 peaks(enemy/shot/orb/effect)=124/27/36/75 sim_steps=54011 wall=0.90s
TACTICAL | weapon=2 seed=215 hazards_peak=5 special_attacks_peak=3
PASS | Final multiseed 900-second run 5
BOSS | weapon=0 seed=216 spawned=180.00s lifetime=7.91s
BOSS | weapon=0 seed=216 spawned=360.01s lifetime=29.23s
BOSS | weapon=0 seed=216 spawned=540.01s lifetime=17.93s
BOSS | weapon=0 seed=216 spawned=720.00s lifetime=32.29s
BOT | weapon=0 seed=216 immortal=True time=900.0s level=52 kills=5355 hp=100000.0 upgrades=51 interval_mean=17.52s median=19.18s max=26.36s target15to25=32/51 peaks(enemy/shot/orb/effect)=127/27/50/72 sim_steps=54011 wall=0.79s
TACTICAL | weapon=0 seed=216 hazards_peak=6 special_attacks_peak=3
PASS | Final multiseed 900-second run 6
BOSS | weapon=1 seed=217 spawned=180.00s lifetime=16.18s
BOSS | weapon=1 seed=217 spawned=360.01s lifetime=21.19s
BOSS | weapon=1 seed=217 spawned=540.01s lifetime=5.42s
BOSS | weapon=1 seed=217 spawned=720.00s lifetime=13.95s
BOT | weapon=1 seed=217 immortal=True time=900.0s level=52 kills=5346 hp=100000.0 upgrades=51 interval_mean=17.54s median=19.33s max=27.71s target15to25=28/51 peaks(enemy/shot/orb/effect)=116/30/64/66 sim_steps=54011 wall=0.71s
TACTICAL | weapon=1 seed=217 hazards_peak=5 special_attacks_peak=3
PASS | Final multiseed 900-second run 7
BOSS | weapon=2 seed=218 spawned=180.00s lifetime=4.18s
BOSS | weapon=2 seed=218 spawned=360.01s lifetime=26.33s
BOSS | weapon=2 seed=218 spawned=540.01s lifetime=8.55s
BOSS | weapon=2 seed=218 spawned=720.00s lifetime=27.39s
BOT | weapon=2 seed=218 immortal=True time=900.0s level=52 kills=5344 hp=99994.9 upgrades=51 interval_mean=17.58s median=19.48s max=28.99s target15to25=26/51 peaks(enemy/shot/orb/effect)=117/26/117/72 sim_steps=54011 wall=0.84s
TACTICAL | weapon=2 seed=218 hazards_peak=6 special_attacks_peak=3
PASS | Final multiseed 900-second run 8
RESULT | passed=26 failed=0
