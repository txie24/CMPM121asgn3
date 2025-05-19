using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class ChooseClassManager : MonoBehaviour
{
    // UI elements
    public GameObject classSelectionUI;
    public TextMeshProUGUI titleText;
    public Button mageButton;
    public Button warlockButton;
    public Button battlemageButton;

    // Optional UI elements for displaying class stats
    public TextMeshProUGUI classDescriptionText;

    // Store the selected level name and spawner
    private string selectedLevelName;
    private EnemySpawner spawner;

    // Class data
    private Dictionary<string, JObject> classDefinitions;

    // Static property to hold the selected class name
    public static string SelectedClass { get; private set; } = "mage";

    void Awake()
    {
        // Load class definitions early
        LoadClassDefinitions();
    }

    void Start()
    {
        // Hide UI at startup
        if (classSelectionUI != null)
            classSelectionUI.SetActive(false);

        // Set title
        if (titleText != null)
            titleText.text = "Choose Your Class";

        // Setup button click handlers
        if (mageButton != null)
        {
            mageButton.onClick.AddListener(() => SelectClass("mage"));
            // Optional: add hover listeners to show class description
            AddHoverListeners(mageButton, "mage");
        }

        if (warlockButton != null)
        {
            warlockButton.onClick.AddListener(() => SelectClass("warlock"));
            AddHoverListeners(warlockButton, "warlock");
        }

        if (battlemageButton != null)
        {
            battlemageButton.onClick.AddListener(() => SelectClass("battlemage"));
            AddHoverListeners(battlemageButton, "battlemage");
        }
    }

    // Load class definitions from classes.json
    private void LoadClassDefinitions()
    {
        var classesJson = Resources.Load<TextAsset>("classes");
        if (classesJson == null)
        {
            Debug.LogError("ChooseClassManager: classes.json not found in Resources folder!");
            classDefinitions = new Dictionary<string, JObject>();
            return;
        }

        try
        {
            classDefinitions = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(classesJson.text);
            Debug.Log($"ChooseClassManager: Loaded {classDefinitions.Count} class definitions.");

            // Log class details for debugging
            foreach (var className in classDefinitions.Keys)
            {
                Debug.Log($"Class: {className} - {classDefinitions[className]}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ChooseClassManager: Error parsing classes.json: {e.Message}");
            classDefinitions = new Dictionary<string, JObject>();
        }
    }

    // Add hover listeners to show class descriptions (optional enhancement)
    private void AddHoverListeners(Button button, string className)
    {
        if (classDescriptionText != null)
        {
            var eventTrigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (eventTrigger == null)
                eventTrigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            // Mouse enter event
            var enter = new UnityEngine.EventSystems.EventTrigger.Entry();
            enter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            enter.callback.AddListener((data) => { ShowClassDescription(className); });
            eventTrigger.triggers.Add(enter);

            // Mouse exit event
            var exit = new UnityEngine.EventSystems.EventTrigger.Entry();
            exit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            exit.callback.AddListener((data) => { HideClassDescription(); });
            eventTrigger.triggers.Add(exit);
        }
    }

    // Show description of a class when hovering over its button
    private void ShowClassDescription(string className)
    {
        if (classDescriptionText == null || classDefinitions == null) return;

        if (classDefinitions.TryGetValue(className, out var classData))
        {
            // Create description based on class data
            string desc = $"Class: {className}\n";

            if (classData["health"] != null)
                desc += $"Health: {classData["health"]}\n";

            if (classData["mana"] != null)
                desc += $"Mana: {classData["mana"]}\n";

            if (classData["mana_regeneration"] != null)
                desc += $"Mana Regen: {classData["mana_regeneration"]}\n";

            if (classData["spellpower"] != null)
                desc += $"Spell Power: {classData["spellpower"]}\n";

            if (classData["speed"] != null)
                desc += $"Speed: {classData["speed"]}\n";

            classDescriptionText.text = desc;
        }
        else
        {
            classDescriptionText.text = $"Class: {className}\nNo data available";
        }
    }

    // Hide class description
    private void HideClassDescription()
    {
        if (classDescriptionText != null)
            classDescriptionText.text = "";
    }

    // Called by MenuSelectorController
    public void ShowClassSelection(string levelName, EnemySpawner levelSpawner)
    {
        selectedLevelName = levelName;
        spawner = levelSpawner;

        // Show the class selection UI
        if (classSelectionUI != null)
            classSelectionUI.SetActive(true);
    }

    // Called when a class button is clicked
    private void SelectClass(string className)
    {
        Debug.Log($"Selected class: {className}");

        // Store the selection
        SelectedClass = className;

        // Hide UI
        if (classSelectionUI != null)
            classSelectionUI.SetActive(false);

        // Continue with the original game flow
        if (spawner != null && !string.IsNullOrEmpty(selectedLevelName))
        {
            spawner.StartLevel(selectedLevelName);
        }
    }

    // Get class data for a given class name
    public JObject GetClassData(string className)
    {
        if (classDefinitions != null && classDefinitions.TryGetValue(className, out var classData))
            return classData;

        Debug.LogWarning($"GetClassData: No data found for class '{className}'");
        return null;
    }

    // Calculate class stats for a given wave
    public Dictionary<string, float> GetClassStatsForWave(string className, int wave)
    {
        var result = new Dictionary<string, float>
        {
            { "health", 95 + wave * 5 },
            { "mana", 90 + wave * 10 },
            { "mana_regeneration", 10 + wave },
            { "spellpower", wave * 10 },
            { "speed", 5 }
        };

        if (classDefinitions == null || !classDefinitions.TryGetValue(className, out var classData))
            return result;

        var vars = new Dictionary<string, float> { { "wave", wave } };

        // Override default values with class-specific ones
        if (classData["health"] != null)
            result["health"] = RPNEvaluator.SafeEvaluateFloat(classData["health"].ToString(), vars, result["health"]);

        if (classData["mana"] != null)
            result["mana"] = RPNEvaluator.SafeEvaluateFloat(classData["mana"].ToString(), vars, result["mana"]);

        if (classData["mana_regeneration"] != null)
            result["mana_regeneration"] = RPNEvaluator.SafeEvaluateFloat(classData["mana_regeneration"].ToString(), vars, result["mana_regeneration"]);

        if (classData["spellpower"] != null)
            result["spellpower"] = RPNEvaluator.SafeEvaluateFloat(classData["spellpower"].ToString(), vars, result["spellpower"]);

        if (classData["speed"] != null)
            result["speed"] = RPNEvaluator.SafeEvaluateFloat(classData["speed"].ToString(), vars, result["speed"]);

        return result;
    }
}