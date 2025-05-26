using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RelicUI : MonoBehaviour
{
    [Header("Required References")]
    public GameObject relicSlotPrefab; // Drag RelicSlot prefab here
    public Transform contentParent;    // Drag Content object here

    // Internal tracking
    private List<GameObject> activeRelicSlots = new List<GameObject>();
    private List<string> displayedRelicNames = new List<string>();

    public static RelicUI Instance { get; private set; }

    void Awake()
    {
        Instance = this;

        // Auto-find content parent if not assigned
        if (contentParent == null)
        {
            contentParent = transform.Find("Content");
            if (contentParent == null)
            {
                Debug.LogError("RelicUI: Could not find 'Content' child object!");
            }
        }

        Debug.Log("RelicUI: Initialized");
    }

    void Start()
    {
        // Start with empty display
        ClearAllRelics();
    }

    /// <summary>
    /// Add a new relic icon to the top bar
    /// </summary>
    public void AddRelic(Relic relic)
    {
        if (relic == null)
        {
            Debug.LogError("RelicUI: Attempted to add null relic");
            return;
        }

        // Check if already displayed
        if (displayedRelicNames.Contains(relic.Name))
        {
            Debug.LogWarning($"RelicUI: Relic '{relic.Name}' already displayed");
            return;
        }

        Debug.Log($"RelicUI: Adding relic '{relic.Name}' to top bar");
        CreateRelicSlot(relic);
    }

    /// <summary>
    /// Remove a relic from the display
    /// </summary>
    public void RemoveRelic(string relicName)
    {
        int index = displayedRelicNames.IndexOf(relicName);
        if (index >= 0 && index < activeRelicSlots.Count)
        {
            Debug.Log($"RelicUI: Removing relic '{relicName}' from display");

            if (activeRelicSlots[index] != null)
            {
                Destroy(activeRelicSlots[index]);
            }

            activeRelicSlots.RemoveAt(index);
            displayedRelicNames.RemoveAt(index);
        }
    }

    /// <summary>
    /// Clear all displayed relics
    /// </summary>
    public void ClearAllRelics()
    {
        foreach (var slot in activeRelicSlots)
        {
            if (slot != null)
            {
                Destroy(slot);
            }
        }

        activeRelicSlots.Clear();
        displayedRelicNames.Clear();
        Debug.Log("RelicUI: Cleared all relic displays");
    }

    /// <summary>
    /// Create a visual slot for a relic
    /// </summary>
    private void CreateRelicSlot(Relic relic)
    {
        // Validation
        if (relicSlotPrefab == null)
        {
            Debug.LogError("RelicUI: relicSlotPrefab is not assigned! Drag the RelicSlot prefab to the RelicUI component.");
            return;
        }

        if (contentParent == null)
        {
            Debug.LogError("RelicUI: contentParent is not assigned! Drag the Content object to the RelicUI component.");
            return;
        }

        // Create the slot
        GameObject newSlot = Instantiate(relicSlotPrefab, contentParent);
        newSlot.name = $"RelicSlot_{relic.Name}";

        // Find the Image component - based on your prefab structure, it's directly on the root
        Image iconImage = newSlot.GetComponent<Image>();

        // If not on root, try to find Icon child
        if (iconImage == null)
        {
            Transform iconTransform = newSlot.transform.Find("Icon");
            if (iconTransform != null)
            {
                iconImage = iconTransform.GetComponent<Image>();
            }
        }

        // If still not found, try "icon" (lowercase)
        if (iconImage == null)
        {
            Transform iconTransform = newSlot.transform.Find("icon");
            if (iconTransform != null)
            {
                iconImage = iconTransform.GetComponent<Image>();
            }
        }

        if (iconImage == null)
        {
            Debug.LogError($"RelicUI: Could not find Image component in RelicSlot prefab! Make sure your prefab has an Image component.");
            return;
        }

        Debug.Log($"RelicUI: Found Image component for relic '{relic.Name}'. Attempting to set sprite index {relic.SpriteIndex}");

        // Set the relic sprite
        if (GameManager.Instance == null)
        {
            Debug.LogError("RelicUI: GameManager.Instance is null!");
            iconImage.color = Color.red;
            return;
        }

        if (GameManager.Instance.relicIconManager == null)
        {
            Debug.LogError("RelicUI: GameManager.Instance.relicIconManager is null!");
            iconImage.color = Color.red;
            return;
        }

        try
        {
            // Get the sprite directly to verify it exists
            Sprite relicSprite = GameManager.Instance.relicIconManager.Get(relic.SpriteIndex);
            if (relicSprite == null)
            {
                Debug.LogError($"RelicUI: Sprite at index {relic.SpriteIndex} is null! Check your RelicIconManager sprite array.");
                iconImage.color = Color.red;
                return;
            }

            // Assign the sprite directly
            iconImage.sprite = relicSprite;
            iconImage.color = Color.white; // Make sure it's visible

            Debug.Log($"RelicUI: Successfully set sprite '{relicSprite.name}' for relic '{relic.Name}'");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"RelicUI: Failed to set sprite for relic '{relic.Name}': {e.Message}");
            iconImage.color = Color.red; // Make it red so you know it failed
        }

        // Optional: Add click handler for tooltip
        Button button = newSlot.GetComponent<Button>();
        if (button == null)
        {
            button = newSlot.AddComponent<Button>();
        }
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => Debug.Log($"Clicked relic: {relic.Name}"));

        // Track this slot
        activeRelicSlots.Add(newSlot);
        displayedRelicNames.Add(relic.Name);

        Debug.Log($"RelicUI: Successfully created slot for relic '{relic.Name}'. Total relics displayed: {activeRelicSlots.Count}");
    }

    /// <summary>
    /// Get count of displayed relics
    /// </summary>
    public int GetRelicCount()
    {
        return activeRelicSlots.Count;
    }

    // Debug method - call this to test
    [ContextMenu("Test Add Fake Relic")]
    public void TestAddFakeRelic()
    {
        // Create a fake relic for testing
        var fakeRelicData = new RelicData
        {
            name = "Test Relic",
            sprite = 0,
            trigger = new TriggerData { type = "test" },
            effect = new EffectData { type = "test" }
        };
        var fakeRelic = new Relic(fakeRelicData);
        AddRelic(fakeRelic);
    }

    [ContextMenu("Debug Sprite Manager")]
    public void DebugSpriteManager()
    {
        Debug.Log("=== SPRITE MANAGER DEBUG ===");
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is null!");
            return;
        }

        if (GameManager.Instance.relicIconManager == null)
        {
            Debug.LogError("GameManager.Instance.relicIconManager is null!");
            return;
        }

        int spriteCount = GameManager.Instance.relicIconManager.GetCount();
        Debug.Log($"RelicIconManager has {spriteCount} sprites");

        for (int i = 0; i < spriteCount; i++)
        {
            Sprite sprite = GameManager.Instance.relicIconManager.Get(i);
            Debug.Log($"Sprite {i}: {(sprite != null ? sprite.name : "NULL")}");
        }
        Debug.Log("=== END DEBUG ===");
    }
}