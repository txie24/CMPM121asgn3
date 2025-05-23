using UnityEngine;

public class Relic
{
    public string Name { get; }
    public int SpriteIndex { get; }

    readonly IRelicTrigger trigger;
    readonly IRelicEffect effect;

    public Relic(RelicData d)
    {
        Name = d.name;      // ←— use name, not type
        SpriteIndex = d.sprite;
        trigger = RelicTriggers.Create(d.trigger, this);
        effect = RelicEffects.Create(d.effect, this);
    }

    public void Init() => trigger.Subscribe();
    public void Fire() => effect.Activate();
    public void End() => effect.Deactivate();
}
