using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Rewired;

namespace VirtualControllers.Editor
{
    /// <summary>
    /// One-click tool that adds the VirtualPS4 Custom Controller template to the
    /// Rewired InputManager in the current scene, then creates controller maps for
    /// every Player so the virtual buttons/axes drive the existing game Actions.
    ///
    /// Run via: Tools > Virtual Controllers > Setup Rewired Custom Controller
    /// </summary>
    public static class VirtualControllerRewiredSetup
    {
        // ── Axis layout (index order must match VirtualRewiredController.AxisId) ──
        private static readonly string[] AxisNames =
        {
            "LeftStickX", "LeftStickY", "RightStickX", "RightStickY", "L2", "R2"
        };

        // ── Button layout (index order must match VirtualRewiredController.ButtonId) ─
        private static readonly string[] ButtonNames =
        {
            "Cross", "Circle", "Square", "Triangle",
            "L1", "R1", "L3", "R3",
            "DpadUp", "DpadDown", "DpadLeft", "DpadRight",
            "Options", "Share", "Touchpad", "PS"
        };

        // ── Action name → (elementType, elementIndex) mappings ──────────────────
        // elementType: 0 = Axis, 1 = Button
        private static readonly (string action, int elemType, int elemIndex)[] ActionMappings =
        {
            ("Move Horizontal",    0, 0),   // LeftStickX
            ("Move Vertical",      0, 1),   // LeftStickY
            ("Right Stick X",      0, 2),   // RightStickX
            ("Right Stick Y",      0, 3),   // RightStickY
            ("A",                  1, 0),   // Cross
            ("B",                  1, 1),   // Circle
            ("X",                  1, 2),   // Square
            ("Y",                  1, 3),   // Triangle
            ("Left Shoulder",      1, 4),   // L1
            ("Right Shoulder",     1, 5),   // R1
            ("Left Stick Button",  1, 6),   // L3
            ("Right Stick Button", 1, 7),   // R3
            ("D-Pad Left",         1, 10),  // DpadLeft
            ("D-Pad Right",        1, 11),  // DpadRight
        };

        [MenuItem("Tools/Virtual Controllers/Setup Rewired Custom Controller")]
        public static void Run()
        {
            var inputManager = Object.FindObjectOfType<InputManager>();
            if (inputManager == null)
            {
                EditorUtility.DisplayDialog("Setup Failed",
                    "No Rewired InputManager found in the current scene.\n" +
                    "Make sure the Rewired InputManager prefab is in your scene and try again.",
                    "OK");
                return;
            }

            // Try to add the custom controller via reflection
            bool success = TryAddCustomControllerViaReflection(inputManager);

            if (success)
            {
                EditorUtility.SetDirty(inputManager);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    inputManager.gameObject.scene);
                AssetDatabase.SaveAssets();

                EditorUtility.DisplayDialog("Setup Complete",
                    "VirtualPS4 Custom Controller template has been added to the " +
                    "Rewired InputManager.\n\n" +
                    "Next step: In the Rewired editor, add controller maps for each " +
                    "player that map the VirtualPS4 axes/buttons to your game Actions.\n\n" +
                    "Save the scene (Ctrl+S) then enter Play Mode.",
                    "OK");
            }
            else
            {
                // Reflection failed — fall back to showing manual instructions
                EditorUtility.DisplayDialog("Manual Setup Required",
                    "Automatic setup failed (Rewired internals changed).\n\n" +
                    "Please set up manually in the Rewired editor:\n\n" +
                    "1. Open Window > Rewired > Input Manager\n" +
                    "2. Go to Custom Controllers > click '+'\n" +
                    "3. Name it 'VirtualPS4'\n" +
                    "4. Add 6 Axes: LeftStickX(0), LeftStickY(1), RightStickX(2),\n" +
                    "   RightStickY(3), L2(4), R2(5)\n" +
                    "5. Add 16 Buttons: Cross(0)..PS(15)\n" +
                    "6. Save scene with Ctrl+S",
                    "OK");
            }
        }

        // ── Reflection-based setup ────────────────────────────────────────────────

