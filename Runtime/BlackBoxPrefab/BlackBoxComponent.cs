#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Plugins._EditorFreeTools.Runtime.BlackBoxPrefab
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class BlackBoxComponent : MonoBehaviour
    {
        [Header("BlackBox Settings")]
        [Tooltip("Componentele care pot fi vizibile în scenă (în afară de Transform și BlackBoxComponent).")]
        public Component[] exposedComponents;

        private readonly List<GameObject> hiddenChildren = new List<GameObject>();
        private readonly List<Component> hiddenComponents = new List<Component>();

        // ================== Gizmos ==================
        [Header("Gizmos Settings")]
        public Color gizmosColor = new Color(0f, 0f, 0f, 0.12f); // #0000001E
        public Vector3 gizmosOffset = Vector3.zero;
        public Vector3 gizmosSize = Vector3.one;


        private void Reset()
        {
            // ✅ Verifică dacă obiectul este parte dintr-un prefab
            var assetType = PrefabUtility.GetPrefabAssetType(gameObject);
            var instanceStatus = PrefabUtility.GetPrefabInstanceStatus(gameObject);

            bool isPrefab =
                assetType != PrefabAssetType.NotAPrefab ||
                instanceStatus == PrefabInstanceStatus.Connected ||
                instanceStatus == PrefabInstanceStatus.MissingAsset;

            if (!isPrefab)
            {
                Debug.LogError("[BlackBox] Componentul poate fi adăugat doar pe obiecte care sunt parte dintr-un Prefab!");
                EditorApplication.delayCall += () => { if (this != null) DestroyImmediate(this); };
                return;
            }

            // ✅ Verifică dacă prefab-ul sursă este un model FBX
            var prefabAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
            if (!string.IsNullOrEmpty(prefabAssetPath) && prefabAssetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError($"[BlackBox] Componentul nu poate fi adăugat pe modele FBX! ({prefabAssetPath})");
                EditorApplication.delayCall += () => { if (this != null) DestroyImmediate(this); };
                return;
            }

            // Calculează dimensiunea box-ului automat
            CalculateGizmosSize();
        }

        private void Awake() => EditorApplication.delayCall += () => { if (this == null) return; TryApplyHide(); };
        private void OnEnable() => TryApplyHide();
        private void OnValidate() => TryApplyHide();
        private void OnDisable() => RestoreSceneVisibility();
        private void OnDestroy() { if (!EditorApplication.isPlayingOrWillChangePlaymode) RestoreSceneVisibility(); }

        private bool IsPrefabAsset() => PrefabUtility.IsPartOfPrefabAsset(gameObject) && !PrefabUtility.IsPartOfPrefabInstance(gameObject);

        private bool IsEditingInPrefabMode()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            return stage != null && stage.prefabContentsRoot == gameObject;
        }

        private void TryApplyHide()
        {
            if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
                return;

            if (IsEditingInPrefabMode())
            {
                RestoreSceneVisibility();
                return;
            }

            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                HideAll();
            }
        }

        private void HideChildrenRecursive(Transform parent)
        {
            foreach (Transform child in parent)
            {
                if (child == null) continue;

                if (child.gameObject.scene.IsValid())
                {
                    child.gameObject.hideFlags = HideFlags.HideInHierarchy;
                    hiddenChildren.Add(child.gameObject);
                    HideChildrenRecursive(child);
                }
            }
        }

        private void HideComponentsExceptAllowed()
        {
            foreach (var comp in GetComponents<Component>())
            {
                if (comp == null) continue;
                if (comp is Transform || comp == this) continue;

                bool isExposed = exposedComponents != null && Array.Exists(exposedComponents, e => e == comp);
                if (!isExposed)
                {
                    comp.hideFlags = HideFlags.HideInInspector;
                    hiddenComponents.Add(comp);
                }
            }
        }

        public void HideAll()
        {
            hiddenChildren.Clear();
            hiddenComponents.Clear();

            HideChildrenRecursive(transform);
            HideComponentsExceptAllowed();

            EditorApplication.RepaintHierarchyWindow();
        }

        public void RestoreSceneVisibility()
        {
            foreach (var child in hiddenChildren)
            {
                if (child != null)
                    child.hideFlags = HideFlags.None;
            }

            foreach (var comp in hiddenComponents)
            {
                if (comp != null)
                    comp.hideFlags = HideFlags.None;
            }

            hiddenChildren.Clear();
            hiddenComponents.Clear();

            EditorApplication.RepaintHierarchyWindow();
        }

        public void SerializeExposedComponents()
        {
            if (exposedComponents == null) return;

            foreach (var comp in exposedComponents)
            {
                if (comp == null) continue;

                string json = JsonUtility.ToJson(comp);
                string path = $"Assets/BlackBox/Serialized/{gameObject.name}_{comp.GetType().Name}.json";
                System.IO.Directory.CreateDirectory("Assets/BlackBox/Serialized");
                System.IO.File.WriteAllText(path, json);
            }

            Debug.Log($"[BlackBox] Components serialized for prefab {gameObject.name}");
        }

        // ================== Gizmos ==================
        private void CalculateGizmosSize()
        {
            // Începem cu un Bounds invalid
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            bool hasBounds = false;

            // Funcție helper pentru a adăuga mesh-uri în Bounds
            void EncapsulateMesh(Mesh mesh, Transform t)
            {
                if (mesh == null) return;
                var vertices = mesh.vertices;
                if (vertices == null || vertices.Length == 0) return;

                foreach (var v in vertices)
                {
                    Vector3 worldPos = t.TransformPoint(v);
                    if (!hasBounds)
                    {
                        bounds = new Bounds(worldPos, Vector3.zero);
                        hasBounds = true;
                    }
                    else bounds.Encapsulate(worldPos);
                }
            }

            // --- MeshFilter ---
            foreach (var mf in GetComponentsInChildren<MeshFilter>())
            {
                EncapsulateMesh(mf.sharedMesh, mf.transform);
            }

            // --- SkinnedMeshRenderer ---
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                EncapsulateMesh(smr.sharedMesh, smr.transform);
            }

            // --- Colliders deja existente ---
            foreach (var col in GetComponentsInChildren<Collider>())
            {
                if (!hasBounds)
                {
                    bounds = col.bounds;
                    hasBounds = true;
                }
                else bounds.Encapsulate(col.bounds);
            }

            // Dacă nu avem nimic, fallback la 1m cube
            if (!hasBounds)
            {
                gizmosSize = Vector3.one;
                gizmosOffset = Vector3.zero;
                return;
            }

            gizmosSize = bounds.size;
            gizmosOffset = bounds.center - transform.position;
        }


        private void OnDrawGizmos()
        {
            if (gizmosSize == Vector3.zero) CalculateGizmosSize();

            Gizmos.color = gizmosColor;

            Vector3 pos = transform.position + gizmosOffset;

            // Wireframe
            Gizmos.DrawWireCube(pos, gizmosSize);

            // Solid
            Gizmos.DrawCube(pos, gizmosSize);
        }
    }

// ===== CUSTOM INSPECTOR =====
    [CustomEditor(typeof(BlackBoxComponent))]
    public class BlackBoxComponentEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var bb = (BlackBoxComponent)target;

            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("=== Debug Tools ===", EditorStyles.boldLabel);

            if (GUILayout.Button("👁️ Show All (Test)"))
            {
                bb.RestoreSceneVisibility();
            }

            if (GUILayout.Button("🙈 Hide All (Test)"))
            {
                bb.HideAll();
            }

            EditorGUILayout.HelpBox(
                "Aceste butoane afectează doar scena curentă.\nDupă reîncărcare sau reatașare, prefab-ul se va ascunde automat.",
                MessageType.Info);
        }
    }
}
#endif
