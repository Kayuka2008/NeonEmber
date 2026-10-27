# v0.4 tactical and world-system behavior verification
Deterministic core simulation only. These fixtures verify actual movement, damage, interruption and state transitions; they do not establish human difficulty, rendered FPS, or guaranteed escape from every possible crowd/bullet arrangement.
PASS | Observed movement predicts ahead; locked warning does not track reversal
ROUTES | straight_hp=75.00 reversed_hp=100.00
PASS | Counterfactual route: straight movement is hit; reversal avoids the same predicted blast
PASS | Relay assigns coordinated lateral fire positions and shooters move to different sides
PASS | Guard physically moves between shooter and player; mitigation ends outside lane or after death
PASS | Killing relay cancels follower warning and interrupts the whole linked squad
DENSE | active_peak=3 hazards_peak=5
PASS | Dense mixed enemies respect attack budget and retain uncovered arena space
PASS | Ember brand causes damage over time after the original weapon stops
PASS | Breathing phase stops fresh normal attacks but lets announced attacks finish
PASS | Boss receives an attack slot despite a crowd of earlier-listed shooters
PASS | Maximum entity occupancy survives spawn, death rewards and attack allocation
PASS | Echo dash interrupts a charged attack and its hazard
PASS | Decoy redirects only new attack locks while existing target remains fixed
PASS | Forge ward absorbs real hostile hit, waits, then regenerates
PASS | Forge dash hits once per target and disables guard protection
PASS | Interceptor removes nearby hostile projectile without consuming friendly shots
PASS | Mixed-weapon trace rewards alternating actual hit sources
PASS | School ranks cap at three and awaken at six independent of weapon rank
PASS | Ember awakening and propagation damage nearby enemies after a burning death
PASS | Echo awakening expiry pulse interrupts nearby fresh enemy
PASS | Forge awakening safely absorbs shield-break overflow inside projectile iteration
PASS | Cross-school synergies change burn damage and recovery
PASS | Story unlocks only on actual boss deaths, caps chapter and resets with run
PASS | Pause, level and shop freeze all damage, hazards, burn and cooldowns
RESULT | passed=23 failed=0
