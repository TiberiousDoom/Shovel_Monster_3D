using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

/// <summary>
/// Editor tool to fix the SkeletonNecromancer prefab.
/// Removes NavMeshAgent, sets up proper Animator, and configures collider.
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

        // 4. Find and setup Animator from model
        Animator animator = prefabInstance.GetComponent<Animator>();
        Transform modelTransform = prefabInstance.transform.Find("Model");

        if (modelTransform != null)
        {
            // Check if model has an animator with controller
            Animator modelAnimator = modelTransform.GetComponentInChildren<Animator>();
            if (modelAnimator != null)
            {
                // Copy the avatar from the model
                if (animator == null)
                {
                    animator = prefabInstance.AddComponent<Animator>();
                }
                animator.avatar = modelAnimator.avatar;
                animator.applyRootMotion = false;

                if (modelAnimator.runtimeAnimatorController != null)
                {
                    animator.runtimeAnimatorController = modelAnimator.runtimeAnimatorController;
                    Debug.Log($"Copied Animator from model: {modelAnimator.runtimeAnimatorController.name}");
                }
                else
                {
                    // Try to create or load a basic animator controller
                    string controllerPath = "Assets/_Project/Animations/NecromancerController.controller";
                    AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

                    if (controller == null)
                    {
                        Debug.LogWarning("No AnimatorController found. Creating a placeholder...\n" +
                            "To add animations:\n" +
                            "1. Open Window > Animation > Animator\n" +
                            "2. Select the Necromancer prefab\n" +
                            "3. Create states for: Idle, Walk, Attack, Summon, Death\n" +
                            "4. Add animation clips from the Feyloom FBX or create your own");

                        // Create the Animations folder if it doesn't exist
                        if (!Directory.Exists("Assets/_Project/Animations"))
                        {
                            Directory.CreateDirectory("Assets/_Project/Animations");
                        }

                        // Create a basic controller with empty states
                        controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

                        // Add parameters that match NecromancerAI's animation triggers
                        controller.AddParameter("Idle", AnimatorControllerParameterType.Trigger);
                        controller.AddParameter("Walk", AnimatorControllerParameterType.Trigger);
                        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
                        controller.AddParameter("Summon", AnimatorControllerParameterType.Trigger);
                        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);

                        Debug.Log($"Created placeholder AnimatorController at: {controllerPath}");
                    }

                    animator.runtimeAnimatorController = controller;
                }
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

        // Try to find the Feyloom model (check multiple possible paths)
        string[] possiblePaths = new[]
        {
            "Assets/Feyloom/Skeleton_Necromancer/Renders/URP/Prefab/SKM_Skeleton_Necromancer.prefab",
            "Assets/Feyloom/Skeleton_Necromancer/Prefab/SKM_Skeleton_Necromancer.prefab",
            "Assets/Feyloom/Skeleton Necromancer/Renders/URP/Prefab/SKM_Skeleton_Necromancer.prefab"
        };

        GameObject feyloomModel = null;
        foreach (var path in possiblePaths)
        {
            feyloomModel = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (feyloomModel != null)
            {
                Debug.Log($"Found Feyloom model at: {path}");
                break;
            }
        }

        if (feyloomModel == null)
        {
            Debug.LogError("Feyloom SKM_Skeleton_Necromancer prefab not found! Searched paths:\n" +
                string.Join("\n", possiblePaths));
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
}
