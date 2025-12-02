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
            SerializedObject so = new SerializedObject(timeManager);
            
            SerializedProperty currentTime = so.FindProperty("_currentTime") 
                ?? so.FindProperty("currentTime")
                ?? so.FindProperty("_timeOfDay")
                ?? so.FindProperty("timeOfDay");
            
            if (currentTime != null)
            {
                currentTime.floatValue = 20f; // 8 PM
                so.ApplyModifiedProperties();
                Debug.Log("✓ Skipped to 20:00 (night time)!");
            }
            else
            {
                Debug.LogError("Could not find time field on TimeManager");
            }
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
    [SerializeField] private bool skipToNightOnStart = true;
    [SerializeField] private float nightTime = 20f; // 8 PM
    
    void Start()
    {
        if (skipToNightOnStart)
        {
            TimeManager timeManager = FindObjectOfType<TimeManager>();
            if (timeManager != null)
            {
                // Use reflection to set the time
                var field = typeof(TimeManager).GetField("_currentTime", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?? typeof(TimeManager).GetField("currentTime", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (field != null)
                {
                    field.SetValue(timeManager, nightTime);
                    Debug.Log($"Skipped to time: {nightTime}:00 (night)");
                }
            }
        }
    }
}
