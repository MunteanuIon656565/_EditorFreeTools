using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[ExecuteAlways]
public class BlackBoxComponent : MonoBehaviour
{
    [Header("BlackBox Settings")]
    [Tooltip("Listează componentele care pot fi modificate în scenă")]
    public Component[] exposedComponents;

    private List<GameObject> hiddenChildren = new List<GameObject>();

#if UNITY_EDITOR
    private void Reset()
    {
        // Log dacă nu e asset, fără ștergere
        if (!IsPrefabAsset())
        {
            Debug.LogError($"[BlackBox] Componentul trebuie adăugat doar pe prefab asset! Obiect: {gameObject.name}");
        }
    }

    private void Awake()
    {
        HandleSceneInstance();
    }

    private void OnEnable()
    {
        HandleSceneInstance();
    }

    private void OnDisable()
    {
        RestoreSceneVisibility();
    }

    private void OnDestroy()
    {
        RestoreSceneVisibility();
    }

    private bool IsPrefabAsset()
    {
#if UNITY_2018_1_OR_NEWER
        return PrefabUtility.IsPartOfPrefabAsset(gameObject) && !PrefabUtility.IsPartOfPrefabInstance(gameObject);
#else
        return PrefabUtility.GetPrefabType(gameObject) == PrefabType.Prefab;
#endif
    }

    private void HandleSceneInstance()
    {
        // Ascundem doar dacă e instanță în scenă
        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded) return;

        HideChildrenRecursive(transform);
        HideComponentsInScene();
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

    private void HideComponentsInScene()
    {
        if (exposedComponents == null) return;

        foreach (var comp in GetComponents<Component>())
        {
            if (comp == null || comp == this) continue;

            bool isExposed = Array.Exists(exposedComponents, e => e == comp);
            if (!isExposed)
                comp.hideFlags = HideFlags.HideInInspector;
        }
    }

    public void RestoreSceneVisibility()
    {
        foreach (var child in hiddenChildren)
        {
            if (child != null)
                child.hideFlags = HideFlags.None;
        }

        foreach (var comp in GetComponents<Component>())
        {
            if (comp != null)
                comp.hideFlags = HideFlags.None;
        }
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
#endif
}
