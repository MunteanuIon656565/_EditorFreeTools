#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad] //decomment to enable script
public class AutoFocusPreviewWindow
{
    private const string HIERARCHY = "Hierarchy";
    private const string SCENE = "Scene";
    private const string PREVIEW = "Preview";
    private const string PROJECT = "Project";
    private const string PREF_KEY_LAST_OPENED = "AutoFocusPreviewWindow.lastOpenedWindow";
    
    private static string LastOpenedWindow
    {
        get => EditorPrefs.GetString(PREF_KEY_LAST_OPENED, "");
        set => EditorPrefs.SetString(PREF_KEY_LAST_OPENED, value ?? "");
    }

    
    [Obsolete("Obsolete")]
    static AutoFocusPreviewWindow()
    {
        Selection.selectionChanged += OnSelectionChanged;
    }

    [Obsolete("Obsolete")]
    private static void OnSelectionChanged()
    {
        Object selectedObject = Selection.activeObject;
        if (selectedObject == null) return;

        if (!EditorApplication.isPlaying)
        {
            if (IsSelectionInHierarchy(selectedObject))
            {
                FocusOnSceneView();
                return;
            }
        }

        if (IsSelectionInProjectWindow(selectedObject) && !IsObjectForPreview(selectedObject))
        {
            FocusOnPreviewWindow();
        }
    }

    private static bool IsSelectionInHierarchy(Object selectedObject)
    {
        return selectedObject is GameObject && !AssetDatabaseContains(selectedObject);
    }

    private static bool AssetDatabaseContains(Object selectedObject)
    {
        string assetPath = AssetDatabase.GetAssetPath(selectedObject);
        return !string.IsNullOrEmpty(assetPath) && !AssetDatabase.IsValidFolder(assetPath);
    }

    [Obsolete("Obsolete")]
    private static bool IsSelectionInProjectWindow(Object selectedObject)
    {
        return EditorWindow.focusedWindow?.title == PROJECT && AssetDatabaseContains(selectedObject);
    }

    private static bool IsObjectForPreview(Object selectedObject)
    {
        string path = AssetDatabase.GetAssetPath(selectedObject);

        return !AssetDatabase.IsValidFolder(path) 
               && AssetDatabaseContains(selectedObject) 
               && !(selectedObject is GameObject || selectedObject is AnimationClip || selectedObject is Mesh 
                    || selectedObject is AudioClip || selectedObject is Texture || selectedObject is Sprite 
                    || selectedObject is Material || path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase));
    }

    private static void FocusOnSceneView()
    {
        if (IsYetOpenedWindow(SCENE)) return;
        
        var asm = typeof(EditorWindow).Assembly;
        var sceneType = asm.GetType("UnityEditor.SceneView") ?? asm.GetType("UnityEditor.SceneWindow");
        if (sceneType == null) return;

        Object[] found = Resources.FindObjectsOfTypeAll(sceneType);

        if (found != null && found.Length > 0)
        {
            (found[0] as EditorWindow)?.ShowTab();
        }
    }

    private static void FocusOnPreviewWindow()
    {
        if (IsYetOpenedWindow(PREVIEW)) return;
        
        var previewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.PreviewWindow");
        if (previewType == null) return; // type not found for some Unity versions

        UnityEngine.Object[] found = UnityEngine.Resources.FindObjectsOfTypeAll(previewType);
        if (found == null || found.Length == 0)
        {
            // Preview window isn't opened separately (it's likely only available inside the Inspector) - do nothing.
            return;
        }

        // Focus the first existing PreviewWindow instance (user opened it manually)
        var previewWindow = found[0] as EditorWindow;
        if (previewWindow != null)
        {
            previewWindow.ShowTab();
        }
    }

    private static bool IsYetOpenedWindow(string windowTitle)
    {
        EditorWindow focusedWindow = EditorWindow.focusedWindow;

        // Consider different if focused window exists and last saved window differs or is empty
        bool isFocusedWindowDifferent = focusedWindow != null 
                                        && (LastOpenedWindow != windowTitle || string.IsNullOrEmpty(LastOpenedWindow));
        
        if (windowTitle.Equals(PREVIEW))
        {
            LastOpenedWindow = SCENE;
        }
        else if (windowTitle.Equals(SCENE))
        {
            LastOpenedWindow = PREVIEW;
        }

        return isFocusedWindowDifferent;
    }
}

#endif
