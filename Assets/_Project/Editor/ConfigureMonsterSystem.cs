using UnityEngine;
using UnityEditor;
using VoxelRPG.Combat;
using System.IO;

public class ConfigureMonsterSystem : EditorWindow
{
    [MenuItem("Tools/Configure Monster System")]
    static void Configure()
    {
        Debug.Log("Starting Monster System Configuration...");

        // Ensure directories exist
        EnsureDirectoriesExist();

        // Step 1: Create missing prefabs
        CreateMissingPrefabs();

        // Step 2: Link prefabs to MonsterDefinitions
        LinkPrefabsToDefinitions();

        // Step 3: Configure MonsterSpawner in scene
        ConfigureMonsterSpawner();

        // Step 4: Link projectile to NecromancerAI
        ConfigureNecromancerProjectile();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Monster System Configuration Complete!");
    }

    static void EnsureDirectoriesExist()
    {
        string[] directories = new string[]
        {
            "Assets/_Project/Prefabs",
            "Assets/_Project/Prefabs/Enemies",
            "Assets/_Project/Prefabs/Effects"
        };

        foreach (var dir in directories)
        {
            if (!AssetDatabase.IsValidFolder(dir))
            {
                string parent = Path.GetDirectoryName(dir).Replace("\\", "/");
                string folderName = Path.GetFileName(dir);
                AssetDatabase.CreateFolder(parent, folderName);
                Debug.Log($"Created folder: {dir}");
            }
        }
    }

