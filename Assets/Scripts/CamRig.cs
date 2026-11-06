using UnityEngine;
using UnityEngine.InputSystem;

namespace DinoWorlds {
  /// <summary>Fixed elevated camera: pan and zoom only (no orbit/yaw).</summary>
  public class CamRig : MonoBehaviour {
    public Camera cam; public float height = 18f, tilt = 55f, zoom = 22f, zoomMin = 12f, zoomMax = 36f;
    public Vector2 pan, panMin = new Vector2(2, 2), panMax = new Vector2(IsleLayout.W - 2, IsleLayout.H - 2);
    public float panSpeed = 14f, zoomSpeed = 6f;
    void Awake(){ if (!cam) cam = Camera.main; Apply(); }
    public void Apply(){
      pan.x = Mathf.Clamp(pan.x, panMin.x, panMax.x); pan.y = Mathf.Clamp(pan.y, panMin.y, panMax.y);
      zoom = Mathf.Clamp(zoom, zoomMin, zoomMax);
      var look = new Vector3(pan.x, 0, pan.y);
      var rot = Quaternion.Euler(tilt, 0f, 0f); // fixed yaw = 0
      cam.transform.rotation = rot;
      cam.transform.position = look - rot * Vector3.forward * zoom + Vector3.up * (height * 0.05f);
      cam.orthographic = false; cam.fieldOfView = 35f;
    }
    public void Tick(float dt){
      var kb = Keyboard.current; var mouse = Mouse.current;
      Vector2 d = Vector2.zero;
      if (kb != null){ if (kb.wKey.isPressed || kb.upArrowKey.isPressed) d.y += 1; if (kb.sKey.isPressed || kb.downArrowKey.isPressed) d.y -= 1;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) d.x -= 1; if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) d.x += 1; }
      if (mouse != null && (mouse.rightButton.isPressed || mouse.middleButton.isPressed)){
        var delta = mouse.delta.ReadValue(); d += new Vector2(-delta.x, -delta.y) * 0.04f;
      }
      if (d.sqrMagnitude > 0) pan += d.normalized * panSpeed * (zoom / 22f) * dt;
      if (mouse != null){ float scroll = mouse.scroll.ReadValue().y; if (Mathf.Abs(scroll) > 0.01f) zoom -= Mathf.Sign(scroll) * zoomSpeed; }
      Apply();
    }
    public Ray ScreenRay(){ return cam.ScreenPointToRay(Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero); }
  }
}
