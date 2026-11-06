using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DinoWorlds {
  public class Game : MonoBehaviour {
    public Battle battle; public CamRig cam; public int sceneryVariant = 1;
    Transform world, sceneryRoot, unitRoot, defRoot, craftRoot, shoreRoot;
    readonly Dictionary<int, Transform> unitT = new Dictionary<int, Transform>();
    readonly Dictionary<int, Transform> defT = new Dictionary<int, Transform>();
    Mesh meshRaptor, meshAnky, meshTower, meshHeart, meshHedge, meshBoulder, meshCraft, meshTile;
    Material matGround, matShore, matWater, matCraft;
    Material[] matUnit = new Material[2]; Material[] matDef = new Material[5];
    int selectedCraft = -1; Kind trayKind = Kind.Velociraptor;
    bool paused; bool qaDrive; float groundY = 0f;
    // QA / path traces
    public string lastFp = ""; public List<string> fpSamples = new List<string>(); public float qaMaxIdle; float qaIdle; float qaLastHit = -1f;

    void Awake(){
      Application.targetFrameRate = 60;
      battle = new Battle(sceneryVariant);
      cam = gameObject.AddComponent<CamRig>(); cam.cam = Camera.main; cam.pan = new Vector2(IsleLayout.W * 0.5f, IsleLayout.H * 0.35f); cam.zoom = 24f; cam.Apply();
      BuildMeshes(); BuildWorld(); SpawnDefs(); SpawnScenery(sceneryVariant); DrawShore();
    }

    void BuildMeshes(){
      meshRaptor = ToyArt.Capsuleish("Raptor", 0.35f, 0.7f); meshAnky = ToyArt.Ankylo("Anky");
      meshTower = ToyArt.Tower("Tower", 1.2f); meshHeart = ToyArt.HeartNest("Heart");
      meshHedge = ToyArt.Box("Hedge", new Vector3(0.9f, 0.55f, 0.9f)); meshBoulder = ToyArt.Box("Boulder", new Vector3(1.1f, 0.9f, 1.1f));
      meshCraft = ToyArt.Box("Craft", new Vector3(1.1f, 0.35f, 0.7f)); meshTile = ToyArt.Box("Tile", new Vector3(0.95f, 0.08f, 0.95f));
      matGround = ToyArt.Mat(new Color(0.45f, 0.78f, 0.4f)); matShore = ToyArt.Mat(new Color(0.93f, 0.86f, 0.55f));
      matWater = ToyArt.Mat(new Color(0.35f, 0.7f, 0.95f)); matCraft = ToyArt.Mat(new Color(0.95f, 0.55f, 0.2f));
      matUnit[0] = ToyArt.Mat(UnitSpec.Of(Kind.Velociraptor).color); matUnit[1] = ToyArt.Mat(UnitSpec.Of(Kind.Ankylosaurus).color);
      matDef[0] = ToyArt.Mat(DefSpec.Of(DefKind.RoostHeart).color); matDef[1] = ToyArt.Mat(DefSpec.Of(DefKind.PeckTower).color);
      matDef[2] = ToyArt.Mat(DefSpec.Of(DefKind.FanSpitter).color); matDef[3] = ToyArt.Mat(DefSpec.Of(DefKind.SpikeHedge).color);
      matDef[4] = ToyArt.Mat(DefSpec.Of(DefKind.BoulderNest).color);
    }

    Transform Part(string n, Transform parent, Mesh mesh, Material mat, Vector3 pos){
      var go = new GameObject(n); go.transform.SetParent(parent, false); go.transform.position = pos;
      go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = mat; return go.transform;
    }

    void BuildWorld(){
      world = new GameObject("World").transform; unitRoot = new GameObject("Units").transform; unitRoot.SetParent(world);
      defRoot = new GameObject("Defs").transform; defRoot.SetParent(world); craftRoot = new GameObject("Crafts").transform; craftRoot.SetParent(world);
      shoreRoot = new GameObject("Shore").transform; shoreRoot.SetParent(world); sceneryRoot = new GameObject("Scenery").transform; sceneryRoot.SetParent(world);
      // ground plane
      var ground = Part("Ground", world, ToyArt.Box("G", new Vector3(IsleLayout.W + 4, 0.2f, IsleLayout.H + 4)), matGround, new Vector3(IsleLayout.W * 0.5f, -0.12f, IsleLayout.H * 0.5f));
      Part("WaterS", world, ToyArt.Box("W", new Vector3(IsleLayout.W + 8, 0.15f, 4f)), matWater, new Vector3(IsleLayout.W * 0.5f, -0.2f, -1.5f));
      // soft light
      var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.color = new Color(1f, 0.96f, 0.88f); sun.intensity = 1.1f; sun.transform.rotation = Quaternion.Euler(50f, -30f, 0);
      RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.55f, 0.65f, 0.75f);
    }

    void SpawnDefs(){
      foreach (Transform c in defRoot) Destroy(c.gameObject); defT.Clear();
      foreach (var d in battle.defs){
        Mesh mesh = d.kind == DefKind.RoostHeart ? meshHeart : d.kind == DefKind.SpikeHedge ? meshHedge : d.kind == DefKind.BoulderNest ? meshBoulder : meshTower;
        var t = Part("D" + d.id, defRoot, mesh, matDef[(int)d.kind], new Vector3(d.pos.x, groundY, d.pos.y)); defT[d.id] = t;
      }
    }

    public void SpawnScenery(int variant){
      foreach (Transform c in sceneryRoot) Destroy(c.gameObject);
      var rng = new System.Random(variant * 9176 + 3); // cosmetic only
      int n = 18 + variant % 5;
      for (int i = 0; i < n; i++){
        int x = rng.Next(1, IsleLayout.W - 1), y = rng.Next(2, IsleLayout.H - 1);
        if (battle.Blocked(new Cell(x, y))) continue;
        bool palm = rng.Next(0, 2) == 0;
        var mat = ToyArt.Mat(palm ? new Color(0.2f, 0.65f, 0.3f) : new Color(0.55f, 0.5f, 0.45f));
        var mesh = palm ? ToyArt.Box("Palm", new Vector3(0.25f, 1.2f + (float)rng.NextDouble(), 0.25f)) : ToyArt.Box("Rock", new Vector3(0.4f, 0.3f, 0.4f));
        Part("S" + i, sceneryRoot, mesh, mat, new Vector3(x + 0.5f, 0, y + 0.5f));
      }
    }

    void DrawShore(){
      foreach (Transform c in shoreRoot) Destroy(c.gameObject);
      foreach (var s in IsleLayout.Shore) Part("Shore", shoreRoot, meshTile, matShore, new Vector3(s.x + 0.5f, 0.02f, s.y + 0.5f));
    }

    void DrawCrafts(){
      foreach (Transform c in craftRoot) Destroy(c.gameObject);
      for (int i = 0; i < battle.crafts.Count; i++){
        var c = battle.crafts[i]; var t = Part("C" + i, craftRoot, meshCraft, matCraft, new Vector3(c.cell.x + 0.5f, 0.2f, c.cell.y + 0.5f));
        if (i == selectedCraft) t.localScale = Vector3.one * 1.15f;
      }
    }

    void SyncUnits(){
      var live = new HashSet<int>();
      foreach (var u in battle.units){
        live.Add(u.id);
        if (!unitT.TryGetValue(u.id, out var t)){
          var mesh = u.kind == Kind.Velociraptor ? meshRaptor : meshAnky;
          t = Part("U" + u.id, unitRoot, mesh, matUnit[(int)u.kind], Vector3.zero); unitT[u.id] = t;
        }
        t.gameObject.SetActive(u.Alive);
        if (u.Alive){ t.position = new Vector3(u.pos.x, 0, u.pos.y); if (u.targetDef >= 0 && u.targetDef < battle.defs.Count){ var d = battle.defs[u.targetDef].pos - u.pos; if (d.sqrMagnitude > 0.01f) t.rotation = Quaternion.LookRotation(new Vector3(d.x, 0, d.y)); } }
      }
      var dead = new List<int>(); foreach (var kv in unitT) if (!live.Contains(kv.Key)) dead.Add(kv.Key);
      foreach (var id in dead){ Destroy(unitT[id].gameObject); unitT.Remove(id); }
    }

    void SyncDefs(){
      foreach (var d in battle.defs){
        if (!defT.TryGetValue(d.id, out var t)) continue;
        t.gameObject.SetActive(d.Alive);
        if (d.Alive){ float k = Mathf.Lerp(0.7f, 1f, d.hp / d.maxHp); t.localScale = Vector3.one * k; }
      }
    }

    Cell? HoverCell(){
      if (Mouse.current == null) return null;
      var ray = cam.ScreenRay(); if (Mathf.Abs(ray.direction.y) < 1e-4f) return null;
      float t = -ray.origin.y / ray.direction.y; if (t < 0) return null;
      var p = ray.origin + ray.direction * t; return IsleLayout.Clamp(new Cell(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.z)));
    }

    void Update(){
      float dt = (paused || qaDrive) ? 0f : Time.deltaTime; cam.Tick(Time.unscaledDeltaTime);
      var kb = Keyboard.current; var mouse = Mouse.current;
      if (kb != null && kb.escapeKey.wasPressedThisFrame) paused = !paused;
      if (battle.phase == Phase.Deploy){
        if (kb != null){
          if (kb.digit1Key.wasPressedThisFrame) trayKind = Kind.Velociraptor;
          if (kb.digit2Key.wasPressedThisFrame) trayKind = Kind.Ankylosaurus;
          if (kb.spaceKey.wasPressedThisFrame) TryStart();
          if (kb.rKey.wasPressedThisFrame && selectedCraft >= 0) battle.RemoveFromCraft(selectedCraft);
          if (kb.deleteKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame){ if (selectedCraft >= 0){ var cell = battle.crafts[selectedCraft].cell; battle.RemoveCraftAt(cell); selectedCraft = -1; } }
        }
        if (mouse != null && mouse.leftButton.wasPressedThisFrame){
          var cell = HoverCell(); if (cell.HasValue){
            int hit = -1; for (int i = 0; i < battle.crafts.Count; i++) if (battle.crafts[i].cell.Equals(cell.Value)) hit = i;
            if (hit >= 0){ selectedCraft = hit; battle.AddToCraft(selectedCraft, trayKind); }
            else if (battle.ShoreOk(cell.Value)){ if (battle.PlaceCraft(cell.Value)){ selectedCraft = battle.crafts.Count - 1; battle.AddToCraft(selectedCraft, trayKind); } }
          }
        }
        DrawCrafts();
      } else if (battle.phase == Phase.Fight){
        battle.Tick(dt); SyncUnits(); SyncDefs();
        if (Mathf.FloorToInt(battle.fightClock * 2f) != Mathf.FloorToInt((battle.fightClock - dt) * 2f)){ lastFp = battle.PathFingerprint(); fpSamples.Add(lastFp); }
      } else {
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.rKey.wasPressedThisFrame)) DoRetry();
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) DoRetry();
      }
    }

    void TryStart(){
      if (!battle.StartFight()) return;
      unitT.Clear(); foreach (Transform c in unitRoot) Destroy(c.gameObject);
      fpSamples.Clear(); SyncUnits(); Debug.Log($"[DW] fight start pop={battle.TotalPop()} crafts={battle.crafts.Count} scenery={battle.scenerySeed}");
    }

    public void DoRetry(){
      battle.Retry(); selectedCraft = battle.crafts.Count > 0 ? 0 : -1;
      foreach (Transform c in unitRoot) Destroy(c.gameObject); unitT.Clear();
      SpawnDefs(); DrawCrafts(); Debug.Log($"[DW] retry -> deploy crafts={battle.crafts.Count} scenery={battle.scenerySeed}");
    }

    public void SetScenery(int v){ sceneryVariant = v; battle.scenerySeed = v; SpawnScenery(v); Debug.Log($"[DW] scenery variant={v} (cosmetic only)"); }

