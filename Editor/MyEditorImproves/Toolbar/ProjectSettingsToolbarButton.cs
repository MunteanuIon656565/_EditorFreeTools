#if UNITY_6_3_OR_NEWER
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Plugins._EditorFreeTools.Editor.MyEditorImproves.Toolbar
{
    public class ProjectSettingsToolbarButton
    {
        [MainToolbarElement("My Tools/Open Project Settings Btn", defaultDockPosition = MainToolbarDockPosition.Left)]
        public static MainToolbarElement ProjectSettingsButton()
        {
            var icon = EditorGUIUtility.IconContent("SettingsIcon").image as Texture2D;
            var content = new MainToolbarContent(icon);
            return new MainToolbarButton(content, () => { SettingsService.OpenProjectSettings(); });
        }
    }
}
#endif