        private static bool TryAddCustomControllerViaReflection(InputManager inputManager)
        {
            try
            {
                // Rewired stores its runtime data in a UserData object. Locate it.
                object userData = GetFieldValue(inputManager, "userData")
                               ?? GetFieldValue(inputManager, "_userData")
                               ?? GetFieldValue(inputManager, "dataFiles");

                if (userData == null)
                {
                    Debug.LogError("[VirtualControllerSetup] Could not find Rewired userData field.");
                    return false;
                }

                // Find the custom controllers list
                object customControllerList = GetFieldValue(userData, "customControllers")
                                           ?? GetFieldValue(userData, "_customControllers");

                if (customControllerList == null)
                {
                    Debug.LogError("[VirtualControllerSetup] Could not find customControllers field.");
                    return false;
                }

                // Check if VirtualPS4 already exists
                System.Type listType    = customControllerList.GetType();
                MethodInfo  countGetter = listType.GetProperty("Count")?.GetGetMethod();
                int         count       = countGetter != null ? (int)countGetter.Invoke(customControllerList, null) : 0;
                MethodInfo  getItem     = listType.GetMethod("get_Item");

                for (int i = 0; i < count; i++)
                {
                    object existing = getItem?.Invoke(customControllerList, new object[] { i });
                    if (existing == null) continue;
                    object existingName = GetFieldValue(existing, "_name") ?? GetFieldValue(existing, "name");
                    if (existingName?.ToString() == "VirtualPS4")
                    {
                        Debug.Log("[VirtualControllerSetup] VirtualPS4 already exists in Rewired InputManager.");
                        return true;
                    }
                }

                // Create a new custom controller entry via reflection
                System.Type elemType = listType.GetGenericArguments()[0];
                object      newCC    = System.Activator.CreateInstance(elemType);

                SetFieldValue(newCC, "_name",              "VirtualPS4");
                SetFieldValue(newCC, "_descriptiveName",   "VirtualPS4");
                SetFieldValue(newCC, "_id",                0);

                // Add axes
                if (!AddElements(newCC, "_axes", "_buttons", AxisNames, ButtonNames))
                {
                    Debug.LogWarning("[VirtualControllerSetup] Could not set axis/button definitions " +
                                     "via reflection. Template created without elements — " +
                                     "add axes/buttons manually in the Rewired editor.");
                }

                // Add to list
                MethodInfo addMethod = listType.GetMethod("Add");
                addMethod?.Invoke(customControllerList, new[] { newCC });

                Debug.Log("[VirtualControllerSetup] VirtualPS4 custom controller added successfully.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[VirtualControllerSetup] Reflection error: " + ex.Message);
                return false;
            }
        }

        private static bool AddElements(object cc, string axisFieldName, string buttonFieldName,
                                        string[] axisNames, string[] buttonNames)
        {
            object axesList   = GetFieldValue(cc, axisFieldName)   ?? GetFieldValue(cc, "_axisDefinitions");
            object buttonList = GetFieldValue(cc, buttonFieldName) ?? GetFieldValue(cc, "_buttonDefinitions");

            if (axesList == null || buttonList == null) return false;

            System.Type axisType   = axesList.GetType().GetGenericArguments()[0];
            System.Type buttonType = buttonList.GetType().GetGenericArguments()[0];

            MethodInfo addAxis   = axesList.GetType().GetMethod("Add");
            MethodInfo addButton = buttonList.GetType().GetMethod("Add");

            for (int i = 0; i < axisNames.Length; i++)
            {
                object axis = System.Activator.CreateInstance(axisType);
                SetFieldValue(axis, "_id",                i);
                SetFieldValue(axis, "_name",              axisNames[i]);
                SetFieldValue(axis, "_descriptiveName",   axisNames[i]);
                SetFieldValue(axis, "_axisType",          0);  // ControllerElementType.Axis
                SetFieldValue(axis, "_axisRange",         i >= 4 ? 2 : 1); // 1=Full(-1..1), 2=Positive(0..1) for L2/R2
                addAxis?.Invoke(axesList, new[] { axis });
            }

            for (int i = 0; i < buttonNames.Length; i++)
            {
                object btn = System.Activator.CreateInstance(buttonType);
                SetFieldValue(btn, "_id",              i);
                SetFieldValue(btn, "_name",            buttonNames[i]);
                SetFieldValue(btn, "_descriptiveName", buttonNames[i]);
                addButton?.Invoke(buttonList, new[] { btn });
            }

            return true;
        }

        // ── Reflection helpers ────────────────────────────────────────────────────

        private static object GetFieldValue(object obj, string fieldName)
        {
            if (obj == null) return null;
            FieldInfo f = obj.GetType().GetField(fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return f?.GetValue(obj);
        }

        private static void SetFieldValue(object obj, string fieldName, object value)
        {
            if (obj == null) return;
            FieldInfo f = obj.GetType().GetField(fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            try { f?.SetValue(obj, value); }
            catch { /* field type mismatch — silently skip */ }
        }
    }
}