#if DW_QA
    // ---- QA hooks (compiled out of release) ----
    public void QAStatus(){ Debug.Log($"[DW] status phase={battle.phase} t={battle.fightClock:F2} pop={battle.TotalPop()} crafts={battle.crafts.Count} units={battle.units.Count} alive={AliveUnits()} heart={HeartHp():F0} scenery={battle.scenerySeed} pan={cam.pan.x:F1},{cam.pan.y:F1} zoom={cam.zoom:F1} yaw={cam.cam.transform.eulerAngles.y:F1} maxIdle={battle.maxCombatIdle:F2}"); }
    public void QAClear(){ qaDrive = true; battle = new Battle(sceneryVariant); selectedCraft = -1; foreach (Transform c in unitRoot) Destroy(c.gameObject); unitT.Clear(); SpawnDefs(); DrawCrafts(); fpSamples.Clear(); Debug.Log("[DW] qa clear"); }
    public void QAPlace(string a){ // "x,y,kind,kind,..."
      var p = a.Split(','); int x = int.Parse(p[0]), y = int.Parse(p[1]); var cell = new Cell(x, y);
      if (!battle.PlaceCraft(cell)){ for (int i = 0; i < battle.crafts.Count; i++) if (battle.crafts[i].cell.Equals(cell)) selectedCraft = i; }
      else selectedCraft = battle.crafts.Count - 1;
      for (int i = 2; i < p.Length; i++){ var k = p[i].StartsWith("A") || p[i].StartsWith("a") ? Kind.Ankylosaurus : Kind.Velociraptor; battle.AddToCraft(selectedCraft, k); }
      DrawCrafts(); Debug.Log($"[DW] qa place {a} pop={battle.TotalPop()}");
    }
    public void QAStart(){ qaDrive = true; TryStart(); }
    public void QARetry(){ DoRetry(); }
    public void QAScenery(string n){ SetScenery(int.Parse(n)); }
    public void QATick(string n){ qaDrive = true; float t = float.Parse(n, System.Globalization.CultureInfo.InvariantCulture); float step = 1f / 30f; int N = Mathf.Max(1, Mathf.RoundToInt(t / step)); for (int i = 0; i < N; i++){ if (battle.phase != Phase.Fight) break; battle.Tick(step); if (i % 15 == 0) fpSamples.Add(battle.PathFingerprint()); } SyncUnits(); SyncDefs(); lastFp = battle.PathFingerprint(); Debug.Log($"[DW] qa tick {t:F2}s -> phase={battle.phase} fight={battle.fightClock:F2} maxIdle={battle.maxCombatIdle:F2} fp={lastFp.GetHashCode()}"); }
    public void QAFingerprint(){ Debug.Log($"[DW] fp {battle.PathFingerprint().GetHashCode()} len={battle.PathFingerprint().Length} samples={fpSamples.Count}"); }
    public void QAWinLoad(){ QAClear(); QAPlace("7,0,A,A,A,A"); QAPlace("11,0,A,A,V"); QAPlace("15,0,V"); Debug.Log("[DW] qa win load pop=14 crafts=3 :: 7,0 AAAA | 11,0 AAV | 15,0 V"); }
    public void QALoseLoad(){ QAClear(); QAPlace("7,0,A,A,A"); QAPlace("15,0,V,V"); Debug.Log("[DW] qa lose load pop=8 crafts=2 :: 7,0 AAA | 15,0 VV (substantial fail)"); }
    int AliveUnits(){ int n = 0; foreach (var u in battle.units) if (u.Alive) n++; return n; }
    float HeartHp(){ foreach (var d in battle.defs) if (d.kind == DefKind.RoostHeart) return d.hp; return 0; }
