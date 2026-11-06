using System;
using System.Collections.Generic;
using UnityEngine;

namespace DinoWorlds {
  public enum BldKind { RoostHeart, Hatchery, Workshop, AmberPit, Nest }
  public enum Mode { Home, Map, Deploy, Fight, Result }

  public class Building {
    public BldKind kind; public Cell cell; public int level = 1;
  }

  /// <summary>Home roost + Amber/Bone Shards. Instant spends. Never attacked.</summary>
  public class Progress {
    public const string SaveKey = "dino_worlds_m2";
    public int amber = 80, shards = 40;
    public int hatchery = 1, workshop = 1, storage = 1;
    public int[] unitLvl = { 1, 1, 1, 1 }; // per Kind
    public bool[] isleClear = new bool[4];
    public List<Building> buildings = new List<Building>();
    public int PopCap => 12 + hatchery * 2; // 14 at L1, up to 18 at L3
    public int AmberCap => 100 + storage * 50;
    public int ShardCap => 80 + storage * 40;

    public Progress(){ EnsureHeart(); }

    void EnsureHeart(){
      bool has = false; foreach (var b in buildings) if (b.kind == BldKind.RoostHeart) has = true;
      if (!has) buildings.Add(new Building{ kind = BldKind.RoostHeart, cell = new Cell(4, 4), level = 1 });
    }

    public bool Unlocked(Kind k){
      if (k == Kind.Velociraptor || k == Kind.Ankylosaurus) return true;
      if (k == Kind.Dilophosaurus) return hatchery >= 2;
      if (k == Kind.Tyrannosaurus) return hatchery >= 3;
      return false;
    }

    public bool IsleOpen(int i){
      if (i <= 0) return true;
      return isleClear[i - 1];
    }

    public int Footprint(BldKind k){ return k == BldKind.RoostHeart ? 2 : 1; }

    public bool CanPlace(BldKind k, Cell c){
      if (k == BldKind.RoostHeart) return false; // fixed
      int n = 0; foreach (var b in buildings) if (b.kind == k) n++;
      if (k == BldKind.Hatchery && n >= 1) return false;
      if (k == BldKind.Workshop && n >= 1) return false;
      if (k == BldKind.AmberPit && n >= 1) return false;
      if (k == BldKind.Nest && n >= 4) return false;
      int fp = Footprint(k);
      if (c.x < 0 || c.y < 0 || c.x + fp > Home.W || c.y + fp > Home.H) return false;
      foreach (var b in buildings){
        int bf = Footprint(b.kind);
        if (c.x < b.cell.x + bf && c.x + fp > b.cell.x && c.y < b.cell.y + bf && c.y + fp > b.cell.y) return false;
      }
      return true;
    }

    public int BuildCost(BldKind k){
      switch (k){ case BldKind.Hatchery: return 40; case BldKind.Workshop: return 35; case BldKind.AmberPit: return 25; case BldKind.Nest: return 10; }
      return 999;
    }

    public bool TryBuild(BldKind k, Cell c){
      int cost = BuildCost(k); if (amber < cost || !CanPlace(k, c)) return false;
      amber -= cost; buildings.Add(new Building{ kind = k, cell = c, level = 1 });
      if (k == BldKind.Hatchery) hatchery = Mathf.Max(hatchery, 1);
      if (k == BldKind.Workshop) workshop = Mathf.Max(workshop, 1);
      if (k == BldKind.AmberPit) storage = Mathf.Max(storage, 1);
      Save(); return true;
    }

    public bool TryUpgradeHatchery(){
      if (hatchery >= 3) return false; int cost = hatchery == 1 ? 60 : 100;
      bool has = false; foreach (var b in buildings) if (b.kind == BldKind.Hatchery) has = true;
      if (!has || amber < cost) return false;
      amber -= cost; hatchery++; foreach (var b in buildings) if (b.kind == BldKind.Hatchery) b.level = hatchery;
      Save(); return true;
    }

    public bool TryUpgradeWorkshop(){
      if (workshop >= 3) return false; int cost = workshop == 1 ? 50 : 80;
      bool has = false; foreach (var b in buildings) if (b.kind == BldKind.Workshop) has = true;
      if (!has || amber < cost) return false;
      amber -= cost; workshop++; foreach (var b in buildings) if (b.kind == BldKind.Workshop) b.level = workshop;
      Save(); return true;
    }

