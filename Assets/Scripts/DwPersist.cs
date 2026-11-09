using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace DinoWorlds {
  /// <summary>Save durability on WebGL: PlayerPrefs live in an IDBFS file that Emscripten only syncs
  /// to IndexedDB asynchronously. Flush() forces FS.syncfs right after every save; the jslib also
  /// flushes on pagehide and visibilitychange(hidden) so a fast refresh/tab close keeps the save.</summary>
  public static class DwPersist {
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void DwSyncFs();
    [DllImport("__Internal")] static extern void DwInstallFlushHooks();
    [DllImport("__Internal")] static extern int DwSyncCount();
#endif
    static bool hooked;
    public static void Install(){
      if (hooked) return; hooked = true;
#if UNITY_WEBGL && !UNITY_EDITOR
      DwInstallFlushHooks();
#endif
      Debug.Log("[DW] persist hooks installed");
    }
    public static void Flush(){
#if UNITY_WEBGL && !UNITY_EDITOR
      DwSyncFs();
#endif
    }
    public static int Count(){
#if UNITY_WEBGL && !UNITY_EDITOR
      return DwSyncCount();
#else
      return 0;
#endif
    }
  }
}
