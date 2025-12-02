using UnityEngine;
using UnityEditor;
using VoxelRPG.Core;

public class TimeControlTools : EditorWindow
{
    [MenuItem("Tools/Set Start Time to Night")]
    static void SetStartTimeToNight()
    {
        // Try to find TimeManager settings
        // This might be a ScriptableObject or a component setting

        Debug.Log("Looking for TimeManager settings...");

        // Search for TimeManager ScriptableObject assets
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject TimeManager");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

            if (asset != null)
            {
                SerializedObject so = new SerializedObject(asset);

                // Try to find start time field
                SerializedProperty startTime = so.FindProperty("_startTime")
                    ?? so.FindProperty("startTime")
                    ?? so.FindProperty("_initialTime")
                    ?? so.FindProperty("initialTime");

                if (startTime != null)
                {
                    // Set to 20:00 (8 PM - nighttime)
                    startTime.floatValue = 20f;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(asset);
                    Debug.Log($"✓ Set start time to 20:00 (night) in {asset.name}");
                    AssetDatabase.SaveAssets();
                    return;
                }
            }
        }

        Debug.LogWarning("Could not find TimeManager settings automatically. Try the runtime skip method.");
    }

    [MenuItem("Tools/Skip to Night (Runtime)")]
    static void SkipToNight()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("This tool only works during Play mode. Press Play first, then run this.");
            return;
        }

        // Try to find TimeManager in the scene
        TimeManager timeManager = Object.FindObjectOfType<TimeManager>();

        if (timeManager != null)
        {
            // Use the public SetTimeOfDay method - 0.75 = sunset/night start
            timeManager.SetTimeOfDay(0.80f); // Just after night starts
            Debug.Log("✓ Skipped to night time (20:00)!");
        }
        else
        {
            Debug.LogError("TimeManager not found in scene");
        }
    }

    [MenuItem("Tools/Skip to Day (Runtime)")]
    static void SkipToDay()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("This tool only works during Play mode. Press Play first, then run this.");
            return;
        }

        TimeManager timeManager = Object.FindObjectOfType<TimeManager>();

        if (timeManager != null)
        {
            // Use the public SetTimeOfDay method - 0.25 = sunrise/day start
            timeManager.SetTimeOfDay(0.30f); // Just after day starts
            Debug.Log("✓ Skipped to day time (07:00)!");
        }
        else
        {
            Debug.LogError("TimeManager not found in scene");
        }
    }

    [MenuItem("Tools/Skip to Next Phase (Runtime)")]
    static void SkipToNextPhase()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("This tool only works during Play mode. Press Play first, then run this.");
            return;
        }

        TimeManager timeManager = Object.FindObjectOfType<TimeManager>();

        if (timeManager != null)
        {
            timeManager.SkipToNextPhase();
            Debug.Log($"✓ Skipped to {timeManager.CurrentPhase} ({timeManager.GetFormattedTime()})");
        }
        else
        {
            Debug.LogError("TimeManager not found in scene");
        }
    }
}

// Runtime component you can add to any GameObject for quick testing
public class SkipToNightOnStart : MonoBehaviour
{
    [SerializeField] private bool _skipToNightOnStart = true;
    [SerializeField] private float _nightTime = 0.80f; // Normalized time (0.75 = sunset)

    void Start()
    {
        if (_skipToNightOnStart)
        {
            TimeManager timeManager = FindObjectOfType<TimeManager>();
            if (timeManager != null)
            {
                timeManager.SetTimeOfDay(_nightTime);
                Debug.Log($"Skipped to time: {timeManager.GetFormattedTime()} (night)");
            }
        }
    }
}
