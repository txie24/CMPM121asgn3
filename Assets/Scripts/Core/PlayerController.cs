using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public Hittable hp;
    public HealthBar healthui;
    public ManaBar manaui;
    public SpellCaster spellcaster;

    public SpellUI spellui;   // slot 0
    public SpellUI spellui2;  // slot 1
    public SpellUI spellui3;  // slot 2
    public SpellUI spellui4;  // slot 3

    public int speed;
    public Unit unit;

    void Start()
    {
        unit = GetComponent<Unit>();
        GameManager.Instance.player = gameObject;
        // StartLevel is called by EnemySpawner
    }

    public void StartLevel()
    {
        // Get or add the SpellCaster component
        if (spellcaster == null)
            spellcaster = GetComponent<SpellCaster>() 
                         ?? gameObject.AddComponent<SpellCaster>();

        // Initialize HP (EnemySpawner will set actual values)
        hp = new Hittable(100, Hittable.Team.PLAYER, gameObject); // Placeholder
        hp.OnDeath += Die;
        hp.team = Hittable.Team.PLAYER;

        // Set team for spellcaster
        spellcaster.team = Hittable.Team.PLAYER;

        // Wire up health & mana UI
        healthui.SetHealth(hp);
        manaui.SetSpellCaster(spellcaster);

        // Update all spell UI slots
        UpdateSpellUI();
    }
    public void UpdateSpellUI()
    {
        if (spellui != null)
            spellui.SetSpell(spellcaster.spells.Count > 0 ? spellcaster.spells[0] : null);
        
        if (spellui2 != null)
            spellui2.SetSpell(spellcaster.spells.Count > 1 ? spellcaster.spells[1] : null);
        
        if (spellui3 != null)
            spellui3.SetSpell(spellcaster.spells.Count > 2 ? spellcaster.spells[2] : null);
        
        if (spellui4 != null)
            spellui4.SetSpell(spellcaster.spells.Count > 3 ? spellcaster.spells[3] : null);
        
        ShowOrHideSpellUI();
    }
    
    private void ShowOrHideSpellUI()
    {
        if (spellui != null && spellui.gameObject != null)
            spellui.gameObject.SetActive(spellcaster.spells.Count > 0 && spellcaster.spells[0] != null);
            
        if (spellui2 != null && spellui2.gameObject != null)
            spellui2.gameObject.SetActive(spellcaster.spells.Count > 1 && spellcaster.spells[1] != null);
            
        if (spellui3 != null && spellui3.gameObject != null)
            spellui3.gameObject.SetActive(spellcaster.spells.Count > 2 && spellcaster.spells[2] != null);
            
        if (spellui4 != null && spellui4.gameObject != null)
            spellui4.gameObject.SetActive(spellcaster.spells.Count > 3 && spellcaster.spells[3] != null);
    }

    void OnAttack(InputValue value)
    {
        if (GameManager.Instance.state == GameManager.GameState.PREGAME ||
            GameManager.Instance.state == GameManager.GameState.GAMEOVER)
            return;
        if (spellcaster == null) return;

        Vector2 ms = Mouse.current.position.ReadValue();
        Vector3 mw = Camera.main.ScreenToWorldPoint(ms);
        mw.z = 0;
        
        for (int i = 0; i < spellcaster.spells.Count; i++)
        {
            if (spellcaster.spells[i] != null)
            {
                StartCoroutine(spellcaster.CastSlot(i, transform.position, mw));
            }
        }
    }

    void OnMove(InputValue value)
    {
        if (GameManager.Instance.state == GameManager.GameState.PREGAME ||
            GameManager.Instance.state == GameManager.GameState.GAMEOVER)
            return;
        unit.movement = value.Get<Vector2>() * speed;
    }

    void Die()
    {
        Debug.Log("You Lost");
        GameManager.Instance.IsPlayerDead = true;
        GameManager.Instance.state = GameManager.GameState.GAMEOVER;
    }
}