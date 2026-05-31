using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates 512×512 PNG thumbnails for every character prefab listed in
/// GameManager.characterPrefabs using Unity's PreviewRenderUtility, then
/// imports them as Sprites and assigns them to GameManager.characterThumbnails.
///
/// Usage:  Tools → Generate Character Thumbnails
/// Requires the main game scene (containing GameManager) to be open.
/// </summary>
public static class ThumbnailGenerator
{
    const string OutputDir  = "Assets/Thumbnails/Characters";
    const int    ThumbSize  = 512;

    // ── Entry point ───────────────────────────────────────────────────────────

    [MenuItem("Tools/Generate Character Thumbnails")]
    public static void Generate()
    {
        var gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null)
        {
            EditorUtility.DisplayDialog("Thumbnail Generator",
                "No GameManager found in the open scene.\n\nOpen the main game scene first.",
                "OK");
            return;
        }

        if (gm.characterPrefabs == null || gm.characterPrefabs.Count == 0)
        {
            EditorUtility.DisplayDialog("Thumbnail Generator",
                "GameManager.characterPrefabs is empty.\n\nPopulate it first (Tools → Populate Character Prefabs).",
                "OK");
            return;
        }

        if (!Directory.Exists(OutputDir))
            Directory.CreateDirectory(OutputDir);

        var sprites   = new List<Sprite>();
        int succeeded = 0;

        for (int i = 0; i < gm.characterPrefabs.Count; i++)
        {
            var prefab = gm.characterPrefabs[i];
            if (prefab == null)
            {
                Debug.LogWarning($"[ThumbnailGen] Null prefab at index {i} — skipped.");
                sprites.Add(null);
                continue;
            }

            EditorUtility.DisplayProgressBar(
                "Generating Thumbnails",
                $"Rendering {prefab.name}  ({i + 1} / {gm.characterPrefabs.Count})",
                (float)i / gm.characterPrefabs.Count);

            try
            {
                Texture2D tex    = RenderPrefabThumbnail(prefab);
                string    path   = $"{OutputDir}/{prefab.name}_thumb.png";
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                ConfigureAsSprite(path);

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                sprites.Add(sprite);
                succeeded++;
                Debug.Log($"[ThumbnailGen] ✓ {prefab.name}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ThumbnailGen] Failed on {prefab.name}: {ex.Message}");
                sprites.Add(null);
            }
        }

        EditorUtility.ClearProgressBar();

        // Assign sprites to GameManager ----------------------------------------
        var so   = new SerializedObject(gm);
        var prop = so.FindProperty("characterThumbnails");
        prop.arraySize = sprites.Count;
        for (int i = 0; i < sprites.Count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(gm);
        AssetDatabase.SaveAssets();

        Debug.Log($"[ThumbnailGen] Done — {succeeded}/{gm.characterPrefabs.Count} thumbnails generated and assigned.");
        EditorUtility.DisplayDialog("Thumbnail Generator",
            $"Complete!\n\n{succeeded} of {gm.characterPrefabs.Count} thumbnails rendered and assigned to GameManager.",
            "OK");
    }

    // ── Rendering ─────────────────────────────────────────────────────────────

    static Texture2D RenderPrefabThumbnail(GameObject prefab)
    {
        var prev = new PreviewRenderUtility();
        try
        {
            // ── Camera ────────────────────────────────────────────────────────
            prev.camera.clearFlags      = CameraClearFlags.SolidColor;
            prev.camera.backgroundColor = new Color(0.13f, 0.13f, 0.18f, 1f);
            prev.camera.farClipPlane    = 1000f;
            prev.camera.nearClipPlane   = 0.01f;
            prev.camera.fieldOfView     = 38f;  // moderate FOV, low distortion

            // ── Three-point lighting ──────────────────────────────────────────
            // Key light: warm, high and to the left-front
            prev.lights[0].intensity          = 1.6f;
            prev.lights[0].color              = new Color(1.00f, 0.96f, 0.88f);
            prev.lights[0].transform.rotation = Quaternion.Euler(45f, -50f, 0f);

            // Fill light: cool, low and to the right
            prev.lights[1].intensity          = 0.80f;
            prev.lights[1].color              = new Color(0.80f, 0.88f, 1.00f);
            prev.lights[1].transform.rotation = Quaternion.Euler(15f, 130f, 0f);

            // ── Instantiate ───────────────────────────────────────────────────
            var go = prev.InstantiatePrefabInScene(prefab);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            // ── Frame the whole model ─────────────────────────────────────────
            // Calculate tight bounds from every renderer in the hierarchy
            var bounds = CalculateBounds(go);

            // FOV-based distance: guarantees the largest axis fits in frame
            // with 25 % breathing room on each side.
            float halfFovRad  = prev.camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float halfExtent  = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            float dist        = (halfExtent / Mathf.Tan(halfFovRad)) * 1.25f;
            dist              = Mathf.Max(dist, 0.5f);

            // Mostly front-facing with a gentle upward look-down
            Vector3 camDir = new Vector3(0.10f, 0.22f, 1f).normalized;
            prev.camera.transform.position = bounds.center + camDir * dist;
            prev.camera.transform.LookAt(bounds.center);

            // ── Render ────────────────────────────────────────────────────────
            var rect = new Rect(0, 0, ThumbSize, ThumbSize);
            prev.BeginPreview(rect, GUIStyle.none);
            prev.camera.Render();
            Texture renderTex = prev.EndPreview();

            // Copy to a readable Texture2D via a temporary RenderTexture
            var rt = RenderTexture.GetTemporary(ThumbSize, ThumbSize, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(renderTex, rt);
            RenderTexture.active = rt;
            var tex2d = new Texture2D(ThumbSize, ThumbSize, TextureFormat.RGBA32, false);
            tex2d.ReadPixels(new Rect(0, 0, ThumbSize, ThumbSize), 0, 0);
            tex2d.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return tex2d;
        }
        finally
        {
            prev.Cleanup();
        }
    }

    static Bounds CalculateBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(go.transform.position, Vector3.one);

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    // ── Asset import ──────────────────────────────────────────────────────────

    static void ConfigureAsSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.textureType      = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled    = false;
        importer.filterMode       = FilterMode.Bilinear;
        importer.maxTextureSize   = 512;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType    = SpriteMeshType.FullRect;
        settings.spriteAlignment   = (int)SpriteAlignment.Center;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
    }
}
