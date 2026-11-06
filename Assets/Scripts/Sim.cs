// Deterministic combat simulation seed: same deploy + same layout => same paths, even if scenery cosmetics differ.
using System.Collections.Generic;
using UnityEngine;

namespace DinoWorlds {
  public enum Kind { Velociraptor, Ankylosaurus }
  public enum DefKind { RoostHeart, PeckTower, FanSpitter, SpikeHedge, BoulderNest }
  public enum Phase { Deploy, Fight, Win, Lose }

  public struct Cell { public int x, y; public Cell(int x, int y){ this.x = x; this.y = y; }
    public static int Dist(Cell a, Cell b){ return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y); }
    public override bool Equals(object o){ return o is Cell c && c.x == x && c.y == y; }
    public override int GetHashCode(){ return x * 397 ^ y; }
  }

  public class UnitSpec {
    public Kind kind; public string name; public Color color;
    public float hp, dmg, range, speed, radius; public int pop;
    public static readonly UnitSpec[] All = {
      new UnitSpec{ kind = Kind.Velociraptor, name = "Velociraptor", color = new Color(0.55f, 0.92f, 0.25f), hp = 85, dmg = 3.0f, range = 1.1f, speed = 2.35f, radius = 0.35f, pop = 1 },
      new UnitSpec{ kind = Kind.Ankylosaurus, name = "Ankylosaurus", color = new Color(0.45f, 0.52f, 0.62f), hp = 360, dmg = 4.2f, range = 1.2f, speed = 1.12f, radius = 0.55f, pop = 2 },
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

  /// <summary>Fixed hand-designed Shore isle. Cosmetic scenery is stored separately and never feeds pathing.</summary>
  public static class IsleLayout {
    public const int W = 22, H = 18;
    public static readonly Cell[] Shore = {
      new Cell(3,0), new Cell(5,0), new Cell(7,0), new Cell(9,0), new Cell(11,0), new Cell(13,0), new Cell(15,0), new Cell(17,0),
      new Cell(4,1), new Cell(8,1), new Cell(12,1), new Cell(16,1)
    };
    // defenses: heart, towers, spitters, hedges, boulder
    public static readonly (DefKind k, int x, int y)[] Defs = {
      (DefKind.RoostHeart, 11, 15),
      (DefKind.PeckTower, 6, 8), (DefKind.PeckTower, 16, 8), (DefKind.PeckTower, 11, 11),
      (DefKind.FanSpitter, 8, 6), (DefKind.FanSpitter, 14, 6),
      (DefKind.BoulderNest, 11, 9),
      (DefKind.SpikeHedge, 5, 5), (DefKind.SpikeHedge, 6, 5), (DefKind.SpikeHedge, 7, 5),
      (DefKind.SpikeHedge, 14, 5), (DefKind.SpikeHedge, 15, 5), (DefKind.SpikeHedge, 16, 5),
      (DefKind.SpikeHedge, 10, 12), (DefKind.SpikeHedge, 12, 12),
    };
    public static Vector2 World(Cell c){ return new Vector2(c.x + 0.5f, c.y + 0.5f); }
    public static Cell Clamp(Cell c){ return new Cell(Mathf.Clamp(c.x, 0, W - 1), Mathf.Clamp(c.y, 0, H - 1)); }
  }

  public class Battle {
    public const int PopCap = 14, CraftCap = 4, CraftSlots = 4;
    public List<Defense> defs = new List<Defense>();
    public List<Unit> units = new List<Unit>();
    public List<Craft> crafts = new List<Craft>();
    public Phase phase = Phase.Deploy;
    public float time; public int seed; public int scenerySeed; // scenerySeed is cosmetic only
    public int nextId; public bool[] blocked;
    public float fightClock; public string lastResult = ""; public float lastCombatHit; public float maxCombatIdle;

    public Battle(int scenerySeed = 1){
      this.scenerySeed = scenerySeed; seed = 7; // fixed combat seed; never derived from scenery
      blocked = new bool[IsleLayout.W * IsleLayout.H];
      foreach (var d in IsleLayout.Defs){
        var sp = DefSpec.Of(d.k); var def = new Defense{ id = nextId++, kind = d.k, cell = new Cell(d.x, d.y), pos = IsleLayout.World(new Cell(d.x, d.y)), hp = sp.hp, maxHp = sp.hp };
        defs.Add(def); if (sp.blocks) blocked[d.x + d.y * IsleLayout.W] = true;
      }
    }

    public bool ShoreOk(Cell c){ foreach (var s in IsleLayout.Shore) if (s.Equals(c)) return true; return false; }
    public bool Blocked(Cell c){ if (c.x < 0 || c.y < 0 || c.x >= IsleLayout.W || c.y >= IsleLayout.H) return true; return blocked[c.x + c.y * IsleLayout.W]; }
    public void RefreshBlocks(){ for (int i = 0; i < blocked.Length; i++) blocked[i] = false; foreach (var d in defs) if (d.Blocks) blocked[d.cell.x + d.cell.y * IsleLayout.W] = true; }

    public int TotalPop(){ int n = 0; foreach (var c in crafts) n += c.Pop; return n; }

    public bool PlaceCraft(Cell c){
      if (phase != Phase.Deploy || !ShoreOk(c) || crafts.Count >= CraftCap) return false;
      foreach (var x in crafts) if (x.cell.Equals(c)) return false;
      crafts.Add(new Craft{ cell = c }); return true;
    }
    public bool RemoveCraftAt(Cell c){ if (phase != Phase.Deploy) return false; for (int i = 0; i < crafts.Count; i++) if (crafts[i].cell.Equals(c)){ crafts.RemoveAt(i); return true; } return false; }
    public bool AddToCraft(int i, Kind k){ if (phase != Phase.Deploy || i < 0 || i >= crafts.Count) return false; var c = crafts[i]; if (c.load.Count >= CraftSlots) return false; if (TotalPop() + UnitSpec.Of(k).pop > PopCap) return false; c.load.Add(k); return true; }
    public bool RemoveFromCraft(int i){ if (phase != Phase.Deploy || i < 0 || i >= crafts.Count || crafts[i].load.Count == 0) return false; crafts[i].load.RemoveAt(crafts[i].load.Count - 1); return true; }

    public bool StartFight(){
      if (phase != Phase.Deploy || crafts.Count == 0 || TotalPop() == 0) return false;
      units.Clear(); nextId = defs.Count + 10; int uid = 0;
      foreach (var c in crafts){
        int slot = 0; foreach (var k in c.load){
          var sp = UnitSpec.Of(k); var off = new Vector2((slot % 2) * 0.35f - 0.15f, (slot / 2) * 0.35f);
          units.Add(new Unit{ id = uid++, kind = k, pos = IsleLayout.World(c.cell) + off, hp = sp.hp, maxHp = sp.hp, craft = crafts.IndexOf(c) });
          slot++;
        }
      }
      phase = Phase.Fight; fightClock = 0; lastResult = ""; lastCombatHit = 0; maxCombatIdle = 0; return true;
    }

    public void Retry(){
      // keep craft placements and loads; reset defs and units
      var kept = crafts; int sc = scenerySeed;
      defs.Clear(); units.Clear(); crafts = kept; phase = Phase.Deploy; fightClock = 0; time = 0; lastResult = ""; nextId = 0;
      blocked = new bool[IsleLayout.W * IsleLayout.H];
      foreach (var d in IsleLayout.Defs){
        var sp = DefSpec.Of(d.k); var def = new Defense{ id = nextId++, kind = d.k, cell = new Cell(d.x, d.y), pos = IsleLayout.World(new Cell(d.x, d.y)), hp = sp.hp, maxHp = sp.hp };
        defs.Add(def); if (sp.blocks) blocked[d.x + d.y * IsleLayout.W] = true;
      }
      scenerySeed = sc; // cosmetics may be regenerated by the view; combat seed stays fixed
    }

    // ---------- pathfinding (deterministic BFS; blocked cells from living blocky defenses only) ----------
    public List<Cell> PathTo(Cell from, Cell goal){
      if (from.Equals(goal)) return new List<Cell>{ from };
      var prev = new Dictionary<Cell, Cell>(); var q = new Queue<Cell>(); q.Enqueue(from); prev[from] = from;
      int[] dx = { 0, 1, 0, -1, 1, 1, -1, -1 }, dy = { 1, 0, -1, 0, 1, -1, 1, -1 };
      while (q.Count > 0){
        var c = q.Dequeue();
        for (int i = 0; i < 8; i++){ // fixed neighbor order = deterministic
          var n = new Cell(c.x + dx[i], c.y + dy[i]);
          if (Blocked(n) && !n.Equals(goal)) continue;
          if (prev.ContainsKey(n)) continue;
          prev[n] = c; if (n.Equals(goal)){ var path = new List<Cell>(); for (var p = n; ; p = prev[p]){ path.Add(p); if (p.Equals(from)) break; } path.Reverse(); return path; }
          q.Enqueue(n);
        }
      }
      return new List<Cell>{ from }; // stuck
    }

    int PickTarget(Unit u){
      // Velociraptor: nearest living; Ankylosaurus: prefer shooters, then nearest
      int best = -1; float bestScore = 1e9f;
      for (int i = 0; i < defs.Count; i++){
        var d = defs[i]; if (!d.Alive) continue;
        float dist = Vector2.Distance(u.pos, d.pos);
        float score = dist;
        if (u.kind == Kind.Ankylosaurus){
          if (d.kind == DefKind.RoostHeart) score -= 0.5f;
          else if (d.Spec.shoots) score -= 1.5f;
        } else {
          if (d.kind == DefKind.RoostHeart) score += 0.8f; // swarm opens a path first
        }
        if (score < bestScore){ bestScore = score; best = i; }
      }
      return best;
    }

    public void Tick(float dt){
      if (phase != Phase.Fight) return;
      fightClock += dt; time += dt; if (phase == Phase.Fight){ float idle = fightClock - lastCombatHit; if (idle > maxCombatIdle) maxCombatIdle = idle; }
      // units act
      foreach (var u in units){
        if (!u.Alive) continue;
        if (u.targetDef < 0 || u.targetDef >= defs.Count || !defs[u.targetDef].Alive){ u.targetDef = PickTarget(u); u.path.Clear(); u.pathI = 0; }
        if (u.targetDef < 0) continue;
        var t = defs[u.targetDef]; float dist = Vector2.Distance(u.pos, t.pos);
        var sp = u.Spec;
        if (dist <= sp.range){
          u.atkCd -= dt; if (u.atkCd <= 0){ u.atkCd = u.kind == Kind.Velociraptor ? 0.7f : 1.1f; t.hp -= sp.dmg; lastCombatHit = fightClock;
            if (!t.Alive){ RefreshBlocks(); foreach (var o in units){ o.path.Clear(); o.pathI = 0; if (o.targetDef == u.targetDef) o.targetDef = -1; } }
          }
        } else {
          var goal = t.cell; var here = new Cell(Mathf.FloorToInt(u.pos.x), Mathf.FloorToInt(u.pos.y));
          if (u.path.Count == 0 || u.pathI >= u.path.Count){ u.path = PathTo(here, goal); u.pathI = 0; }
          if (u.path.Count > 0){
            if (u.pathI < u.path.Count - 1 && Vector2.Distance(u.pos, IsleLayout.World(u.path[u.pathI])) < 0.15f) u.pathI++;
            var dest = IsleLayout.World(u.path[Mathf.Min(u.pathI, u.path.Count - 1)]);
            u.pos = Vector2.MoveTowards(u.pos, dest, sp.speed * dt);
          }
        }
      }
      // defenses shoot
      foreach (var d in defs){
        if (!d.Alive || !d.Spec.shoots) continue;
        d.atkCd -= dt; if (d.atkCd > 0) continue;
        Unit best = null; float bestScore = 1e9f;
        foreach (var u in units){ if (!u.Alive) continue; float dist = Vector2.Distance(d.pos, u.pos); if (dist > d.Spec.range) continue;
          float score = dist; if (d.kind == DefKind.BoulderNest && u.kind == Kind.Ankylosaurus) score -= 2f;
          if (d.kind == DefKind.FanSpitter && u.kind == Kind.Velociraptor) score -= 1f;
          if (score < bestScore){ bestScore = score; best = u; } }
        if (best == null) continue;
        d.atkCd = d.Spec.cooldown; best.hp -= d.Spec.dmg; lastCombatHit = fightClock;
        if (d.Spec.splash > 0) foreach (var u in units) if (u.Alive && u != best && Vector2.Distance(u.pos, best.pos) <= d.Spec.splash){ u.hp -= d.Spec.dmg * 0.6f; lastCombatHit = fightClock; }
      }
      // spike contact
      foreach (var d in defs) if (d.Alive && d.kind == DefKind.SpikeHedge){
        d.atkCd -= dt; if (d.atkCd > 0) continue; bool hit = false;
        foreach (var u in units) if (u.Alive && Vector2.Distance(u.pos, d.pos) < 0.75f){ u.hp -= d.Spec.dmg; hit = true; lastCombatHit = fightClock; }
        if (hit) d.atkCd = d.Spec.cooldown;
      }
      // outcomes
      bool heartDead = true; foreach (var d in defs) if (d.kind == DefKind.RoostHeart && d.Alive) heartDead = false;
      if (heartDead){ phase = Phase.Win; lastResult = $"Win in {fightClock:F1}s"; Debug.Log($"[DW] outcome Win t={fightClock:F2} maxIdle={maxCombatIdle:F2}"); return; }
      bool any = false; foreach (var u in units) if (u.Alive){ any = true; break; }
      if (!any){ phase = Phase.Lose; lastResult = $"Lose in {fightClock:F1}s"; Debug.Log($"[DW] outcome Lose t={fightClock:F2} maxIdle={maxCombatIdle:F2} heart={HeartHp():F0}"); }
    }

    /// <summary>Fingerprint of living unit cells + targets for determinism checks (ignores scenery).</summary>
    float HeartHp(){ foreach (var d in defs) if (d.kind == DefKind.RoostHeart) return d.hp; return 0; }
    public string PathFingerprint(){
      var sb = new System.Text.StringBuilder();
      foreach (var u in units){ sb.Append(u.id).Append(':').Append(u.pos.x.ToString("F2")).Append(',').Append(u.pos.y.ToString("F2")).Append('>').Append(u.targetDef).Append('|'); }
      return sb.ToString();
    }
  }
}
