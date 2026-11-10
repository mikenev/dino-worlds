using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace DinoWorlds {
  /// <summary>M5: visual-only Idle / Walk / Attack blend for the rigged dinos via Playables (no controller asset).
  /// Driven manually from Game with the frame dt, so pause and QA ticking never touch the Sim.</summary>
  public class DinoAnim : MonoBehaviour {
    static readonly Dictionary<string, AnimationClip[]> clipCache = new Dictionary<string, AnimationClip[]>();
    public static int active, attacks, walkers; public static float evalT;
    PlayableGraph graph; AnimationMixerPlayable mix; AnimationClipPlayable idle, walk, atk;
    float atkLeft, atkLen, atkSpeed = 1f, walkW;
    Vector3 lastPos; bool hasLast;

    static AnimationClip[] Clips(string model){
      if (clipCache.TryGetValue(model, out var c)) return c;
      AnimationClip i = null, w = null, a = null;
      foreach (var clip in Resources.LoadAll<AnimationClip>("DW/Models/" + model)){
        if (clip.name.StartsWith("__preview__")) continue;
        if (clip.name.EndsWith("Idle")) i = clip; else if (clip.name.EndsWith("Walk")) w = clip; else if (clip.name.EndsWith("Attack")) a = clip;
      }
      c = (i != null && w != null && a != null) ? new[]{ i, w, a } : null; clipCache[model] = c; return c;
    }

    /// <summary>Attach to a spawned dino; returns null (static model) if it has no Animator or clips.</summary>
    public static DinoAnim Attach(Transform root, string model, float atkCd){
      var an = root.GetComponentInChildren<Animator>(); var c = Clips(model); if (an == null || c == null) return null;
      an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.CullCompletely;
      var d = an.gameObject.AddComponent<DinoAnim>();
      d.graph = PlayableGraph.Create("DW_" + model); d.graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
      d.mix = AnimationMixerPlayable.Create(d.graph, 3);
      d.idle = AnimationClipPlayable.Create(d.graph, c[0]); d.walk = AnimationClipPlayable.Create(d.graph, c[1]); d.atk = AnimationClipPlayable.Create(d.graph, c[2]);
      d.graph.Connect(d.idle, 0, d.mix, 0); d.graph.Connect(d.walk, 0, d.mix, 1); d.graph.Connect(d.atk, 0, d.mix, 2);
      d.idle.SetTime((root.GetInstanceID() & 15) / 16f * c[0].length); // desync idles (no UnityEngine.Random: keeps any RNG untouched)
      d.atkLen = c[2].length; d.atkSpeed = atkCd > 0 ? Mathf.Max(1f, d.atkLen / atkCd) : 1f; d.atk.SetSpeed(d.atkSpeed);
      d.mix.SetInputWeight(0, 1f);
      var o = AnimationPlayableOutput.Create(d.graph, "out", an); o.SetSourcePlayable(d.mix);
      d.graph.Play(); d.graph.Evaluate(0f); active++;
      return d;
    }

    public void Attack(){ attacks++; atkLeft = atkLen / atkSpeed; atk.SetTime(0); atk.SetDone(false); }

    /// <summary>dt = 0 freezes the pose (pause / QA drive).</summary>
    public void Step(float dt, Vector3 pos){
      bool moving = hasLast && dt > 0 && (pos - lastPos).sqrMagnitude > 1e-6f; lastPos = pos; hasLast = true;
      if (dt <= 0) return;
      walkW = Mathf.MoveTowards(walkW, moving ? 1f : 0f, dt * 8f);
      float aw = 0f; if (atkLeft > 0){ atkLeft -= dt; aw = Mathf.Clamp01(Mathf.Min(atkLeft * 10f, 1f)); }
      mix.SetInputWeight(2, aw); mix.SetInputWeight(1, walkW * (1 - aw)); mix.SetInputWeight(0, (1 - walkW) * (1 - aw));
      if (walkW > 0.5f) walkers++; evalT += dt; graph.Evaluate(dt);
    }

    void OnDestroy(){ if (graph.IsValid()){ graph.Destroy(); active--; } }
  }
}
