using UnityEngine;

namespace DinoWorlds {
  /// <summary>M4: original recorded-synth WAVs from Resources/DW/Audio (see Assets/Audio/MANIFEST.md).
  /// Each clip falls back to the M3 procedural synth if it fails to load. Volumes/mute persist locally.</summary>
  public class DwAudio : MonoBehaviour {
    AudioSource music, sfx;
    AudioClip homeLoop, raidLoop, winSting, loseSting, uiPop, craftSplash, peck, spit, smash;
    readonly AudioClip[] speciesCall = new AudioClip[4];
    readonly float[] callGain = { 0.6f, 1.0f, 1.0f, 0.8f };
    readonly float[] callNext = new float[4];
    string bed = "none";
    public float musicVol = 0.35f, sfxVol = 0.55f; public bool muted;
    public int playCount, loaded, synthFallback;
    const string KMus = "dw_opt_music", KSfx = "dw_opt_sfx", KMute = "dw_opt_mute";

    public void Init(){
      musicVol = PlayerPrefs.GetFloat(KMus, musicVol); sfxVol = PlayerPrefs.GetFloat(KSfx, sfxVol); muted = PlayerPrefs.GetInt(KMute, 0) == 1;
      music = gameObject.AddComponent<AudioSource>(); music.loop = true; music.playOnAwake = false;
      sfx = gameObject.AddComponent<AudioSource>(); sfx.loop = false; sfx.playOnAwake = false;
      homeLoop = Load("home_loop") ?? SynthLoop("home", 2.0f, HomeTone);
      raidLoop = Load("raid_loop") ?? SynthLoop("raid", 1.6f, RaidTone);
      winSting = Load("win_sting") ?? SynthOneShot("win", 0.55f, WinTone);
      loseSting = Load("lose_sting") ?? SynthOneShot("lose", 0.55f, LoseTone);
      uiPop = Load("ui_pop") ?? SynthOneShot("ui", 0.08f, (t, _) => Mathf.Sin(2 * Mathf.PI * 880 * t) * Mathf.Exp(-t * 40f) * 0.4f);
      craftSplash = Load("craft_splash") ?? SynthOneShot("splash", 0.2f, (t, _) => (Mathf.PerlinNoise(t * 40f, 0.2f) * 2 - 1) * Mathf.Exp(-t * 8f) * 0.5f);
      peck = Load("peck_hit") ?? SynthOneShot("peck", 0.1f, (t, _) => Mathf.Sin(2 * Mathf.PI * 420 * t) * Mathf.Exp(-t * 28f) * 0.45f);
      spit = Load("spit_attack") ?? SynthOneShot("spit", 0.12f, (t, _) => Mathf.Sin(2 * Mathf.PI * (520 + 200 * t) * t) * Mathf.Exp(-t * 18f) * 0.4f);
      smash = Load("smash_hit") ?? SynthOneShot("smash", 0.18f, (t, _) => (Mathf.Sin(2 * Mathf.PI * 90 * t) + 0.4f * Mathf.Sin(2 * Mathf.PI * 45 * t)) * Mathf.Exp(-t * 10f) * 0.55f);
      speciesCall[0] = Load("velociraptor_call"); speciesCall[1] = Load("ankylosaurus_call");
      speciesCall[2] = Load("dilophosaurus_call"); speciesCall[3] = Load("tyrannosaurus_call");
      for (int i = 0; i < 4; i++) if (speciesCall[i] == null) speciesCall[i] = peck;
      ApplyVolumes();
      Debug.Log($"[DW] audio ready clips=13 loaded={loaded} synthFallback={synthFallback} mute={(muted?1:0)}");
    }

    AudioClip Load(string n){ var c = Resources.Load<AudioClip>("DW/Audio/" + n); if (c != null) loaded++; else { synthFallback++; Debug.LogWarning("[DW] audio missing " + n + " -> synth"); } return c; }
    public void ApplyVolumes(){ if (music) { music.volume = musicVol; music.mute = muted; } if (sfx) sfx.mute = muted; }
    public void SaveOptions(){ PlayerPrefs.SetFloat(KMus, musicVol); PlayerPrefs.SetFloat(KSfx, sfxVol); PlayerPrefs.SetInt(KMute, muted ? 1 : 0); PlayerPrefs.Save(); DwPersist.Flush(); ApplyVolumes(); }
    public void SetMusic(float v){ musicVol = Mathf.Clamp01(Mathf.Round(v * 10f) / 10f); SaveOptions(); }
    public void SetSfx(float v){ sfxVol = Mathf.Clamp01(Mathf.Round(v * 10f) / 10f); SaveOptions(); }
    public void SetMute(bool m){ muted = m; SaveOptions(); }

