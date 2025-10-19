#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEditor.Presets;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Plugins._EditorFreeTools.Editor
{
    public class FbxEditor : UnityEditor.Editor
    {
        private static string originalFbxPath;
        private static string tempPrefabPath;
        private static string tempPresetPath;
        private static string tempPrefabName;

        [MenuItem("CONTEXT/ModelImporter/Edit FBX Mode")]
        public static void OpenFbxEditMode(MenuCommand command)
        {
            originalFbxPath = AssetDatabase.GetAssetPath(command.context);

            if (!originalFbxPath.ToLower().EndsWith(".fbx"))
            {
                Debug.LogError("Fișierul selectat nu este un FBX.");
                return;
            }

            GameObject fbxObject = AssetDatabase.LoadAssetAtPath<GameObject>(originalFbxPath);
            if (fbxObject == null)
            {
                Debug.LogError("Nu s-a putut încărca FBX-ul.");
                return;
            }

            // Creează prefab temporar
            tempPrefabName = Path.GetFileNameWithoutExtension(originalFbxPath) + "_TempPrefab";
            tempPrefabPath = Path.Combine(Path.GetDirectoryName(originalFbxPath), tempPrefabName + ".prefab")
                .Replace("\\", "/");

            // Șterge prefab-ul temporar existent dacă există
            if (AssetDatabase.LoadAssetAtPath<GameObject>(tempPrefabPath) != null)
            {
                AssetDatabase.DeleteAsset(tempPrefabPath);
                AssetDatabase.Refresh();
            }

            // Instanțiază doar root-ul FBX-ului
            GameObject tempInstance = Object.Instantiate(fbxObject);
            tempInstance.name = fbxObject.name + "_TempPrefab";

            // Resetează transformul pentru a evita duplicarea poziției
            tempInstance.transform.position = Vector3.zero;
            tempInstance.transform.rotation = Quaternion.identity;
            tempInstance.transform.localScale = Vector3.one;

            // Salvează prefab-ul temporar
            PrefabUtility.SaveAsPrefabAsset(tempInstance, tempPrefabPath);
            Object.DestroyImmediate(tempInstance);
            AssetDatabase.Refresh();


            // Deschide Prefab Mode
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(tempPrefabPath));
            Debug.LogWarning("⚠️ Nu uita să dezactivezi Auto Save daca ai careva probleme înainte de a edita prefab-ul temporar!");
        }

        private static void OnPrefabStageClosing(PrefabStage stage)
        {
            if (stage == null || stage.assetPath != tempPrefabPath)
                return;

            PrefabStage.prefabStageClosing -= OnPrefabStageClosing;

            GameObject editedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(tempPrefabPath);
            if (editedPrefab == null)
            {
                Debug.LogError("Prefab-ul temporar nu a putut fi încărcat la închidere.");
                return;
            }

            // 1️⃣ Salvăm preset temporar al FBX-ului original
            ModelImporter importer = AssetImporter.GetAtPath(originalFbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError("Nu s-a găsit ModelImporter pentru FBX-ul original.");
                return;
            }

            tempPresetPath = "Assets/TempFbxPreset.preset";
            Preset preset = new Preset(importer);
            AssetDatabase.CreateAsset(preset, tempPresetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        
            // 2️⃣ Exportăm prefab-ul modificat în FBX
            try
            {
                ModelExporter.ExportObject(originalFbxPath, editedPrefab);
                Debug.Log($"✅ FBX-ul '{Path.GetFileName(originalFbxPath)}' a fost exportat cu modificările.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Eroare la exportul FBX: {e.Message}");
            }

            // 3️⃣ Re-importăm FBX-ul și aplicăm preset-ul pentru setările originale
            AssetDatabase.ImportAsset(originalFbxPath, ImportAssetOptions.ForceUpdate);
            Preset loadedPreset = AssetDatabase.LoadAssetAtPath<Preset>(tempPresetPath);
            if (loadedPreset != null)
            {
                loadedPreset.ApplyTo(AssetImporter.GetAtPath(originalFbxPath));
                AssetDatabase.ImportAsset(originalFbxPath, ImportAssetOptions.ForceUpdate);
            }

            // 4️⃣ Curățăm fișierele temporare
            AssetDatabase.DeleteAsset(tempPrefabPath);
            AssetDatabase.DeleteAsset(tempPresetPath);
            AssetDatabase.Refresh();

            Debug.Log($"✅ FBX-ul '{Path.GetFileName(originalFbxPath)}' a fost actualizat și toate setările originale au fost restaurate.");
        }
    }
}

#endif