    static void CreateMissingPrefabs()
    {
        // Create Zombie prefab if missing
        if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/Zombie.prefab") == null)
        {
            CreateZombiePrefab();
        }

        // Create SkeletonNecromancer prefab if missing
        if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/SkeletonNecromancer.prefab") == null)
        {
            CreateSkeletonNecromancerPrefab();
        }

        // Create NecromancerProjectile prefab if missing
        if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Effects/NecromancerProjectile.prefab") == null)
        {
            CreateNecromancerProjectilePrefab();
        }
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

            if (zombiePrefab != null)
            {
                SetPrefabField(zombieDef, zombiePrefab);
                Debug.Log("Linked Zombie prefab to Zombie MonsterDefinition");
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
                SetPrefabField(necromancerDef, necromancerPrefab);
                Debug.Log("Linked SkeletonNecromancer prefab to SkeletonNecromancer MonsterDefinition");
            }
        }
        else
        {
            Debug.LogWarning("SkeletonNecromancer.asset not found at Assets/_Project/ScriptableObjects/Monsters/SkeletonNecromancer.asset");
        }
    }

    static void SetPrefabField(MonsterDefinition definition, GameObject prefab)
    {
        var field = typeof(MonsterDefinition).GetField("_prefab",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            field.SetValue(definition, prefab);
            EditorUtility.SetDirty(definition);
        }
        else
        {
            Debug.LogError("Could not find _prefab field on MonsterDefinition");
        }
    }

    static GameObject CreateZombiePrefab()
    {
        GameObject zombie = new GameObject("Zombie");

        // Add components
        zombie.AddComponent<MonsterHealth>();
        zombie.AddComponent<BasicMonsterAI>();

        // Add collider
        CapsuleCollider collider = zombie.AddComponent<CapsuleCollider>();
        collider.height = 2f;
        collider.radius = 0.5f;
        collider.center = new Vector3(0, 1f, 0);

        // Add Rigidbody
        Rigidbody rb = zombie.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Create visual placeholder (green capsule for zombie)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual";
        visual.transform.SetParent(zombie.transform);
        visual.transform.localPosition = new Vector3(0, 1f, 0);
        visual.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        Object.DestroyImmediate(visual.GetComponent<Collider>());

        // Set green material for zombie
        var renderer = visual.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.2f, 0.5f, 0.2f); // Dark green
            renderer.sharedMaterial = mat;
        }

        // Set layer
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        zombie.layer = enemyLayer != -1 ? enemyLayer : 0;

        // Save as prefab
        string path = "Assets/_Project/Prefabs/Enemies/Zombie.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(zombie, path);
        Object.DestroyImmediate(zombie);

        Debug.Log("Created Zombie prefab at " + path);
        return prefab;
    }

    static GameObject CreateSkeletonNecromancerPrefab()
    {
        GameObject necromancer = new GameObject("SkeletonNecromancer");

        // Try to use the Feyloom model if available
        GameObject feyloomModel = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Feyloom/Skeleton_Necromancer/Renders/URP/Prefab/SKM_Skeleton_Necromancer.prefab");

        if (feyloomModel != null)
        {
            // Instantiate the model as a child
            GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(feyloomModel);
            modelInstance.name = "Model";
            modelInstance.transform.SetParent(necromancer.transform);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;

            Debug.Log("Using Feyloom Skeleton Necromancer model");
        }
        else
        {
            // Create placeholder visual (purple capsule for necromancer)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(necromancer.transform);
            visual.transform.localPosition = new Vector3(0, 1f, 0);
            visual.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            var renderer = visual.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.4f, 0.1f, 0.5f); // Purple
                renderer.sharedMaterial = mat;
            }

            Debug.LogWarning("Feyloom model not found, using placeholder visual");
        }

        // Add components
        necromancer.AddComponent<MonsterHealth>();
        necromancer.AddComponent<NecromancerAI>();

        // Add collider
        CapsuleCollider collider = necromancer.AddComponent<CapsuleCollider>();
        collider.height = 2f;
        collider.radius = 0.5f;
        collider.center = new Vector3(0, 1f, 0);

        // Add Rigidbody
        Rigidbody rb = necromancer.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Set layer
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        necromancer.layer = enemyLayer != -1 ? enemyLayer : 0;

        // Save as prefab
        string path = "Assets/_Project/Prefabs/Enemies/SkeletonNecromancer.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(necromancer, path);
        Object.DestroyImmediate(necromancer);

        Debug.Log("Created SkeletonNecromancer prefab at " + path);
        return prefab;
    }

    static GameObject CreateNecromancerProjectilePrefab()
    {
        GameObject projectile = new GameObject("NecromancerProjectile");

        // Create visual (small dark sphere)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Visual";
        visual.transform.SetParent(projectile.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        Object.DestroyImmediate(visual.GetComponent<Collider>());

        var renderer = visual.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.2f, 0f, 0.3f); // Dark purple
            mat.SetFloat("_Smoothness", 0.8f);
            renderer.sharedMaterial = mat;
        }

        // Add sphere collider as trigger
        SphereCollider collider = projectile.AddComponent<SphereCollider>();
        collider.radius = 0.15f;
        collider.isTrigger = true;

        // Add Rigidbody (kinematic for projectile movement)
        Rigidbody rb = projectile.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        // Add projectile script if it exists
        System.Type projectileType = System.Type.GetType("VoxelRPG.Combat.Projectile, Assembly-CSharp");
        if (projectileType != null)
        {
            projectile.AddComponent(projectileType);
        }
        else
        {
            Debug.LogWarning("Projectile script not found - you may need to add it manually");
        }

        // Save as prefab
        string path = "Assets/_Project/Prefabs/Effects/NecromancerProjectile.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectile, path);
        Object.DestroyImmediate(projectile);

        Debug.Log("Created NecromancerProjectile prefab at " + path);
        return prefab;
    }

    static void ConfigureMonsterSpawner()
    {
        // Find MonsterSpawner in scene
        MonsterSpawner spawner = Object.FindFirstObjectByType<MonsterSpawner>();

        if (spawner == null)
        {
            Debug.LogWarning("MonsterSpawner not found in scene - skipping spawner configuration");
            return;
        }

        // Load monster definitions
        MonsterDefinition zombieDef = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
            "Assets/_Project/ScriptableObjects/Monsters/Zombie.asset");
        MonsterDefinition necromancerDef = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
            "Assets/_Project/ScriptableObjects/Monsters/SkeletonNecromancer.asset");

        // Build list of available definitions
        var definitions = new System.Collections.Generic.List<MonsterDefinition>();
        if (zombieDef != null) definitions.Add(zombieDef);
        if (necromancerDef != null) definitions.Add(necromancerDef);

        if (definitions.Count == 0)
        {
            Debug.LogError("No monster definitions found!");
            return;
        }

        // Set the _monsterTypes array using reflection
        var field = typeof(MonsterSpawner).GetField("_monsterTypes",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            field.SetValue(spawner, definitions.ToArray());
            EditorUtility.SetDirty(spawner);
            Debug.Log($"Configured MonsterSpawner with {definitions.Count} monster type(s)");
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
            Debug.LogWarning("SkeletonNecromancer prefab not found - skipping projectile configuration");
            return;
        }

        // Load the projectile prefab
        GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Project/Prefabs/Effects/NecromancerProjectile.prefab");

        if (projectilePrefab == null)
        {
            Debug.LogWarning("NecromancerProjectile prefab not found - skipping projectile configuration");
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
                Debug.LogWarning("Could not find _projectilePrefab field on NecromancerAI");
            }
        }
        else
        {
            Debug.LogWarning("NecromancerAI component not found on SkeletonNecromancer prefab");
        }

        // Unload the prefab
        PrefabUtility.UnloadPrefabContents(prefabInstance);
    }
}
