using UnityEngine;
using UnityEditor;

public class FixNecromancerVisual
{
    [MenuItem("Tools/Fix Necromancer Visual")]
    static void FixVisual()
    {
        // Load the SkeletonNecromancer prefab
        string prefabPath = "Assets/_Project/Prefabs/Enemies/SkeletonNecromancer.prefab";
        GameObject necromancerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (necromancerPrefab == null)
        {
            Debug.LogError("SkeletonNecromancer prefab not found!");
            return;
        }

        // Load the Feyloom model (URP version)
        GameObject feyloomModel = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Feyloom/Skeleton_Necromancer/Renders/URP/Prefab/SKM_Skeleton_Necromancer.prefab");

        if (feyloomModel == null)
        {
            Debug.LogError("Feyloom SKM_Skeleton_Necromancer prefab not found! Check path.");
            return;
        }

        // Open prefab for editing
        GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);

        // Find and remove the placeholder visual
        Transform placeholder = prefabInstance.transform.Find("Visual_Placeholder");
        if (placeholder != null)
        {
            Object.DestroyImmediate(placeholder.gameObject);
            Debug.Log("Removed Visual_Placeholder");
        }

        // Check if model already exists
        Transform existingModel = prefabInstance.transform.Find("Model");
        if (existingModel != null)
        {
            Object.DestroyImmediate(existingModel.gameObject);
            Debug.Log("Removed existing Model");
        }

        // Instantiate the Feyloom model as a child
        GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(feyloomModel, prefabInstance.transform);
        modelInstance.name = "Model";
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one;

        // Save the prefab
        PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabInstance);

        Debug.Log("Successfully updated SkeletonNecromancer prefab with Feyloom model!");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
