using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws a solid border around every active player's camera viewport using a
/// Screen Space Overlay Canvas so it composites correctly behind other Canvas UI.
///
/// sortingOrder = 1 — renders behind all game menus (PostGameUI uses 100, etc.)
/// but still on top of the 3D scene.
///
/// Strips are built once at Create() time from the live camera rects, so the
/// border must be created AFTER PreGame.SetViewPort() has assigned them.
///
/// Usage:
///   var border = CameraViewportBorder.Create();   // Battle.OnLoad (first run)
///   border.DestroyBorder();                       // PostGame on ChooseCharacters / Leave
/// </summary>
public class CameraViewportBorder : MonoBehaviour
{
    // ── Appearance ────────────────────────────────────────────────────────────
    public float borderThickness = 3f;
    public Color borderColor     = Color.black;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static CameraViewportBorder Create(float thickness = 3f, Color? color = null)
    {
        var go       = new GameObject("CameraViewportBorder");
        var instance = go.AddComponent<CameraViewportBorder>();
        instance.borderThickness = thickness;
        instance.borderColor     = color ?? Color.black;
        instance.Build();
        return instance;
    }

    public void DestroyBorder() => Destroy(gameObject);

    // ── Build ─────────────────────────────────────────────────────────────────

    void Build()
    {
        // Canvas at sorting order 1 — always behind game menus (PostGameUI = 100, etc.)
        var canvas          = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1;

        // ConstantPixelSize (default) → borderThickness is in real screen pixels.
        gameObject.AddComponent<CanvasScaler>();

        foreach (LocalPlayerManager player in LocalPlayerManager.ActivePlayers)
        {
            Camera cam = player.cameraControler?.camera;
            if (cam == null) continue;
            BuildViewportBorder(cam.rect);
        }
    }

    void BuildViewportBorder(Rect vp)
    {
        float x = vp.x;
        float y = vp.y;
        float w = vp.width;
        float h = vp.height;

        // Camera.rect uses bottom-left origin [0,1] — same convention as
        // RectTransform anchors on a Screen Space Overlay canvas, so no
        // coordinate flip is needed here (unlike the old IMGUI approach).

        // Four edge strips. Each strip is anchored to the relevant edge of
        // the viewport so it scales correctly if the screen resolution changes.

        // Bottom
        MakeStrip("Bottom",
            anchorMin: new Vector2(x,     y),
            anchorMax: new Vector2(x + w, y),
            size:      new Vector2(0f, borderThickness));

        // Top
        MakeStrip("Top",
            anchorMin: new Vector2(x,     y + h),
            anchorMax: new Vector2(x + w, y + h),
            size:      new Vector2(0f, borderThickness));

        // Left
        MakeStrip("Left",
            anchorMin: new Vector2(x, y),
            anchorMax: new Vector2(x, y + h),
            size:      new Vector2(borderThickness, 0f));

        // Right
        MakeStrip("Right",
            anchorMin: new Vector2(x + w, y),
            anchorMax: new Vector2(x + w, y + h),
            size:      new Vector2(borderThickness, 0f));
    }

    void MakeStrip(string stripName, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        var go = new GameObject(stripName, typeof(RectTransform));
        go.transform.SetParent(transform, false);

        var rt              = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = size;     // offsets anchor: gives thickness in the non-stretched axis

        go.AddComponent<Image>().color = borderColor;
    }
}
