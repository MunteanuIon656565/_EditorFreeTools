#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using TMPro;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace _CodeTools
{
    public class TextLocalizationTool : EditorWindow
    {
        private bool applyToSelectionOnly = false;
        private bool includeAlreadyLocalized = false;

        [MenuItem("Tools/Localization/Generate Text GUIDs")]
        public static void ShowWindow()
        {
            var window = GetWindow<TextLocalizationTool>("Text Localization Tool");
            window.minSize = new Vector2(420, 180);
        }

        private void OnGUI()
        {
            GUILayout.Label("Text Localization GUID Generator", EditorStyles.boldLabel);
            GUILayout.Space(8);

            applyToSelectionOnly = EditorGUILayout.Toggle("Affect Only Selected Object(s)", applyToSelectionOnly);
            includeAlreadyLocalized = EditorGUILayout.Toggle("Include Already Localized Texts", includeAlreadyLocalized);
            GUILayout.Space(10);

            if (GUILayout.Button("Generate Text GUIDs", GUILayout.Height(35)))
            {
                GenerateTextGUIDs(applyToSelectionOnly, includeAlreadyLocalized);
            }
        }

        private static void GenerateTextGUIDs(bool onlySelection, bool includeExisting)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var sceneName = scene.name;

            List<TMP_Text> allTexts = new List<TMP_Text>();

            if (onlySelection)
            {
                var selectedObjects = Selection.gameObjects;
                foreach (var obj in selectedObjects)
                {
                    if (obj == null) continue;
                    var tmps = obj.GetComponentsInChildren<TMP_Text>(true);
                    allTexts.AddRange(tmps);
                }
            }
            else
            {
                allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>()
                    .Where(t => t != null
                                && t.gameObject.scene.IsValid()
                                && t.gameObject.scene.isLoaded
                                && t.gameObject.scene.name == sceneName)
                    .ToList();
            }

            // 🔹 Filtrăm în funcție de bifa includeExisting
            if (!includeExisting)
            {
                allTexts = allTexts
                    .Where(t => t != null && t.GetComponent<LocalizationTextSetter>() == null)
                    .ToList();
            }

            if (allTexts.Count == 0)
            {
                EditorUtility.DisplayDialog("No Text Found",
                    onlySelection
                        ? "No TextMeshPro components found in selected objects (based on filters)."
                        : "No TextMeshPro components found in the scene (based on filters).",
                    "OK");
                return;
            }

            StringBuilder result = new StringBuilder();
            Undo.RegisterCompleteObjectUndo(allTexts.Select(t => t.gameObject).ToArray(), "Add/Update LocalizationTextSetter");

            foreach (var tmp in allTexts)
            {
                if (tmp == null) continue;
                var go = tmp.gameObject;

                string parentRoot = go.transform.root.name;
                string parent = go.transform.parent ? go.transform.parent.name : "NoParent";
                string objName = go.name;
                string textContent = tmp.text?.Replace("\n", " ").Replace("\r", " ").Trim();

                string guid = $"[{sceneName}][{parentRoot}][{parent}][{objName}][{textContent}]";
                guid = SanitizeGUID(guid);

                result.AppendLine(guid);

                var setter = go.GetComponent<LocalizationTextSetter>();
                if (setter == null)
                    setter = Undo.AddComponent<LocalizationTextSetter>(go);

                var method = setter.GetType().GetMethod("UpdateTextID",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (method != null)
                    method.Invoke(setter, new object[] { guid, false });

                EditorUtility.SetDirty(setter);
            }

            EditorGUIUtility.systemCopyBuffer = result.ToString();

            Debug.Log(
                $"<b>Processed {allTexts.Count} TextMeshPro components</b> {(onlySelection ? "(Selection Mode)" : "(Scene Mode)")}\n" +
                (includeExisting ? "<color=yellow>Included already localized texts</color>\n" : "<color=green>New texts only</color>\n") +
                $"\n{result}");

            EditorUtility.DisplayDialog("Done",
                $"Processed {allTexts.Count} texts.\n" +
                (includeExisting ? "Included already localized texts.\n" : "Excluded already localized texts.\n") +
                "GUID list copied to clipboard ✅",
                "OK");
        }

        private static string SanitizeGUID(string input)
        {
            var invalids = System.IO.Path.GetInvalidFileNameChars();
            foreach (var c in invalids)
                input = input.Replace(c.ToString(), "_");

            return input.Replace(" ", "_");
        }
    }
}

#endif
