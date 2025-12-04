using UnityEngine;
using UnityEditor;
using VoxelRPG.Combat;

public class SetupNecromancerStaff : EditorWindow
{
    [MenuItem("Tools/Setup Necromancer Staff")]
    static void Setup()
    {
        Debug.Log("Setting up Necromancer Staff...");
        
        // Load the necromancer prefab
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Project/Prefabs/Enemies/SkeletonNecromancer.prefab");
        
        if (prefab == null)
        {
            Debug.LogError("SkeletonNecromancer prefab not found at Assets/_Project/Prefabs/Enemies/SkeletonNecromancer.prefab");
            return;
        }
        
        // Open prefab for editing
        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);
        
        try
        {
            // Find the staff (search recursively)
            Transform staff = FindChildRecursive(prefabInstance.transform, "Staff");
            
            if (staff == null)
            {
                Debug.LogWarning("Staff not found in prefab. Looking for it to add...");
                
                // Try to load and add the staff
                GameObject staffFBX = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Feyloom/Skeleton_Necromancer/Mesh/SM_Staff.fbx");
                
                if (staffFBX != null)
                {
                    // Find the right hand bone
                    Transform rightHand = FindChildRecursive(prefabInstance.transform, "Hand") 
                        ?? FindChildRecursive(prefabInstance.transform, "RightHand")
                        ?? FindChildRecursive(prefabInstance.transform, "R_Hand");
                    
                    if (rightHand != null)
                    {
                        GameObject staffInstance = (GameObject)PrefabUtility.InstantiatePrefab(staffFBX, rightHand);
                        staff = staffInstance.transform;
                        Debug.Log($"Added staff to {rightHand.name}");
                    }
                    else
                    {
                        // Just add it to root if we can't find hand
                        GameObject staffInstance = (GameObject)PrefabUtility.InstantiatePrefab(staffFBX, prefabInstance.transform);
                        staff = staffInstance.transform;
                        Debug.LogWarning("Could not find hand bone. Added staff to root. You'll need to position it manually.");
                    }
                }
                else
                {
                    Debug.LogError("Could not find SM_Staff.fbx at Assets/Feyloom/Skeleton_Necromancer/Mesh/SM_Staff.fbx");
                    PrefabUtility.UnloadPrefabContents(prefabInstance);
                    return;
                }
            }
            
            // Find or create ProjectileSpawnPoint
            Transform spawnPoint = staff.Find("ProjectileSpawnPoint");
            
            if (spawnPoint == null)
            {
                GameObject spawnPointObj = new GameObject("ProjectileSpawnPoint");
                spawnPointObj.transform.SetParent(staff);
                spawnPoint = spawnPointObj.transform;
                Debug.Log("Created ProjectileSpawnPoint");
            }
            
            // Position it at the tip of the staff
            // Assuming staff is vertical, tip is at the top
            Renderer staffRenderer = staff.GetComponent<Renderer>();
            if (staffRenderer != null)
            {
                // Position at top of staff bounds
                Bounds bounds = staffRenderer.bounds;
                spawnPoint.position = staff.position + new Vector3(0, bounds.extents.y, 0);
            }
            else
            {
                // Default position - 1 unit above staff
                spawnPoint.localPosition = new Vector3(0, 1f, 0);
                Debug.LogWarning("Could not find staff renderer. Using default spawn point position.");
            }
            
            spawnPoint.localRotation = Quaternion.identity;
            
            Debug.Log($"Positioned ProjectileSpawnPoint at: {spawnPoint.localPosition}");
            
            // Try to link to NecromancerAI
            NecromancerAI necroAI = prefabInstance.GetComponent<NecromancerAI>();
            
            if (necroAI != null)
            {
                SerializedObject so = new SerializedObject(necroAI);
                
                // Try different possible field names
                SerializedProperty spawnPointProp = so.FindProperty("_projectileSpawnPoint")
                    ?? so.FindProperty("projectileSpawnPoint")
                    ?? so.FindProperty("_spawnPoint")
                    ?? so.FindProperty("spawnPoint")
                    ?? so.FindProperty("_attackPoint")
                    ?? so.FindProperty("attackPoint");
                
                if (spawnPointProp != null)
                {
                    spawnPointProp.objectReferenceValue = spawnPoint;
                    so.ApplyModifiedProperties();
                    Debug.Log($"Linked ProjectileSpawnPoint to NecromancerAI.{spawnPointProp.name}");
                }
                else
                {
                    Debug.LogWarning("NecromancerAI does not have a spawn point field. You may need to add one or assign it manually.");
                    Debug.Log("Add this to your NecromancerAI.cs:");
                    Debug.Log("[SerializeField] private Transform _projectileSpawnPoint;");
                }
            }
            else
            {
                Debug.LogError("NecromancerAI component not found on prefab!");
            }
            
            // Save the prefab
            PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
            Debug.Log("Necromancer Staff setup complete!");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabInstance);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    
    static Transform FindChildRecursive(Transform parent, string name)
    {
        // Check immediate children first
        Transform result = parent.Find(name);
        if (result != null) return result;
        
        // Check if any child contains the name
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name))
                return child;
        }
        
        // Search recursively
        foreach (Transform child in parent)
        {
            result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        
        return null;
    }
}
