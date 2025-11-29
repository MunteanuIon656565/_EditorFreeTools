#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ShaderReplacementTool : EditorWindow
{
    [System.Serializable]
    public class ShaderPair
    {
        public string targetShaderName;
        public string replacementShaderName;
    }

    public List<ShaderPair> shaderPairs = new List<ShaderPair>();

    [MenuItem("Tools/Rendering/Shader Replacement Tool")]
    public static void ShowWindow()
    {
        GetWindow<ShaderReplacementTool>("Shader Replacement Tool");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Shader Replacement Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Lista shader-pair
        int removeIndex = -1;
        for (int i = 0; i < shaderPairs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            shaderPairs[i].targetShaderName = EditorGUILayout.TextField("Target Shader", shaderPairs[i].targetShaderName);
            shaderPairs[i].replacementShaderName = EditorGUILayout.TextField("Replacement Shader", shaderPairs[i].replacementShaderName);
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                removeIndex = i;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (removeIndex >= 0)
            shaderPairs.RemoveAt(removeIndex);

        if (GUILayout.Button("Add Shader Pair"))
        {
            shaderPairs.Add(new ShaderPair());
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Replace Shaders"))
        {
            ReplaceShaders();
        }
    }

    private void ReplaceShaders()
    {
        List<Object> modifiedMaterials = new List<Object>();
        string[] guids = AssetDatabase.FindAssets("t:Material");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null) continue;

            foreach (var pair in shaderPairs)
            {
                if (string.IsNullOrEmpty(pair.targetShaderName) || string.IsNullOrEmpty(pair.replacementShaderName))
                    continue;

                Shader targetShader = Shader.Find(pair.targetShaderName);
                Shader replacementShader = Shader.Find(pair.replacementShaderName);

                if (targetShader == null)
                {
                    Debug.LogWarning($"Target shader not found: {pair.targetShaderName}");
                    continue;
                }

                if (replacementShader == null)
                {
                    Debug.LogWarning($"Replacement shader not found: {pair.replacementShaderName}");
                    continue;
                }

                if (mat.shader == targetShader)
                {
                    if (AssetDatabase.IsOpenForEdit(mat))
                    {
                        mat.shader = replacementShader;
                        Debug.Log($"Replaced shader in: {path}");
                    }
                    else
                    {
                        string newPath = path.Replace(".mat", "_Modified.mat");
                        Material newMat = new Material(mat);
                        newMat.shader = replacementShader;
                        AssetDatabase.CreateAsset(newMat, newPath);
                        Debug.Log($"Created modified material: {newPath}");
                    }

                    modifiedMaterials.Add(mat);
                    break; // trece la următorul material
                }
            }
        }

        if (modifiedMaterials.Count > 0)
        {
            Selection.objects = modifiedMaterials.ToArray();
            Debug.Log($"Processed {modifiedMaterials.Count} materials.");
        }
        else
        {
            Debug.LogWarning("No materials matched the shader pairs.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif
