using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

/// <summary>
/// Creates an AnimatorController for the Necromancer using Kevin Iglesias Human Animations.
/// </summary>
public class CreateNecromancerAnimator
{
    private const string ControllerPath = "Assets/_Project/Animations/NecromancerController.controller";
    private const string AnimationsFolder = "Assets/Kevin Iglesias/Human Animations/Animations/Male";

    [MenuItem("Tools/Create Necromancer Animator Controller")]
    public static void CreateController()
    {
        // Ensure folder exists
        string folder = Path.GetDirectoryName(ControllerPath);
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }

        // Delete existing controller if present
        if (File.Exists(ControllerPath))
        {
            AssetDatabase.DeleteAsset(ControllerPath);
        }

        // Create controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        // Add parameters - using bools instead of triggers for state-based animation
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsAttacking", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsSummoning", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Trigger);

        // Get the base layer
        AnimatorControllerLayer baseLayer = controller.layers[0];
        AnimatorStateMachine stateMachine = baseLayer.stateMachine;

        // Find animation clips
        AnimationClip idleClip = FindAnimationClip("Idles", "HumanM@Idle01");
        AnimationClip walkClip = FindAnimationClip("Walk", "HumanM@Walk01_Forward");

        if (idleClip == null)
        {
            Debug.LogWarning("Could not find idle animation, will create placeholder");
        }
        if (walkClip == null)
        {
            Debug.LogWarning("Could not find walk animation, will create placeholder");
        }

        // Create states
        AnimatorState idleState = stateMachine.AddState("Idle", new Vector3(300, 0, 0));
        idleState.motion = idleClip;

        AnimatorState walkState = stateMachine.AddState("Walk", new Vector3(300, 100, 0));
        walkState.motion = walkClip;

        AnimatorState attackState = stateMachine.AddState("Attack", new Vector3(500, 50, 0));
        attackState.motion = walkClip; // Use walk as placeholder for attack

        AnimatorState summonState = stateMachine.AddState("Summon", new Vector3(500, 150, 0));
        summonState.motion = idleClip; // Use idle as placeholder for summon

        AnimatorState deathState = stateMachine.AddState("Death", new Vector3(300, 200, 0));
        deathState.motion = null; // No animation - stays in last pose

        // Set default state
        stateMachine.defaultState = idleState;

        // Create transitions
        // Idle -> Walk (when IsMoving becomes true)
        AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0.1f;

        // Walk -> Idle (when IsMoving becomes false)
        AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0.1f;

        // Any State -> Attack
        AnimatorStateTransition anyToAttack = stateMachine.AddAnyStateTransition(attackState);
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
        anyToAttack.hasExitTime = false;
        anyToAttack.duration = 0.1f;

        // Attack -> Idle (after animation)
        AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.9f;
        attackToIdle.duration = 0.1f;

        // Any State -> Summon
        AnimatorStateTransition anyToSummon = stateMachine.AddAnyStateTransition(summonState);
        anyToSummon.AddCondition(AnimatorConditionMode.If, 0, "IsSummoning");
        anyToSummon.hasExitTime = false;
        anyToSummon.duration = 0.1f;

        // Summon -> Idle (after animation)
        AnimatorStateTransition summonToIdle = summonState.AddTransition(idleState);
        summonToIdle.hasExitTime = true;
        summonToIdle.exitTime = 0.9f;
        summonToIdle.duration = 0.1f;

        // Any State -> Death
        AnimatorStateTransition anyToDeath = stateMachine.AddAnyStateTransition(deathState);
        anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
        anyToDeath.hasExitTime = false;
        anyToDeath.duration = 0.1f;

        // Save
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created NecromancerController at {ControllerPath}\n" +
            $"Idle clip: {(idleClip != null ? idleClip.name : "none")}\n" +
            $"Walk clip: {(walkClip != null ? walkClip.name : "none")}\n\n" +
            "NOTE: The NecromancerAI script needs to be updated to use IsMoving bool instead of Walk trigger.");

        // Select the created controller
        Selection.activeObject = controller;
    }

    private static AnimationClip FindAnimationClip(string subFolder, string clipName)
    {
        // Search in multiple locations
        string[] searchPaths = new[]
        {
            $"{AnimationsFolder}/{subFolder}/{clipName}.fbx",
            $"{AnimationsFolder}/Movement/{subFolder}/{clipName}.fbx",
            $"{AnimationsFolder}/Social/Conversation/{clipName}.fbx",
            $"Assets/Kevin Iglesias/Human Animations/Animations/Male/{subFolder}/{clipName}.fbx",
            $"Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/{subFolder}/{clipName}.fbx"
        };

        foreach (string path in searchPaths)
        {
            // Load the FBX and get the animation clip from it
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
                {
                    Debug.Log($"Found animation: {clip.name} at {path}");
                    return clip;
                }
            }
        }

        // Try a broader search
        string[] guids = AssetDatabase.FindAssets($"{clipName} t:AnimationClip",
            new[] { "Assets/Kevin Iglesias" });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (clip != null)
            {
                Debug.Log($"Found animation via search: {clip.name}");
                return clip;
            }
        }

        return null;
    }
}
