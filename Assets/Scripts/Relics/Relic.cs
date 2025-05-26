using UnityEngine;

public class Relic
{
    public string Name { get; }
    public int SpriteIndex { get; }

    // expose these so RewardScreenManager can read .description
    public TriggerData TriggerData { get; }
    public EffectData EffectData { get; }

    readonly IRelicTrigger trigger;
    readonly IRelicEffect effect;

    public Relic(RelicData d)
    {
        Name = d.name;
        SpriteIndex = d.sprite;

        TriggerData = d.trigger;
        EffectData = d.effect;

        trigger = RelicTriggers.Create(d.trigger, this);
        effect = RelicEffects.Create(d.effect, this);
    }

    public void Init() => trigger.Subscribe();
    public void Fire() => effect.Activate();
    public void End() => effect.Deactivate();
}
