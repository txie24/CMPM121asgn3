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
        "gain-spellpower" => new GainSpellPower(d.amount, r.Name, d.until),
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
        readonly string until;
        private int addedSpellPower = 0;
        private bool isActive = false;

        public GainSpellPower(string f, string name, string untilCondition)
        {
            formula = f;
            relicName = name;
            until = untilCondition;
        }

        public void Activate()
        {
            var vars = new Dictionary<string, int> { { "wave", GameManager.Instance.wavesCompleted } };
            int v = RPNEvaluator.Evaluate(formula, vars);

            var pc = GameManager.Instance.player
                          .GetComponent<PlayerController>();

            // If this is a temporary effect (has "until" condition)
            if (!string.IsNullOrEmpty(until))
            {
                // If we're already active, don't stack - just refresh
                if (isActive)
                {
                    Debug.Log($"[Relic] '{relicName}' → effect refreshed, still +{addedSpellPower} spell power.");
                    return;
                }

                // Add temporary spell power
                addedSpellPower = v;
                isActive = true;
                pc.AddSpellPower(v);
                Debug.Log($"[Relic] '{relicName}' → +{v} temporary spell power (formula '{formula}'). Now {pc.spellcaster.spellPower} SP.");
            }
            else
            {
                // Permanent effect - just add it
                pc.AddSpellPower(v);
                Debug.Log($"[Relic] '{relicName}' → +{v} permanent spell power (formula '{formula}'). Now {pc.spellcaster.spellPower} SP.");
            }
        }

        public void Deactivate()
        {
            // Only remove spell power if this was a temporary effect and it's currently active
            if (!string.IsNullOrEmpty(until) && isActive && addedSpellPower > 0)
            {
                var pc = GameManager.Instance.player
                              .GetComponent<PlayerController>();

                pc.AddSpellPower(-addedSpellPower); // Remove the spell power we added
                Debug.Log($"[Relic] '{relicName}' → removed {addedSpellPower} temporary spell power. Now {pc.spellcaster.spellPower} SP.");

                isActive = false;
                addedSpellPower = 0;
            }
            else
            {
                Debug.Log($"[Relic] '{relicName}' effect ended (permanent effect, no change).");
            }
        }
    }
}