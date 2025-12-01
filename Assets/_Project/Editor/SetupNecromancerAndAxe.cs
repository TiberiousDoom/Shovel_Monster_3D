using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using System.IO;
using VoxelRPG.Combat;
using VoxelRPG.Core.Items;

public class SetupNecromancerAndAxe : EditorWindow
{
    [MenuItem("Tools/Setup Necromancer and Axe")]
    static void Setup()
    {
        // Create directories if they don't exist
        CreateDirectoryIfNeeded("Assets/_Project/ScriptableObjects/Monsters");
        CreateDirectoryIfNeeded("Assets/_Project/ScriptableObjects/Items");
        CreateDirectoryIfNeeded("Assets/_Project/Prefabs/Enemies");
        CreateDirectoryIfNeeded("Assets/_Project/Prefabs/Items");
        CreateDirectoryIfNeeded("Assets/_Project/Prefabs/Effects");

        // Create Monster Definition
        CreateNecromancerDefinition();

        // Create Axe Item Definition
        CreateAxeDefinition();

        // Create Prefabs
        CreateNecromancerPrefab();
        CreateAxePrefab();
        CreateProjectilePrefab();

        // Link prefabs to definitions
        LinkPrefabsToDefinitions();

        // Configure MonsterSpawner in scene
        ConfigureMonsterSpawner();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Skeleton Necromancer and Axe setup complete!");
    }

    [MenuItem("Tools/Configure Monster Spawner")]
    static void ConfigureMonsterSpawnerMenu()
    {
        ConfigureMonsterSpawner();
    }

    static void CreateDirectoryIfNeeded(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parentFolder = Path.GetDirectoryName(path).Replace("\\", "/");
            string newFolderName = Path.GetFileName(path);

            if (!AssetDatabase.IsValidFolder(parentFolder))
            {
                string[] parts = parentFolder.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    current = next;
                }
            }

