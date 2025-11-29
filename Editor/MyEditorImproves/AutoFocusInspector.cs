#if UNITY_EDITOR

using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad] // Decomment to enable script
public class AutoFocusInspector
{
    [Obsolete("Obsolete")]
    static AutoFocusInspector()
    {
        Selection.selectionChanged += OnSelectionChanged;
    }

    [Obsolete("Obsolete")]
    private static void OnSelectionChanged()
    {
        Object selectedObject = Selection.activeObject;
        if (selectedObject == null) return;

        // Focus Inspector whenever an object is selected from Hierarchy or Project window
        FocusOnInspector();
    }

    private static async void FocusOnInspector()
    {
        // Get the Inspector window
        var inspectorType = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");
        if (inspectorType == null) return; // Type not found for some Unity versions

        Object[] found = Resources.FindObjectsOfTypeAll(inspectorType);
        if (found == null || found.Length == 0)
        {
            // Inspector window isn't opened - open it
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
            return;
        }

        // Focus the first existing Inspector instance (the main one)
        foreach (Object obj in found)
        {
            var inspectorWindow = obj as EditorWindow;
            if (inspectorWindow != null && inspectorWindow.GetType() == inspectorType)
            {
                await Task.Delay(100);
                inspectorWindow.Focus();
                break;
            }
        }
        
    }
}

#endif

