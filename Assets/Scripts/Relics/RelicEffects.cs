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
        // choose effect based on type and "until" field in relics.json :contentReference[oaicite:0]{index=0}
        if (d.type == "gain-mana")
            return new GainMana(int.Parse(d.amount), r.Name);

        if (d.type == "gain-spellpower")
        {
            // Golden Mask: only until next cast
            if (d.until == "cast-spell")
                return new GainSpellPowerOnce(int.Parse(d.amount), r.Name);

            // Jade Elephant: only until you move again
            if (d.until == "move")
                return new GainSpellPowerUntilMove(d.amount, r.Name);

            // any other spell-power relic
            return new GainSpellPower(d.amount, r.Name);
        }

        throw new Exception($"Unknown relic effect type: {d.type}");
    }

    class GainMana : IRelicEffect
    {
        readonly int amt;
        readonly string relicName;

        public GainMana(int a, string name) { amt = a; relicName = name; }

        public void Activate()
        {
            Debug.Log($"[RelicEffect] “{relicName}”: +{amt} mana");
            GameManager.Instance.player
                .GetComponent<PlayerController>()
                .GainMana(amt);
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
            GameManager.Instance.player
                .GetComponent<PlayerController>()
                .AddSpellPower(v);
        }

        public void Deactivate() { }
    }

    // GOLDEN MASK: one-shot +SP until next cast, robust against multiple hits
    class GainSpellPowerOnce : IRelicEffect
    {
        readonly int amt;
        readonly string relicName;
        bool pending = false;

        public GainSpellPowerOnce(int a, string name)
        {
            amt = a;
            relicName = name;
        }

        public void Activate()
        {
            // only buff if no pending buff already
            if (pending)
            {
                Debug.Log($"[RelicEffect] “{relicName}”: buff already pending, ignoring extra Activate");
                return;
            }

            pending = true;
            Debug.Log($"[RelicEffect] “{relicName}”: +{amt} SP (one-shot), will remove after next cast");
            var pc = GameManager.Instance.player.GetComponent<PlayerController>();
            pc.AddSpellPower(amt);

            // subscribe once
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
            // in case relic is dropped before use
            if (pending)
            {
                Debug.Log($"[RelicEffect] “{relicName}”: Deactivate called, cleaning up pending buff");
                SpellCaster.OnSpellCast -= HandleSpellCast;
                pending = false;
            }
        }
    }

    // JADE ELEPHANT: +SP while standing still, removed on first movement
    class GainSpellPowerUntilMove : IRelicEffect
    {
        readonly string formula;
        readonly string relicName;
        int buffAmt = 0;
        bool active = false;

        public GainSpellPowerUntilMove(string f, string name)
        {
            formula = f;
            relicName = name;
        }

        public void Activate()
        {
            // only apply once per stand-still
            if (active) return;

            var vars = new Dictionary<string, int> { { "wave", GameManager.Instance.wavesCompleted } };
            buffAmt = RPNEvaluator.Evaluate(formula, vars);
            Debug.Log($"[RelicEffect] “{relicName}”: +{buffAmt} SP (until move) (formula: {formula})");

            GameManager.Instance.player
                .GetComponent<PlayerController>()
                .AddSpellPower(buffAmt);

            active = true;
        }

        public void Deactivate()
        {
            // only remove if it was active
            if (!active) return;

            Debug.Log($"[RelicEffect] “{relicName}”: –{buffAmt} SP (buff removed on move)");
            GameManager.Instance.player
                .GetComponent<PlayerController>()
                .AddSpellPower(-buffAmt);

            active = false;
        }
    }
}
