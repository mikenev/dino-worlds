# Dino Worlds M4 audio: local asset manifest (not integrated)
Source: `/workspace/dino-worlds` branch `m3`, `Assets/Scripts/Audio.cs` (class `DinoWorlds.DwAudio`), read with `git show`. Species come from `Sim.cs` `enum Kind`.
Format: 44.1 kHz 16-bit WAV. Music is stereo, SFX are mono. All of it is original procedural synthesis (generator: `/workspace/dino-gen/dino_m4.py` + `dino_gen.py`, outside any repo).
Code volumes: `musicVol = 0.35`, `sfxVol = 0.55`. The suggested gain multiplies the code volume.

## Files that replace existing Audio.cs fields
| File | Audio.cs field -> method/usage | Dur (s) | Loop | Peak dBFS | Suggested gain |
|---|---|---|---|---|---|
| home_loop.wav (reused dino_cozy_loop, 108 BPM) | `homeLoop` -> `SetBed("home")` | 35.556 | yes (`music.loop = true`, already set) | -3.0 | 1.0 |
| raid_loop.wav (new, 128 BPM, 24 bars) | `raidLoop` -> `SetBed("raid")` | 45.000 | yes (`music.loop = true`) | -3.0 | 1.0 |
| win_sting.wav (reused) | `winSting` -> `Win()` | 2.400 | no | -6.0 | 1.0 |
| lose_sting.wav (reused) | `loseSting` -> `Lose()` | 2.000 | no | -6.0 | 1.0 |
| ui_pop.wav (reused) | `uiPop` -> `Ui()` (options, build, tray, upgrades, toggles) | 0.140 | no | -6.0 | 0.8 (fires often) |
| craft_splash.wav (new: toy boat plop and splash) | `craftSplash` -> `Splash()` (placing a raft on the shore / launch) | 0.800 | no | -6.0 | 1.0 |
| peck_hit.wav (reused) | `peck` -> `Peck()` (generic combat hit, rate-limited 0.12 s in Game.cs) | 0.250 | no | -6.0 | 0.7 (fires often) |
| spit_attack.wav (reused) | `spit` -> `Spit()` (exists; nothing in Game.cs calls it on m3) | 0.550 | no | -6.0 | 1.0 |
| smash_hit.wav (reused) | `smash` -> `Smash()` (exists; nothing in Game.cs calls it on m3) | 0.900 | no | -6.0 | 1.0 |

The home and raid loop slots already exist (`homeLoop` and `raidLoop` are made by `SynthLoop`), so the integrator does not need to add a new music slot. They only need to swap the clip source (for example a serialized/Resources AudioClip in place of `SynthLoop`).

## Species sounds: NEW slots the integrator must add (no per-species fields exist in Audio.cs)
| File | Species (`Kind`) | Character | Dur (s) | Peak | Gain |
|---|---|---|---|---|---|
| velociraptor_call.wav | `Kind.Velociraptor` (fast claw swarm) | quick double chirp-squeak + claw scratch | 0.450 | -6.0 | 0.6 (swarm, loudest RMS) |
| ankylosaurus_call.wav | `Kind.Ankylosaurus` (tank) | low hollow honk + wooden tail-club thunk | 1.000 | -6.0 | 1.0 |
| dilophosaurus_call.wav | `Kind.Dilophosaurus` (stops short and spits) | rattly frill trill + wet spit pop | 0.850 | -6.0 | 1.0 |
| tyrannosaurus_call.wav | `Kind.Tyrannosaurus` (heavy smash) | friendly toy-horn "ROH-oh" + padded stomp | 1.400 | -6.0 | 0.8 |
Suggestion: a `AudioClip[] speciesCall` indexed by `(int)Kind`, played on deploy or on attack with a per-species cooldown. That is the integrator's call; this pack contains no code.

## Unity importer settings
- Music (home_loop, raid_loop): Load Type **Streaming**, Vorbis about q0.5–0.7, Load In Background on, Preload off, Force To Mono off, **Normalize off**. `AudioSource.loop = true`.
- SFX (everything else): Load Type **DecompressOnLoad**, Vorbis about q0.4 (or PCM/ADPCM for the tiny clips), Preload Audio Data on, **Normalize off**. `AudioSource.loop = false` (`PlayOneShot`).
- Loops are sample-exact WAV, so keep them in WAV/OGG. Do not use MP3 for loops.
