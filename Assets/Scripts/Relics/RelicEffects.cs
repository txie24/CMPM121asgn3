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
        "gain-mana" => new GainMana(int.Parse(d.amount)),
        "gain-spellpower" => new GainSpellPower(d.amount),
        _ => throw new Exception($"Unknown effect {d.type}")
    };

    class GainMana : IRelicEffect
    {
        readonly int amt;
        public GainMana(int a) { amt = a; }
        public void Activate()
            => GameManager.Instance.player.GetComponent<PlayerController>().GainMana(amt);
        public void Deactivate() { }
    }

    class GainSpellPower : IRelicEffect
    {
        readonly string formula;
        public GainSpellPower(string f) { formula = f; }
        public void Activate()
        {
            var vars = new Dictionary<string, int> { { "wave", GameManager.Instance.wavesCompleted } };
            int v = RPNEvaluator.Evaluate(formula, vars);
            GameManager.Instance.player.GetComponent<PlayerController>().AddSpellPower(v);
        }
        public void Deactivate() { }
    }
}
