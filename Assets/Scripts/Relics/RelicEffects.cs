using System;
using UnityEngine;

public interface IRelicEffect
{
    void Activate();
    void Deactivate();
}

public static class RelicEffects
{
    public static IRelicEffect Create(EffectData data, Relic relic)
    {
        return data.type switch
        {
            "gain-mana" => new GainManaEffect(int.Parse(data.amount)),
            "gain-spellpower" => new GainSpellPowerEffect(data.amount),
            _ => throw new Exception($"Unknown effect type: {data.type}")
        };
    }

    class GainManaEffect : IRelicEffect
    {
        readonly int amount;
        public GainManaEffect(int amt) { amount = amt; }

        public void Activate()
        {
            GameManager.Instance.Player.GainMana(amount);
        }

        public void Deactivate() { }
    }

    class GainSpellPowerEffect : IRelicEffect
    {
        readonly string formula;
        public GainSpellPowerEffect(string f) { formula = f; }

        public void Activate()
        {
            int val = RPNEvaluator.Evaluate(formula);
            GameManager.Instance.Player.AddSpellPower(val);
        }

        public void Deactivate() { }
    }
}
