using UnityEngine;
using UnityEngine.UI;

public class ChooseClassManager : MonoBehaviour
{
    [Header("Class Selection UI")]
    public GameObject classSelectionUI;
    public Button mageButton;
    public Button warlockButton;
    public Button battlemageButton;

    public static string SelectedClass { get; private set; } = "mage";

    // set by MenuSelectorController when you pick a level
    private string selectedLevelName;
    private EnemySpawner spawner;

    void Start()
    {
        classSelectionUI.SetActive(false);
        mageButton.onClick.AddListener(() => Select("mage"));
        warlockButton.onClick.AddListener(() => Select("warlock"));
        battlemageButton.onClick.AddListener(() => Select("battlemage"));
    }

    // called by your level-select UI before showing the class buttons
    public void ShowClassSelection(string levelName, EnemySpawner sp)
    {
        selectedLevelName = levelName;
        spawner = sp;
        classSelectionUI.SetActive(true);
    }

    private void Select(string cls)
    {
        Debug.Log($"[ChooseClass] button clicked: {cls}");
        SelectedClass = cls;
        classSelectionUI.SetActive(false);

        if (spawner != null)
        {
            spawner.StartLevel(selectedLevelName);
        }
        else
        {
            Debug.LogError("[ChooseClass] spawner reference is null!");
        }
    }
}
