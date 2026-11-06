# Dino Worlds (M1 local review)

Toy-like Unity WebGL raid prototype: shore deployment, then autonomous Velociraptor + Ankylosaurus combat on one fixed isle.

## M1 scope

- One fixed hand-designed enemy isle (cosmetic scenery may vary; pathing ignores it)
- Starter pair: Velociraptor (scout/swarm) + Ankylosaurus (tank) — proposed plan mapping
- Shore deployment, then fully autonomous fight (no mid-battle orders)
- Win / lose / retry without reload
- Fixed elevated camera with pan and zoom only
- Desktop controls (WASD/arrows/right-drag pan, wheel zoom, click shore, Space to start)
- No home economy, no full roster

## Local play

Release build (no QA hooks):

    python3 -m http.server 8000 --directory Builds/WebGL

Then open http://localhost:8000

## Remote steps (NOT done; need Mike's authorization)

This tree is local-only. Creating a GitHub repo, pushing, opening a PR, enabling Pages, or changing visibility needs an explicit go from Mike (plan: via Lead Developer Bot). Existing authorization for other projects does not cover Dino Worlds.
