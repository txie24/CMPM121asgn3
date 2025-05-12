// File: Assets/Scripts/Spells/CompositeProjectileMovement.cs
using UnityEngine;
using System.Collections.Generic;

public class CompositeProjectileMovement : ProjectileMovement
{
    private readonly List<ProjectileMovement> movements;

    public CompositeProjectileMovement(List<ProjectileMovement> movements)
        : base(0f)  // we don’t use base.speed here; each sub‐movement has its own
    {
        this.movements = movements;
    }

    // delegate every frame’s Movement call to each sub‐movement
    public override void Movement(Transform transform)
    {
        foreach (var m in movements)
            m.Movement(transform);
    }
}