#endif

    void OnGUI(){
      float s = Mathf.Max(1f, Screen.height / 900f); GUI.skin.label.fontSize = Mathf.RoundToInt(16 * s); GUI.skin.button.fontSize = Mathf.RoundToInt(16 * s);
      GUILayout.BeginArea(new Rect(12 * s, 12 * s, 420 * s, Screen.height - 24 * s));
      GUILayout.Label("Dino Worlds — Shore Isle");
      GUILayout.Label($"Phase: {battle.phase}   {(battle.phase == Phase.Fight ? $"t={battle.fightClock:F1}s" : "")}");
      if (battle.phase == Phase.Deploy){
        GUILayout.Label($"Pop {battle.TotalPop()} / {Battle.PopCap}   Crafts {battle.crafts.Count} / {Battle.CraftCap}");
        GUILayout.Label("Click shore to place a craft · click craft to add the selected kind");
        GUILayout.BeginHorizontal();
        if (GUILayout.Toggle(trayKind == Kind.Velociraptor, "1 Velociraptor", GUILayout.Height(36 * s))) trayKind = Kind.Velociraptor;
        if (GUILayout.Toggle(trayKind == Kind.Ankylosaurus, "2 Ankylosaurus", GUILayout.Height(36 * s))) trayKind = Kind.Ankylosaurus;
        GUILayout.EndHorizontal();
        if (selectedCraft >= 0 && selectedCraft < battle.crafts.Count){
          var c = battle.crafts[selectedCraft]; GUILayout.Label($"Craft {selectedCraft + 1} at ({c.cell.x},{c.cell.y}): {string.Join(", ", c.load)}");
          if (GUILayout.Button("Remove last dino (R)", GUILayout.Height(32 * s))) battle.RemoveFromCraft(selectedCraft);
        }
        GUI.enabled = battle.TotalPop() > 0; if (GUILayout.Button("Start raid (Space)", GUILayout.Height(40 * s))) TryStart(); GUI.enabled = true;
        GUILayout.Label("WASD / arrows / right-drag pan · wheel zoom · Esc pause");
      } else if (battle.phase == Phase.Fight){
        GUILayout.Label("Autonomous fight — no mid-battle orders");
        foreach (var d in battle.defs) if (d.kind == DefKind.RoostHeart) GUILayout.Label($"Roost Heart HP {Mathf.CeilToInt(d.hp)} / {Mathf.CeilToInt(d.maxHp)}");
        int alive = 0; foreach (var u in battle.units) if (u.Alive) alive++; GUILayout.Label($"Dinosaurs alive: {alive}");
        if (paused) GUILayout.Label("PAUSED");
      } else {
        GUILayout.Label(battle.lastResult);
        GUILayout.Label(battle.phase == Phase.Win ? "The Roost Heart fell!" : "Your flock was wiped out.");
        if (GUILayout.Button("Retry (Space / R)", GUILayout.Height(40 * s))) DoRetry();
      }
      GUILayout.EndArea();
    }
  }
}
