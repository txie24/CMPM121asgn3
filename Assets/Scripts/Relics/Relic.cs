using UnityEngine;

public class Relic
{
    public string Name { get; }
    public int SpriteIndex { get; }

    IRelicTrigger trigger;
    IRelicEffect effect;

    public Relic(RelicData data)
    {
        Name = data.name;
        SpriteIndex = data.sprite;
        trigger = RelicTriggers.Create(data.trigger, this);
        effect = RelicEffects.Create(data.effect, this);
    }

    /// <summary>Called once when the player picks this relic.</summary>
    public void Init()
    {
        trigger.Subscribe();
    }

    /// <summary>Called by triggers to fire effect.</summary>
    public void Fire()
    {
        effect.Activate();
    }

    /// <summary>Called by triggers to end “until” effects.</summary>
    public void End()
    {
        effect.Deactivate();
        trigger.Unsubscribe();
    }
}
