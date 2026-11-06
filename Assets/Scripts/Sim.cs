// Deterministic combat: same deploy + same layout => same paths (scenery cosmetic only).
using System.Collections.Generic;
using UnityEngine;

namespace DinoWorlds {
  public enum Kind { Velociraptor, Ankylosaurus, Dilophosaurus, Tyrannosaurus }
  public enum DefKind { RoostHeart, PeckTower, FanSpitter, SpikeHedge, BoulderNest }
  public enum Phase { Deploy, Fight, Win, Lose }

  public struct Cell { public int x, y; public Cell(int x, int y){ this.x = x; this.y = y; }
    public static int Dist(Cell a, Cell b){ return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y); }
    public override bool Equals(object o){ return o is Cell c && c.x == x && c.y == y; }
    public override int GetHashCode(){ return x * 397 ^ y; }
  }

  public class UnitSpec {
    public Kind kind; public string name; public Color color; public string blurb;
    public float hp, dmg, range, speed, radius, atkCd; public int pop;
    public static readonly UnitSpec[] All = {
      new UnitSpec{ kind = Kind.Velociraptor, name = "Velociraptor", color = new Color(0.55f, 0.92f, 0.25f), blurb = "Priority: nearest living defense. Fast claw swarm.", hp = 85, dmg = 3.0f, range = 1.1f, speed = 2.35f, radius = 0.35f, atkCd = 0.7f, pop = 1 },
      new UnitSpec{ kind = Kind.Ankylosaurus, name = "Ankylosaurus", color = new Color(0.45f, 0.52f, 0.62f), blurb = "Priority: shooters first, then Heart. Tank soak.", hp = 360, dmg = 4.2f, range = 1.2f, speed = 1.12f, radius = 0.55f, atkCd = 1.1f, pop = 2 },
      new UnitSpec{ kind = Kind.Dilophosaurus, name = "Dilophosaurus", color = new Color(0.25f, 0.78f, 0.85f), blurb = "Priority: towers/spitters. Stops short and spits.", hp = 110, dmg = 5.5f, range = 3.6f, speed = 1.7f, radius = 0.4f, atkCd = 0.85f, pop = 2 },
      new UnitSpec{ kind = Kind.Tyrannosaurus, name = "Tyrannosaurus", color = new Color(0.92f, 0.55f, 0.2f), blurb = "Priority: Roost Heart / buildings. Heavy smash.", hp = 520, dmg = 14f, range = 1.4f, speed = 0.95f, radius = 0.7f, atkCd = 1.35f, pop = 3 },
    };
    public static UnitSpec Of(Kind k){ return All[(int)k]; }
  }

  public class DefSpec {
    public DefKind kind; public string name; public Color color;
    public float hp, dmg, range, cooldown, splash; public bool blocks, shoots;
    public static DefSpec Of(DefKind k){
      switch (k){
        case DefKind.RoostHeart: return new DefSpec{ kind = k, name = "Roost Heart", color = new Color(1f, 0.45f, 0.55f), hp = 2350, dmg = 0, range = 0, cooldown = 0, blocks = true, shoots = false };
        case DefKind.PeckTower: return new DefSpec{ kind = k, name = "Peck Tower", color = new Color(0.95f, 0.75f, 0.25f), hp = 200, dmg = 4.0f, range = 4.5f, cooldown = 0.95f, blocks = true, shoots = true };
        case DefKind.FanSpitter: return new DefSpec{ kind = k, name = "Fan Spitter", color = new Color(0.35f, 0.85f, 0.9f), hp = 220, dmg = 3.2f, range = 3.8f, cooldown = 1.3f, splash = 1.7f, blocks = true, shoots = true };
        case DefKind.SpikeHedge: return new DefSpec{ kind = k, name = "Spike Hedge", color = new Color(0.55f, 0.35f, 0.2f), hp = 150, dmg = 2.2f, range = 0.6f, cooldown = 0.55f, blocks = true, shoots = false };
        case DefKind.BoulderNest: return new DefSpec{ kind = k, name = "Boulder Nest", color = new Color(0.7f, 0.55f, 0.4f), hp = 280, dmg = 10f, range = 6f, cooldown = 2.1f, blocks = true, shoots = true };
      }
      return null;
    }
  }

  public class Unit {
    public int id; public Kind kind; public Vector2 pos; public float hp, maxHp, atkCd; public int craft;
    public List<Cell> path = new List<Cell>(); public int pathI; public int targetDef = -1;
    public UnitSpec Spec => UnitSpec.Of(kind);
    public bool Alive => hp > 0.01f;
  }

  public class Defense {
    public int id; public DefKind kind; public Cell cell; public Vector2 pos; public float hp, maxHp, atkCd;
    public DefSpec Spec => DefSpec.Of(kind);
    public bool Alive => hp > 0.01f;
    public bool Blocks => Spec.blocks && Alive;
  }

  public class Craft { public Cell cell; public List<Kind> load = new List<Kind>(); public int Pop { get { int n = 0; foreach (var k in load) n += UnitSpec.Of(k).pop; return n; } } }

  public class IsleData {
    public string name, band; public int W, H; public Cell[] Shore; public (DefKind k, int x, int y)[] Defs; public float hpScale;
    public Vector2 World(Cell c){ return new Vector2(c.x + 0.5f, c.y + 0.5f); }
    public Cell Clamp(Cell c){ return new Cell(Mathf.Clamp(c.x, 0, W - 1), Mathf.Clamp(c.y, 0, H - 1)); }
  }

  /// <summary>Four fixed hand-designed isles. Isle 0 (Shore) keeps M1 layout and timing.</summary>
  public static class Isles {
    public static readonly IsleData[] All = {
      // 0 Shore Isle — M1/M2 layout unchanged (hpScale ignored at runtime for isle 0)
      new IsleData{ name = "Shore Isle", band = "Shore", W = 22, H = 18, hpScale = 1f,
        Shore = new[]{ new Cell(3,0), new Cell(5,0), new Cell(7,0), new Cell(9,0), new Cell(11,0), new Cell(13,0), new Cell(15,0), new Cell(17,0), new Cell(4,1), new Cell(8,1), new Cell(12,1), new Cell(16,1) },
        Defs = new[]{ (DefKind.RoostHeart,11,15),(DefKind.PeckTower,6,8),(DefKind.PeckTower,16,8),(DefKind.PeckTower,11,11),(DefKind.FanSpitter,8,6),(DefKind.FanSpitter,14,6),(DefKind.BoulderNest,11,9),
          (DefKind.SpikeHedge,5,5),(DefKind.SpikeHedge,6,5),(DefKind.SpikeHedge,7,5),(DefKind.SpikeHedge,14,5),(DefKind.SpikeHedge,15,5),(DefKind.SpikeHedge,16,5),(DefKind.SpikeHedge,10,12),(DefKind.SpikeHedge,12,12) } },
      // 1 Tide Cove
      new IsleData{ name = "Tide Cove", band = "Shore", W = 22, H = 18, hpScale = 1.05f,
        Shore = new[]{ new Cell(2,0), new Cell(5,0), new Cell(8,0), new Cell(11,0), new Cell(14,0), new Cell(17,0), new Cell(4,1), new Cell(10,1), new Cell(16,1) },
        Defs = new[]{ (DefKind.RoostHeart,11,16),(DefKind.PeckTower,4,7),(DefKind.PeckTower,18,7),(DefKind.PeckTower,11,10),(DefKind.FanSpitter,7,5),(DefKind.FanSpitter,15,5),(DefKind.FanSpitter,11,8),(DefKind.BoulderNest,8,12),(DefKind.BoulderNest,14,12),
          (DefKind.SpikeHedge,6,4),(DefKind.SpikeHedge,7,4),(DefKind.SpikeHedge,8,4),(DefKind.SpikeHedge,13,4),(DefKind.SpikeHedge,14,4),(DefKind.SpikeHedge,15,4),(DefKind.SpikeHedge,10,13),(DefKind.SpikeHedge,12,13) } },
      // 2 Lagoon Shelf
      new IsleData{ name = "Lagoon Shelf", band = "Shore", W = 22, H = 18, hpScale = 1.08f,
        Shore = new[]{ new Cell(3,0), new Cell(6,0), new Cell(9,0), new Cell(12,0), new Cell(15,0), new Cell(18,0), new Cell(5,1), new Cell(14,1) },
        Defs = new[]{ (DefKind.RoostHeart,11,15),(DefKind.PeckTower,5,6),(DefKind.PeckTower,17,6),(DefKind.PeckTower,11,9),(DefKind.FanSpitter,8,5),(DefKind.FanSpitter,14,5),(DefKind.BoulderNest,7,11),(DefKind.BoulderNest,15,11),
          (DefKind.SpikeHedge,4,4),(DefKind.SpikeHedge,5,4),(DefKind.SpikeHedge,6,4),(DefKind.SpikeHedge,15,4),(DefKind.SpikeHedge,16,4),(DefKind.SpikeHedge,17,4),(DefKind.SpikeHedge,10,12),(DefKind.SpikeHedge,12,12) } },
      // 3 Ridge Pass
      new IsleData{ name = "Ridge Pass", band = "Ridge", W = 22, H = 18, hpScale = 1.15f,
        Shore = new[]{ new Cell(4,0), new Cell(7,0), new Cell(10,0), new Cell(13,0), new Cell(16,0), new Cell(6,1), new Cell(14,1) },
        Defs = new[]{ (DefKind.RoostHeart,11,15),(DefKind.PeckTower,5,9),(DefKind.PeckTower,17,9),(DefKind.PeckTower,11,12),(DefKind.FanSpitter,9,6),(DefKind.FanSpitter,13,6),(DefKind.BoulderNest,11,8),(DefKind.BoulderNest,6,11),(DefKind.BoulderNest,16,11),
          (DefKind.SpikeHedge,8,5),(DefKind.SpikeHedge,9,5),(DefKind.SpikeHedge,10,5),(DefKind.SpikeHedge,11,5),(DefKind.SpikeHedge,12,5),(DefKind.SpikeHedge,13,5),(DefKind.SpikeHedge,9,14),(DefKind.SpikeHedge,13,14) } },
      // 4 Stone Spur
      new IsleData{ name = "Stone Spur", band = "Ridge", W = 22, H = 18, hpScale = 1.18f,
        Shore = new[]{ new Cell(2,0), new Cell(5,0), new Cell(8,0), new Cell(11,0), new Cell(14,0), new Cell(17,0), new Cell(3,1), new Cell(15,1) },
        Defs = new[]{ (DefKind.RoostHeart,11,16),(DefKind.PeckTower,4,8),(DefKind.PeckTower,18,8),(DefKind.PeckTower,8,11),(DefKind.PeckTower,14,11),(DefKind.FanSpitter,11,6),(DefKind.FanSpitter,6,5),(DefKind.FanSpitter,16,5),(DefKind.BoulderNest,11,9),
          (DefKind.SpikeHedge,7,4),(DefKind.SpikeHedge,8,4),(DefKind.SpikeHedge,9,4),(DefKind.SpikeHedge,12,4),(DefKind.SpikeHedge,13,4),(DefKind.SpikeHedge,14,4),(DefKind.SpikeHedge,10,13),(DefKind.SpikeHedge,12,13) } },
      // 5 Wind Cut
      new IsleData{ name = "Wind Cut", band = "Ridge", W = 22, H = 18, hpScale = 1.22f,
        Shore = new[]{ new Cell(4,0), new Cell(7,0), new Cell(10,0), new Cell(13,0), new Cell(16,0), new Cell(9,1), new Cell(12,1) },
        Defs = new[]{ (DefKind.RoostHeart,11,15),(DefKind.PeckTower,6,7),(DefKind.PeckTower,16,7),(DefKind.PeckTower,11,11),(DefKind.FanSpitter,8,5),(DefKind.FanSpitter,14,5),(DefKind.FanSpitter,11,8),(DefKind.BoulderNest,5,12),(DefKind.BoulderNest,17,12),(DefKind.BoulderNest,11,13),
          (DefKind.SpikeHedge,9,4),(DefKind.SpikeHedge,10,4),(DefKind.SpikeHedge,11,4),(DefKind.SpikeHedge,12,4),(DefKind.SpikeHedge,13,4),(DefKind.SpikeHedge,8,14),(DefKind.SpikeHedge,14,14) } },
      // 6 Ember Spire
      new IsleData{ name = "Ember Spire", band = "Caldera", W = 22, H = 18, hpScale = 1.28f,
        Shore = new[]{ new Cell(3,0), new Cell(6,0), new Cell(9,0), new Cell(12,0), new Cell(15,0), new Cell(18,0), new Cell(5,1), new Cell(15,1) },
        Defs = new[]{ (DefKind.RoostHeart,11,16),(DefKind.PeckTower,5,8),(DefKind.PeckTower,17,8),(DefKind.PeckTower,8,11),(DefKind.PeckTower,14,11),(DefKind.FanSpitter,11,6),(DefKind.FanSpitter,6,5),(DefKind.FanSpitter,16,5),(DefKind.BoulderNest,11,10),(DefKind.BoulderNest,9,13),(DefKind.BoulderNest,13,13),
          (DefKind.SpikeHedge,7,4),(DefKind.SpikeHedge,8,4),(DefKind.SpikeHedge,9,4),(DefKind.SpikeHedge,12,4),(DefKind.SpikeHedge,13,4),(DefKind.SpikeHedge,14,4),(DefKind.SpikeHedge,10,14),(DefKind.SpikeHedge,12,14) } },
      // 7 Ash Crown
      new IsleData{ name = "Ash Crown", band = "Caldera", W = 22, H = 18, hpScale = 1.35f,
        Shore = new[]{ new Cell(4,0), new Cell(7,0), new Cell(10,0), new Cell(13,0), new Cell(16,0), new Cell(6,1), new Cell(14,1) },
        Defs = new[]{ (DefKind.RoostHeart,11,16),(DefKind.PeckTower,3,7),(DefKind.PeckTower,19,7),(DefKind.PeckTower,7,10),(DefKind.PeckTower,15,10),(DefKind.PeckTower,11,12),(DefKind.FanSpitter,9,5),(DefKind.FanSpitter,13,5),(DefKind.FanSpitter,11,8),(DefKind.BoulderNest,5,13),(DefKind.BoulderNest,17,13),(DefKind.BoulderNest,11,14),
          (DefKind.SpikeHedge,8,4),(DefKind.SpikeHedge,9,4),(DefKind.SpikeHedge,10,4),(DefKind.SpikeHedge,11,4),(DefKind.SpikeHedge,12,4),(DefKind.SpikeHedge,13,4),(DefKind.SpikeHedge,9,15),(DefKind.SpikeHedge,13,15) } },
    };
  }

  // Back-compat alias used by CamRig / older code paths
  public static class IsleLayout {
    public static int W => Isles.All[0].W; public static int H => Isles.All[0].H;
    public static Cell[] Shore => Isles.All[0].Shore;
    public static (DefKind k, int x, int y)[] Defs => Isles.All[0].Defs;
    public static Vector2 World(Cell c) => Isles.All[0].World(c);
    public static Cell Clamp(Cell c) => Isles.All[0].Clamp(c);
  }

  public class Battle {
    public const int CraftCap = 4, CraftSlots = 4;
    public int popCap = 14; public int isleIndex;
    public IsleData isle;
    public List<Defense> defs = new List<Defense>();
    public List<Unit> units = new List<Unit>();
    public List<Craft> crafts = new List<Craft>();
    public Phase phase = Phase.Deploy;
    public float time; public int seed; public int scenerySeed;
    public int nextId; public bool[] blocked;
    public float fightClock; public string lastResult = ""; public float lastCombatHit; public float maxCombatIdle;
    public System.Func<Kind, float> hpMul = _ => 1f, dmgMul = _ => 1f;

    public Battle(int isleIndex = 0, int scenerySeed = 1){
      this.isleIndex = isleIndex; this.scenerySeed = scenerySeed; seed = 7;
      isle = Isles.All[Mathf.Clamp(isleIndex, 0, Isles.All.Length - 1)];
      blocked = new bool[isle.W * isle.H];
      foreach (var d in isle.Defs){
        var sp = DefSpec.Of(d.k); float hp = sp.hp * (d.k == DefKind.RoostHeart || d.k == DefKind.PeckTower || d.k == DefKind.FanSpitter || d.k == DefKind.BoulderNest || d.k == DefKind.SpikeHedge ? isle.hpScale : 1f);
        // Shore (0) keeps exact M1 heart/tower HP: hpScale 1
        if (isleIndex == 0) hp = sp.hp;
        var def = new Defense{ id = nextId++, kind = d.k, cell = new Cell(d.x, d.y), pos = isle.World(new Cell(d.x, d.y)), hp = hp, maxHp = hp };
        defs.Add(def); if (sp.blocks) blocked[d.x + d.y * isle.W] = true;
      }
    }

    public bool ShoreOk(Cell c){ foreach (var s in isle.Shore) if (s.Equals(c)) return true; return false; }
    public bool Blocked(Cell c){ if (c.x < 0 || c.y < 0 || c.x >= isle.W || c.y >= isle.H) return true; return blocked[c.x + c.y * isle.W]; }
    public void RefreshBlocks(){ for (int i = 0; i < blocked.Length; i++) blocked[i] = false; foreach (var d in defs) if (d.Blocks) blocked[d.cell.x + d.cell.y * isle.W] = true; }
    public int TotalPop(){ int n = 0; foreach (var c in crafts) n += c.Pop; return n; }

    public bool PlaceCraft(Cell c){
      if (phase != Phase.Deploy || !ShoreOk(c) || crafts.Count >= CraftCap) return false;
      foreach (var x in crafts) if (x.cell.Equals(c)) return false;
      crafts.Add(new Craft{ cell = c }); return true;
    }
    public bool RemoveCraftAt(Cell c){ if (phase != Phase.Deploy) return false; for (int i = 0; i < crafts.Count; i++) if (crafts[i].cell.Equals(c)){ crafts.RemoveAt(i); return true; } return false; }
    public bool AddToCraft(int i, Kind k){ if (phase != Phase.Deploy || i < 0 || i >= crafts.Count) return false; var c = crafts[i]; if (c.load.Count >= CraftSlots) return false; if (TotalPop() + UnitSpec.Of(k).pop > popCap) return false; c.load.Add(k); return true; }
    public bool RemoveFromCraft(int i){ if (phase != Phase.Deploy || i < 0 || i >= crafts.Count || crafts[i].load.Count == 0) return false; crafts[i].load.RemoveAt(crafts[i].load.Count - 1); return true; }

    public bool StartFight(){
      if (phase != Phase.Deploy || crafts.Count == 0 || TotalPop() == 0) return false;
      units.Clear(); nextId = defs.Count + 10; int uid = 0;
      foreach (var c in crafts){
        int slot = 0; foreach (var k in c.load){
          var sp = UnitSpec.Of(k); var off = new Vector2((slot % 2) * 0.35f - 0.15f, (slot / 2) * 0.35f);
          float hp = sp.hp * hpMul(k);
          units.Add(new Unit{ id = uid++, kind = k, pos = isle.World(c.cell) + off, hp = hp, maxHp = hp, craft = crafts.IndexOf(c) });
          slot++;
        }
      }
      phase = Phase.Fight; fightClock = 0; lastResult = ""; lastCombatHit = 0; maxCombatIdle = 0; return true;
    }

    public void Retry(){
      var kept = crafts; int sc = scenerySeed; int ix = isleIndex; int pc = popCap; var hm = hpMul; var dm = dmgMul;
      defs.Clear(); units.Clear(); crafts = kept; phase = Phase.Deploy; fightClock = 0; time = 0; lastResult = ""; nextId = 0;
      isle = Isles.All[ix]; blocked = new bool[isle.W * isle.H];
      foreach (var d in isle.Defs){
        var sp = DefSpec.Of(d.k); float hp = ix == 0 ? sp.hp : sp.hp * isle.hpScale;
        var def = new Defense{ id = nextId++, kind = d.k, cell = new Cell(d.x, d.y), pos = isle.World(new Cell(d.x, d.y)), hp = hp, maxHp = hp };
        defs.Add(def); if (sp.blocks) blocked[d.x + d.y * isle.W] = true;
      }
      scenerySeed = sc; popCap = pc; hpMul = hm; dmgMul = dm;
    }

    public List<Cell> PathTo(Cell from, Cell goal){
      if (from.Equals(goal)) return new List<Cell>{ from };
      var prev = new Dictionary<Cell, Cell>(); var q = new Queue<Cell>(); q.Enqueue(from); prev[from] = from;
      int[] dx = { 0, 1, 0, -1, 1, 1, -1, -1 }, dy = { 1, 0, -1, 0, 1, -1, 1, -1 };
      while (q.Count > 0){
        var c = q.Dequeue();
        for (int i = 0; i < 8; i++){
          var n = new Cell(c.x + dx[i], c.y + dy[i]);
          if (Blocked(n) && !n.Equals(goal)) continue;
          if (prev.ContainsKey(n)) continue;
          prev[n] = c; if (n.Equals(goal)){ var path = new List<Cell>(); for (var p = n; ; p = prev[p]){ path.Add(p); if (p.Equals(from)) break; } path.Reverse(); return path; }
          q.Enqueue(n);
        }
      }
      return new List<Cell>{ from };
    }

    int PickTarget(Unit u){
      int best = -1; float bestScore = 1e9f;
      for (int i = 0; i < defs.Count; i++){
        var d = defs[i]; if (!d.Alive) continue;
        float dist = Vector2.Distance(u.pos, d.pos); float score = dist;
        switch (u.kind){
          case Kind.Ankylosaurus:
            if (d.kind == DefKind.RoostHeart) score -= 0.5f; else if (d.Spec.shoots) score -= 1.5f; break;
          case Kind.Dilophosaurus:
            if (d.Spec.shoots) score -= 2.2f; if (d.kind == DefKind.RoostHeart) score += 0.5f; break;
          case Kind.Tyrannosaurus:
            if (d.kind == DefKind.RoostHeart) score -= 2.5f; else if (!d.Spec.shoots) score -= 1.0f; break;
          default:
            if (d.kind == DefKind.RoostHeart) score += 0.8f; break;
        }
        if (score < bestScore){ bestScore = score; best = i; }
      }
      return best;
    }

    public void Tick(float dt){
      if (phase != Phase.Fight) return;
      fightClock += dt; time += dt; float idle = fightClock - lastCombatHit; if (idle > maxCombatIdle) maxCombatIdle = idle;
      foreach (var u in units){
        if (!u.Alive) continue;
        if (u.targetDef < 0 || u.targetDef >= defs.Count || !defs[u.targetDef].Alive){ u.targetDef = PickTarget(u); u.path.Clear(); u.pathI = 0; }
        if (u.targetDef < 0) continue;
        var t = defs[u.targetDef]; float dist = Vector2.Distance(u.pos, t.pos); var sp = u.Spec;
        if (dist <= sp.range){
          u.atkCd -= dt; if (u.atkCd <= 0){
            u.atkCd = sp.atkCd; float dmg = sp.dmg * dmgMul(u.kind);
            if (u.kind == Kind.Tyrannosaurus && (t.kind == DefKind.RoostHeart || t.kind == DefKind.SpikeHedge)) dmg *= 1.25f;
            t.hp -= dmg; lastCombatHit = fightClock;
            if (!t.Alive){ RefreshBlocks(); foreach (var o in units){ o.path.Clear(); o.pathI = 0; if (o.targetDef == u.targetDef) o.targetDef = -1; } }
          }
        } else {
          var goal = t.cell; var here = new Cell(Mathf.FloorToInt(u.pos.x), Mathf.FloorToInt(u.pos.y));
          if (u.path.Count == 0 || u.pathI >= u.path.Count){ u.path = PathTo(here, goal); u.pathI = 0; }
          if (u.path.Count > 0){
            if (u.pathI < u.path.Count - 1 && Vector2.Distance(u.pos, isle.World(u.path[u.pathI])) < 0.15f) u.pathI++;
            var dest = isle.World(u.path[Mathf.Min(u.pathI, u.path.Count - 1)]);
            u.pos = Vector2.MoveTowards(u.pos, dest, sp.speed * dt);
          }
        }
      }
      foreach (var d in defs){
        if (!d.Alive || !d.Spec.shoots) continue;
        d.atkCd -= dt; if (d.atkCd > 0) continue;
        Unit best = null; float bestScore = 1e9f;
        foreach (var u in units){ if (!u.Alive) continue; float dist = Vector2.Distance(d.pos, u.pos); if (dist > d.Spec.range) continue;
          float score = dist;
          if (d.kind == DefKind.BoulderNest && (u.kind == Kind.Ankylosaurus || u.kind == Kind.Tyrannosaurus)) score -= 2f;
          if (d.kind == DefKind.FanSpitter && u.kind == Kind.Velociraptor) score -= 1f;
          if (score < bestScore){ bestScore = score; best = u; } }
        if (best == null) continue;
        d.atkCd = d.Spec.cooldown; best.hp -= d.Spec.dmg; lastCombatHit = fightClock;
        if (d.Spec.splash > 0) foreach (var u in units) if (u.Alive && u != best && Vector2.Distance(u.pos, best.pos) <= d.Spec.splash){ u.hp -= d.Spec.dmg * 0.6f; lastCombatHit = fightClock; }
      }
      foreach (var d in defs) if (d.Alive && d.kind == DefKind.SpikeHedge){
        d.atkCd -= dt; if (d.atkCd > 0) continue; bool hit = false;
        foreach (var u in units) if (u.Alive && Vector2.Distance(u.pos, d.pos) < 0.75f){ u.hp -= d.Spec.dmg; hit = true; lastCombatHit = fightClock; }
        if (hit) d.atkCd = d.Spec.cooldown;
      }
      bool heartDead = true; foreach (var d in defs) if (d.kind == DefKind.RoostHeart && d.Alive) heartDead = false;
      if (heartDead){ phase = Phase.Win; lastResult = $"Win in {fightClock:F1}s"; Debug.Log($"[DW] outcome Win t={fightClock:F2} maxIdle={maxCombatIdle:F2} isle={isleIndex}"); return; }
      bool any = false; foreach (var u in units) if (u.Alive){ any = true; break; }
      if (!any){ phase = Phase.Lose; lastResult = $"Lose in {fightClock:F1}s"; Debug.Log($"[DW] outcome Lose t={fightClock:F2} maxIdle={maxCombatIdle:F2} heart={HeartHp():F0} isle={isleIndex}"); }
    }

    float HeartHp(){ foreach (var d in defs) if (d.kind == DefKind.RoostHeart) return d.hp; return 0; }
    public string PathFingerprint(){
      var sb = new System.Text.StringBuilder();
      foreach (var u in units) sb.Append(u.id).Append(':').Append(u.pos.x.ToString("F2")).Append(',').Append(u.pos.y.ToString("F2")).Append('>').Append(u.targetDef).Append('|');
      return sb.ToString();
    }
  }
}
