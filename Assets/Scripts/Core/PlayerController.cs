// Assets/Scripts/PlayerController.cs
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // relic hook
    public static event Action<Vector3> OnPlayerMove;
    public int relicMaxHPBonus = 0;
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
        InitializeComponents();
    }

    void Start()
    {
        unit = GetComponent<Unit>() ?? gameObject.AddComponent<Unit>();
        GameManager.Instance.player = gameObject;
    }

    // make public so EnemySpawner can call it
    public void InitializeComponents()
    {
        if (hp == null)
        {
            hp = new Hittable(100, Hittable.Team.PLAYER, gameObject);
            hp.OnDeath += Die;
        }

        if (spellcaster == null)
        {
            spellcaster = GetComponent<SpellCaster>();
            if (spellcaster == null)
                spellcaster = gameObject.AddComponent<SpellCaster>();
            spellcaster.team = Hittable.Team.PLAYER;
        }
    }

    public void StartLevel()
    {
        InitializeComponents();

        if (healthui != null && hp != null)
            healthui.SetHealth(hp);
        if (manaui != null && spellcaster != null)
            manaui.SetSpellCaster(spellcaster);

        UpdateSpellUI();
    }

    public void UpdateSpellUI()
    {
        if (spellcaster == null)
            return;

        spellui?.SetSpell(spellcaster.spells.Count > 0 ? spellcaster.spells[0] : null);
        spellui2?.SetSpell(spellcaster.spells.Count > 1 ? spellcaster.spells[1] : null);
        spellui3?.SetSpell(spellcaster.spells.Count > 2 ? spellcaster.spells[2] : null);
        spellui4?.SetSpell(spellcaster.spells.Count > 3 ? spellcaster.spells[3] : null);

        ShowOrHideSpellUI();
    }

    private void ShowOrHideSpellUI()
    {
        if (spellcaster == null) return;

        spellui?.gameObject.SetActive(spellcaster.spells.Count > 0 && spellcaster.spells[0] != null);
        spellui2?.gameObject.SetActive(spellcaster.spells.Count > 1 && spellcaster.spells[1] != null);
        spellui3?.gameObject.SetActive(spellcaster.spells.Count > 2 && spellcaster.spells[2] != null);
        spellui4?.gameObject.SetActive(spellcaster.spells.Count > 3 && spellcaster.spells[3] != null);
    }

    void OnAttack(InputValue value)
    {
        if (GameManager.Instance.state == GameManager.GameState.PREGAME ||
            GameManager.Instance.state == GameManager.GameState.GAMEOVER)
            return;

        Vector2 ms = Mouse.current.position.ReadValue();
        Vector3 mw = Camera.main.ScreenToWorldPoint(ms);
        mw.z = 0;

        for (int i = 0; i < spellcaster.spells.Count; i++)
        {
            if (spellcaster.spells[i] != null)
                StartCoroutine(spellcaster.CastSlot(i, transform.position, mw));
        }
    }

    void OnMove(InputValue value)
    {
        if (GameManager.Instance.state == GameManager.GameState.PREGAME ||
            GameManager.Instance.state == GameManager.GameState.GAMEOVER)
            return;

        Vector2 mv2 = value.Get<Vector2>() * speed;
        unit.movement = mv2;

        // fire relic hook
        OnPlayerMove?.Invoke(new Vector3(mv2.x, mv2.y, 0f));
    }

    /// <summary>
    /// Called by relics to give you mana.
    /// </summary>
    public void GainMana(int amount)
    {
        if (spellcaster == null) InitializeComponents();
        spellcaster.mana = Mathf.Min(spellcaster.max_mana, spellcaster.mana + amount);
        manaui?.SetSpellCaster(spellcaster);
    }

    /// <summary>
    /// Called by relics to increase your spell power.
    /// </summary>
    public void AddSpellPower(int amount)
    {
        if (spellcaster == null) InitializeComponents();
        spellcaster.spellPower += amount;
    }

    private void Die()
    {
        Debug.Log("You Lost");
        GameManager.Instance.IsPlayerDead = true;
        GameManager.Instance.state = GameManager.GameState.GAMEOVER;
    }
}
