using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DinoWorlds {
  public class Game : MonoBehaviour {
    public Progress progress; public Battle battle; public CamRig cam; public DwAudio dwAudio;
    public Mode mode = Mode.Home; public int sceneryVariant = 1; public int selectedIsle;
    Transform world, sceneryRoot, unitRoot, defRoot, craftRoot, shoreRoot, homeRoot, rangeRoot;
    readonly Dictionary<int, Transform> unitT = new Dictionary<int, Transform>();
    readonly Dictionary<int, DinoAnim> unitAnim = new Dictionary<int, DinoAnim>(); readonly Dictionary<int, float> unitCd = new Dictionary<int, float>(); float animDt;
    readonly Dictionary<int, Transform> defT = new Dictionary<int, Transform>();
    Mesh meshRaptor, meshAnky, meshDilo, meshRex, meshTower, meshHeart, meshHedge, meshBoulder, meshCraft, meshTile, meshHut, meshDisc;
    Material matGround, matShore, matShoreRim, matWater, matCraft, matHome, matRange, matTile, matPalm, matRock, matLocked;
    MaterialPropertyBlock shorePadBlock;
    Mesh meshPalm, meshRock;
    public float uiScale = 1f; const string KUi = "dw_opt_ui";
    GUIStyle sBtn, sTog, sPanel, sLabel; float styledAt = -1f; Vector2 uiScroll;
    Material[] matUnit = new Material[4]; Material[] matDef = new Material[5];
    Material[] matBld = new Material[5];
    int selectedCraft = -1; Kind trayKind = Kind.Velociraptor; BldKind buildKind = BldKind.Hatchery;
    bool paused, qaDrive, showRanges = true; string toast = ""; float toastT;
    string resetDraft = ""; bool resetPrompt;
    public string lastFp = ""; public List<string> fpSamples = new List<string>();
    float sfxTimer;

    void Awake(){
      Application.targetFrameRate = 60;
      progress = Progress.LoadOrNew();
      cam = gameObject.AddComponent<CamRig>(); cam.cam = Camera.main;
      DwPersist.Install();
      dwAudio = gameObject.AddComponent<DwAudio>(); dwAudio.Init();
      uiScale = Mathf.Clamp(PlayerPrefs.GetFloat(KUi, 1f), 1f, 1.75f);
      BuildMeshes(); BuildWorldRoots();
      EnterHome();
      Debug.Log($"[DW] art {ArtLib.Status()}");
      Debug.Log($"[DW] ready m4 amber={progress.amber} shards={progress.shards} hatch={progress.hatchery} isles={Progress.IsleCount}");
    }

    void BuildMeshes(){
      meshRaptor = ToyArt.Capsuleish("Raptor", 0.35f, 0.7f); meshAnky = ToyArt.Ankylo("Anky");
      meshDilo = ToyArt.Dilopho("Dilo"); meshRex = ToyArt.Rex("Rex"); meshHut = ToyArt.Hut("Hut");
      meshTower = ToyArt.Tower("Tower", 1.2f); meshHeart = ToyArt.HeartNest("Heart");
      meshHedge = ToyArt.Box("Hedge", new Vector3(0.9f, 0.55f, 0.9f)); meshBoulder = ToyArt.Box("Boulder", new Vector3(1.1f, 0.9f, 1.1f));
      meshCraft = ToyArt.Box("Craft", new Vector3(1.1f, 0.35f, 0.7f)); meshTile = ToyArt.Box("Tile", new Vector3(0.95f, 0.08f, 0.95f));
      meshDisc = ToyArt.Disc("RangeDisc", 1f, 28);
      matGround = ToyArt.Mat(new Color(0.45f, 0.78f, 0.4f)); matShore = ToyArt.Mat(new Color(0.86f, 0.62f, 0.28f));
      matShoreRim = ToyArt.Mat(new Color(0.38f, 0.24f, 0.12f));
      matWater = ToyArt.Mat(new Color(0.35f, 0.7f, 0.95f)); matCraft = ToyArt.Mat(new Color(0.95f, 0.55f, 0.2f));
      matHome = ToyArt.Mat(new Color(0.5f, 0.82f, 0.48f));
      matRange = ToyArt.Mat(new Color(1f, 0.85f, 0.2f, 0.28f));
      matTile = ToyArt.Mat(new Color(0.55f, 0.75f, 0.4f)); matPalm = ToyArt.Mat(new Color(0.2f, 0.65f, 0.3f)); matRock = ToyArt.Mat(new Color(0.55f, 0.5f, 0.45f));
      matLocked = ToyArt.Mat(new Color(0.35f, 0.4f, 0.45f));
      meshPalm = ToyArt.Box("Palm", new Vector3(0.25f, 1f, 0.25f)); meshRock = ToyArt.Box("Rock", new Vector3(0.4f, 0.3f, 0.4f));
      for (int i = 0; i < 4; i++) matUnit[i] = ToyArt.Mat(UnitSpec.Of((Kind)i).color);
      matDef[0] = ToyArt.Mat(DefSpec.Of(DefKind.RoostHeart).color); matDef[1] = ToyArt.Mat(DefSpec.Of(DefKind.PeckTower).color);
      matDef[2] = ToyArt.Mat(DefSpec.Of(DefKind.FanSpitter).color); matDef[3] = ToyArt.Mat(DefSpec.Of(DefKind.SpikeHedge).color);
      matDef[4] = ToyArt.Mat(DefSpec.Of(DefKind.BoulderNest).color);
      matBld[0] = ToyArt.Mat(new Color(1f, 0.5f, 0.55f)); matBld[1] = ToyArt.Mat(new Color(0.85f, 0.7f, 0.35f));
      matBld[2] = ToyArt.Mat(new Color(0.6f, 0.65f, 0.8f)); matBld[3] = ToyArt.Mat(new Color(0.95f, 0.75f, 0.3f)); matBld[4] = ToyArt.Mat(new Color(0.7f, 0.85f, 0.55f));
    }

    /// <summary>M5 prop: FBX model at its ground-pivot position, else the M4 primitive at its centred position.</summary>
    Transform Prop(string model, string n, Transform parent, Vector3 modelPos, Mesh mesh, Material mat, Vector3 primPos){
      if (ArtLib.Prefab(model) != null) return ArtLib.Spawn(model, n, parent, modelPos, 0, null, null);
      return Part(n, parent, mesh, mat, primPos);
    }
    Transform Part(string n, Transform parent, Mesh mesh, Material mat, Vector3 pos){
      var go = new GameObject(n); go.transform.SetParent(parent, false); go.transform.position = pos;
      go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = mat; return go.transform;
    }

    void BuildWorldRoots(){
      world = new GameObject("World").transform;
      unitRoot = new GameObject("Units").transform; unitRoot.SetParent(world);
      defRoot = new GameObject("Defs").transform; defRoot.SetParent(world);
      craftRoot = new GameObject("Crafts").transform; craftRoot.SetParent(world);
      shoreRoot = new GameObject("Shore").transform; shoreRoot.SetParent(world);
      sceneryRoot = new GameObject("Scenery").transform; sceneryRoot.SetParent(world);
      homeRoot = new GameObject("Home").transform; homeRoot.SetParent(world);
      rangeRoot = new GameObject("Ranges").transform; rangeRoot.SetParent(world);
      var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.color = new Color(1f, 0.96f, 0.88f); sun.intensity = 1.1f; sun.transform.rotation = Quaternion.Euler(50f, -30f, 0);
      RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.55f, 0.65f, 0.75f);
    }

    void ClearKids(Transform t){ if (!t) return; foreach (Transform c in t) Destroy(c.gameObject); }
    void Toast(string m){ toast = m; toastT = 2.5f; }

    public void EnterHome(){
      mode = Mode.Home; battle = null; unitT.Clear(); defT.Clear(); resetPrompt = false;
      ClearKids(unitRoot); ClearKids(defRoot); ClearKids(craftRoot); ClearKids(shoreRoot); ClearKids(sceneryRoot); ClearKids(homeRoot); ClearKids(rangeRoot);
      Part("Ground", homeRoot, ToyArt.Box("G", new Vector3(Home.W + 2, 0.2f, Home.H + 2)), matHome, new Vector3(Home.W * 0.5f, -0.12f, Home.H * 0.5f));
      Part("Water", homeRoot, ToyArt.Box("W", new Vector3(Home.W + 6, 0.15f, 3f)), matWater, new Vector3(Home.W * 0.5f, -0.2f, -1.2f));
      for (int y = 0; y < Home.H; y++) for (int x = 0; x < Home.W; x++)
        Prop(((x * 7 + y * 13) % 5 == 0) ? "DW_Tile_B" : "DW_Tile", "t", homeRoot, new Vector3(x + 0.5f, -0.03f, y + 0.5f), meshTile, matTile, new Vector3(x + 0.5f, 0.01f, y + 0.5f));
      DrawHomeBuildings();
      cam.SetBounds(Home.W, Home.H); cam.pan = new Vector2(Home.W * 0.5f, Home.H * 0.4f); cam.zoom = 18f; cam.Apply();
      dwAudio.SetBed("home");
      Debug.Log($"[DW] mode=Home amber={progress.amber} shards={progress.shards}");
    }

    void DrawHomeBuildings(){
      var old = homeRoot.Find("Blds"); if (old) Destroy(old.gameObject);
      var root = new GameObject("Blds").transform; root.SetParent(homeRoot, false);
      foreach (var b in progress.buildings){
        float fp = progress.Footprint(b.kind); Vector3 pos = new Vector3(b.cell.x + fp * 0.5f, 0, b.cell.y + fp * 0.5f);
        Mesh mesh = b.kind == BldKind.RoostHeart ? meshHeart : meshHut;
        var t = ArtLib.Spawn(ArtLib.BldModel(b.kind), b.kind.ToString(), root, pos, fp * 0.98f, mesh, matBld[(int)b.kind]);
        if (fp > 1 && ArtLib.Prefab(ArtLib.BldModel(b.kind)) == null) t.localScale = Vector3.one * 1.3f;
      }
    }

    public void EnterMap(){
      mode = Mode.Map; ClearKids(unitRoot); ClearKids(defRoot); ClearKids(craftRoot); ClearKids(shoreRoot); ClearKids(sceneryRoot); ClearKids(homeRoot); ClearKids(rangeRoot);
      Part("MapSea", homeRoot, ToyArt.Box("S", new Vector3(40, 0.2f, 24)), matWater, new Vector3(16, -0.2f, 10));
      for (int i = 0; i < Progress.IsleCount; i++){
        var isle = Isles.All[i]; bool open = progress.IsleOpen(i);
        Color col = isle.band == "Shore" ? new Color(0.45f, 0.78f, 0.4f) : isle.band == "Ridge" ? new Color(0.7f, 0.65f, 0.4f) : new Color(0.85f, 0.45f, 0.3f);
        if (!open) col = new Color(0.35f, 0.4f, 0.45f);
        int row = i / 4, colI = i % 4;
        var ip = new Vector3(4 + colI * 5.5f, 0.1f, 6 + row * 6f);
        var it = ArtLib.Spawn("DW_Island_" + i, "Isle" + i, homeRoot, ip, 4.4f, ToyArt.Box("I", new Vector3(4, 0.4f, 3)), ToyArt.Mat(col));
        if (!open && ArtLib.Prefab("DW_Island_" + i) != null) foreach (var r in it.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = matLocked;
      }
      cam.SetBounds(28, 20); cam.pan = new Vector2(14, 9); cam.zoom = 26f; cam.Apply();
      dwAudio.SetBed("home");
      Debug.Log("[DW] mode=Map isles=" + Progress.IsleCount);
    }

    public void EnterRaid(int isleIndex){
      if (!progress.IsleOpen(isleIndex)){ Toast("Clear the previous isle first"); Debug.Log($"[DW] raid locked isle={isleIndex}"); return; }
      selectedIsle = isleIndex; mode = Mode.Deploy;
      ClearKids(homeRoot); ClearKids(unitRoot); ClearKids(defRoot); ClearKids(craftRoot); ClearKids(shoreRoot); ClearKids(sceneryRoot); ClearKids(rangeRoot);
      battle = new Battle(isleIndex, sceneryVariant){ popCap = progress.PopCap, hpMul = progress.UnitHpMul, dmgMul = progress.UnitDmgMul };
      selectedCraft = -1;
      var isle = battle.isle;
      if (ArtLib.Prefab("DW_Island_" + isleIndex) != null){
        ArtLib.Spawn("DW_Island_" + isleIndex, "Ground", homeRoot, new Vector3(11, 0, 9), 0, null, null);
        if (ArtLib.Prefab("DW_Shore_" + isleIndex) != null) ArtLib.Spawn("DW_Shore_" + isleIndex, "ShoreStrip", homeRoot, new Vector3(11, 0.025f, 1), 0, null, null);
      } else Part("Ground", homeRoot, ToyArt.Box("G", new Vector3(isle.W + 4, 0.2f, isle.H + 4)), matGround, new Vector3(isle.W * 0.5f, -0.12f, isle.H * 0.5f));
      Part("WaterS", homeRoot, ToyArt.Box("W", new Vector3(isle.W + 8, 0.15f, 4f)), matWater, new Vector3(isle.W * 0.5f, -0.2f, -1.5f));
      SpawnDefs(); SpawnScenery(sceneryVariant); DrawShore(); DrawRangeDiscs();
      cam.SetBounds(isle.W, isle.H); cam.pan = new Vector2(isle.W * 0.5f, isle.H * 0.35f); cam.zoom = 24f; cam.Apply();
      dwAudio.SetBed("raid");
      Debug.Log($"[DW] mode=Deploy isle={isleIndex} {isle.name} band={isle.band} popCap={battle.popCap}");
    }

    void SpawnDefs(){
      ClearKids(defRoot); defT.Clear();
      foreach (var d in battle.defs){
        Mesh mesh = d.kind == DefKind.RoostHeart ? meshHeart : d.kind == DefKind.SpikeHedge ? meshHedge : d.kind == DefKind.BoulderNest ? meshBoulder : meshTower;
        defT[d.id] = ArtLib.Spawn(ArtLib.DefModel(d.kind), "D" + d.id, defRoot, new Vector3(d.pos.x, 0, d.pos.y), d.kind == DefKind.RoostHeart ? 2.2f : 1.3f, mesh, matDef[(int)d.kind]);
      }
    }
    public void SpawnScenery(int variant){
      ClearKids(sceneryRoot); if (battle == null) return;
      var rng = new System.Random(variant * 9176 + 3 + selectedIsle * 17);
      int n = 14 + variant % 5;
      for (int i = 0; i < n; i++){
        int x = rng.Next(1, battle.isle.W - 1), y = rng.Next(2, battle.isle.H - 1);
        if (battle.Blocked(new Cell(x, y))) continue;
        bool palm = rng.Next(0, 2) == 0;
        float ph = 1.2f + (float)rng.NextDouble();
        string pm = (palm ? "DW_Palm" : "DW_Rock") + (i % 3 == 2 ? "_B" : "");
        if (ArtLib.Prefab(pm) != null){ var st = ArtLib.Spawn(pm, "S" + i, sceneryRoot, new Vector3(x + 0.5f, 0f, y + 0.5f), 0, null, null);
          st.rotation = Quaternion.Euler(0, (i * 137) % 360, 0) * st.rotation; /* keep FBX import axis fix */ if (palm) st.localScale = Vector3.one * (ph / 1.47f); }
        else { var st = Part("S" + i, sceneryRoot, palm ? meshPalm : meshRock, palm ? matPalm : matRock, new Vector3(x + 0.5f, palm ? ph * 0.5f : 0.15f, y + 0.5f));
          if (palm) st.localScale = new Vector3(1f, ph, 1f); }
      }
      Debug.Log($"[DW] scenery variant={variant} cosmeticOnly=1 isle={selectedIsle}");
    }
    void DrawShore(){ ClearKids(shoreRoot);
      // Dark rim + warmer gold tint so sand placement pads read against the pale beach (luminance contrast, colorblind-safe).
      foreach (var s in battle.isle.Shore){
        var rim = Part("ShoreRim", shoreRoot, meshTile, matShoreRim, new Vector3(s.x + 0.5f, 0.03f, s.y + 0.5f));
        rim.localScale = new Vector3(1.16f, 0.65f, 1.16f);
        var t = Prop("DW_Tile_Sand", "Shore", shoreRoot, new Vector3(s.x + 0.5f, 0.04f, s.y + 0.5f), meshTile, matShore, new Vector3(s.x + 0.5f, 0.07f, s.y + 0.5f));
        TintShorePad(t);
      }
    }
    void TintShorePad(Transform t){
      if (shorePadBlock == null){ shorePadBlock = new MaterialPropertyBlock(); var c = new Color(0.90f, 0.64f, 0.24f); shorePadBlock.SetColor("_BaseColor", c); shorePadBlock.SetColor("_Color", c); }
      foreach (var r in t.GetComponentsInChildren<Renderer>()) r.SetPropertyBlock(shorePadBlock);
    }
    void DrawRangeDiscs(){
      ClearKids(rangeRoot); if (!showRanges || battle == null) return;
      foreach (var d in battle.defs){
        float r = d.Spec.range; if (r < 0.5f) r = d.kind == DefKind.SpikeHedge ? 0.75f : 0.6f;
        if (d.kind == DefKind.RoostHeart) continue;
        var t = Part("R" + d.id, rangeRoot, meshDisc, matRange, new Vector3(d.pos.x, 0.05f, d.pos.y));
        t.localScale = new Vector3(r * 2f, 1f, r * 2f);
      }
      Debug.Log($"[DW] ranges shown={showRanges} defs={battle.defs.Count}");
    }
    void DrawCrafts(){
      ClearKids(craftRoot);
      for (int i = 0; i < battle.crafts.Count; i++){
        var c = battle.crafts[i]; var t = Prop(i % 2 == 1 ? "DW_Craft_B" : "DW_Craft", "C" + i, craftRoot, new Vector3(c.cell.x + 0.5f, 0f, c.cell.y + 0.5f), meshCraft, matCraft, new Vector3(c.cell.x + 0.5f, 0.2f, c.cell.y + 0.5f));
        if (i == selectedCraft) t.localScale = Vector3.one * 1.15f;
      }
    }
    Mesh MeshFor(Kind k){ switch (k){ case Kind.Ankylosaurus: return meshAnky; case Kind.Dilophosaurus: return meshDilo; case Kind.Tyrannosaurus: return meshRex; default: return meshRaptor; } }
    void SyncUnits(){
      var live = new HashSet<int>();
      foreach (var u in battle.units){
        live.Add(u.id);
        if (!unitT.TryGetValue(u.id, out var t)){ t = ArtLib.Spawn(ArtLib.UnitModel(u.kind), "U" + u.id, unitRoot, Vector3.zero, 0, MeshFor(u.kind), matUnit[(int)u.kind]); unitT[u.id] = t;
          var da = DinoAnim.Attach(t, ArtLib.UnitModel(u.kind), UnitSpec.Of(u.kind).atkCd); if (da != null) unitAnim[u.id] = da; }
        t.gameObject.SetActive(u.Alive);
        if (u.Alive){ t.position = new Vector3(u.pos.x, 0, u.pos.y); if (u.targetDef >= 0 && u.targetDef < battle.defs.Count){ var d = battle.defs[u.targetDef].pos - u.pos; if (d.sqrMagnitude > 0.01f) t.rotation = Quaternion.LookRotation(new Vector3(d.x, 0, d.y)); }
          // Visual only: an attack fired this frame when the Sim reset the unit's cooldown upward.
          if (unitAnim.TryGetValue(u.id, out var an)){ bool fired = unitCd.TryGetValue(u.id, out var pc) && u.atkCd > pc + 1e-4f; unitCd[u.id] = u.atkCd; if (fired) an.Attack(); an.Step(animDt, t.position); } }
      }
      var dead = new List<int>(); foreach (var kv in unitT) if (!live.Contains(kv.Key)) dead.Add(kv.Key);
      foreach (var id in dead){ Destroy(unitT[id].gameObject); unitT.Remove(id); unitAnim.Remove(id); unitCd.Remove(id); }
    }
    void SyncDefs(){ foreach (var d in battle.defs){ if (!defT.TryGetValue(d.id, out var t)) continue; t.gameObject.SetActive(d.Alive); if (d.Alive) t.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, d.hp / d.maxHp); } }

    Cell? PointerCell(bool home){
      Vector2 screen = Vector2.zero; bool ok = false;
      if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame){ screen = Touchscreen.current.primaryTouch.position.ReadValue(); ok = true; }
      else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame){ screen = Mouse.current.position.ReadValue(); ok = true; }
      if (!ok) return null;
      var ray = cam.ScreenRay(screen); if (Mathf.Abs(ray.direction.y) < 1e-4f) return null;
      float t = -ray.origin.y / ray.direction.y; if (t < 0) return null;
      var p = ray.origin + ray.direction * t;
      if (home) return new Cell(Mathf.Clamp(Mathf.FloorToInt(p.x), 0, Home.W - 1), Mathf.Clamp(Mathf.FloorToInt(p.z), 0, Home.H - 1));
      return battle.isle.Clamp(new Cell(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.z)));
    }

    void Update(){
      if (toastT > 0) toastT -= Time.unscaledDeltaTime;
      float dt = (paused || qaDrive) ? 0f : Time.deltaTime; cam.Tick(Time.unscaledDeltaTime);
      var kb = Keyboard.current;
      if (kb != null && kb.escapeKey.wasPressedThisFrame){ if (mode == Mode.Options) EnterHome(); else if (mode == Mode.Home) EnterOptions(); else paused = !paused; }
      if (mode == Mode.Home) TickHome(kb);
      else if (mode == Mode.Map) TickMap();
      else if (mode == Mode.Deploy) TickDeploy(kb);
      else if (mode == Mode.Fight){
        battle.Tick(dt); animDt = dt; SyncUnits(); SyncDefs();
        if (dt > 0 && battle.hitEvents.Count > 0 && Time.unscaledTime > sfxTimer){ sfxTimer = Time.unscaledTime + 0.12f; PlayHitSfx(); }
        battle.hitEvents.Clear();
        if (battle.phase == Phase.Win){ progress.GrantWin(selectedIsle); mode = Mode.Result; dwAudio.Win(); Debug.Log($"[DW] result Win loot isle={selectedIsle}"); }
        else if (battle.phase == Phase.Lose){ progress.GrantLose(); mode = Mode.Result; dwAudio.Lose(); Debug.Log($"[DW] result Lose consolation softlock=0"); }
      } else if (mode == Mode.Result) TickResult(kb);
      else if (mode == Mode.Options){ /* GUI driven */ }
    }

    /// <summary>Pick one combat SFX per audio tick: heavy smash (Rex / Boulder Nest) beats spit (Dilo / Fan Spitter) beats peck.</summary>
    void PlayHitSfx(){
      int tier = 0;
      foreach (var e in battle.hitEvents){
        int t = 0;
        if (e.byUnit){ if (e.kind == (int)Kind.Tyrannosaurus) t = 2; else if (e.kind == (int)Kind.Dilophosaurus) t = 1; }
        else { if (e.kind == (int)DefKind.BoulderNest) t = 2; else if (e.kind == (int)DefKind.FanSpitter) t = 1; }
        if (t > tier) tier = t;
      }
      if (tier == 2) dwAudio.Smash(); else if (tier == 1) dwAudio.Spit(); else dwAudio.Peck();
    }

    void EnterOptions(){ mode = Mode.Options; resetPrompt = false; resetDraft = ""; dwAudio.Ui(); Debug.Log("[DW] mode=Options"); }

    void TickHome(Keyboard kb){
      if (kb != null){
        if (kb.digit1Key.wasPressedThisFrame) buildKind = BldKind.Hatchery;
        if (kb.digit2Key.wasPressedThisFrame) buildKind = BldKind.Workshop;
        if (kb.digit3Key.wasPressedThisFrame) buildKind = BldKind.AmberPit;
        if (kb.digit4Key.wasPressedThisFrame) buildKind = BldKind.Nest;
        if (kb.mKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) EnterMap();
        if (kb.oKey.wasPressedThisFrame) EnterOptions();
      }
      var cell = PointerCell(true); if (!cell.HasValue) return;
      if (progress.TryBuild(buildKind, cell.Value)){ DrawHomeBuildings(); dwAudio.Ui(); Toast($"Built {buildKind}"); Debug.Log($"[DW] build {buildKind} at {cell.Value.x},{cell.Value.y} amber={progress.amber}"); }
    }

    void TickMap(){
      Vector2 screen = Vector2.zero; bool press = false;
      if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame){ screen = Touchscreen.current.primaryTouch.position.ReadValue(); press = true; }
      else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame){ screen = Mouse.current.position.ReadValue(); press = true; }
      if (!press) return;
      var ray = cam.ScreenRay(screen); float t = -ray.origin.y / ray.direction.y; if (t < 0) return;
      var p = ray.origin + ray.direction * t;
      for (int i = 0; i < Progress.IsleCount; i++){
        int row = i / 4, colI = i % 4; float cx = 4 + colI * 5.5f, cz = 6 + row * 6f;
        if (Mathf.Abs(p.x - cx) < 2.2f && Mathf.Abs(p.z - cz) < 2f){ EnterRaid(i); return; }
      }
    }

    void TickDeploy(Keyboard kb){
      if (kb != null){
        if (kb.digit1Key.wasPressedThisFrame && progress.Unlocked(Kind.Velociraptor)) trayKind = Kind.Velociraptor;
        if (kb.digit2Key.wasPressedThisFrame && progress.Unlocked(Kind.Ankylosaurus)) trayKind = Kind.Ankylosaurus;
        if (kb.digit3Key.wasPressedThisFrame && progress.Unlocked(Kind.Dilophosaurus)) trayKind = Kind.Dilophosaurus;
        if (kb.digit4Key.wasPressedThisFrame && progress.Unlocked(Kind.Tyrannosaurus)) trayKind = Kind.Tyrannosaurus;
        if (kb.spaceKey.wasPressedThisFrame) TryStart();
        if (kb.rKey.wasPressedThisFrame && selectedCraft >= 0) battle.RemoveFromCraft(selectedCraft);
        if (kb.hKey.wasPressedThisFrame) EnterHome();
        if (kb.gKey.wasPressedThisFrame){ showRanges = !showRanges; DrawRangeDiscs(); }
      }
      var cell = PointerCell(false); if (cell.HasValue){
        int hit = -1; for (int i = 0; i < battle.crafts.Count; i++) if (battle.crafts[i].cell.Equals(cell.Value)) hit = i;
        if (hit >= 0){ selectedCraft = hit; if (!progress.Unlocked(trayKind)) Toast("Species locked"); else { if (battle.AddToCraft(selectedCraft, trayKind)) dwAudio.Call(trayKind); else dwAudio.Ui(); } }
        else if (battle.ShoreOk(cell.Value)){ if (battle.PlaceCraft(cell.Value)){ selectedCraft = battle.crafts.Count - 1; dwAudio.Splash(); if (progress.Unlocked(trayKind)) battle.AddToCraft(selectedCraft, trayKind); } }
      }
      DrawCrafts();
    }

    void TickResult(Keyboard kb){
      // Always allow retry after lose — raid is free; no soft-lock even at 0 amber/shards
      if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.rKey.wasPressedThisFrame)){ if (battle.phase == Phase.Lose) DoRetry(); else EnterHome(); }
      Vector2 screen = Vector2.zero; bool press = false;
      if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) press = true;
      else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) press = true;
      if (press){ if (battle.phase == Phase.Lose) DoRetry(); else EnterHome(); }
    }

    void TryStart(){
      if (battle == null || !battle.StartFight()) return;
      mode = Mode.Fight; unitT.Clear(); ClearKids(unitRoot); ClearKids(rangeRoot); fpSamples.Clear(); SyncUnits();
      dwAudio.Splash(); battle.hitEvents.Clear();
      int heavy = -1; foreach (var c in battle.crafts) foreach (var k in c.load) if ((int)k > heavy) heavy = (int)k;
      if (heavy >= 0) dwAudio.Call((Kind)heavy);
      Debug.Log($"[DW] fight start isle={selectedIsle} pop={battle.TotalPop()} crafts={battle.crafts.Count} scenery={battle.scenerySeed}");
    }
    public void DoRetry(){
      // Soft-lock guard: retry never costs resources
      battle.Retry(); mode = Mode.Deploy; selectedCraft = battle.crafts.Count > 0 ? 0 : -1;
      ClearKids(unitRoot); unitT.Clear(); SpawnDefs(); DrawCrafts(); DrawRangeDiscs();
      dwAudio.SetBed("raid"); dwAudio.Ui();
      Debug.Log($"[DW] retry -> deploy crafts={battle.crafts.Count} scenery={battle.scenerySeed} softlock=0 amber={progress.amber} shards={progress.shards}");
    }
    public void SetScenery(int v){ sceneryVariant = v; if (battle != null){ battle.scenerySeed = v; SpawnScenery(v); } Debug.Log($"[DW] scenery variant={v} (cosmetic only)"); }

    bool ApplyTypedReset(string phrase){
      bool ok = Progress.TryTypedReset(phrase);
      if (ok){ progress = Progress.LoadOrNew(); EnterHome(); Toast("Progress reset"); }
      else Toast("Type RESET to confirm");
      return ok;
    }

