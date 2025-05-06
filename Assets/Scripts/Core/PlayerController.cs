using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public Hittable      hp;
    public HealthBar     healthui;
    public ManaBar       manaui;
    public SpellCaster   spellcaster;

    // Spell UI slots: keep your original slot plus three more
    public SpellUI spellui;   // slot 0
    public SpellUI spellui2;  // slot 1
    public SpellUI spellui3;  // slot 2
    public SpellUI spellui4;  // slot 3

    public int  speed;
    public Unit unit;

    void Start()
    {
        unit = GetComponent<Unit>();
        GameManager.Instance.player = gameObject;
        StartLevel();
    }

    public void StartLevel()
    {
        // get or add the SpellCaster component
        if (spellcaster == null)
            spellcaster = GetComponent<SpellCaster>() 
                         ?? gameObject.AddComponent<SpellCaster>();

        // configure its fields
        spellcaster.max_mana = 125;
        spellcaster.mana     = spellcaster.max_mana;
        spellcaster.mana_reg = 8;
        spellcaster.team     = Hittable.Team.PLAYER;

        // set up HP
        hp = new Hittable(100, Hittable.Team.PLAYER, gameObject);
        hp.OnDeath += Die;
        hp.team = Hittable.Team.PLAYER;

        // wire up health & mana UI
        healthui.SetHealth(hp);
        manaui .SetSpellCaster(spellcaster);

        // populate all four SpellUI slots from spellcaster.spells
        spellui .SetSpell(spellcaster.spells.Count > 0 ? spellcaster.spells[0] : null);
        spellui2?.SetSpell(spellcaster.spells.Count > 1 ? spellcaster.spells[1] : null);
        spellui3?.SetSpell(spellcaster.spells.Count > 2 ? spellcaster.spells[2] : null);
        spellui4?.SetSpell(spellcaster.spells.Count > 3 ? spellcaster.spells[3] : null);
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
        StartCoroutine(spellcaster.CastSlot(0, transform.position, mw));
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
        GameManager.Instance.state        = GameManager.GameState.GAMEOVER;
    }
}