    public bool TryUpgradeStorage(){
      if (storage >= 3) return false; int cost = storage == 1 ? 40 : 70;
      bool has = false; foreach (var b in buildings) if (b.kind == BldKind.AmberPit) has = true;
      if (!has || amber < cost) return false;
      amber -= cost; storage++; foreach (var b in buildings) if (b.kind == BldKind.AmberPit) b.level = storage;
      Save(); return true;
    }

    public bool TryUpgradeUnit(Kind k){
      int i = (int)k; if (i < 0 || i > 3 || unitLvl[i] >= 3 || !Unlocked(k)) return false;
      int cost = unitLvl[i] == 1 ? 30 : 55; if (shards < cost) return false;
      shards -= cost; unitLvl[i]++; Save(); return true;
    }

    public float UnitHpMul(Kind k){ return 1f + 0.18f * (unitLvl[(int)k] - 1) + 0.05f * (workshop - 1); }
    public float UnitDmgMul(Kind k){ return 1f + 0.15f * (unitLvl[(int)k] - 1) + 0.04f * (workshop - 1); }

    public void GrantWin(int isle){
      int[] amb = { 35, 45, 55, 70 }, sh = { 20, 28, 36, 48 };
      amber = Mathf.Min(AmberCap, amber + amb[isle]); shards = Mathf.Min(ShardCap, shards + sh[isle]);
      if (!isleClear[isle]){ isleClear[isle] = true; amber = Mathf.Min(AmberCap, amber + 15); shards = Mathf.Min(ShardCap, shards + 10); }
      Save();
    }
    public void GrantLose(){ shards = Mathf.Min(ShardCap, shards + 5); Save(); }

    public void Save(){
      var sb = new System.Text.StringBuilder();
      sb.Append(amber).Append(',').Append(shards).Append(',').Append(hatchery).Append(',').Append(workshop).Append(',').Append(storage).Append('|');
      sb.Append(string.Join(",", unitLvl)).Append('|');
      sb.Append(string.Join(",", Array.ConvertAll(isleClear, b => b ? "1" : "0"))).Append('|');
      foreach (var b in buildings) sb.Append((int)b.kind).Append(':').Append(b.cell.x).Append(':').Append(b.cell.y).Append(':').Append(b.level).Append(';');
      PlayerPrefs.SetString(SaveKey, sb.ToString()); PlayerPrefs.Save();
      Debug.Log($"[DW] save amber={amber} shards={shards} hatch={hatchery} clear={string.Join("", Array.ConvertAll(isleClear, b => b ? "1" : "0"))}");
    }

    public static Progress LoadOrNew(){
      var p = new Progress();
      if (!PlayerPrefs.HasKey(SaveKey)) { p.Save(); return p; }
      try {
        var raw = PlayerPrefs.GetString(SaveKey); var parts = raw.Split('|');
        var a = parts[0].Split(','); p.amber = int.Parse(a[0]); p.shards = int.Parse(a[1]); p.hatchery = int.Parse(a[2]); p.workshop = int.Parse(a[3]); p.storage = int.Parse(a[4]);
        var u = parts[1].Split(','); for (int i = 0; i < 4 && i < u.Length; i++) p.unitLvl[i] = int.Parse(u[i]);
        var c = parts[2].Split(','); for (int i = 0; i < 4 && i < c.Length; i++) p.isleClear[i] = c[i] == "1";
        p.buildings.Clear();
        if (parts.Length > 3 && parts[3].Length > 0) foreach (var tok in parts[3].Split(new[]{';'}, StringSplitOptions.RemoveEmptyEntries)){
          var t = tok.Split(':'); p.buildings.Add(new Building{ kind = (BldKind)int.Parse(t[0]), cell = new Cell(int.Parse(t[1]), int.Parse(t[2])), level = int.Parse(t[3]) });
        }
        p.EnsureHeart();
        Debug.Log($"[DW] load amber={p.amber} shards={p.shards} hatch={p.hatchery}");
      } catch (Exception e){ Debug.LogWarning("[DW] save corrupt, fresh: " + e.Message); p = new Progress(); p.Save(); }
      return p;
    }

    public static void ClearSave(){ PlayerPrefs.DeleteKey(SaveKey); PlayerPrefs.Save(); Debug.Log("[DW] save cleared"); }
  }

  public static class Home {
    public const int W = 9, H = 9;
  }
}