#if DW_QA
    public void QAStatus(){
      string ph = mode == Mode.Fight || mode == Mode.Deploy || mode == Mode.Result ? battle.phase.ToString() : mode.ToString();
      float t = battle != null ? battle.fightClock : 0; int pop = battle != null ? battle.TotalPop() : 0;
      Debug.Log($"[DW] status mode={mode} phase={ph} t={t:F2} pop={pop} amber={progress.amber} shards={progress.shards} hatch={progress.hatchery} workshop={progress.workshop} storage={progress.storage} popCap={progress.PopCap} isle={selectedIsle} clear={progress.ClearBits()} pan={cam.pan.x:F1},{cam.pan.y:F1} zoom={cam.zoom:F1} yaw={cam.cam.transform.eulerAngles.y:F1} maxIdle={(battle != null ? battle.maxCombatIdle : 0):F2} homeAttack=0 ranges={(showRanges?1:0)} audio={dwAudio.Status()}");
    }
    public void QAClear(){ qaDrive = true; Progress.ClearSave(); progress = Progress.LoadOrNew(); EnterHome(); Debug.Log("[DW] qa clear"); }
    public void QAHome(){ EnterHome(); }
    public void QAMap(){ EnterMap(); }
    public void QARaid(string n){ EnterRaid(int.Parse(n)); }
    public void QAPlace(string a){
      var p = a.Split(','); int x = int.Parse(p[0]), y = int.Parse(p[1]); var cell = new Cell(x, y);
      if (!battle.PlaceCraft(cell)){ for (int i = 0; i < battle.crafts.Count; i++) if (battle.crafts[i].cell.Equals(cell)) selectedCraft = i; }
      else selectedCraft = battle.crafts.Count - 1;
      for (int i = 2; i < p.Length; i++){
        Kind k = Kind.Velociraptor; char c = char.ToUpper(p[i][0]);
        if (c == 'A') k = Kind.Ankylosaurus; else if (c == 'D') k = Kind.Dilophosaurus; else if (c == 'T') k = Kind.Tyrannosaurus;
        battle.AddToCraft(selectedCraft, k);
      }
      DrawCrafts(); Debug.Log($"[DW] qa place {a} pop={battle.TotalPop()}");
    }
    public void QAStart(){ qaDrive = true; TryStart(); }
    public void QARetry(){ DoRetry(); }
    public void QAScenery(string n){ SetScenery(int.Parse(n)); }
    public void QATick(string n){ qaDrive = true; float t = float.Parse(n, System.Globalization.CultureInfo.InvariantCulture); float step = 1f / 30f; int N = Mathf.Max(1, Mathf.RoundToInt(t / step));
      for (int i = 0; i < N; i++){ if (battle == null || battle.phase != Phase.Fight) break; battle.Tick(step); if (i % 15 == 0) fpSamples.Add(battle.PathFingerprint());
        if (battle.phase == Phase.Win){ progress.GrantWin(selectedIsle); mode = Mode.Result; dwAudio.Win(); break; }
        if (battle.phase == Phase.Lose){ progress.GrantLose(); mode = Mode.Result; dwAudio.Lose(); break; }
      }
      if (battle != null) battle.hitEvents.Clear();
      SyncUnits(); SyncDefs(); lastFp = battle != null ? battle.PathFingerprint() : "";
      Debug.Log($"[DW] qa tick {t:F2}s -> mode={mode} phase={(battle != null ? battle.phase.ToString() : "-")} fight={(battle != null ? battle.fightClock : 0):F2} maxIdle={(battle != null ? battle.maxCombatIdle : 0):F2} fp={lastFp.GetHashCode()}");
      if (battle != null && (battle.phase == Phase.Win || battle.phase == Phase.Lose))
        Debug.Log($"[DW] outcome {battle.phase} t={battle.fightClock:F2} isle={selectedIsle}");
    }
    public void QAFingerprint(){ lastFp = battle.PathFingerprint(); Debug.Log($"[DW] fp {lastFp.GetHashCode()} len={lastFp.Length} raw={lastFp.Substring(0, Mathf.Min(80, lastFp.Length))}"); }
    public void QAWinLoad(){
      if (mode != Mode.Deploy) EnterRaid(selectedIsle);
      var sh = battle.isle.Shore; if (sh.Length < 1) return;
      void Load(int si, string kinds){ if (si >= sh.Length) return; var c = sh[si]; QAPlace($"{c.x},{c.y},{kinds}"); }
      // Prefer Anky-heavy for Shore timing; for later isles still strong
      if (selectedIsle == 0){ Load(2, "A,A,A,A"); Load(4, "A,A,V"); Load(6, "V"); }
      else { Load(0, "A,A,T"); Load(Mathf.Min(2, sh.Length-1), "D,D,A"); Load(Mathf.Min(4, sh.Length-1), "A,V,V"); }
      Debug.Log($"[DW] qa win load isle={selectedIsle} pop={battle.TotalPop()} crafts={battle.crafts.Count}");
    }
    public void QALoseLoad(){ if (mode != Mode.Deploy) EnterRaid(0); var sh = battle.isle.Shore; QAPlace($"{sh[2].x},{sh[2].y},A,A,A"); QAPlace($"{sh[6].x},{sh[6].y},V,V"); Debug.Log("[DW] qa lose load pop=8 crafts=2"); }
    public void QAGive(string a){ var p = a.Split(','); progress.amber = Mathf.Max(0, progress.amber + int.Parse(p[0])); progress.shards = Mathf.Max(0, progress.shards + int.Parse(p[1])); progress.Save(); Debug.Log($"[DW] qa give amber={progress.amber} shards={progress.shards}"); }
    public void QABuild(string a){ var p = a.Split(','); var k = (BldKind)System.Enum.Parse(typeof(BldKind), p[0], true); var c = new Cell(int.Parse(p[1]), int.Parse(p[2])); bool ok = progress.TryBuild(k, c); if (mode == Mode.Home) DrawHomeBuildings(); Debug.Log($"[DW] qa build {k} @{p[1]},{p[2]} ok={ok} amber={progress.amber}"); }
    public void QAUpgrade(string what){
      bool ok = false; int cost = -2;
      if (what == "hatchery"){ cost = progress.HatchUpgradeCost(); ok = progress.TryUpgradeHatchery(); }
      else if (what == "workshop"){ cost = progress.WorkshopUpgradeCost(); ok = progress.TryUpgradeWorkshop(); }
      else if (what == "storage"){ cost = progress.StorageUpgradeCost(); ok = progress.TryUpgradeStorage(); }
      else if (what.StartsWith("unit")){ var k = (Kind)System.Enum.Parse(typeof(Kind), what.Substring(4), true); cost = progress.UnitUpgradeCost(k); ok = progress.TryUpgradeUnit(k); }
      Debug.Log($"[DW] qa upgrade {what} ok={ok} cost={cost} hatch={progress.hatchery} workshop={progress.workshop} storage={progress.storage} amber={progress.amber} shards={progress.shards} unitLvl={string.Join(",", progress.unitLvl)}");
    }
    public void QAReload(){ progress = Progress.LoadOrNew(); EnterHome(); Debug.Log($"[DW] qa reload amber={progress.amber} shards={progress.shards} hatch={progress.hatchery} clear={progress.ClearBits()}"); }
    public void QAReset(string phrase){ bool ok = ApplyTypedReset(phrase); Debug.Log($"[DW] qa reset ok={ok} amber={progress.amber} shards={progress.shards}"); }
    public void QASetClear(string bits){
      for (int i = 0; i < Progress.IsleCount && i < bits.Length; i++) progress.isleClear[i] = bits[i] == '1';
      progress.Save(); Debug.Log($"[DW] qa setclear {progress.ClearBits()}");
    }
    public void QARanges(string on){ showRanges = on == "1" || on.ToLower() == "true"; if (mode == Mode.Deploy) DrawRangeDiscs(); Debug.Log($"[DW] qa ranges={(showRanges?1:0)}"); }
    public void QAAudio(){ Debug.Log($"[DW] qa audio {dwAudio.Status()}"); dwAudio.Ui(); dwAudio.Splash(); dwAudio.Peck(); dwAudio.Spit(); dwAudio.Smash(); for (int i = 0; i < 4; i++) dwAudio.Call((Kind)i); Debug.Log($"[DW] qa audio after {dwAudio.Status()}"); }
    public void QAArt(){ Debug.Log($"[DW] qa art {ArtLib.Status()} anim={DinoAnim.active} attacks={DinoAnim.attacks} walkFrames={DinoAnim.walkers} evalT={DinoAnim.evalT:F1} mode={mode}"); }
    public void QAOpt(string a){ var p = a.Split(','); var v = float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture);
      if (p[0] == "music") dwAudio.SetMusic(v); else if (p[0] == "sfx") dwAudio.SetSfx(v); else if (p[0] == "mute") dwAudio.SetMute(v > 0.5f); else if (p[0] == "ui") SetUiScale(v);
      Debug.Log($"[DW] qa opt {a} ui={uiScale:F2} audio={dwAudio.Status()} sync={DwPersist.Count()}"); }
    public void QADrive(string on){ qaDrive = on == "1"; Debug.Log($"[DW] qa drive={(qaDrive?1:0)}"); }
    public void QASync(){ Debug.Log($"[DW] qa sync count={DwPersist.Count()}"); }
    public void QABlurb(){ for (int i = 0; i < 4; i++) Debug.Log($"[DW] blurb {(Kind)i}: {UnitSpec.Of((Kind)i).blurb}"); }
    public void QAAssertNoHomeAttack(){ Debug.Log("[DW] homeAttack=0 never"); }
    public void QAStarve(){ progress.amber = 0; progress.shards = 0; progress.Save(); Debug.Log("[DW] qa starve amber=0 shards=0"); }
    public void QAForceLose(){
      if (battle == null || battle.phase != Phase.Fight){ Debug.Log("[DW] qa forcelose skip"); return; }
      foreach (var u in battle.units) u.hp = 0;
      battle.Tick(0.05f);
      if (battle.phase == Phase.Lose){ progress.GrantLose(); mode = Mode.Result; dwAudio.Lose(); }
      Debug.Log($"[DW] outcome Lose t={battle.fightClock:F2} isle={selectedIsle} forced=1 softlock=0");
    }
