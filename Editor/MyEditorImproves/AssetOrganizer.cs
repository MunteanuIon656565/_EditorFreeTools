#if UNITY_EDITOR

using System.IO;
using UnityEditor;

namespace Plugins._EditorFreeTools.Editor
{
    public class AssetOrganizer
    {
        private const string menuPath = "Assets/Fbx & Prefab Organizer/";

        [MenuItem(menuPath + "Organize Dependencies", true)]
        [MenuItem(menuPath + "Organize Dependencies into New Folder", true)]
        private static bool ValidateSelection()
        {
            if (Selection.objects.Length != 1)
                return false;

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!File.Exists(path))
                return false;

            string extension = Path.GetExtension(path).ToLower();
            if (extension == ".fbx" || extension == ".prefab")
            {
                if (extension == ".prefab")
                {
                    PrefabType prefabType = PrefabUtility.GetPrefabType(Selection.activeObject);
                    return prefabType == PrefabType.Prefab || prefabType == PrefabType.ModelPrefab;
                }
                return true;
            }
            return false;
        }

        [MenuItem(menuPath + "Organize Dependencies", false, 9999)]
        private static void OrganizeDependencies()
        {
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            string assetDirectory = Path.GetDirectoryName(assetPath);

            ProcessAsset(assetPath, assetDirectory);

            DeleteEmptyFoldersLoop(assetDirectory);
        }

        [MenuItem(menuPath + "Organize Dependencies into New Folder", false, 10000)]
        private static void OrganizeDependenciesIntoNewFolder()
        {
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            string assetDirectory = Path.GetDirectoryName(assetPath);
            string assetName = Path.GetFileNameWithoutExtension(assetPath);
            string newFolderPath = Path.Combine(assetDirectory, assetName);

            if (!AssetDatabase.IsValidFolder(newFolderPath))
            {
                AssetDatabase.CreateFolder(assetDirectory, assetName);
            }

            string newAssetPath = Path.Combine(newFolderPath, Path.GetFileName(assetPath));
            if (assetPath != newAssetPath)
            {
                AssetDatabase.MoveAsset(assetPath, newAssetPath);
            }

            ProcessAsset(newAssetPath, newFolderPath);

            DeleteEmptyFoldersLoop(newFolderPath);

            AssetDatabase.Refresh();
        }

        private static void ProcessAsset(string assetPath, string targetDirectory)
        {
            CreateSubfolder(targetDirectory, "Textures");
            CreateSubfolder(targetDirectory, "Materials");
            CreateSubfolder(targetDirectory, "Meshes");

            string[] dependencies = AssetDatabase.GetDependencies(assetPath, true);

            foreach (string dependencyPath in dependencies)
            {
                if (dependencyPath.EndsWith(".cs") || dependencyPath == assetPath)
                    continue;

                string dependencyExtension = Path.GetExtension(dependencyPath).ToLower();
                string targetSubfolder = "";

                switch (dependencyExtension)
                {
                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                    case ".tga":
                    case ".psd":
                        targetSubfolder = "Textures";
                        break;
                    case ".mat":
                        targetSubfolder = "Materials";
                        break;
                    case ".fbx":
                    case ".obj":
                        // Nu muta FBX-ul principal în Meshes
                        if (dependencyPath != assetPath)
                            targetSubfolder = "Meshes";
                        break;
                }

                if (!string.IsNullOrEmpty(targetSubfolder))
                {
                    string targetPath = Path.Combine(targetDirectory, targetSubfolder, Path.GetFileName(dependencyPath));
                    if (dependencyPath != targetPath)
                    {
                        AssetDatabase.MoveAsset(dependencyPath, targetPath);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateSubfolder(string parentDirectory, string subfolderName)
        {
            string path = Path.Combine(parentDirectory, subfolderName);
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parentDirectory, subfolderName);
            }
        }

        private static void DeleteEmptyFoldersLoop(string startDirectory)
        {
            bool foundEmpty;
            do
            {
                foundEmpty = DeleteEmptyFolders(startDirectory);
            } while (foundEmpty);
        }

        private static bool DeleteEmptyFolders(string startDirectory)
        {
            bool anyDeleted = false;

            // Folosește API-ul Unity pentru subfoldere
            string[] subfolders = AssetDatabase.GetSubFolders(startDirectory);
            foreach (string folder in subfolders)
            {
                // Recursiv
                anyDeleted |= DeleteEmptyFolders(folder);

                // Verifică dacă folderul este gol (fără asset-uri)
                string[] assets = AssetDatabase.FindAssets("", new[] { folder });
                if (assets.Length == 0)
                {
                    if (AssetDatabase.IsValidFolder(folder))
                    {
                        AssetDatabase.DeleteAsset(folder);
                        anyDeleted = true;
                    }
                }
            }

            return anyDeleted;
        }

    }
}
#endif
