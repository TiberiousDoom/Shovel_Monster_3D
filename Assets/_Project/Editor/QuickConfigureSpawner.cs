using UnityEngine;
using UnityEditor;
using VoxelRPG.Combat;

public class QuickConfigureSpawner
{
    [MenuItem("Tools/Quick Configure Spawner")]
    public static void Configure()
    {
        MonsterSpawner spawner = Object.FindFirstObjectByType<MonsterSpawner>();
        if (spawner == null)
        {
            Debug.LogError("No MonsterSpawner found in scene! Make sure a scene with MonsterSpawner is open.");
            return;
        }

        // Load monster definitions
        MonsterDefinition zombie = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
            "Assets/_Project/ScriptableObjects/Monsters/Zombie.asset");
        MonsterDefinition necromancer = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
            "Assets/_Project/ScriptableObjects/Monsters/SkeletonNecromancer.asset");

        if (zombie == null)
        {
            Debug.LogError("Zombie.asset not found at Assets/_Project/ScriptableObjects/Monsters/");
            return;
        }

        if (necromancer == null)
        {
            Debug.LogError("SkeletonNecromancer.asset not found at Assets/_Project/ScriptableObjects/Monsters/");
            return;
        }

        // Use SerializedObject to set the array
        SerializedObject so = new SerializedObject(spawner);
        SerializedProperty monsterTypesProp = so.FindProperty("_monsterTypes");

        if (monsterTypesProp == null)
        {
            Debug.LogError("_monsterTypes property not found on MonsterSpawner!");
            return;
        }

        monsterTypesProp.ClearArray();
        monsterTypesProp.arraySize = 2;
        monsterTypesProp.GetArrayElementAtIndex(0).objectReferenceValue = zombie;
        monsterTypesProp.GetArrayElementAtIndex(1).objectReferenceValue = necromancer;

        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(spawner);

        Debug.Log($"MonsterSpawner configured with:\n" +
            $"  - Zombie (weight: {zombie.SpawnWeight})\n" +
            $"  - Skeleton Necromancer (weight: {necromancer.SpawnWeight})");
    }
}
