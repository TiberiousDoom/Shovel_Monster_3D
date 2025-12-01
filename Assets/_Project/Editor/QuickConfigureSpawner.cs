using UnityEngine;
using UnityEditor;
using VoxelRPG.Combat;

public class QuickConfigureSpawner
{
    [MenuItem("Tools/Quick Configure Spawner")]
    public static void Configure()
    {
        MonsterSpawner spawner = Object.FindObjectOfType<MonsterSpawner>();
        if (spawner == null)
        {
            Debug.LogError("No MonsterSpawner found!");
            return;
        }

        // Load monster definitions
        MonsterDefinition zombie = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
            "Assets/_Project/ScriptableObjects/Monsters/Zombie.asset");
        MonsterDefinition necromancer = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
            "Assets/_Project/ScriptableObjects/Monsters/SkeletonNecromancer.asset");

        if (zombie == null)
        {
            Debug.LogError("Zombie.asset not found!");
            return;
        }
        
        if (necromancer == null)
        {
            Debug.LogError("SkeletonNecromancer.asset not found!");
            return;
        }

        // Use SerializedObject to set the array
        SerializedObject so = new SerializedObject(spawner);
        SerializedProperty monsterTypesProp = so.FindProperty("_monsterTypes");
        
        if (monsterTypesProp == null)
        {
            Debug.LogError("_monsterTypes property not found!");
            return;
        }

        monsterTypesProp.ClearArray();
        monsterTypesProp.arraySize = 2;
        monsterTypesProp.GetArrayElementAtIndex(0).objectReferenceValue = zombie;
        monsterTypesProp.GetArrayElementAtIndex(1).objectReferenceValue = necromancer;
        
        so.ApplyModifiedProperties();
        
        EditorUtility.SetDirty(spawner);
        
        Debug.Log("MonsterSpawner configured with Zombie and SkeletonNecromancer!");
    }
}