            AssetDatabase.CreateFolder(parentFolder, newFolderName);
        }
    }

    static void CreateNecromancerDefinition()
    {
        string path = "Assets/_Project/ScriptableObjects/Monsters/SkeletonNecromancer.asset";

        // Check if already exists
        if (AssetDatabase.LoadAssetAtPath<MonsterDefinition>(path) != null)
        {
            Debug.Log("Skeleton Necromancer MonsterDefinition already exists");
            return;
        }

        MonsterDefinition necromancer = ScriptableObject.CreateInstance<MonsterDefinition>();

        // Use SerializedObject to set private serialized fields
        SerializedObject so = new SerializedObject(necromancer);
        so.FindProperty("_id").stringValue = "skeleton_necromancer";
        so.FindProperty("_displayName").stringValue = "Skeleton Necromancer";
        so.FindProperty("_description").stringValue = "A skeletal mage that raises the dead to fight for it.";
        so.FindProperty("_maxHealth").floatValue = 80f;
        so.FindProperty("_attackDamage").floatValue = 12f;
        so.FindProperty("_attackCooldown").floatValue = 2f;
        so.FindProperty("_attackRange").floatValue = 10f;
        so.FindProperty("_wanderSpeed").floatValue = 1.5f;
        so.FindProperty("_chaseSpeed").floatValue = 3f;
        so.FindProperty("_detectionRange").floatValue = 20f;
        so.FindProperty("_loseTargetRange").floatValue = 30f;
        so.FindProperty("_aggression").floatValue = 0.9f;
        so.FindProperty("_fleesAtLowHealth").boolValue = true;
        so.FindProperty("_fleeHealthThreshold").floatValue = 0.3f;
        so.FindProperty("_nightOnly").boolValue = true;
        so.FindProperty("_burnsInDaylight").boolValue = true;
        so.FindProperty("_spawnWeight").floatValue = 0.5f;
        so.FindProperty("_minGroupSize").intValue = 1;
        so.FindProperty("_maxGroupSize").intValue = 1;
        so.FindProperty("_experienceValue").intValue = 50;
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(necromancer, path);
        Debug.Log("Created Skeleton Necromancer MonsterDefinition");
    }

    static void CreateAxeDefinition()
    {
        string path = "Assets/_Project/ScriptableObjects/Items/IronAxe.asset";

        // Check if already exists
        if (AssetDatabase.LoadAssetAtPath<ItemDefinition>(path) != null)
        {
            Debug.Log("Iron Axe ItemDefinition already exists");
            return;
        }

        ItemDefinition axe = ScriptableObject.CreateInstance<ItemDefinition>();

        // Use SerializedObject to set private serialized fields
        SerializedObject so = new SerializedObject(axe);
        so.FindProperty("_id").stringValue = "iron_axe";
        so.FindProperty("_displayName").stringValue = "Iron Axe";
        so.FindProperty("_description").stringValue = "A sturdy axe for chopping wood and enemies.";
        so.FindProperty("_maxStackSize").intValue = 1;
        so.FindProperty("_category").enumValueIndex = (int)ItemCategory.Weapon;
        so.FindProperty("_isConsumable").boolValue = false;
        so.FindProperty("_isEquippable").boolValue = true;
        so.FindProperty("_isPlaceable").boolValue = false;
        so.FindProperty("_baseValue").intValue = 20;
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(axe, path);
        Debug.Log("Created Iron Axe ItemDefinition");
    }

    static void CreateNecromancerPrefab()
    {
        string path = "Assets/_Project/Prefabs/Enemies/SkeletonNecromancer.prefab";

        // Check if already exists
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log("Skeleton Necromancer Prefab already exists");
            return;
        }

        GameObject necromancer = new GameObject("SkeletonNecromancer");

        // Add NavMeshAgent
        NavMeshAgent agent = necromancer.AddComponent<NavMeshAgent>();
        agent.speed = 3f;
        agent.angularSpeed = 120f;
        agent.stoppingDistance = 1f;

        // Add CapsuleCollider
        CapsuleCollider collider = necromancer.AddComponent<CapsuleCollider>();
        collider.height = 2f;
        collider.radius = 0.5f;
        collider.center = new Vector3(0, 1f, 0);

        // Add MonsterHealth
        necromancer.AddComponent<MonsterHealth>();

        // Add NecromancerAI
        NecromancerAI ai = necromancer.AddComponent<NecromancerAI>();

        // Set the definition reference via SerializedObject
        MonsterDefinition def = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
            "Assets/_Project/ScriptableObjects/Monsters/SkeletonNecromancer.asset");
        if (def != null)
        {
            SerializedObject so = new SerializedObject(ai);
            so.FindProperty("_definition").objectReferenceValue = def;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Add Animator
        necromancer.AddComponent<Animator>();

        // Create attack point
        GameObject attackPoint = new GameObject("AttackPoint");
        attackPoint.transform.SetParent(necromancer.transform);
        attackPoint.transform.localPosition = new Vector3(0.5f, 1.2f, 0.5f);

        // Create summon point
        GameObject summonPoint = new GameObject("SummonPoint");
        summonPoint.transform.SetParent(necromancer.transform);
        summonPoint.transform.localPosition = new Vector3(0, 0, 2f);

        // Create visual representation (placeholder)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual_Placeholder";
        visual.transform.SetParent(necromancer.transform);
        visual.transform.localPosition = new Vector3(0, 1f, 0);
        visual.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        Object.DestroyImmediate(visual.GetComponent<Collider>());

        // Set color to dark purple for necromancer theme
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.3f, 0.1f, 0.4f);
        visual.GetComponent<Renderer>().sharedMaterial = mat;

        // Save as prefab
        PrefabUtility.SaveAsPrefabAsset(necromancer, path);
        Object.DestroyImmediate(necromancer);
        Debug.Log("Created Skeleton Necromancer Prefab");
    }

    static void CreateAxePrefab()
    {
        string path = "Assets/_Project/Prefabs/Items/IronAxe_Equipped.prefab";

        // Check if already exists
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log("Iron Axe Prefab already exists");
            return;
        }

        GameObject axe = new GameObject("IronAxe_Equipped");

        // Add MeleeWeapon component
        MeleeWeapon meleeWeapon = axe.AddComponent<MeleeWeapon>();

        // Configure via SerializedObject
        SerializedObject so = new SerializedObject(meleeWeapon);
        so.FindProperty("_baseDamage").floatValue = 15f;
        so.FindProperty("_attackCooldown").floatValue = 0.8f;
        so.FindProperty("_attackRange").floatValue = 2f;
        so.FindProperty("_knockbackForce").floatValue = 5f;
        so.FindProperty("_hitboxActivateDelay").floatValue = 0.1f;
        so.FindProperty("_hitboxActiveDuration").floatValue = 0.2f;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Create visual (placeholder axe head)
        GameObject axeHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
        axeHead.name = "AxeHead";
        axeHead.transform.SetParent(axe.transform);
        axeHead.transform.localPosition = new Vector3(0, 0, 0.3f);
        axeHead.transform.localScale = new Vector3(0.3f, 0.15f, 0.4f);
        Object.DestroyImmediate(axeHead.GetComponent<Collider>());

        // Set material
        Material axeMat = new Material(Shader.Find("Standard"));
        axeMat.color = new Color(0.5f, 0.5f, 0.5f); // Gray for iron
        axeHead.GetComponent<Renderer>().sharedMaterial = axeMat;

        // Create handle
        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "Handle";
        handle.transform.SetParent(axe.transform);
        handle.transform.localPosition = new Vector3(0, 0, -0.3f);
        handle.transform.localScale = new Vector3(0.05f, 0.4f, 0.05f);
        handle.transform.localRotation = Quaternion.Euler(90, 0, 0);
        Object.DestroyImmediate(handle.GetComponent<Collider>());

        Material handleMat = new Material(Shader.Find("Standard"));
        handleMat.color = new Color(0.4f, 0.25f, 0.1f); // Brown for wood
        handle.GetComponent<Renderer>().sharedMaterial = handleMat;

        // Create hitbox child
        GameObject hitboxObj = new GameObject("Hitbox");
        hitboxObj.transform.SetParent(axe.transform);
        hitboxObj.transform.localPosition = new Vector3(0, 0, 0.3f);

        Hitbox hitbox = hitboxObj.AddComponent<Hitbox>();

        // Configure hitbox
        SerializedObject hitboxSo = new SerializedObject(hitbox);
        hitboxSo.FindProperty("_baseDamage").floatValue = 15f;
        hitboxSo.FindProperty("_knockbackForce").floatValue = 5f;
        hitboxSo.ApplyModifiedPropertiesWithoutUndo();

        BoxCollider hitboxCollider = hitboxObj.AddComponent<BoxCollider>();
        hitboxCollider.isTrigger = true;
        hitboxCollider.size = new Vector3(0.5f, 0.3f, 0.6f);

        // Link hitbox to MeleeWeapon
        so = new SerializedObject(meleeWeapon);
        so.FindProperty("_hitbox").objectReferenceValue = hitbox;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Save as prefab
        PrefabUtility.SaveAsPrefabAsset(axe, path);
        Object.DestroyImmediate(axe);
        Debug.Log("Created Iron Axe Prefab");
    }

    static void CreateProjectilePrefab()
    {
        string path = "Assets/_Project/Prefabs/Effects/NecromancerProjectile.prefab";

        // Check if already exists
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log("Necromancer Projectile Prefab already exists");
            return;
        }

        GameObject projectile = new GameObject("NecromancerProjectile");

        // Add Rigidbody
        Rigidbody rb = projectile.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Add Collider
        SphereCollider collider = projectile.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.3f;

        // Add NecromancerProjectile script
        NecromancerProjectile projScript = projectile.AddComponent<NecromancerProjectile>();

        // Configure via SerializedObject
        SerializedObject so = new SerializedObject(projScript);
        so.FindProperty("_damage").floatValue = 12f;
        so.FindProperty("_speed").floatValue = 12f;
        so.FindProperty("_homing").boolValue = true;
        so.FindProperty("_homingStrength").floatValue = 2f;
        so.FindProperty("_maxLifetime").floatValue = 5f;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Create visual (glowing sphere)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Visual";
        visual.transform.SetParent(projectile.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one * 0.4f;
        Object.DestroyImmediate(visual.GetComponent<Collider>());

        // Make it glow (purple/green for necromancer theme)
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.4f, 1f, 0.4f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.3f, 0.8f, 0.3f) * 2f);
        visual.GetComponent<Renderer>().sharedMaterial = mat;

        // Add point light
        GameObject lightObj = new GameObject("Light");
        lightObj.transform.SetParent(projectile.transform);
        lightObj.transform.localPosition = Vector3.zero;
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.4f, 1f, 0.4f);
        light.range = 3f;
        light.intensity = 2f;

        // Save as prefab
        PrefabUtility.SaveAsPrefabAsset(projectile, path);
        Object.DestroyImmediate(projectile);
        Debug.Log("Created Necromancer Projectile Prefab");
    }

    static void LinkPrefabsToDefinitions()
    {
        // Link Necromancer prefab to definition
        MonsterDefinition necroDef = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
            "Assets/_Project/ScriptableObjects/Monsters/SkeletonNecromancer.asset");
        GameObject necroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Project/Prefabs/Enemies/SkeletonNecromancer.prefab");

        if (necroDef != null && necroPrefab != null)
        {
            SerializedObject so = new SerializedObject(necroDef);
            so.FindProperty("_prefab").objectReferenceValue = necroPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(necroDef);
            Debug.Log("Linked SkeletonNecromancer prefab to definition");
        }

        // Link Zombie prefab to definition (if exists)
        MonsterDefinition zombieDef = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
            "Assets/_Project/ScriptableObjects/Monsters/Zombie.asset");
        GameObject zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Project/Prefabs/Enemies/Zombie.prefab");

        if (zombieDef != null && zombiePrefab != null)
        {
            SerializedObject so = new SerializedObject(zombieDef);
            so.FindProperty("_prefab").objectReferenceValue = zombiePrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(zombieDef);
            Debug.Log("Linked Zombie prefab to definition");
        }
        else if (zombieDef != null && zombiePrefab == null)
        {
            // Create a basic Zombie prefab if it doesn't exist
            CreateZombiePrefab();
            zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Enemies/Zombie.prefab");
            if (zombiePrefab != null)
            {
                SerializedObject so = new SerializedObject(zombieDef);
                so.FindProperty("_prefab").objectReferenceValue = zombiePrefab;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(zombieDef);
                Debug.Log("Created and linked Zombie prefab to definition");
            }
        }

        // Link projectile to NecromancerAI in prefab
        if (necroPrefab != null)
        {
            GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Effects/NecromancerProjectile.prefab");

            if (projectilePrefab != null)
            {
                // Load prefab for editing
                string prefabPath = AssetDatabase.GetAssetPath(necroPrefab);
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

                NecromancerAI ai = prefabRoot.GetComponent<NecromancerAI>();
                if (ai != null)
                {
                    SerializedObject so = new SerializedObject(ai);
                    so.FindProperty("_projectilePrefab").objectReferenceValue = projectilePrefab;

                    // Also link attack point and summon point
                    Transform attackPoint = prefabRoot.transform.Find("AttackPoint");
                    Transform summonPoint = prefabRoot.transform.Find("SummonPoint");

                    if (attackPoint != null)
                        so.FindProperty("_attackPoint").objectReferenceValue = attackPoint;
                    if (summonPoint != null)
                        so.FindProperty("_summonPoint").objectReferenceValue = summonPoint;

                    so.ApplyModifiedPropertiesWithoutUndo();

                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                    Debug.Log("Linked projectile prefab to NecromancerAI");
                }

                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
    }

    static void CreateZombiePrefab()
    {
        string path = "Assets/_Project/Prefabs/Enemies/Zombie.prefab";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            return;
        }

        GameObject zombie = new GameObject("Zombie");

        // Add NavMeshAgent
        NavMeshAgent agent = zombie.AddComponent<NavMeshAgent>();
        agent.speed = 4f;
        agent.angularSpeed = 120f;
        agent.stoppingDistance = 1.5f;

        // Add CapsuleCollider
        CapsuleCollider collider = zombie.AddComponent<CapsuleCollider>();
        collider.height = 2f;
        collider.radius = 0.5f;
        collider.center = new Vector3(0, 1f, 0);

        // Add MonsterHealth
        zombie.AddComponent<MonsterHealth>();

        // Add BasicMonsterAI
        BasicMonsterAI ai = zombie.AddComponent<BasicMonsterAI>();

        // Set the definition reference
        MonsterDefinition def = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
            "Assets/_Project/ScriptableObjects/Monsters/Zombie.asset");
        if (def != null)
        {
            SerializedObject so = new SerializedObject(ai);
            so.FindProperty("_definition").objectReferenceValue = def;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Add Animator
        zombie.AddComponent<Animator>();

        // Create attack point
        GameObject attackPoint = new GameObject("AttackPoint");
        attackPoint.transform.SetParent(zombie.transform);
        attackPoint.transform.localPosition = new Vector3(0, 1f, 0.8f);

        // Create visual representation (placeholder)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual_Placeholder";
        visual.transform.SetParent(zombie.transform);
        visual.transform.localPosition = new Vector3(0, 1f, 0);
        visual.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        Object.DestroyImmediate(visual.GetComponent<Collider>());

        // Set color to greenish for zombie
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.3f, 0.5f, 0.3f);
        visual.GetComponent<Renderer>().sharedMaterial = mat;

        // Save as prefab
        PrefabUtility.SaveAsPrefabAsset(zombie, path);
        Object.DestroyImmediate(zombie);
        Debug.Log("Created Zombie Prefab");
    }

    static void ConfigureMonsterSpawner()
    {
        // Find MonsterSpawner in scene
        MonsterSpawner spawner = Object.FindObjectOfType<MonsterSpawner>();

        if (spawner == null)
        {
            Debug.LogWarning("No MonsterSpawner found in scene! Please add one to a GameObject.");
            return;
        }

        // Load all monster definitions
        MonsterDefinition zombieDef = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
            "Assets/_Project/ScriptableObjects/Monsters/Zombie.asset");
        MonsterDefinition necroDef = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
            "Assets/_Project/ScriptableObjects/Monsters/SkeletonNecromancer.asset");

        // Build list of valid definitions
        var definitions = new System.Collections.Generic.List<MonsterDefinition>();
        if (zombieDef != null) definitions.Add(zombieDef);
        if (necroDef != null) definitions.Add(necroDef);

        if (definitions.Count == 0)
        {
            Debug.LogWarning("No monster definitions found!");
            return;
        }

        // Assign to spawner
        SerializedObject so = new SerializedObject(spawner);
        SerializedProperty monsterTypesProperty = so.FindProperty("_monsterTypes");
        monsterTypesProperty.arraySize = definitions.Count;

        for (int i = 0; i < definitions.Count; i++)
        {
            monsterTypesProperty.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(spawner);

        Debug.Log($"Configured MonsterSpawner with {definitions.Count} monster types: " +
                  string.Join(", ", definitions.ConvertAll(d => d.DisplayName)));
    }
}
