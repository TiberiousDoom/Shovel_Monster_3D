using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

/// <summary>
/// Editor tool to fix the SkeletonNecromancer prefab.
/// Removes NavMeshAgent, sets up proper Animator, and configures collider.
/// Updated to use Built-in render pipeline prefabs.
/// </summary>
public class FixNecromancerPrefab
{
    [MenuItem("Tools/Fix Necromancer Prefab (Complete)")]
    static void FixPrefab()
    {
        string prefabPath = "Assets/_Project/Prefabs/Enemies/SkeletonNecromancer.prefab";
        GameObject necromancerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (necromancerPrefab == null)
        {
            Debug.LogError("SkeletonNecromancer prefab not found!");
            return;
        }

        // Open prefab for editing
        GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);

        // 1. Remove NavMeshAgent if present
        NavMeshAgent navAgent = prefabInstance.GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            Object.DestroyImmediate(navAgent);
            Debug.Log("Removed NavMeshAgent from SkeletonNecromancer");
        }

        // 2. Ensure CapsuleCollider exists and is properly sized
        CapsuleCollider capsule = prefabInstance.GetComponent<CapsuleCollider>();
        if (capsule == null)
        {
            capsule = prefabInstance.AddComponent<CapsuleCollider>();
        }
        capsule.center = new Vector3(0, 1f, 0);
        capsule.radius = 0.4f;
        capsule.height = 2f;
        Debug.Log("Configured CapsuleCollider");

        // 3. Ensure Rigidbody exists for physics
        Rigidbody rb = prefabInstance.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = prefabInstance.AddComponent<Rigidbody>();
        }
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        Debug.Log("Configured Rigidbody");

        // 4. Find and setup Animator
        Animator animator = prefabInstance.GetComponent<Animator>();
        Transform modelTransform = prefabInstance.transform.Find("Model");

        // Always load the NecromancerController (with Kevin Iglesias animations)
        string controllerPath = "Assets/_Project/Animations/NecromancerController.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

        if (controller == null)
        {
            Debug.LogError($"NecromancerController not found at {controllerPath}! Run 'Tools > Create Necromancer Animator Controller' first.");
        }

        if (modelTransform != null)
        {
            // Get avatar from the model's SkinnedMeshRenderer hierarchy
            Animator modelAnimator = modelTransform.GetComponentInChildren<Animator>();
            Avatar avatar = modelAnimator?.avatar;

            // If no animator on model, try to get avatar from the FBX directly
            if (avatar == null)
            {
                // Try loading avatar from Feyloom FBX
                string fbxPath = "Assets/Feyloom/Skeleton_Necromancer/Mesh/SKM_Skeleton_Necromancer.fbx";
                GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                if (fbx != null)
                {
                    Animator fbxAnimator = fbx.GetComponent<Animator>();
                    if (fbxAnimator != null)
                    {
                        avatar = fbxAnimator.avatar;
                        Debug.Log($"Got avatar from FBX: {avatar?.name ?? "null"}");
                    }
                }
            }

            // Ensure we have an animator on the root
            if (animator == null)
            {
                animator = prefabInstance.AddComponent<Animator>();
            }

            animator.avatar = avatar;
            animator.applyRootMotion = false;

            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
                Debug.Log($"Assigned NecromancerController to prefab (avatar: {avatar?.name ?? "null"})");
            }

            // Remove any animator on the child model to avoid conflicts
            if (modelAnimator != null && modelAnimator != animator)
            {
                Object.DestroyImmediate(modelAnimator);
                Debug.Log("Removed duplicate Animator from child Model");
            }
        }
        else
        {
            Debug.LogWarning("No 'Model' child found in prefab. Run 'Fix Necromancer Visual' first.");
        }

        // Save the prefab
        PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabInstance);

        Debug.Log("Successfully fixed SkeletonNecromancer prefab!");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

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

        // Use Built-in render pipeline prefab (not URP - causes pink materials)
        string builtInPath = "Assets/Feyloom/Skeleton_Necromancer/Renders/Built-in/Prefab/SKM_Skeleton_Necromancer.prefab";
        GameObject feyloomModel = AssetDatabase.LoadAssetAtPath<GameObject>(builtInPath);

        if (feyloomModel != null)
        {
            Debug.Log($"Using Built-in prefab: {builtInPath}");
        }
        else
        {
            // Fallback to URP if Built-in not found
            string urpPath = "Assets/Feyloom/Skeleton_Necromancer/Renders/URP/Prefab/SKM_Skeleton_Necromancer.prefab";
            feyloomModel = AssetDatabase.LoadAssetAtPath<GameObject>(urpPath);
            if (feyloomModel != null)
            {
                Debug.LogWarning($"Built-in prefab not found, falling back to URP: {urpPath}");
            }
        }

        if (feyloomModel == null)
        {
            Debug.LogError("Feyloom SKM_Skeleton_Necromancer prefab not found!");
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

    [MenuItem("Tools/Fix Necromancer Staff")]
    static void FixStaff()
    {
        string prefabPath = "Assets/_Project/Prefabs/Enemies/SkeletonNecromancer.prefab";
        GameObject necromancerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (necromancerPrefab == null)
        {
            Debug.LogError("SkeletonNecromancer prefab not found!");
            return;
        }

        // Load the staff prefab - use Built-in
        string staffPrefabPath = "Assets/Feyloom/Skeleton_Necromancer/Renders/Built-in/Prefab/SM_Staff.prefab";
        GameObject staffPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(staffPrefabPath);

        if (staffPrefab == null)
        {
            Debug.LogError($"Staff prefab not found at: {staffPrefabPath}");
            return;
        }

        // Open prefab for editing
        GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);

        // Find the model
        Transform modelTransform = prefabInstance.transform.Find("Model");
        if (modelTransform == null)
        {
            Debug.LogError("No 'Model' child found. Run 'Fix Necromancer Visual' first.");
            PrefabUtility.UnloadPrefabContents(prefabInstance);
            return;
        }

        // Find the right hand bone - common bone names to try
        string[] handBoneNames = new[]
        {
            "Hand_R", "hand_R", "RightHand", "hand.R", "Hand.R",
            "mixamorig:RightHand", "Bip001 R Hand", "R Hand",
            "Right_Hand", "right_hand"
        };

        Transform handBone = null;
        foreach (string boneName in handBoneNames)
        {
            handBone = FindChildRecursive(modelTransform, boneName);
            if (handBone != null)
            {
                Debug.Log($"Found hand bone: {handBone.name}");
                break;
            }
        }

        if (handBone == null)
        {
            // List all bones to help debug
            Debug.LogWarning("Could not find hand bone. Listing all transforms in model:");
            ListAllChildren(modelTransform, 0);
            PrefabUtility.UnloadPrefabContents(prefabInstance);
            return;
        }

        // Remove existing staff if present
        Transform existingStaff = FindChildRecursive(prefabInstance.transform, "Staff");
        if (existingStaff != null)
        {
            Object.DestroyImmediate(existingStaff.gameObject);
            Debug.Log("Removed existing Staff");
        }

        // Also remove any staff that might be in the model
        Transform modelStaff = FindChildRecursive(modelTransform, "SM_Staff");
        if (modelStaff != null)
        {
            Object.DestroyImmediate(modelStaff.gameObject);
            Debug.Log("Removed existing SM_Staff from model");
        }

        // Instantiate the staff and parent to hand
        GameObject staffInstance = (GameObject)PrefabUtility.InstantiatePrefab(staffPrefab, handBone);
        staffInstance.name = "Staff";

        // Position the staff in hand (these values may need adjustment based on the model)
        staffInstance.transform.localPosition = Vector3.zero;
        staffInstance.transform.localRotation = Quaternion.identity;
        staffInstance.transform.localScale = Vector3.one;

        Debug.Log($"Attached staff to {handBone.name}");

        // Save the prefab
        PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabInstance);

        Debug.Log("Successfully added staff to SkeletonNecromancer!");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name || child.name.Contains(name))
            {
                return child;
            }
            Transform found = FindChildRecursive(child, name);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    static void ListAllChildren(Transform parent, int depth)
    {
        string indent = new string(' ', depth * 2);
        Debug.Log($"{indent}{parent.name}");
        foreach (Transform child in parent)
        {
            ListAllChildren(child, depth + 1);
        }
    }
}
