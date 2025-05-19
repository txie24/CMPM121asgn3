using UnityEngine;
using TMPro;

public class MenuSelectorController : MonoBehaviour
{
    public TextMeshProUGUI label;
    public string level;
    public EnemySpawner spawner;

    public void SetLevel(string text)
    {
        level = text;
        if (label != null) label.text = text;
    }

    public void StartLevel()
    {
        // Find the class manager
        ChooseClassManager classManager = Object.FindFirstObjectByType<ChooseClassManager>();

        if (classManager != null)
        {
            // Show class selection UI instead of starting level directly
            Debug.Log("Start: " + level + " (showing class selection first)");
            classManager.ShowClassSelection(level, spawner);
        }
        else
        {
            // Original behavior if class manager is not found
            if (spawner != null)
            {
                Debug.Log("Start: " + level);
                spawner.StartLevel(level);
            }
        }
    }
}