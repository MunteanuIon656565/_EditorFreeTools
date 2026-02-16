#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Plugins._EditorFreeTools.Runtime
{
    public class DebugSpawnPlayer : MonoBehaviour
    {
        private async void Start()
        {
            try
            {
                await Task.Delay(100);
            
                if (!PlayModeSpawnData.HasSpawn)
                    return;

                transform.SetPositionAndRotation(
                    PlayModeSpawnData.Position,
                    PlayModeSpawnData.Rotation
                );

                PlayModeSpawnData.HasSpawn = false;
            }
            catch (Exception e)
            {
                // ignore
            }
        }
    }
}
#endif
