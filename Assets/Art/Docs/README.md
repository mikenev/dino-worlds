# Dino Worlds — M4 art delivery

Local assets only. No repository changes, commits, pushes, publishing or integration. Original Blender/Pillow art, continuing the approved toy-vinyl sample direction. Names were read from `/workspace/dino-worlds` branch `m3`: `Sim.cs` (`Kind`, `DefKind`, `Isles.All`) and `Meta.cs` (`BldKind`).

## Drop-in conventions

- Copy this folder with its `.meta` sidecars into a chosen art folder. Keep GUIDs together: the FBX importers already remap to the supplied shared material. No gameplay scripts are included.
- Unity scale **1**, file units enabled. **Y up, +Z forward**, ground-centred pivot. 1 unit = 1 metre = 1 existing Sim cell. `Anchor_Forward` is (0,0,1); `Anchor_Top` is (0,1,0), verified in Unity. Treat these as coordinate reference anchors, not muzzle points.
- Dino foot bottoms and building bases are Y=0. Island/shore top surfaces are Y=0; thickness extends below. Buildings face +Z. Full dimensions and triangle counts are in `manifest.json`.
- One submesh/shared material per FBX; static unrigged meshes, no clips. Keep existing simulation radii, collisions, damage, pathfinding and deployment data. Do not auto-generate mesh colliders.
- `DW_ToyVinyl.mat` uses built-in **Standard**, metallic 0, smoothness 0.58, white tint + atlas. For URP, convert/remap to **URP Lit** with the same base map; do not tint the whole atlas by unit colour.
- Import normals; no tangents needed without normal maps; Read/Write off, mesh compression off initially, animation None. Model texture: 512px sRGB, mipmaps on, Clamp/Bilinear. Supplied textures are uncompressed for clean review; choose tested WebGL compression at integration.

## Model file list and exact mappings

All paths below are under `Models/`.

| File | Exact code mapping | Unity-import triangles |
|---|---|---:|
| DW_Velociraptor.fbx | `Kind.Velociraptor` | 5980 |
| DW_Ankylosaurus.fbx | `Kind.Ankylosaurus` | 4896 |
| DW_Dilophosaurus.fbx | `Kind.Dilophosaurus` | 5812 |
| DW_Tyrannosaurus.fbx | `Kind.Tyrannosaurus` | 4948 |
| DW_RoostHeart.fbx | `DefKind / BldKind.RoostHeart` | 1308 |
| DW_Hatchery.fbx | `BldKind.Hatchery` | 804 |
| DW_Workshop.fbx | `BldKind.Workshop` | 624 |
| DW_AmberPit.fbx | `BldKind.AmberPit` | 168 |
| DW_Nest.fbx | `BldKind.Nest` | 1680 |
| DW_PeckTower.fbx | `DefKind.PeckTower` | 516 |
| DW_FanSpitter.fbx | `DefKind.FanSpitter` | 420 |
| DW_SpikeHedge.fbx | `DefKind.SpikeHedge` | 216 |
| DW_BoulderNest.fbx | `DefKind.BoulderNest` | 528 |

`RoostHeart` is shared by HQ at home (`BldKind`) and the raid win-condition HQ (`DefKind`). `AmberPit` and `Nest` are included so the full current home-building set is covered. No separate Storage building exists: Storage upgrades belong to AmberPit.

## Island and shore list

Each base is 22×18 units; each shore strip is 22×2 units. These are cosmetic, collision-free recolors, not alternate battle layouts.

| Files (under Models/) | Exact `Isles.All` mapping |
|---|---|
| `DW_Island_0.fbx` + `DW_Shore_0.fbx` | `Isles.All[0]` — Shore Isle |
| `DW_Island_1.fbx` + `DW_Shore_1.fbx` | `Isles.All[1]` — Tide Cove |
| `DW_Island_2.fbx` + `DW_Shore_2.fbx` | `Isles.All[2]` — Lagoon Shelf |
| `DW_Island_3.fbx` + `DW_Shore_3.fbx` | `Isles.All[3]` — Ridge Pass |
| `DW_Island_4.fbx` + `DW_Shore_4.fbx` | `Isles.All[4]` — Stone Spur |
| `DW_Island_5.fbx` + `DW_Shore_5.fbx` | `Isles.All[5]` — Wind Cut |
| `DW_Island_6.fbx` + `DW_Shore_6.fbx` | `Isles.All[6]` — Ember Spire |
| `DW_Island_7.fbx` + `DW_Shore_7.fbx` | `Isles.All[7]` — Ash Crown |

Place island bases at **(11,0,9)** to span Sim x=0..22, z=0..18. Place shore strips at **(11,0.025,1)** to cover the first two shoreline rows without z-fighting. The allowed deploy cells must still come from `IsleData.Shore`; the continuous sand strip does not mark every cell as valid. Do not add obstacles or change any fixed defense positions.

