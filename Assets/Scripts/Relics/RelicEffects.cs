using System;
using System.Collections.Generic;
using UnityEngine;

public interface IRelicEffect
{
    void Activate();
    void Deactivate();
}

public static class RelicEffects
{
    public static IRelicEffect Create(EffectData d, Relic r)
    {
        if (d.type == "gain-mana")
            return new GainMana(int.Parse(d.amount), r.Name);

        if (d.type == "gain-health")
            return new GainHealth(int.Parse(d.amount), r.Name);

        if (d.type == "gain-spellpower")
        {
            if (d.until == "cast-spell")
                return new GainSpellPowerOnce(int.Parse(d.amount), r.Name);
            if (d.until == "move")
                return new GainSpellPowerUntilMove(d.amount, r.Name);
            if (d.until == "damage")
                return new GainSpellPowerUntilDamage(int.Parse(d.amount), r.Name);
            return new GainSpellPower(d.amount, r.Name);
        }

        if (d.type == "gain-maxhp")
            return new GainMaxHP(int.Parse(d.amount), r.Name);

        throw new Exception($"Unknown effect type: {d.type}");
    }

    class GainMana : IRelicEffect
    {
        readonly int amt;
        readonly string relicName;
        public GainMana(int a, string name) { amt = a; relicName = name; }
        public void Activate()
        {
            Debug.Log($"[RelicEffect] “{relicName}”: +{amt} mana");
            var pc = GameManager.Instance.player.GetComponent<PlayerController>();
            pc.GainMana(amt);
        }
        public void Deactivate() { }
    }

    class GainHealth : IRelicEffect
    {
        readonly int amt;
        readonly string relicName;
        public GainHealth(int a, string name) { amt = a; relicName = name; }

        public void Activate()
        {
            Debug.Log($"[RelicEffect] “{relicName}”: +{amt} HP");
            var pc = GameManager.Instance.player.GetComponent<PlayerController>();
            pc.hp.hp = Mathf.Min(pc.hp.max_hp, pc.hp.hp + amt);
            pc.healthui.SetHealth(pc.hp);
        }

        public void Deactivate() { }
    }

    class GainSpellPower : IRelicEffect
    {
        readonly string formula;
        readonly string relicName;
        public GainSpellPower(string f, string name) { formula = f; relicName = name; }
        public void Activate()
        {
            var vars = new Dictionary<string, int> { { "wave", GameManager.Instance.wavesCompleted } };
            int v = RPNEvaluator.Evaluate(formula, vars);
            Debug.Log($"[RelicEffect] “{relicName}”: +{v} SP (formula: {formula})");
            GameManager.Instance.player.GetComponent<PlayerController>().AddSpellPower(v);
        }
        public void Deactivate() { }
    }

    class GainSpellPowerOnce : IRelicEffect
    {
        readonly int amt;
        readonly string relicName;
        bool pending = false;

        public GainSpellPowerOnce(int a, string name) { amt = a; relicName = name; }

        public void Activate()
        {
            if (pending)
            {
                Debug.Log($"[RelicEffect] “{relicName}”: buff pending, skipping");
                return;
            }

            pending = true;
            Debug.Log($"[RelicEffect] “{relicName}”: +{amt} SP (one-shot), will remove after cast");
            var pc = GameManager.Instance.player.GetComponent<PlayerController>();
            pc.AddSpellPower(amt);
            SpellCaster.OnSpellCast += HandleSpellCast;
        }

        void HandleSpellCast()
        {
            if (!pending) return;
            var pc = GameManager.Instance.player.GetComponent<PlayerController>();
            Debug.Log($"[RelicEffect] “{relicName}”: –{amt} SP (one-shot buff removed)");
            pc.AddSpellPower(-amt);
            pending = false;
            SpellCaster.OnSpellCast -= HandleSpellCast;
        }

        public void Deactivate()
        {
            if (pending)
            {
                Debug.Log($"[RelicEffect] “{relicName}”: Deactivate cleaning pending buff");
                SpellCaster.OnSpellCast -= HandleSpellCast;
                pending = false;
            }
        }
    }

    class GainSpellPowerUntilMove : IRelicEffect
    {
        readonly string formula;
        readonly string relicName;
        int buffAmt = 0;
        bool active = false;

        public GainSpellPowerUntilMove(string f, string name) { formula = f; relicName = name; }

        public void Activate()
        {
            if (active) return;
            var vars = new Dictionary<string, int> { { "wave", GameManager.Instance.wavesCompleted } };
            buffAmt = RPNEvaluator.Evaluate(formula, vars);
            Debug.Log($"[RelicEffect] “{relicName}”: +{buffAmt} SP (until move) (formula: {formula})");
            GameManager.Instance.player.GetComponent<PlayerController>().AddSpellPower(buffAmt);
            active = true;
        }

        public void Deactivate()
        {
            if (!active) return;
            Debug.Log($"[RelicEffect] “{relicName}”: –{buffAmt} SP (buff removed on move)");
            GameManager.Instance.player.GetComponent<PlayerController>().AddSpellPower(-buffAmt);
            active = false;
        }
    }

    class GainMaxHP : IRelicEffect
    {
        readonly int amt;
        readonly string relicName;
        public GainMaxHP(int a, string name) { amt = a; relicName = name; }

        public void Activate()
        {
            var pc = GameManager.Instance.player.GetComponent<PlayerController>();

            // keep track of the total relic‐bonus
            pc.relicMaxHPBonus += amt;

            // immediately bump you up by amt
            Debug.Log($"[RelicEffect] “{relicName}”: +{amt} max HP (total relic bonus={pc.relicMaxHPBonus})");
            pc.hp.SetMaxHP(pc.hp.max_hp + amt, true);
            pc.healthui.SetHealth(pc.hp);
        }

        public void Deactivate() { }
    }


    class GainSpellPowerUntilDamage : IRelicEffect
    {
        readonly int amt;
        readonly string relicName;
        public GainSpellPowerUntilDamage(int a, string name) { amt = a; relicName = name; }

        public void Activate()
        {
            Debug.Log($"[RelicEffect] “{relicName}”: +{amt} SP (until damaged)");
            var pc = GameManager.Instance.player.GetComponent<PlayerController>();
            pc.AddSpellPower(amt);
            EventBus.Instance.OnDamage += OnPlayerDamaged;
        }

        void OnPlayerDamaged(Vector3 _, Damage __, Hittable target)
        {
            if (target.team == Hittable.Team.PLAYER)
            {
                Debug.Log($"[RelicEffect] “{relicName}”: –{amt} SP (removed on damage)");
                var pc = GameManager.Instance.player.GetComponent<PlayerController>();
                pc.AddSpellPower(-amt);
                EventBus.Instance.OnDamage -= OnPlayerDamaged;
            }
        }

        public void Deactivate()
        {
            EventBus.Instance.OnDamage -= OnPlayerDamaged;
        }
    }
}