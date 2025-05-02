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
        if (spawner != null)
        {
            Debug.Log("Start: " + level);
            spawner.StartLevel(level);
        }
    }
}
