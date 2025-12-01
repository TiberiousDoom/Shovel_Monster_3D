using UnityEngine;
using UnityEditor;
using System.IO;

public class SetupNecromancerAndAxe : EditorWindow
{
    [MenuItem("Tools/Setup Necromancer and Axe")]
    static void Setup()
    {
        // Create directories if they don't exist
        CreateDirectoryIfNeeded("Assets/_Project/Resources/Monsters");
        CreateDirectoryIfNeeded("Assets/_Project/Resources/Items");
        CreateDirectoryIfNeeded("Assets/_Project/Prefabs/Enemies");
        CreateDirectoryIfNeeded("Assets/_Project/Prefabs/Weapons");
        CreateDirectoryIfNeeded("Assets/_Project/Prefabs/Projectiles");
        CreateDirectoryIfNeeded("Assets/_Project/Animation/Necromancer");

        // Create Monster Definition
        CreateNecromancerDefinition();
        
        // Create Axe Item Definition
        CreateAxeDefinition();
        
        // Create Prefabs
        CreateNecromancerPrefab();
        CreateAxePrefab();
        CreateProjectilePrefab();
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("Skeleton Necromancer and Axe setup complete!");
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
        MonsterDefinition necromancer = ScriptableObject.CreateInstance<MonsterDefinition>();
        
        necromancer.monsterName = "Skeleton Necromancer";
        necromancer.maxHealth = 60f;
        necromancer.moveSpeed = 2.5f;
        necromancer.attackDamage = 15f;
        necromancer.attackRange = 12f;
        necromancer.detectionRange = 15f;
        necromancer.attackCooldown = 2.5f;
        
        AssetDatabase.CreateAsset(necromancer, "Assets/_Project/Resources/Monsters/SkeletonNecromancer.asset");
        Debug.Log("Created Skeleton Necromancer MonsterDefinition");
    }
    
    static void CreateAxeDefinition()
    {
        ItemDefinition axe = ScriptableObject.CreateInstance<ItemDefinition>();
        
        axe.itemName = "Iron Axe";
        axe.itemType = ItemType.Weapon;
        axe.maxStackSize = 1;
        axe.isUsable = true;
        
        AssetDatabase.CreateAsset(axe, "Assets/_Project/Resources/Items/IronAxe.asset");
        Debug.Log("Created Iron Axe ItemDefinition");
    }
    
    static void CreateNecromancerPrefab()
    {
        GameObject necromancer = new GameObject("SkeletonNecromancer");
        
        // Add components
        necromancer.AddComponent<MonsterHealth>();
        necromancer.AddComponent<NecromancerAI>();
        necromancer.AddComponent<Animator>();
        
        // Add a capsule collider
        CapsuleCollider collider = necromancer.AddComponent<CapsuleCollider>();
        collider.height = 2f;
        collider.radius = 0.5f;
        collider.center = new Vector3(0, 1f, 0);
        
        // Add Rigidbody for physics
        Rigidbody rb = necromancer.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        // Create visual representation (placeholder cube)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        visual.transform.SetParent(necromancer.transform);
        visual.transform.localPosition = new Vector3(0, 1f, 0);
        visual.transform.localScale = new Vector3(0.8f, 1.8f, 0.8f);
        DestroyImmediate(visual.GetComponent<Collider>()); // Remove collider from visual
        
        // Set layer
        necromancer.layer = LayerMask.NameToLayer("Enemy");
        if (necromancer.layer == -1) necromancer.layer = 0; // Default layer if Enemy doesn't exist
        
        // Save as prefab
        PrefabUtility.SaveAsPrefabAsset(necromancer, "Assets/_Project/Prefabs/Enemies/SkeletonNecromancer.prefab");
        DestroyImmediate(necromancer);
        Debug.Log("Created Skeleton Necromancer Prefab");
    }
    
    static void CreateAxePrefab()
    {
        GameObject axe = new GameObject("IronAxe_Equipped");
        
        // Add MeleeWeapon component
        MeleeWeapon meleeWeapon = axe.AddComponent<MeleeWeapon>();
        meleeWeapon.damage = 25f;
        meleeWeapon.attackDuration = 0.5f;
        meleeWeapon.attackCooldown = 1.0f;
        
        // Create visual (placeholder cube for axe head)
        GameObject axeHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
        axeHead.name = "AxeHead";
        axeHead.transform.SetParent(axe.transform);
        axeHead.transform.localPosition = new Vector3(0, 0, 0.3f);
        axeHead.transform.localScale = new Vector3(0.3f, 0.15f, 0.4f);
        DestroyImmediate(axeHead.GetComponent<Collider>());
        
        // Create handle
        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "Handle";
        handle.transform.SetParent(axe.transform);
        handle.transform.localPosition = new Vector3(0, 0, -0.3f);
        handle.transform.localScale = new Vector3(0.05f, 0.4f, 0.05f);
        handle.transform.localRotation = Quaternion.Euler(90, 0, 0);
        DestroyImmediate(handle.GetComponent<Collider>());
        
        // Create hitbox
        GameObject hitboxObj = new GameObject("Hitbox");
        hitboxObj.transform.SetParent(axe.transform);
        hitboxObj.transform.localPosition = new Vector3(0, 0, 0.3f);
        
        Hitbox hitbox = hitboxObj.AddComponent<Hitbox>();
        hitbox.damage = 25f;
        hitbox.isActive = false;
        
        BoxCollider hitboxCollider = hitboxObj.AddComponent<BoxCollider>();
        hitboxCollider.isTrigger = true;
        hitboxCollider.size = new Vector3(0.5f, 0.3f, 0.6f);
        
        // Save as prefab
        PrefabUtility.SaveAsPrefabAsset(axe, "Assets/_Project/Prefabs/Weapons/IronAxe_Equipped.prefab");
        DestroyImmediate(axe);
        Debug.Log("Created Iron Axe Prefab");
    }
    
    static void CreateProjectilePrefab()
    {
        GameObject projectile = new GameObject("NecromancerProjectile");
        
        // Add NecromancerProjectile component
        NecromancerProjectile projScript = projectile.AddComponent<NecromancerProjectile>();
        projScript.damage = 15f;
        projScript.moveSpeed = 8f;
        projScript.homingStrength = 0.5f;
        projScript.lifetime = 5f;
        projScript.isHoming = true;
        
        // Add Rigidbody
        Rigidbody rb = projectile.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        // Add Collider
        SphereCollider collider = projectile.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.2f;
        
        // Create visual (glowing sphere)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Visual";
        visual.transform.SetParent(projectile.transform);
        visual.transform.localScale = Vector3.one * 0.3f;
        DestroyImmediate(visual.GetComponent<Collider>());
        
        // Make it glow (green/dark color for necromancer theme)
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.2f, 1f, 0.2f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.2f, 1f, 0.2f) * 2f);
        visual.GetComponent<Renderer>().material = mat;
        
        // Save as prefab
        PrefabUtility.SaveAsPrefabAsset(projectile, "Assets/_Project/Prefabs/Projectiles/NecromancerProjectile.prefab");
        DestroyImmediate(projectile);
        Debug.Log("Created Necromancer Projectile Prefab");
    }
}
