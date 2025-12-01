using UnityEngine;
using UnityEditor;
using VoxelRPG.Combat;
using VoxelRPG.Core.Items;

public class ConfigureMonsterSystem : EditorWindow
{
    [MenuItem("Tools/Configure Monster System")]
    static void Configure()
    {
        Debug.Log("Starting Monster System Configuration...");
        
        // Step 1: Link prefabs to MonsterDefinitions
        LinkPrefabsToDefinitions();
        
        // Step 2: Configure MonsterSpawner in scene
        ConfigureMonsterSpawner();
        
        // Step 3: Link projectile to NecromancerAI
        ConfigureNecromancerProjectile();
        
        AssetDatabase.SaveAssets();
        EditorUtility.SetDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[0]);
        
        Debug.Log("Monster System Configuration Complete!");
    }
    
    static void LinkPrefabsToDefinitions()
    {
        // Load Zombie definition and link its prefab
        MonsterDefinition zombieDef = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
            "Assets/_Project/ScriptableObjects/Monsters/Zombie.asset");
        
        if (zombieDef != null)
        {
            GameObject zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Enemies/Zombie.prefab");
            
            if (zombiePrefab == null)
            {
                Debug.LogWarning("Zombie prefab not found. Creating one...");
                zombiePrefab = CreateZombiePrefab();
            }
            
            // Use reflection to set the private _prefab field
            var field = typeof(MonsterDefinition).GetField("_prefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(zombieDef, zombiePrefab);
                EditorUtility.SetDirty(zombieDef);
                Debug.Log("Linked Zombie prefab to Zombie MonsterDefinition");
            }
            else
            {
                Debug.LogError("Could not find _prefab field on MonsterDefinition. Field might be named differently.");
            }
        }
        else
        {
            Debug.LogWarning("Zombie.asset not found at Assets/_Project/ScriptableObjects/Monsters/Zombie.asset");
        }
        
        // Load Skeleton Necromancer definition and link its prefab
        MonsterDefinition necromancerDef = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
            "Assets/_Project/ScriptableObjects/Monsters/SkeletonNecromancer.asset");
        
        if (necromancerDef != null)
        {
            GameObject necromancerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Enemies/SkeletonNecromancer.prefab");
            
            if (necromancerPrefab != null)
            {
                var field = typeof(MonsterDefinition).GetField("_prefab", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (field != null)
                {
                    field.SetValue(necromancerDef, necromancerPrefab);
                    EditorUtility.SetDirty(necromancerDef);
                    Debug.Log("Linked SkeletonNecromancer prefab to SkeletonNecromancer MonsterDefinition");
                }
            }
            else
            {
                Debug.LogError("SkeletonNecromancer prefab not found at Assets/_Project/Prefabs/Enemies/SkeletonNecromancer.prefab");
            }
        }
        else
        {
            Debug.LogWarning("SkeletonNecromancer.asset not found at Assets/_Project/ScriptableObjects/Monsters/SkeletonNecromancer.asset");
        }
    }
    
    static GameObject CreateZombiePrefab()
    {
        GameObject zombie = new GameObject("Zombie");
        
        // Add components
        zombie.AddComponent<MonsterHealth>();
        zombie.AddComponent<BasicMonsterAI>();
        zombie.AddComponent<Animator>();
        
        // Add collider
        CapsuleCollider collider = zombie.AddComponent<CapsuleCollider>();
        collider.height = 2f;
        collider.radius = 0.5f;
        collider.center = new Vector3(0, 1f, 0);
        
        // Add Rigidbody
        Rigidbody rb = zombie.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        // Create visual
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual";
        visual.transform.SetParent(zombie.transform);
        visual.transform.localPosition = new Vector3(0, 1f, 0);
        visual.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        DestroyImmediate(visual.GetComponent<Collider>());
        
        // Set layer
        zombie.layer = LayerMask.NameToLayer("Enemy");
        if (zombie.layer == -1) zombie.layer = 0;
        
        // Save as prefab
        string path = "Assets/_Project/Prefabs/Enemies/Zombie.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(zombie, path);
        DestroyImmediate(zombie);
        
        Debug.Log("Created Zombie prefab at " + path);
        return prefab;
    }
    
    static void ConfigureMonsterSpawner()
    {
        // Find MonsterSpawner in scene
        MonsterSpawner spawner = GameObject.FindObjectOfType<MonsterSpawner>();
        
        if (spawner == null)
        {
            Debug.LogError("MonsterSpawner not found in scene!");
            return;
        }
        
        // Load monster definitions
        MonsterDefinition zombieDef = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
            "Assets/_Project/ScriptableObjects/Monsters/Zombie.asset");
        MonsterDefinition necromancerDef = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
            "Assets/_Project/ScriptableObjects/Monsters/SkeletonNecromancer.asset");
        
        if (zombieDef == null || necromancerDef == null)
        {
            Debug.LogError("Could not load one or both monster definitions!");
            return;
        }
        
        // Set the _monsterTypes array using reflection
        var field = typeof(MonsterSpawner).GetField("_monsterTypes", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            MonsterDefinition[] monsterTypes = new MonsterDefinition[] { zombieDef, necromancerDef };
            field.SetValue(spawner, monsterTypes);
            EditorUtility.SetDirty(spawner);
            Debug.Log("Configured MonsterSpawner with Zombie and SkeletonNecromancer");
        }
        else
        {
            Debug.LogError("Could not find _monsterTypes field on MonsterSpawner");
        }
    }
    
    static void ConfigureNecromancerProjectile()
    {
        // Load the necromancer prefab
        GameObject necromancerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Project/Prefabs/Enemies/SkeletonNecromancer.prefab");
        
        if (necromancerPrefab == null)
        {
            Debug.LogError("SkeletonNecromancer prefab not found!");
            return;
        }
        
        // Load the projectile prefab
        GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Project/Prefabs/Effects/NecromancerProjectile.prefab");
        
        if (projectilePrefab == null)
        {
            Debug.LogError("NecromancerProjectile prefab not found at Assets/_Project/Prefabs/Effects/NecromancerProjectile.prefab");
            return;
        }
        
        // Open the prefab for editing
        string prefabPath = AssetDatabase.GetAssetPath(necromancerPrefab);
        GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);
        
        // Get the NecromancerAI component
        NecromancerAI necromancerAI = prefabInstance.GetComponent<NecromancerAI>();
        
        if (necromancerAI != null)
        {
            // Set the projectile prefab using reflection
            var field = typeof(NecromancerAI).GetField("_projectilePrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(necromancerAI, projectilePrefab);
                
                // Save the prefab
                PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
                Debug.Log("Linked projectile prefab to NecromancerAI");
            }
            else
            {
                Debug.LogError("Could not find _projectilePrefab field on NecromancerAI");
            }
        }
        else
        {
            Debug.LogError("NecromancerAI component not found on SkeletonNecromancer prefab!");
        }
        
        // Unload the prefab
        PrefabUtility.UnloadPrefabContents(prefabInstance);
    }
}
