using UnityEngine;
using UnityEditor;
using VoxelRPG.Combat;

public class LinkMonsterPrefabs : EditorWindow
{
    [MenuItem("Tools/Link Monster Prefabs")]
    static void LinkPrefabs()
    {
        Debug.Log("Linking monster prefabs to definitions...");
        
        // Link Zombie
        MonsterDefinition zombieDef = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
            "Assets/_Project/ScriptableObjects/Monsters/Zombie.asset");
        GameObject zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Project/Prefabs/Enemies/Zombie.prefab");
        
        if (zombieDef != null && zombiePrefab != null)
        {
            SerializedObject so = new SerializedObject(zombieDef);
            SerializedProperty prefabProp = so.FindProperty("_prefab") ?? so.FindProperty("prefab");
            
            if (prefabProp != null)
            {
                prefabProp.objectReferenceValue = zombiePrefab;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(zombieDef);
                Debug.Log("✓ Linked Zombie.prefab to Zombie.asset");
            }
            else
            {
                Debug.LogError("Could not find 'prefab' or '_prefab' field on MonsterDefinition");
            }
        }
        else
        {
            if (zombieDef == null) Debug.LogWarning("Zombie.asset not found");
            if (zombiePrefab == null) Debug.LogWarning("Zombie.prefab not found - needs to be created");
        }
        
        // Link Skeleton Necromancer
        MonsterDefinition necromancerDef = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
            "Assets/_Project/ScriptableObjects/Monsters/SkeletonNecromancer.asset");
        GameObject necromancerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Project/Prefabs/Enemies/SkeletonNecromancer.prefab");
        
        if (necromancerDef != null && necromancerPrefab != null)
        {
            SerializedObject so = new SerializedObject(necromancerDef);
            SerializedProperty prefabProp = so.FindProperty("_prefab") ?? so.FindProperty("prefab");
            
            if (prefabProp != null)
            {
                prefabProp.objectReferenceValue = necromancerPrefab;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(necromancerDef);
                Debug.Log("✓ Linked SkeletonNecromancer.prefab to SkeletonNecromancer.asset");
            }
            else
            {
                Debug.LogError("Could not find 'prefab' or '_prefab' field on MonsterDefinition");
            }
        }
        else
        {
            if (necromancerDef == null) Debug.LogWarning("SkeletonNecromancer.asset not found");
            if (necromancerPrefab == null) Debug.LogWarning("SkeletonNecromancer.prefab not found");
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log("Done! Check the messages above to see what was linked.");
    }
}
