#if UNITY_EDITOR
using Plugins._EditorFreeTools.Runtime;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Plugins._EditorFreeTools.Editor.MyEditorImproves.Toolbar
{
    public class TeleportObjToCameraScenePositionToolbarButton
    {
        [MainToolbarElement("My Tools/TeleportObjToCameraScenePosition Btn", defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement TeleportObjToCameraScenePositionButton()
        {
            var icon = EditorGUIUtility.IconContent("d_PlayButton").image as Texture2D;
            var content = new MainToolbarContent(icon); 
            return new MainToolbarButton(content, OnPlayModeChanged);
        }
        
        private static void OnPlayModeChanged()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null)
                return;

            var camTransform = sceneView.camera.transform;

            PlayModeSpawnData.HasSpawn = true;
            PlayModeSpawnData.Position = camTransform.position;
            PlayModeSpawnData.Rotation = camTransform.rotation;
            
            EditorApplication.isPlaying = true;
        }
    }
}
#endif