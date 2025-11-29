#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Plugins
{
    public static class OverdrawToggle
    {
        private const string ShortcutId = "Rendering/Toggle Overdraw Mode";

        [MenuItem("Tools/Rendering/Toggle Overdraw Mode _&o")]
        [Shortcut(ShortcutId, KeyCode.O, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        private static void ToggleOverdrawMode()
        {
            var renderingPanel = DebugManager.instance?.GetPanel("Rendering", true);
            if (renderingPanel == null)
            {
                Debug.LogWarning("URP Rendering Debug Panel not found!");
                return;
            }

            var overdrawWidget = FindWidgetRecursive(renderingPanel, "Overdraw");
            if (overdrawWidget == null)
            {
                Debug.LogWarning("Overdraw widget not found in Rendering panel!");
                return;
            }

            var getIndexField = overdrawWidget.GetType().GetField("<getIndex>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            var setIndexField = overdrawWidget.GetType().GetField("<setIndex>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (getIndexField?.GetValue(overdrawWidget) is System.Delegate getIndex &&
                setIndexField?.GetValue(overdrawWidget) is System.Delegate setIndex)
            {
                int currentIndex = (int)getIndex.DynamicInvoke();
                int newIndex = currentIndex == 0 ? 2 : 0;
                
                setIndex.DynamicInvoke(newIndex);
                
                string oldMode = GetModeName(currentIndex);
                string newMode = GetModeName(newIndex);
                Debug.Log($"<color=green>Overdraw Mode: {oldMode} → {newMode}</color>");
                
                SceneView.RepaintAll();
            }
            else
            {
                Debug.LogWarning("Could not access overdraw widget fields!");
            }
        }

        private static object FindWidgetRecursive(object parent, string targetName)
        {
            var childrenProp = parent.GetType().GetProperty("children");
            if (childrenProp?.GetValue(parent) is not System.Collections.IEnumerable children)
                return null;
            
            foreach (var item in children)
            {
                var displayNameProp = item.GetType().GetProperty("displayName");
                if (displayNameProp?.GetValue(item) is string displayName)
                {
                    if (displayName.Contains(targetName, System.StringComparison.OrdinalIgnoreCase))
                        return item;
                    
                    var result = FindWidgetRecursive(item, targetName);
                    if (result != null) 
                        return result;
                }
            }
            return null;
        }

        private static string GetModeName(int index) => index switch
        {
            0 => "None",
            2 => "Transparent",
            _ => $"Mode {index}"
        };
    }
}
#endif