#endif

    void SetUiScale(float v){ uiScale = Mathf.Clamp(v, 1f, 1.75f); PlayerPrefs.SetFloat(KUi, uiScale); PlayerPrefs.Save(); DwPersist.Flush(); styledAt = -1f; }

    /// <summary>M4 UI: original nine-slice skins on IMGUI, with readable text scaled by screen height and the UI-scale option.</summary>
    void EnsureStyles(float s){
      if (sBtn != null && Mathf.Approximately(styledAt, s)) return; styledAt = s; padCache.Clear();
      var cream = new Color(1f, 0.97f, 0.88f); var ink = new Color(0.12f, 0.16f, 0.2f);
      sLabel = new GUIStyle(GUI.skin.label){ fontSize = Mathf.RoundToInt(16 * s), wordWrap = true };
      sLabel.normal.textColor = ink;
      Texture2D prim = ArtLib.Ui("DW_Button_Primary"), press = ArtLib.Ui("DW_Button_Pressed"), sec = ArtLib.Ui("DW_Button_Secondary"), dis = ArtLib.Ui("DW_Button_Disabled"), tray = ArtLib.Ui("DW_Tray");
      sBtn = new GUIStyle(GUI.skin.button){ fontSize = Mathf.RoundToInt(15 * s), fontStyle = FontStyle.Bold };
      sTog = new GUIStyle(GUI.skin.button){ fontSize = Mathf.RoundToInt(15 * s), fontStyle = FontStyle.Bold };
      sPanel = new GUIStyle(GUI.skin.box);
      if (prim && press && sec){
        var bd = new RectOffset(14, 14, 14, 14); // 28px source borders drawn at half size to fit 40px rows
        sBtn.border = bd; sBtn.normal.background = prim; sBtn.hover.background = prim; sBtn.active.background = press; sBtn.focused.background = prim;
        sBtn.normal.textColor = sBtn.hover.textColor = sBtn.active.textColor = cream;
        sTog.border = bd; sTog.normal.background = sec; sTog.hover.background = sec; sTog.active.background = press;
        sTog.onNormal.background = prim; sTog.onHover.background = prim; sTog.onActive.background = press;
        sTog.normal.textColor = sTog.hover.textColor = ink; sTog.onNormal.textColor = sTog.onHover.textColor = sTog.active.textColor = sTog.onActive.textColor = cream;
        if (dis){ sBtn.onNormal.background = dis; }
      }
      if (tray){ sPanel.border = new RectOffset(20, 20, 20, 20); sPanel.normal.background = tray; sPanel.padding = new RectOffset(Mathf.RoundToInt(14 * s), Mathf.RoundToInt(14 * s), Mathf.RoundToInt(12 * s), Mathf.RoundToInt(12 * s)); }
      else { sLabel.normal.textColor = Color.white; }
    }

    bool Btn(string t, params GUILayoutOption[] o){ return GUILayout.Button(t, sBtn ?? GUI.skin.button, o); }
    bool Tog(bool v, string t, params GUILayoutOption[] o){ return GUILayout.Toggle(v, t, sTog ?? GUI.skin.button, o); }

    // M5 icons: original DW_Icon_* textures drawn at the left of a control; text stays readable beside it.
    static string IconOf(Kind k){ return "DW_Icon_" + k; }
    static string IconOf(BldKind k){ return "DW_Icon_" + k; }
    static string IconOf(DefKind k){ return "DW_Icon_" + k; }
    float iconPx;
    readonly System.Collections.Generic.Dictionary<string, GUIStyle> padCache = new System.Collections.Generic.Dictionary<string, GUIStyle>();
    GUIStyle Padded(GUIStyle b, string icon){ var t = ArtLib.Ui(icon); if (t == null) return b; string key = (b == sTog ? "t" : "b") + iconPx;
      if (padCache.TryGetValue(key, out var c)) return c; var st = new GUIStyle(b); padCache[key] = st; st.padding = new RectOffset(b.padding.left + Mathf.RoundToInt(iconPx + 4), b.padding.right, b.padding.top, b.padding.bottom); return st; }
    void DrawIconInLast(string icon){
      if (Event.current.type != EventType.Repaint) return; var t = ArtLib.Ui(icon); if (t == null) return;
      var r = GUILayoutUtility.GetLastRect(); float z = Mathf.Min(iconPx, r.height - 6); var box = new Rect(r.x + 8, r.y + (r.height - z) * 0.5f, z, z);
      // Cream disc behind the icon so teal/dark glyphs (Upgrade, Range, Pause, Back) read on the teal button skin.
      if (icon != "DW_Icon_Play") GUI.DrawTexture(box, Disc(), ScaleMode.ScaleToFit, true, 0, new Color(1f, 0.97f, 0.88f, 0.95f), 0, 0); // Play glyph is cream: draw it straight on teal
      var inner = new Rect(box.x + z * 0.08f, box.y + z * 0.08f, z * 0.84f, z * 0.84f);
      // Species portraits have wide transparent margins: sample their centre so they read at button size.
      if (icon.EndsWith("saurus") || icon.EndsWith("raptor")) GUI.DrawTextureWithTexCoords(inner, t, new Rect(0.2f, 0.2f, 0.6f, 0.6f));
      else GUI.DrawTexture(inner, t, ScaleMode.ScaleToFit);
    }
    static Texture2D disc;
    static Texture2D Disc(){
      if (disc != null) return disc; const int n = 64; disc = new Texture2D(n, n, TextureFormat.RGBA32, false){ wrapMode = TextureWrapMode.Clamp };
      var px = new Color32[n * n]; float c = (n - 1) * 0.5f;
      for (int y = 0; y < n; y++) for (int x = 0; x < n; x++){ float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)); byte a = (byte)(Mathf.Clamp01(c - d + 0.5f) * 255); px[y * n + x] = new Color32(255, 255, 255, a); }
      disc.SetPixels32(px); disc.Apply(); return disc;
    }
    bool IBtn(string icon, string t, params GUILayoutOption[] o){ bool b = GUILayout.Button(t, Padded(sBtn ?? GUI.skin.button, icon), o); DrawIconInLast(icon); return b; }
    bool ITog(bool v, string icon, string t, params GUILayoutOption[] o){ bool b = GUILayout.Toggle(v, t, Padded(sTog ?? GUI.skin.button, icon), o); DrawIconInLast(icon); return b; }
    void Icon(string icon){ var t = ArtLib.Ui(icon); if (t == null) return; var r = GUILayoutUtility.GetRect(iconPx, iconPx, GUILayout.Width(iconPx), GUILayout.Height(iconPx)); if (Event.current.type == EventType.Repaint) GUI.DrawTexture(r, t, ScaleMode.ScaleToFit); }
    void ILabel(string icon, string t){ GUILayout.BeginHorizontal(); Icon(icon); GUILayout.Label(t); GUILayout.EndHorizontal(); }

    /// <summary>M5: scouting row on the deploy screen, one defense icon per kind with its count on this isle.</summary>
    void DefenseBadges(float s){
      GUILayout.BeginHorizontal(); GUILayout.Label("Defenses:", GUILayout.ExpandWidth(false));
      for (int d = 1; d < 5; d++){ var k = (DefKind)d; int n = 0; foreach (var x in battle.isle.Defs) if (x.k == k) n++; if (n == 0) continue;
        Icon(IconOf(k)); GUILayout.Label($"x{n}", GUILayout.ExpandWidth(false)); GUILayout.Space(10 * s); }
      GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
    }

    void OnGUI(){
      float s = Mathf.Max(1f, Screen.height / 900f) * uiScale; GUI.skin.label.fontSize = Mathf.RoundToInt(16 * s); GUI.skin.button.fontSize = Mathf.RoundToInt(15 * s);
      EnsureStyles(s);
      GUI.skin.label.normal.textColor = sLabel.normal.textColor; GUI.skin.label.wordWrap = true;
      float bh = 40 * s; iconPx = 26 * s;
      GUILayout.BeginArea(new Rect(10 * s, 10 * s, Mathf.Min(480 * s, Screen.width - 20 * s), Screen.height - 20 * s));
      // Scrolls when large text or a short screen would push buttons off-screen (keeps every control reachable).
      GUI.skin.verticalScrollbar.fixedWidth = 16 * s; GUI.skin.verticalScrollbarThumb.fixedWidth = 16 * s;
      uiScroll = GUILayout.BeginScrollView(uiScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);
      GUILayout.BeginVertical(sPanel);
      GUILayout.Label("Dino Worlds");
      GUILayout.BeginHorizontal(); Icon("DW_Icon_Amber"); GUILayout.Label($"Amber {progress.amber}/{progress.AmberCap}", GUILayout.ExpandWidth(false)); GUILayout.Space(12 * s);
      Icon("DW_Icon_BoneShards"); GUILayout.Label($"Shards {progress.shards}/{progress.ShardCap}", GUILayout.ExpandWidth(false)); GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
      if (toastT > 0) GUILayout.Label(toast);
      if (mode == Mode.Home){
        GUILayout.Label("Home Roost (never attacked) — click empty cell to build");
        GUILayout.BeginHorizontal();
        if (ITog(buildKind == BldKind.Hatchery, IconOf(BldKind.Hatchery), $"Hatchery ({progress.BuildCost(BldKind.Hatchery)}A)", GUILayout.Height(bh))) buildKind = BldKind.Hatchery;
        if (ITog(buildKind == BldKind.Workshop, IconOf(BldKind.Workshop), $"Workshop ({progress.BuildCost(BldKind.Workshop)}A)", GUILayout.Height(bh))) buildKind = BldKind.Workshop;
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (ITog(buildKind == BldKind.AmberPit, IconOf(BldKind.AmberPit), $"Amber Pit ({progress.BuildCost(BldKind.AmberPit)}A)", GUILayout.Height(bh))) buildKind = BldKind.AmberPit;
        if (ITog(buildKind == BldKind.Nest, IconOf(BldKind.Nest), $"Nest ({progress.BuildCost(BldKind.Nest)}A)", GUILayout.Height(bh))) buildKind = BldKind.Nest;
        GUILayout.EndHorizontal();
        GUILayout.Label($"Hatchery Lv {progress.hatchery}/{Progress.MaxBldLvl}  Workshop Lv {progress.workshop}/{Progress.MaxBldLvl}  Storage Lv {progress.storage}/{Progress.MaxBldLvl}");
        ILabel("DW_Icon_Population", $"Pop {progress.PopCap}");
        int hc = progress.HatchUpgradeCost(); if (hc < 0) GUILayout.Label("Hatchery MAX"); else if (IBtn("DW_Icon_Hatchery", $"Upgrade Hatchery ({hc}A)", GUILayout.Height(bh))) if (!progress.TryUpgradeHatchery()) Toast("Can't"); else { DrawHomeBuildings(); dwAudio.Ui(); Toast("Hatchery upgraded"); }
        int wc = progress.WorkshopUpgradeCost(); if (wc < 0) GUILayout.Label("Workshop MAX"); else if (IBtn("DW_Icon_Workshop", $"Upgrade Workshop ({wc}A)", GUILayout.Height(bh))) if (!progress.TryUpgradeWorkshop()) Toast("Can't"); else { dwAudio.Ui(); Toast("Workshop upgraded"); }
        int sc = progress.StorageUpgradeCost(); if (sc < 0) GUILayout.Label("Storage MAX"); else if (IBtn("DW_Icon_Upgrade", $"Upgrade Storage ({sc}A)", GUILayout.Height(bh))) if (!progress.TryUpgradeStorage()) Toast("Can't"); else { dwAudio.Ui(); Toast("Storage upgraded"); }
        GUILayout.Label("Bone Shard unit upgrades (cap 3):");
        for (int i = 0; i < 4; i++){ var k = (Kind)i; if (!progress.Unlocked(k)){ ILabel(IconOf(k), $"{UnitSpec.Of(k).name}: locked"); continue; }
          int uc = progress.UnitUpgradeCost(k);
          if (uc < 0) ILabel(IconOf(k), $"{UnitSpec.Of(k).name} Lv {progress.unitLvl[i]} MAX");
          else if (IBtn(IconOf(k), $"{UnitSpec.Of(k).name} Lv {progress.unitLvl[i]} ({uc}S)", GUILayout.Height(bh))) if (!progress.TryUpgradeUnit(k)) Toast("Can't"); else { dwAudio.Ui(); Toast("Upgraded " + k); } }
        if (IBtn("DW_Icon_Map", "Raid map", GUILayout.Height(bh))) EnterMap();
        if (Btn("Options / Reset", GUILayout.Height(bh))) EnterOptions();
      } else if (mode == Mode.Map){
        GUILayout.Label("Choose an isle — Shore → Ridge → Caldera");
        for (int i = 0; i < Progress.IsleCount; i++){
          var isle = Isles.All[i]; bool open = progress.IsleOpen(i);
          GUI.enabled = open;
          if (Btn($"{i+1}. {isle.name} [{isle.band}] {(progress.isleClear[i] ? "CLEARED" : open ? "OPEN" : "LOCKED")}", GUILayout.Height(bh))) EnterRaid(i);
          GUI.enabled = true;
        }
        if (IBtn("DW_Icon_Back", "Back to roost", GUILayout.Height(bh))) EnterHome();
      } else if (mode == Mode.Deploy){
        GUILayout.Label($"{battle.isle.name} [{battle.isle.band}] — Deploy");
        ILabel("DW_Icon_Population", $"Pop {battle.TotalPop()}/{battle.popCap}");
        DefenseBadges(s);
        GUILayout.Label($"Priority: {UnitSpec.Of(trayKind).blurb}");
        GUILayout.BeginHorizontal();
        for (int i = 0; i < 4; i++){ var k = (Kind)i; if (!progress.Unlocked(k)) continue;
          if (ITog(trayKind == k, IconOf(k), $"{i + 1} {UnitSpec.Of(k).name}", GUILayout.Height(bh))) trayKind = k; }
        GUILayout.EndHorizontal();
        if (IBtn("DW_Icon_Range", showRanges ? "Hide range discs" : "Show range discs", GUILayout.Height(bh))){ showRanges = !showRanges; DrawRangeDiscs(); dwAudio.Ui(); }
        if (IBtn("DW_Icon_Play", "Start raid", GUILayout.Height(bh))) TryStart();
        if (IBtn("DW_Icon_Back", "Back to map", GUILayout.Height(bh))) EnterMap();
      } else if (mode == Mode.Fight){
        GUILayout.Label("Autonomous fight — no mid-battle orders");
        foreach (var d in battle.defs) if (d.kind == DefKind.RoostHeart) ILabel(IconOf(DefKind.RoostHeart), $"Roost Heart {Mathf.CeilToInt(d.hp)}/{Mathf.CeilToInt(d.maxHp)}");
        int alive = 0; foreach (var u in battle.units) if (u.Alive) alive++; GUILayout.Label($"Alive {alive}  t={battle.fightClock:F0}s");
        if (paused) GUILayout.Label("PAUSED");
        if (IBtn(paused ? "DW_Icon_Play" : "DW_Icon_Pause", paused ? "Resume (Esc)" : "Pause (Esc)", GUILayout.Height(bh))){ paused = !paused; dwAudio.Ui(); }
      } else if (mode == Mode.Result){
        GUILayout.Label(battle.lastResult);
        GUILayout.Label(battle.phase == Phase.Win ? "Isle cleared! Loot saved." : "Wiped out — consolation shards. Raid stays free — retry anytime.");
        if (battle.phase == Phase.Lose && Btn("Retry deploy (free)", GUILayout.Height(bh))) DoRetry();
        if (IBtn("DW_Icon_Back", "Return to roost", GUILayout.Height(bh))) EnterHome();
      } else if (mode == Mode.Options){
        GUILayout.Label("Options — save is local to this browser");
        GUILayout.Label($"Audio {dwAudio.Status()}");
        GUILayout.Label($"Music {Mathf.RoundToInt(dwAudio.musicVol * 100)}%   Effects {Mathf.RoundToInt(dwAudio.sfxVol * 100)}%   {(dwAudio.muted ? "MUTED" : "")}");
        GUILayout.BeginHorizontal();
        if (Btn("Music -", GUILayout.Height(bh))) dwAudio.SetMusic(dwAudio.musicVol - 0.1f);
        if (Btn("Music +", GUILayout.Height(bh))) dwAudio.SetMusic(dwAudio.musicVol + 0.1f);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (Btn("Effects -", GUILayout.Height(bh))){ dwAudio.SetSfx(dwAudio.sfxVol - 0.1f); dwAudio.Ui(); }
        if (Btn("Effects +", GUILayout.Height(bh))){ dwAudio.SetSfx(dwAudio.sfxVol + 0.1f); dwAudio.Ui(); }
        GUILayout.EndHorizontal();
        if (Btn(dwAudio.muted ? "Unmute all sound" : "Mute all sound", GUILayout.Height(bh))) dwAudio.SetMute(!dwAudio.muted);
        if (Btn($"Text size {Mathf.RoundToInt(uiScale * 100)}% (tap to change)", GUILayout.Height(bh))){ SetUiScale(uiScale >= 1.74f ? 1f : uiScale + 0.25f); dwAudio.Ui(); }
        GUILayout.Label("Reset progress — type RESET then confirm");
        resetDraft = GUILayout.TextField(resetDraft ?? "", GUILayout.Height(bh));
        if (Btn("Confirm reset", GUILayout.Height(bh))) ApplyTypedReset(resetDraft);
        if (IBtn("DW_Icon_Back", "Back", GUILayout.Height(bh))) EnterHome();
      }
      GUILayout.EndVertical();
      GUILayout.EndScrollView();
      GUILayout.EndArea();
    }
  }
}
