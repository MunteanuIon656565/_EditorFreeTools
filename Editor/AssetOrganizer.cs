#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Un set de unelte de editor pentru a organiza asset-urile și dependențele lor în Project Window.
/// </summary>
public class AssetOrganizer
{
    private const string MENU_PATH = "Assets/Asset Organizer/";

    // --- Configurare ---
    private static readonly string TEXTURES_FOLDER = "Textures";
    private static readonly string MATERIALS_FOLDER = "Materials";
    private static readonly string MESHES_FOLDER = "Meshes";
    private static readonly string ANIMATIONS_FOLDER = "Animations";

    private static readonly string[] TEXTURE_EXTENSIONS = { ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tif", ".bmp" };
    private static readonly string[] MATERIAL_EXTENSIONS = { ".mat" };
    private static readonly string[] MESH_EXTENSIONS = { ".fbx", ".obj", ".dae" };
    private static readonly string[] ANIMATION_EXTENSIONS = { ".anim" };
    // --- Sfârșit Configurare ---

    #region Validation
    /// <summary>
    /// Validează dacă opțiunile din meniu ar trebui să fie active.
    /// Activ doar pentru un singur fișier selectat de tip .fbx sau .prefab.
    /// </summary>
    [MenuItem(MENU_PATH + "Organize Dependencies", true)]
    [MenuItem(MENU_PATH + "Organize Dependencies into New Folder", true)]
    private static bool ValidateSelection()
    {
        if (Selection.objects.Length != 1) return false;

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;

        string extension = Path.GetExtension(path).ToLowerInvariant();
        return extension == ".fbx" || extension == ".prefab";
    }
    #endregion

    #region Menu Items
    /// <summary>
    /// Organizează dependențele în subfoldere în locația curentă a asset-ului.
    /// </summary>
    [MenuItem(MENU_PATH + "Organize Dependencies", false, 999)]
    private static void OrganizeDependencies()
    {
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        string assetDirectory = Path.GetDirectoryName(assetPath);

        ProcessAsset(assetPath, assetDirectory);
        CleanupEmptyFolders(assetDirectory);
        
        EditorUtility.DisplayDialog("Success", $"Dependencies for {Path.GetFileName(assetPath)} have been organized.", "OK");
    }

    /// <summary>
    /// Creează un folder nou cu numele asset-ului și mută atât asset-ul, cât și dependențele organizate în el.
    /// </summary>
    [MenuItem(MENU_PATH + "Organize Dependencies into New Folder", false, 1000)]
    private static void OrganizeDependenciesIntoNewFolder()
    {
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        string assetDirectory = Path.GetDirectoryName(assetPath);
        string assetName = Path.GetFileNameWithoutExtension(assetPath);
        string newFolderPath = Path.Combine(assetDirectory, assetName);

        // Verifică dacă există deja un fișier cu același nume
        if (File.Exists(newFolderPath))
        {
            EditorUtility.DisplayDialog("Error", $"Cannot create folder '{assetName}' because a file with the same name already exists in this location.", "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder(newFolderPath))
        {
            AssetDatabase.CreateFolder(assetDirectory, assetName);
        }

        string newAssetPath = Path.Combine(newFolderPath, Path.GetFileName(assetPath));
        string moveError = AssetDatabase.MoveAsset(assetPath, newAssetPath);
        if (!string.IsNullOrEmpty(moveError))
        {
            Debug.LogError($"Failed to move asset: {assetPath}. Error: {moveError}");
            return;
        }

        ProcessAsset(newAssetPath, newFolderPath);
        CleanupEmptyFolders(assetDirectory);
        
        EditorUtility.DisplayDialog("Success", $"Asset {assetName} and its dependencies have been organized into a new folder.", "OK");
    }
    #endregion

    #region Core Logic
    /// <summary>
    /// Procesează un asset dat: creează subfoldere și mută toate dependențele în ele.
    /// </summary>
    /// <param name="assetPath">Calea către asset-ul principal (ex: .fbx, .prefab).</param>
    /// <param name="targetDirectory">Directorul rădăcină unde vor fi create subfolderele.</param>
    private static void ProcessAsset(string assetPath, string targetDirectory)
    {
        // Optimizare masivă: împiedică reimportarea la fiecare mutare de fișier
        AssetDatabase.StartAssetEditing();
        try
        {
            var dependencies = AssetDatabase.GetDependencies(assetPath, true)
                .Where(p => p != assetPath && !p.EndsWith(".cs"))
                .ToList();

            if (dependencies.Count == 0) return;

            // Creează folderele doar dacă sunt necesare
            CreateRequiredSubfolders(targetDirectory, dependencies);
            
            for (int i = 0; i < dependencies.Count; i++)
            {
                string dependencyPath = dependencies[i];
                EditorUtility.DisplayProgressBar("Organizing Assets...", $"Moving {Path.GetFileName(dependencyPath)}", (float)i / dependencies.Count);

                string targetSubfolder = GetTargetSubfolder(dependencyPath);
                if (string.IsNullOrEmpty(targetSubfolder)) continue;

                string destinationFolder = Path.Combine(targetDirectory, targetSubfolder);
                string destinationPath = Path.Combine(destinationFolder, Path.GetFileName(dependencyPath));

                // Mută doar dacă nu este deja la destinație
                if (Path.GetDirectoryName(dependencyPath).Replace('\\', '/') != destinationFolder.Replace('\\', '/'))
                {
                    string moveError = AssetDatabase.MoveAsset(dependencyPath, destinationPath);
                    if (!string.IsNullOrEmpty(moveError))
                    {
                        Debug.LogError($"Failed to move dependency: {dependencyPath}. Error: {moveError}");
                    }
                }
            }
        }
        finally
        {
            // Asigură-te că aceste comenzi se execută chiar dacă apare o eroare
            EditorUtility.ClearProgressBar();
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// Determină numele subfolderului țintă pe baza extensiei fișierului.
    /// </summary>
    /// <param name="path">Calea fișierului de evaluat.</param>
    /// <returns>Numele folderului sau un string gol dacă nu se potrivește niciunui criteriu.</returns>
    private static string GetTargetSubfolder(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        if (TEXTURE_EXTENSIONS.Contains(extension)) return TEXTURES_FOLDER;
        if (MATERIAL_EXTENSIONS.Contains(extension)) return MATERIALS_FOLDER;
        if (MESH_EXTENSIONS.Contains(extension)) return MESHES_FOLDER;
        if (ANIMATION_EXTENSIONS.Contains(extension)) return ANIMATIONS_FOLDER;
        return string.Empty;
    }

    /// <summary>
    /// Creează subfolderele necesare pe baza dependențelor găsite.
    /// </summary>
    private static void CreateRequiredSubfolders(string parentDirectory, List<string> dependencies)
    {
        var requiredFolders = new HashSet<string>();
        foreach (var dep in dependencies)
        {
            string folder = GetTargetSubfolder(dep);
            if (!string.IsNullOrEmpty(folder))
            {
                requiredFolders.Add(folder);
            }
        }

        foreach (var folder in requiredFolders)
        {
            string path = Path.Combine(parentDirectory, folder);
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parentDirectory, folder);
            }
        }
    }
    #endregion

    #region Utility
    /// <summary>
    /// Curăță recursiv toate subdirectoarele goale dintr-o cale specificată.
    /// </summary>
    /// <param name="path">Directorul de curățat.</param>
    private static void CleanupEmptyFolders(string path)
    {
        if (!Directory.Exists(path)) return;

        // Curăță recursiv subfolderele
        foreach (var directory in Directory.GetDirectories(path))
        {
            CleanupEmptyFolders(directory);
        }

        // Verifică dacă directorul curent este gol (nu conține alte fișiere sau foldere)
        if (!Directory.EnumerateFileSystemEntries(path).Any())
        {
            // Unity va șterge automat și fișierul .meta corespunzător
            AssetDatabase.DeleteAsset(path);
        }
    }
    #endregion
}

#endif