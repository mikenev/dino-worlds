using UnityEngine;

namespace DinoWorlds {
  /// <summary>Original colorful toy-like procedural meshes. No franchise lookalikes.</summary>
  public static class ToyArt {
    public static Mesh Box(string name, Vector3 size){
      var m = new Mesh{ name = name }; float x = size.x * 0.5f, y = size.y * 0.5f, z = size.z * 0.5f;
      var v = new Vector3[]{ new(-x,-y,-z), new(x,-y,-z), new(x,y,-z), new(-x,y,-z), new(-x,-y,z), new(x,-y,z), new(x,y,z), new(-x,y,z) };
      var t = new int[]{ 0,2,1, 0,3,2, 4,5,6, 4,6,7, 0,1,5, 0,5,4, 2,3,7, 2,7,6, 0,4,7, 0,7,3, 1,2,6, 1,6,5 };
      m.vertices = v; m.triangles = t; m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }
    public static Mesh Capsuleish(string name, float r, float h){
      // low-poly toy capsule: stacked boxes approximate a rounded body
      var b = new CombineInstance[3];
      b[0].mesh = Box("a", new Vector3(r * 2, h * 0.5f, r * 1.4f)); b[0].transform = Matrix4x4.Translate(new Vector3(0, h * 0.35f, 0));
      b[1].mesh = Box("b", new Vector3(r * 1.6f, h * 0.4f, r * 1.2f)); b[1].transform = Matrix4x4.Translate(new Vector3(0, h * 0.7f, r * 0.15f));
      b[2].mesh = Box("c", new Vector3(r * 0.9f, r * 0.9f, r * 1.1f)); b[2].transform = Matrix4x4.Translate(new Vector3(0, h * 0.85f, r * 0.55f));
      var m = new Mesh{ name = name }; m.CombineMeshes(b, true, true); m.RecalculateNormals(); return m;
    }
    public static Mesh Ankylo(string name){
      var b = new CombineInstance[4];
      b[0].mesh = Box("body", new Vector3(1.1f, 0.55f, 0.85f)); b[0].transform = Matrix4x4.Translate(new Vector3(0, 0.35f, 0));
      b[1].mesh = Box("shell", new Vector3(1.2f, 0.35f, 0.95f)); b[1].transform = Matrix4x4.Translate(new Vector3(0, 0.65f, 0));
      b[2].mesh = Box("tail", new Vector3(0.7f, 0.25f, 0.25f)); b[2].transform = Matrix4x4.Translate(new Vector3(0, 0.35f, -0.7f));
      b[3].mesh = Box("club", new Vector3(0.35f, 0.3f, 0.35f)); b[3].transform = Matrix4x4.Translate(new Vector3(0, 0.35f, -1.05f));
      var m = new Mesh{ name = name }; m.CombineMeshes(b, true, true); m.RecalculateNormals(); return m;
    }
    public static Mesh Tower(string name, float h){
      var b = new CombineInstance[2];
      b[0].mesh = Box("stem", new Vector3(0.55f, h, 0.55f)); b[0].transform = Matrix4x4.Translate(new Vector3(0, h * 0.5f, 0));
      b[1].mesh = Box("head", new Vector3(0.85f, 0.4f, 0.85f)); b[1].transform = Matrix4x4.Translate(new Vector3(0, h + 0.15f, 0));
      var m = new Mesh{ name = name }; m.CombineMeshes(b, true, true); m.RecalculateNormals(); return m;
    }
    public static Mesh HeartNest(string name){
      var b = new CombineInstance[3];
      b[0].mesh = Box("base", new Vector3(1.6f, 0.4f, 1.6f)); b[0].transform = Matrix4x4.Translate(new Vector3(0, 0.2f, 0));
      b[1].mesh = Box("mid", new Vector3(1.2f, 0.7f, 1.2f)); b[1].transform = Matrix4x4.Translate(new Vector3(0, 0.7f, 0));
      b[2].mesh = Box("gem", new Vector3(0.6f, 0.6f, 0.6f)); b[2].transform = Matrix4x4.TRS(new Vector3(0, 1.25f, 0), Quaternion.Euler(0, 45, 0), Vector3.one);
      var m = new Mesh{ name = name }; m.CombineMeshes(b, true, true); m.RecalculateNormals(); return m;
    }
    public static Mesh Dilopho(string name){
      var b = new CombineInstance[3];
      b[0].mesh = Box("body", new Vector3(0.7f, 0.5f, 1.0f)); b[0].transform = Matrix4x4.Translate(new Vector3(0, 0.35f, 0));
      b[1].mesh = Box("crest", new Vector3(0.55f, 0.35f, 0.2f)); b[1].transform = Matrix4x4.Translate(new Vector3(0, 0.75f, 0.25f));
      b[2].mesh = Box("head", new Vector3(0.35f, 0.3f, 0.45f)); b[2].transform = Matrix4x4.Translate(new Vector3(0, 0.55f, 0.55f));
      var m = new Mesh{ name = name }; m.CombineMeshes(b, true, true); m.RecalculateNormals(); return m;
    }
    public static Mesh Rex(string name){
      var b = new CombineInstance[4];
      b[0].mesh = Box("body", new Vector3(1.0f, 0.85f, 1.4f)); b[0].transform = Matrix4x4.Translate(new Vector3(0, 0.6f, 0));
      b[1].mesh = Box("head", new Vector3(0.55f, 0.5f, 0.7f)); b[1].transform = Matrix4x4.Translate(new Vector3(0, 1.0f, 0.75f));
      b[2].mesh = Box("tail", new Vector3(0.35f, 0.3f, 1.0f)); b[2].transform = Matrix4x4.Translate(new Vector3(0, 0.5f, -0.9f));
      b[3].mesh = Box("leg", new Vector3(0.35f, 0.55f, 0.35f)); b[3].transform = Matrix4x4.Translate(new Vector3(0.25f, 0.25f, -0.1f));
      var m = new Mesh{ name = name }; m.CombineMeshes(b, true, true); m.RecalculateNormals(); return m;
    }
    public static Mesh Hut(string name){
      var b = new CombineInstance[2];
      b[0].mesh = Box("base", new Vector3(1.2f, 0.6f, 1.2f)); b[0].transform = Matrix4x4.Translate(new Vector3(0, 0.3f, 0));
      b[1].mesh = Box("roof", new Vector3(1.4f, 0.35f, 1.4f)); b[1].transform = Matrix4x4.Translate(new Vector3(0, 0.75f, 0));
      var m = new Mesh{ name = name }; m.CombineMeshes(b, true, true); m.RecalculateNormals(); return m;
    }
    public static Material Mat(Color c, bool unlit = true){
      var sh = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
      var m = new Material(sh); if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); else m.color = c; return m;
    }
  }
}
