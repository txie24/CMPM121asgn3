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
    public static IRelicEffect Create(EffectData d, Relic r) => d.type switch
    {
        "gain-mana" => new GainMana(int.Parse(d.amount), r.Name),
        "gain-spellpower" => new GainSpellPower(d.amount, r.Name),
        _ => throw new Exception($"Unknown effect {d.type}")
    };

    class GainMana : IRelicEffect
    {
        readonly int amt;
        readonly string relicName;

        public GainMana(int a, string name)
        {
            amt = a;
            relicName = name;
        }

        public void Activate()
        {
            var pc = GameManager.Instance.player
                          .GetComponent<PlayerController>();
            pc.GainMana(amt);
            // debug log so you can see the change
            Debug.Log($"[Relic] '{relicName}' → +{amt} mana. Now {pc.spellcaster.mana}/{pc.spellcaster.max_mana} mana.");
        }

        public void Deactivate() { }
    }

    class GainSpellPower : IRelicEffect
    {
        readonly string formula;
        readonly string relicName;

        public GainSpellPower(string f, string name)
        {
            formula = f;
            relicName = name;
        }

        public void Activate()
        {
            var vars = new Dictionary<string, int> { { "wave", GameManager.Instance.wavesCompleted } };
            int v = RPNEvaluator.Evaluate(formula, vars);

            var pc = GameManager.Instance.player
                          .GetComponent<PlayerController>();
            pc.AddSpellPower(v);

            // debug log so you can see both the RPN result and the new total
            Debug.Log($"[Relic] '{relicName}' → +{v} spell power (formula '{formula}'). Now {pc.spellcaster.spellPower} SP.");
        }

        public void Deactivate()
        {
            Debug.Log($"[Relic] '{relicName}' effect ended.");
        }
    }
}
