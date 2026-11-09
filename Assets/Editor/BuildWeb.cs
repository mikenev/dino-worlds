using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using DinoWorlds;

public static class BuildWeb {
  [MenuItem("Dino Worlds/Rebuild Main Scene")]
  public static void Rebuild(){
    EnsureURP(); ConfigureM4Assets();
    var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    var camGo = new GameObject("Main Camera"); var cam = camGo.AddComponent<Camera>(); cam.tag = "MainCamera";
    cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.45f, 0.75f, 0.95f);
    camGo.AddComponent<AudioListener>();
    var go = new GameObject("Game"); go.AddComponent<Game>();
    Directory.CreateDirectory("Assets/Scenes");
    EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
    var ebs = EditorBuildSettings.scenes.ToList(); ebs.RemoveAll(s => true);
    ebs.Add(new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true)); EditorBuildSettings.scenes = ebs.ToArray();
    AssetDatabase.SaveAssets(); Debug.Log("[DW] scene rebuilt");
  }

  /// <summary>M4 import settings: audio per MANIFEST.md (WebGL-safe load types), shared vinyl material on URP Lit with the atlas.</summary>
  [MenuItem("Dino Worlds/Configure M4 Assets")]
  public static void ConfigureM4Assets(){
    int audio = 0;
    foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[]{ "Assets/Audio/Resources/DW/Audio" })){
      var path = AssetDatabase.GUIDToAssetPath(guid); var imp = AssetImporter.GetAtPath(path) as AudioImporter; if (imp == null) continue;
      bool loop = path.EndsWith("_loop.wav");
      var st = imp.defaultSampleSettings;
      st.loadType = loop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
      st.compressionFormat = AudioCompressionFormat.Vorbis; st.quality = loop ? 0.6f : 0.4f;
      st.preloadAudioData = !loop;
      imp.defaultSampleSettings = st; imp.forceToMono = false; imp.loadInBackground = loop;
      imp.SaveAndReimport(); audio++;
    }
    var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Resources/DW/Materials/DW_ToyVinyl.mat");
    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Resources/DW/Textures/DW_ToyAtlas.png");
    var lit = Shader.Find("Universal Render Pipeline/Lit");
    if (mat != null && lit != null){
      if (mat.shader != lit) mat.shader = lit;
      if (tex){ mat.SetTexture("_BaseMap", tex); mat.SetTexture("_MainTex", tex); }
      mat.SetColor("_BaseColor", Color.white); mat.SetFloat("_Smoothness", 0.58f); mat.SetFloat("_Metallic", 0f); mat.enableInstancing = true;
      EditorUtility.SetDirty(mat);
    }
    AssetDatabase.SaveAssets();
    Debug.Log($"[DW] m4 assets configured audio={audio} mat={(mat ? mat.shader.name : "missing")} atlas={(tex ? 1 : 0)}");
  }

  static UniversalRenderPipelineAsset EnsureURP(){
    var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Generated/URP_Asset.asset");
    if (!asset){ asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>(); AssetDatabase.CreateAsset(asset, "Assets/Generated/URP_Asset.asset"); }
    GraphicsSettings.defaultRenderPipeline = asset; QualitySettings.renderPipeline = asset; return asset;
  }

  public static void Build(){
    Rebuild();
    bool qa = System.Environment.GetEnvironmentVariable("DW_QA") == "1";
    var outDir = Path.GetFullPath(qa ? "Builds/WebGLQA" : "Builds/WebGL");
    if (Directory.Exists(outDir)) Directory.Delete(outDir, true); Directory.CreateDirectory(outDir);
    var opts = new BuildPlayerOptions{
      scenes = new[]{ "Assets/Scenes/Main.unity" }, target = BuildTarget.WebGL, locationPathName = outDir,
      options = BuildOptions.None, extraScriptingDefines = qa ? new[]{ "DW_QA" } : null
    };
    var report = BuildPipeline.BuildPlayer(opts);
    Debug.Log($"BUILD RESULT: {report.summary.result} size={report.summary.totalSize}");
    if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("Build failed");
    PatchIndex(Path.Combine(outDir, "index.html"), qa);
  }

  static void PatchIndex(string path, bool qa){
    if (!File.Exists(path)) return;
    var html = File.ReadAllText(path);
    if (!html.Contains("dw-boot")){
      html = html.Replace(".then((unityInstance) => {", ".then((unityInstance) => { window.unityInstance = unityInstance;");
      var boot = qa
        ? "<script id='dw-boot'>window.addEventListener('load',function(){var i=setInterval(function(){var u=window.unityInstance;if(!u)return;clearInterval(i);window.dwUnity={SendMessage:function(o,m,a){if(a===undefined)u.SendMessage(o,m);else u.SendMessage(o,m,a);}};console.log('[DW] ready qa=1');},200);});</script>"
        : "<script id='dw-boot'>window.addEventListener('load',function(){var i=setInterval(function(){if(!window.unityInstance)return;clearInterval(i);console.log('[DW] ready qa=0');},200);});</script>";
      html = html.Replace("</body>", boot + "\n</body>");
    }
    File.WriteAllText(path, html);
  }
}
