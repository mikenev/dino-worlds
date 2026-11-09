using System.Collections.Generic;
using UnityEngine;

namespace DinoWorlds {
  /// <summary>M4 art: loads the original toy-vinyl FBX models from Resources/DW/Models.
  /// Any model that fails to load falls back to the ToyArt procedural primitive.</summary>
  public static class ArtLib {
    static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();
    static readonly HashSet<string> missing = new HashSet<string>();
    static Material vinyl; static bool vinylTried;
    public static int modelSpawns, fallbackSpawns;

    public static GameObject Prefab(string name){
      if (cache.TryGetValue(name, out var go)) return go;
      if (missing.Contains(name)) return null;
      go = Resources.Load<GameObject>("DW/Models/" + name);
      if (go == null){ missing.Add(name); Debug.LogWarning($"[DW] art missing {name} -> primitive fallback"); return null; }
      cache[name] = go; return go;
    }

    /// <summary>Shared URP Lit vinyl material (instancing on). Repairs the shader at runtime if import left it on a non-URP shader.</summary>
    public static Material Vinyl(){
      if (vinylTried) return vinyl; vinylTried = true;
      vinyl = Resources.Load<Material>("DW/Materials/DW_ToyVinyl");
      if (vinyl != null && (vinyl.shader == null || !vinyl.shader.name.StartsWith("Universal Render Pipeline"))){
        var sh = Shader.Find("Universal Render Pipeline/Lit"); var tex = Resources.Load<Texture2D>("DW/Textures/DW_ToyAtlas");
        if (sh != null){ vinyl = new Material(sh){ name = "DW_ToyVinyl_URP" }; if (tex) vinyl.SetTexture("_BaseMap", tex); vinyl.SetFloat("_Smoothness", 0.58f); vinyl.SetFloat("_Metallic", 0f); }
      }
      if (vinyl != null) vinyl.enableInstancing = true;
      return vinyl;
    }

    /// <summary>Spawn a model by asset name; fit = max footprint (XZ) in cells, 0 = native scale. Returns the root transform.</summary>
    public static Transform Spawn(string model, string objName, Transform parent, Vector3 pos, float fit, Mesh fallbackMesh, Material fallbackMat){
      var prefab = Prefab(model);
      if (prefab != null){
        var go = Object.Instantiate(prefab, parent, false); go.name = objName; go.transform.position = pos;
        var mat = Vinyl();
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>()){
          if (mat != null && (r.sharedMaterial == null || r.sharedMaterial.shader == null || !r.sharedMaterial.shader.name.StartsWith("Universal Render Pipeline"))) r.sharedMaterial = mat;
          r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        }
        if (fit > 0){
          var b = Bounds(go); float m = Mathf.Max(b.size.x, b.size.z);
          if (m > fit && m > 0.001f){
            var child = new GameObject(objName); child.transform.SetParent(parent, false); child.transform.position = pos;
            go.transform.SetParent(child.transform, true); go.transform.localScale = Vector3.one * (fit / m); go.name = "model";
            modelSpawns++; return child.transform;
          }
        }
        modelSpawns++; return go.transform;
      }
      fallbackSpawns++;
      var f = new GameObject(objName); f.transform.SetParent(parent, false); f.transform.position = pos;
      f.AddComponent<MeshFilter>().sharedMesh = fallbackMesh; f.AddComponent<MeshRenderer>().sharedMaterial = fallbackMat; return f.transform;
    }

    static Bounds Bounds(GameObject go){
      var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
      var b = rs[0].bounds; for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds); return b;
    }

    public static string UnitModel(Kind k){ return "DW_" + k; }
    public static string DefModel(DefKind k){ return "DW_" + k; }
    public static string BldModel(BldKind k){ return "DW_" + k; }

    static readonly Dictionary<string, Texture2D> ui = new Dictionary<string, Texture2D>();
    public static Texture2D Ui(string n){ if (ui.TryGetValue(n, out var t)) return t; t = Resources.Load<Texture2D>("DW/UI/" + n); ui[n] = t; return t; }
    public static string Status(){ return $"models={modelSpawns} fallback={fallbackSpawns} missing={missing.Count} vinyl={(Vinyl() != null ? Vinyl().shader.name : "none")}"; }
  }
}
