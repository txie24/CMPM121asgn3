using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public static event Action<Vector3> OnPlayerMove;
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

    void Awake()
    {
        // Initialize in Awake to ensure these are set before other systems need them
        InitializeComponents();
    }

    void Start()
    {
        unit = GetComponent<Unit>() ?? gameObject.AddComponent<Unit>();
        GameManager.Instance.player = gameObject;
    }

    // Changed to public so it can be called from EnemySpawner
    public void InitializeComponents()
    {
        // Create hp if null
        if (hp == null)
        {
            hp = new Hittable(100, Hittable.Team.PLAYER, gameObject);
            hp.OnDeath += Die;
            Debug.Log("PlayerController: Created new Hittable instance for player");
        }

        // Create or get spellcaster
        if (spellcaster == null)
        {
            spellcaster = GetComponent<SpellCaster>();
            if (spellcaster == null)
            {
                spellcaster = gameObject.AddComponent<SpellCaster>();
                Debug.Log("PlayerController: Added SpellCaster component to player");
            }
            spellcaster.team = Hittable.Team.PLAYER;
        }
    }

    public void StartLevel()
    {
        // Ensure components are initialized
        InitializeComponents();

        // Wire up health & mana UI
        if (healthui != null && hp != null)
        {
            healthui.SetHealth(hp);
        }

        if (manaui != null && spellcaster != null)
        {
            manaui.SetSpellCaster(spellcaster);
        }

        // Update all spell UI slots
        UpdateSpellUI();
    }

    public void UpdateSpellUI()
    {
        if (spellcaster == null)
        {
            Debug.LogError("PlayerController.UpdateSpellUI: spellcaster is null!");
            InitializeComponents(); // Try to initialize again
            if (spellcaster == null) return; // If still null, give up
        }

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
        if (spellcaster == null) return;

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

        if (spellcaster == null)
        {
            InitializeComponents();
            if (spellcaster == null) return;
        }

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

        Vector2 mv2 = value.Get<Vector2>() * speed;
        unit.movement = mv2;

        // ► broadcast the move event:
        Vector3 mv3 = new Vector3(mv2.x, mv2.y, 0f);
        OnPlayerMove?.Invoke(mv3);
    }

    // ► these two methods satisfy the RelicEffects calls:

    /// <summary>
    /// Directly add mana (clamped to max), and update UI.
    /// </summary>
    public void GainMana(int amount)
    {
        if (spellcaster == null) InitializeComponents();
        spellcaster.mana = Mathf.Min(spellcaster.max_mana, spellcaster.mana + amount);
        if (manaui != null)
            manaui.SetSpellCaster(spellcaster);
    }

    /// <summary>
    /// Permanently bumps up your spell power.
    /// </summary>
    public void AddSpellPower(int amount)
    {
        if (spellcaster == null) InitializeComponents();
        spellcaster.spellPower += amount;
    }

    void Die()
    {
        Debug.Log("You Lost");
        GameManager.Instance.IsPlayerDead = true;
        GameManager.Instance.state = GameManager.GameState.GAMEOVER;
    }
}