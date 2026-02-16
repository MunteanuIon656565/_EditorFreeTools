/*#if UNITY_EDITOR
using Plugins._EditorFreeTools.Runtime;
using UnityEditor;

namespace Plugins._EditorFreeTools.Editor.MyPlayModeDebugs
{
    [InitializeOnLoad]
    public static class PlayModePlayerSpawnDebug
    {
        static PlayModePlayerSpawnDebug()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null)
                return;

            var camTransform = sceneView.camera.transform;

            PlayModeSpawnData.HasSpawn = true;
            PlayModeSpawnData.Position = camTransform.position;
            PlayModeSpawnData.Rotation = camTransform.rotation;
        }
    }
}
#endif*/