    delegate float Tone(float t, float dur);

    static float HomeTone(float t, float dur){
      float env = 0.55f + 0.45f * Mathf.Sin(2 * Mathf.PI * t / dur);
      float f1 = 220f, f2 = 277.18f, f3 = 329.63f;
      return (Mathf.Sin(2 * Mathf.PI * f1 * t) * 0.35f + Mathf.Sin(2 * Mathf.PI * f2 * t) * 0.25f + Mathf.Sin(2 * Mathf.PI * f3 * t) * 0.15f) * env * 0.35f;
    }
    static float RaidTone(float t, float dur){
      float beat = Mathf.Pow(Mathf.Abs(Mathf.Sin(2 * Mathf.PI * t * 2.5f)), 8f);
      float bass = Mathf.Sin(2 * Mathf.PI * 110 * t) * 0.3f;
      float snare = (Mathf.PerlinNoise(t * 90f, 1.1f) * 2 - 1) * beat * 0.35f;
      return (bass + snare + Mathf.Sin(2 * Mathf.PI * 330 * t) * 0.12f) * 0.4f;
    }
    static float WinTone(float t, float dur){
      float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
      int i = Mathf.Clamp(Mathf.FloorToInt(t / dur * notes.Length), 0, notes.Length - 1);
      return Mathf.Sin(2 * Mathf.PI * notes[i] * t) * Mathf.Exp(-t * 2.2f) * 0.5f;
    }
    static float LoseTone(float t, float dur){
      float f = Mathf.Lerp(392f, 196f, t / dur);
      return Mathf.Sin(2 * Mathf.PI * f * t) * Mathf.Exp(-t * 2.5f) * 0.4f;
    }

    static AudioClip SynthLoop(string name, float dur, Tone tone){
      int rate = 22050; int n = Mathf.CeilToInt(dur * rate);
      var data = new float[n];
      for (int i = 0; i < n; i++){ float t = i / (float)rate; data[i] = Mathf.Clamp(tone(t, dur), -1f, 1f); }
      var c = AudioClip.Create(name, n, 1, rate, false); c.SetData(data, 0); return c;
    }
    static AudioClip SynthOneShot(string name, float dur, Tone tone){
      int rate = 22050; int n = Mathf.CeilToInt(dur * rate);
      var data = new float[n];
      for (int i = 0; i < n; i++){ float t = i / (float)rate; data[i] = Mathf.Clamp(tone(t, dur), -1f, 1f); }
      var c = AudioClip.Create(name, n, 1, rate, false); c.SetData(data, 0); return c;
    }

    public void SetBed(string which){
      if (bed == which && music.isPlaying) return;
      bed = which;
      music.Stop();
      if (which == "home"){ music.clip = homeLoop; ApplyVolumes(); music.Play(); }
      else if (which == "raid"){ music.clip = raidLoop; ApplyVolumes(); music.Play(); }
      else bed = "none";
      Debug.Log($"[DW] audio bed={bed} playing={(music.isPlaying ? 1 : 0)} clip={(music.clip ? music.clip.name : "-")}");
    }
    void PlaySfx(AudioClip c, float gain = 1f){ if (c == null) return; sfx.PlayOneShot(c, sfxVol * gain); playCount++; Debug.Log($"[DW] sfx {c.name} plays={playCount}"); }
    public void Ui(){ PlaySfx(uiPop, 0.8f); }
    public void Splash(){ PlaySfx(craftSplash); }
    public void Peck(){ PlaySfx(peck, 0.7f); }
    public void Spit(){ PlaySfx(spit); }
    public void Smash(){ PlaySfx(smash); }
    /// <summary>Species call with a per-species cooldown so stacking a craft does not spam.</summary>
    public void Call(Kind k){ int i = (int)k; if (Time.unscaledTime < callNext[i]) return; callNext[i] = Time.unscaledTime + 0.6f; PlaySfx(speciesCall[i], callGain[i]); }
    public void Win(){ music.Stop(); bed = "none"; PlaySfx(winSting); }
    public void Lose(){ music.Stop(); bed = "none"; PlaySfx(loseSting); }
    public string Status(){ return $"bed={bed} music={(music != null && music.isPlaying ? 1 : 0)} plays={playCount} volM={musicVol:F2} volS={sfxVol:F2} mute={(muted?1:0)} loaded={loaded}"; }
  }
}