## UI file list / slicing

Text-free skins let Game Dev render legible runtime labels and counters. Use Unity UI Image `Sliced` for skins, with borders **L,B,R,T in pixels**. Use the selected/disabled states as appropriate. The tray is a pre-raid selection/deployment tray, never a mid-battle command tray.

| File (under UI/) | Pixels | Border L,B,R,T |
|---|---|---|
| DW_Tray.png | 512×256 | 40,40,40,40 |
| DW_Card.png | 256×320 | 24,24,24,24 |
| DW_Card_Selected.png | 256×320 | 24,24,24,24 |
| DW_Card_Disabled.png | 256×320 | 24,24,24,24 |
| DW_Button_Primary.png | 256×96 | 28,28,28,28 |
| DW_Button_Pressed.png | 256×96 | 28,28,28,28 |
| DW_Button_Secondary.png | 256×96 | 28,28,28,28 |
| DW_Button_Disabled.png | 256×96 | 28,28,28,28 |
| DW_Counter.png | 256×80 | 28,28,28,28 |

**128×128 transparent mesh icons** (exact enum mappings match the model list):

- `UI/DW_Icon_Velociraptor.png` → `Kind.Velociraptor`.
- `UI/DW_Icon_Ankylosaurus.png` → `Kind.Ankylosaurus`.
- `UI/DW_Icon_Dilophosaurus.png` → `Kind.Dilophosaurus`.
- `UI/DW_Icon_Tyrannosaurus.png` → `Kind.Tyrannosaurus`.
- `UI/DW_Icon_RoostHeart.png` → `DefKind / BldKind.RoostHeart`.
- `UI/DW_Icon_Hatchery.png` → `BldKind.Hatchery`.
- `UI/DW_Icon_Workshop.png` → `BldKind.Workshop`.
- `UI/DW_Icon_AmberPit.png` → `BldKind.AmberPit`.
- `UI/DW_Icon_Nest.png` → `BldKind.Nest`.
- `UI/DW_Icon_PeckTower.png` → `DefKind.PeckTower`.
- `UI/DW_Icon_FanSpitter.png` → `DefKind.FanSpitter`.
- `UI/DW_Icon_SpikeHedge.png` → `DefKind.SpikeHedge`.
- `UI/DW_Icon_BoulderNest.png` → `DefKind.BoulderNest`.

**128×128 transparent resource/control icons** (UI, not additional Sim enum values):

- `UI/DW_Icon_Amber.png` → resource Amber.
- `UI/DW_Icon_BoneShards.png` → resource BoneShards.
- `UI/DW_Icon_Population.png` → UI control Population.
- `UI/DW_Icon_Range.png` → UI control Range.
- `UI/DW_Icon_Play.png` → UI control Play.
- `UI/DW_Icon_Back.png` → UI control Back.
- `UI/DW_Icon_Upgrade.png` → UI control Upgrade.
- `UI/DW_Icon_Map.png` → UI control Map.
- `UI/DW_Icon_Pause.png` → UI control Pause.
- `UI/DW_Icon_Close.png` → UI control Close.

- `UI/DW_Icons_Atlas.png` — 1024×512, 23 named Multiple sprites, 128×128 cells. Sprite rects are provided both in `.meta` and `manifest.json` using Unity bottom-left coordinates. Use this atlas **or** individual icons, not both in the shipped build.
- All UI: Sprite, FullRect, 100 PPU, centre pivot, alpha transparency, sRGB, Bilinear/Clamp, mipmaps off. No baked runtime text. Primary Play icon is cream for placement on the dark teal button.

## Shared/supporting files

- `Textures/DW_ToyAtlas.png` — one 512×512 palette atlas used by every FBX, with swatch gutters for mip safety.
- `Materials/DW_ToyVinyl.mat` — shared Standard material + texture reference.
- `manifest.json` — every asset mapping, dimensions, triangle counts, texture sizes, borders and import settings.
- `DW_M4_ContactSheet.png` — one 2400×2550 contact-sheet preview of all geometry, skin states and icons.
- `Unity_Import_Report.json` — Unity 6000.3.26f1 native import validation: **29 FBX + 33 UI textures pass**, with 23 named atlas sprites verified in metadata. Triangle counts in the manifest are Unity-imported; authored counts are retained separately (Unity removes zero-area bevel triangles on three buildings).
- `.meta` sidecars accompany imported assets; keep them for shared material bindings and UI slicing.

## Integration caveats

No gameplay/in-game visual or WebGL performance test was run. Static meshes have no skeletal rig, locomotion/attack clips, hitboxes, navmesh or LOD chain; Game Dev owns animation/motion, damage FX, collision and runtime UI text. The mesh body widths are sized to current species roles, while tails may extend beyond the existing simulation radius. Import validation ran only in a throwaway local project, not the game repository.
