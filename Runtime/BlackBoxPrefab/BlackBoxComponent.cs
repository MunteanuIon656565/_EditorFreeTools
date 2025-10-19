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
        if (!IsPrefabAsset())
            Debug.LogError($"[BlackBox] Componentul trebuie adăugat doar pe prefab asset! Obiect: {gameObject.name}");
    }

    private void Awake() => HandleVisibility();
    private void OnEnable() => HandleVisibility();

    private void OnValidate()
    {
        // Asigură-te că atunci când scena e deschisă și componentul e deja prezent,
        // ascunderea se aplică imediat (de ex. după recompilare)
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            HandleVisibility();
    }

    private void OnDisable() => RestoreSceneVisibility();

    private void OnDestroy()
    {
        // Dacă este distrus în editor, restaurăm tot (copii și componente)
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

    private void HandleVisibility()
    {
        // dacă nu e scenă validă -> ignorăm
        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
            return;

        // În Prefab Mode -> nu ascunde nimic
        if (IsEditingInPrefabMode())
        {
            RestoreSceneVisibility();
            return;
        }

        // În scenă (instanță)
        HideChildrenRecursive(transform);
        HideComponentsExceptAllowed();
    }

    private void HideChildrenRecursive(Transform parent)
    {
        hiddenChildren.Clear();

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
        hiddenComponents.Clear();

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

        // Forțăm refresh în editor
        EditorApplication.DirtyHierarchyWindowSorting();
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
#endif
