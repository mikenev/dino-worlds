// Dino Worlds M4: make PlayerPrefs saves durable by syncing Emscripten's IDBFS to IndexedDB
// immediately after each save and whenever the page is hidden or unloaded.
var DwPersistLib = {
  $DwPersistState: { busy: false, again: false, count: 0, hooked: false },

  $DwPersistRun__deps: ['$DwPersistState'],
  $DwPersistRun: function () {
    var st = DwPersistState;
    if (typeof FS === 'undefined' || !FS.syncfs) return;
    if (st.busy) { st.again = true; return; }
    st.busy = true;
    try {
      FS.syncfs(false, function (err) {
        st.busy = false; st.count++;
        if (err) console.warn('[DW] syncfs error', err);
        if (st.again) { st.again = false; DwPersistRun(); }
      });
    } catch (e) { st.busy = false; console.warn('[DW] syncfs threw', e); }
  },

  DwSyncFs__deps: ['$DwPersistRun'],
  DwSyncFs: function () { DwPersistRun(); },

  DwSyncCount__deps: ['$DwPersistState'],
  DwSyncCount: function () { return DwPersistState.count; },

  DwInstallFlushHooks__deps: ['$DwPersistRun', '$DwPersistState'],
  DwInstallFlushHooks: function () {
    if (DwPersistState.hooked) return; DwPersistState.hooked = true;
    var flush = function () { DwPersistRun(); };
    window.addEventListener('pagehide', flush);
    window.addEventListener('beforeunload', flush);
    document.addEventListener('visibilitychange', function () { if (document.visibilityState === 'hidden') flush(); });
    window.dwPersist = { flush: flush, count: function () { return DwPersistState.count; } };
  }
};
mergeInto(LibraryManager.library, DwPersistLib);
