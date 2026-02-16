#if UNITY_EDITOR
using UnityEngine;

namespace Plugins._EditorFreeTools.Runtime
{
    public static class PlayModeSpawnData
    {
        public static bool HasSpawn;
        public static Vector3 Position;
        public static Quaternion Rotation;
    }
}
#endif
