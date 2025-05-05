using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public Hittable hp;
    public HealthBar healthui;
    public ManaBar manaui;
    public SpellCaster spellcaster;
    public SpellUI spellui;
    public int speed;
    public Unit unit;

    void Start()
    {
        unit = GetComponent<Unit>();
        GameManager.Instance.player = gameObject;
        StartLevel();
    }

    public void StartLevel()
    {
        if (spellcaster == null)
            spellcaster = GetComponent<SpellCaster>() ?? gameObject.AddComponent<SpellCaster>();

        spellcaster.max_mana = 125;
        spellcaster.mana     = spellcaster.max_mana;
        spellcaster.mana_reg = 8;
        spellcaster.team     = Hittable.Team.PLAYER;

        hp = new Hittable(100, Hittable.Team.PLAYER, gameObject);
        hp.OnDeath += Die;
        hp.team = Hittable.Team.PLAYER;

        healthui.SetHealth(hp);
        manaui.SetSpellCaster(spellcaster);
        spellui.SetSpell(spellcaster.spells[0]);
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
