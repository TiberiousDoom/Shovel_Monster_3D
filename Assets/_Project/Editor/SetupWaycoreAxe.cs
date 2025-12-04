using UnityEngine;
using UnityEditor;
using VoxelRPG.Combat;
using VoxelRPG.Core.Items;

public class SetupWaycoreAxe : EditorWindow
{
    [MenuItem("Tools/Setup Waycore Axe Weapon")]
    static void Setup()
    {
        Debug.Log("Setting up Waycore Axe as Iron Axe weapon...");
        
        // Find the axe model
        GameObject axeModel = FindAxeModel();
        
        if (axeModel == null)
        {
            Debug.LogError("Could not find one_handed_axe_1 model!");
            return;
        }
        
        // Create the equipped weapon prefab
        GameObject equippedAxe = CreateEquippedAxePrefab(axeModel);
        
        if (equippedAxe == null)
        {
            Debug.LogError("Failed to create equipped axe prefab!");
            return;
        }
        
        // Create the ItemDefinition
        CreateAxeItemDefinition(equippedAxe);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("Waycore Axe setup complete!");
    }
    
    static GameObject FindAxeModel()
    {
        // Search for the axe model in common locations
        string[] searchPaths = new string[]
        {
            "Assets/Waycore Studio/Low Poly Fantasy Weapons/Axes/Models/one_handed_axe_1.fbx",
            "Assets/Waycore Studio/Low Poly Fantasy Weapons/Axes/Models/one_handed_axe_1.prefab",
            "Assets/Low Poly Fantasy Weapons/Axes/Models/one_handed_axe_1.fbx",
        };
        
        foreach (string path in searchPaths)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model != null)
            {
                Debug.Log($"Found axe model at: {path}");
                return model;
            }
        }
        
        // Try to find it by searching all assets
        string[] guids = AssetDatabase.FindAssets("one_handed_axe_1 t:GameObject");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Waycore") || path.Contains("Axes"))
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model != null)
                {
                    Debug.Log($"Found axe model at: {path}");
                    return model;
                }
            }
        }
        
        return null;
    }
    
    static GameObject CreateEquippedAxePrefab(GameObject axeModel)
    {
        // Create the root GameObject
        GameObject equippedAxe = new GameObject("IronAxe_Equipped");
        
        // Instantiate the model as a child
        GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(axeModel, equippedAxe.transform);
        modelInstance.name = "AxeModel";
        
        // Position and rotate the axe for holding
        // Typically axe handle should point down/back, blade forward
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.Euler(0, 0, 0);
        modelInstance.transform.localScale = Vector3.one;
        
        // Add MeleeWeapon component
        MeleeWeapon meleeWeapon = equippedAxe.AddComponent<MeleeWeapon>();
        
        // Use SerializedObject to set private fields
        SerializedObject so = new SerializedObject(meleeWeapon);
        
        SerializedProperty damageProp = so.FindProperty("damage") ?? so.FindProperty("_damage");
        if (damageProp != null) damageProp.floatValue = 25f;
        
        SerializedProperty durationProp = so.FindProperty("attackDuration") ?? so.FindProperty("_attackDuration");
        if (durationProp != null) durationProp.floatValue = 0.5f;
        
        SerializedProperty cooldownProp = so.FindProperty("attackCooldown") ?? so.FindProperty("_attackCooldown");
        if (cooldownProp != null) cooldownProp.floatValue = 1.0f;
        
        so.ApplyModifiedProperties();
        
        Debug.Log("Added MeleeWeapon component with damage=25, duration=0.5s, cooldown=1.0s");
        
        // Create hitbox
        GameObject hitboxObj = new GameObject("Hitbox");
        hitboxObj.transform.SetParent(equippedAxe.transform);
        
        // Position hitbox at the blade (adjust based on axe orientation)
        // For most axes, the blade is forward and up from center
        hitboxObj.transform.localPosition = new Vector3(0, 0.2f, 0.3f);
        hitboxObj.transform.localRotation = Quaternion.identity;
        
        // Add BoxCollider to hitbox FIRST (Hitbox requires a collider)
        BoxCollider hitboxCollider = hitboxObj.AddComponent<BoxCollider>();
        hitboxCollider.isTrigger = true;
        hitboxCollider.size = new Vector3(0.5f, 0.3f, 0.6f);
        
        // Now add Hitbox component
        Hitbox hitbox = hitboxObj.AddComponent<Hitbox>();
        
        SerializedObject hitboxSO = new SerializedObject(hitbox);
        SerializedProperty hitboxDamageProp = hitboxSO.FindProperty("damage") ?? hitboxSO.FindProperty("_damage");
        if (hitboxDamageProp != null) hitboxDamageProp.floatValue = 25f;
        
        SerializedProperty isActiveProp = hitboxSO.FindProperty("isActive") ?? hitboxSO.FindProperty("_isActive");
        if (isActiveProp != null) isActiveProp.boolValue = false;
        
        hitboxSO.ApplyModifiedProperties();
        
        Debug.Log("Created hitbox at blade position");
        
        // Save as prefab
        string prefabPath = "Assets/_Project/Prefabs/Weapons/IronAxe_Equipped.prefab";
        
        // Create directory if it doesn't exist
        string directory = System.IO.Path.GetDirectoryName(prefabPath);
        if (!AssetDatabase.IsValidFolder(directory))
        {
            CreateDirectoryRecursive(directory);
        }
        
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(equippedAxe, prefabPath);
        Object.DestroyImmediate(equippedAxe);
        
        Debug.Log($"Created prefab at: {prefabPath}");
        return prefab;
    }
    
    static void CreateAxeItemDefinition(GameObject equippedPrefab)
    {
        // Create ItemDefinition
        ItemDefinition axeItem = ScriptableObject.CreateInstance<ItemDefinition>();
        
        SerializedObject so = new SerializedObject(axeItem);
        
        SerializedProperty nameProp = so.FindProperty("itemName") ?? so.FindProperty("_itemName");
        if (nameProp != null) nameProp.stringValue = "Iron Axe";
        
        SerializedProperty typeProp = so.FindProperty("itemType") ?? so.FindProperty("_itemType");
        if (typeProp != null)
        {
            // Try to set to Weapon enum value
            // ItemType.Weapon is typically value 3 or 4, but let's try to find it
            System.Type itemTypeEnum = System.Type.GetType("VoxelRPG.Core.Items.ItemType");
            if (itemTypeEnum != null)
            {
                var weaponValue = System.Enum.Parse(itemTypeEnum, "Weapon");
                typeProp.enumValueIndex = (int)weaponValue;
            }
        }
        
        SerializedProperty stackProp = so.FindProperty("maxStackSize") ?? so.FindProperty("_maxStackSize");
        if (stackProp != null) stackProp.intValue = 1;
        
        SerializedProperty usableProp = so.FindProperty("isUsable") ?? so.FindProperty("_isUsable");
        if (usableProp != null) usableProp.boolValue = true;
        
        // Try to link the equipped prefab
        SerializedProperty prefabProp = so.FindProperty("equippedPrefab") ?? so.FindProperty("_equippedPrefab");
        if (prefabProp != null) prefabProp.objectReferenceValue = equippedPrefab;
        
        so.ApplyModifiedProperties();
        
        // Save the asset
        string assetPath = "Assets/_Project/Resources/Items/IronAxe.asset";
        
        // Create directory if needed
        string directory = System.IO.Path.GetDirectoryName(assetPath);
        if (!AssetDatabase.IsValidFolder(directory))
        {
            CreateDirectoryRecursive(directory);
        }
        
        AssetDatabase.CreateAsset(axeItem, assetPath);
        Debug.Log($"Created ItemDefinition at: {assetPath}");
    }
    
    static void CreateDirectoryRecursive(string path)
    {
        path = path.Replace("\\", "/");
        string[] parts = path.Split('/');
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
}
