#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[DisallowMultipleComponent]
[ExecuteAlways]
public class BlackBoxComponent : MonoBehaviour
{
    [Header("BlackBox Settings")]
    [Tooltip("Componentele care pot fi vizibile în scenă (în afară de Transform și BlackBoxComponent).")]
    public Component[] exposedComponents;

    private readonly List<GameObject> hiddenChildren = new List<GameObject>();
    private readonly List<Component> hiddenComponents = new List<Component>();

    private void Reset()
    {
        // ✅ Verifică dacă obiectul este parte dintr-un prefab (asset sau instanță)
        var assetType = PrefabUtility.GetPrefabAssetType(gameObject);
        var instanceStatus = PrefabUtility.GetPrefabInstanceStatus(gameObject);

        bool isPrefab =
            assetType != PrefabAssetType.NotAPrefab ||
            instanceStatus == PrefabInstanceStatus.Connected ||
            instanceStatus == PrefabInstanceStatus.MissingAsset;

        if (!isPrefab)
        {
            Debug.LogError("[BlackBox] Componentul poate fi adăugat doar pe obiecte care sunt parte dintr-un Prefab!");

            // 🔧 Amână distrugerea ca să nu încalce restricțiile de editor
            EditorApplication.delayCall += () =>
            {
                if (this != null)
                    DestroyImmediate(this);
            };

            return;
        }
        
        /*if (!IsPrefabAsset())
            Debug.LogError($"[BlackBox] Componentul trebuie adăugat doar pe prefab asset! Obiect: {gameObject.name}");*/
    }

    private void Awake() => TryApplyHide();
    private void OnEnable() => TryApplyHide();

    private void OnValidate() => TryApplyHide();

    private void OnDisable() => RestoreSceneVisibility();

    private void OnDestroy()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            RestoreSceneVisibility();
    }

    private bool IsPrefabAsset()
    {
        return PrefabUtility.IsPartOfPrefabAsset(gameObject) && !PrefabUtility.IsPartOfPrefabInstance(gameObject);
    }

    private bool IsEditingInPrefabMode()
    {
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        return stage != null && stage.prefabContentsRoot == gameObject;
    }

    /// <summary>
    /// Aplică ascunderea automat doar dacă e în scenă (nu prefab mode)
    /// </summary>
    private void TryApplyHide()
    {
        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
            return;

        if (IsEditingInPrefabMode())
        {
            RestoreSceneVisibility();
            return;
        }

        // Aplica ascunderea la pornirea scenei
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
}


// ===== CUSTOM INSPECTOR =====
[CustomEditor(typeof(BlackBoxComponent))]
public class BlackBoxComponentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var bb = (BlackBoxComponent)target;
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
#endif
