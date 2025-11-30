#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Plugins._EditorFreeTools.Editor.MyEditorImproves
{
    [InitializeOnLoad] 
    [DefaultExecutionOrder(-1000)]
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

            FocusOnInspector();
        }

        private static void FocusOnInspector()
        {
            var inspectorType = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");
            if (inspectorType == null) return; // Type not found for some Unity versions

            Object[] found = Resources.FindObjectsOfTypeAll(inspectorType);
            if (found == null || found.Length == 0)
            {
                EditorApplication.ExecuteMenuItem("Window/General/Inspector");
                return;
            }

            // Focus the first existing Inspector instance (the main one)
            foreach (Object obj in found)
            {
                var inspectorWindow = obj as EditorWindow;
                if (inspectorWindow != null && inspectorWindow.GetType() == inspectorType)
                {
                    inspectorWindow.ShowTab();

                    break;
                }
            }
            
        }
    }
}

#endif
