using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Populate Character Prefabs
/// Finds all 16 character prefabs in Assets/Prefabs and writes them into
/// GameManager.characterPrefabs in colour-wheel order.
/// Run this once after adding new prefabs, then save the scene.
/// </summary>
public static class CharacterPrefabPopulator
{
    // Desired order in the character-select carousel.
    // Matches the 16 colours: 3 original + 13 new.
    private static readonly string[] CharacterOrder =
    {
        "Cyan", "Oranage", "Purple",
        "Red", "Blue", "Green", "Yellow", "Lime",
        "Teal", "Indigo", "Navy", "Crimson",
        "Coral", "Violet", "White", "Jade"
    };

    [MenuItem("Tools/Populate Character Prefabs")]
    private static void Populate()
    {
        // Build a name → prefab lookup from everything in Assets/Prefabs
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
        var byName = new Dictionary<string, GameObject>();
        foreach (string guid in guids)
        {
            string path   = AssetDatabase.GUIDToAssetPath(guid);
            var    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                byName[prefab.name] = prefab;
        }

        // Locate the GameManager in the active scene
#if UNITY_2022_2_OR_NEWER
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
#else
        GameManager gm = Object.FindObjectOfType<GameManager>();
#endif
        if (gm == null)
        {
            Debug.LogError("[CharacterPrefabPopulator] GameManager not found in the active scene. " +
                           "Make sure the correct scene is open before running this tool.");
            return;
        }

        var so   = new SerializedObject(gm);
        var list = so.FindProperty("characterPrefabs");
        list.ClearArray();

        int added   = 0;
        int missing = 0;
        foreach (string name in CharacterOrder)
        {
            if (byName.TryGetValue(name, out GameObject prefab))
            {
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = prefab;
                added++;
            }
            else
            {
                Debug.LogWarning($"[CharacterPrefabPopulator] Prefab '{name}' not found in Assets/Prefabs — skipped.");
                missing++;
            }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(gm);

        Debug.Log($"[CharacterPrefabPopulator] Done. {added} prefabs assigned to GameManager.characterPrefabs" +
                  (missing > 0 ? $", {missing} missing." : "."));
    }
}
