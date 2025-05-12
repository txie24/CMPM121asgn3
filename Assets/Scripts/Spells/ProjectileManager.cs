// File: Assets/Scripts/ProjectileManager.cs
using UnityEngine;
using System;
using System.Collections.Generic;

public class ProjectileManager : MonoBehaviour
{
    [Tooltip("All your projectile prefabs, by sprite index")]
    public GameObject[] projectiles;

    // when non-null, overrides whatever trajectory you pass in
    [HideInInspector] public string forcedTrajectory = null;

    void Start()
    {
        GameManager.Instance.projectileManager = this;
    }

    // 6‑arg version (no lifetime override)
    public void CreateProjectile(int which, string trajectory,
                                 Vector3 where, Vector3 direction,
                                 float speed, Action<Hittable, Vector3> onHit)
    {
        if (which < 0 || which >= projectiles.Length) which = 0;
        var proj = Instantiate(
            projectiles[which],
            where + direction.normalized * 1.1f,
            Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg)
        );

        string traj = string.IsNullOrEmpty(forcedTrajectory)
                      ? trajectory
                      : forcedTrajectory;
        var pc = proj.GetComponent<ProjectileController>();
        pc.movement = MakeMovement(traj, speed);
        pc.OnHit += onHit;
    }

    // 7‑arg version (with lifetime)
    public void CreateProjectile(int which, string trajectory,
                                 Vector3 where, Vector3 direction,
                                 float speed, Action<Hittable, Vector3> onHit,
                                 float lifetime)
    {
        if (which < 0 || which >= projectiles.Length) which = 0;
        var proj = Instantiate(
            projectiles[which],
            where + direction.normalized * 1.1f,
            Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg)
        );

        string traj = string.IsNullOrEmpty(forcedTrajectory)
                      ? trajectory
                      : forcedTrajectory;
        var pc = proj.GetComponent<ProjectileController>();
        pc.movement = MakeMovement(traj, speed);
        pc.OnHit += onHit;
        pc.SetLifetime(lifetime);
    }

    // piercing shot (no lifetime overload)
    public void CreatePiercingProjectile(int which, string trajectory,
                                         Vector3 where, Vector3 direction,
                                         float speed, Action<Hittable, Vector3> onHit)
    {
        if (which < 0 || which >= projectiles.Length) which = 0;
        var proj = Instantiate(
            projectiles[which],
            where + direction.normalized * 1.1f,
            Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg)
        );

        string traj = string.IsNullOrEmpty(forcedTrajectory)
                      ? trajectory
                      : forcedTrajectory;
        var pc = proj.GetComponent<ProjectileController>();
        pc.movement = MakeMovement(traj, speed);
        pc.OnHit += onHit;
        pc.piercing = true;
    }

    // builds straight, homing, spiraling, or any "a+b" combo via CompositeProjectileMovement
    public ProjectileMovement MakeMovement(string name, float speed)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("ProjectileManager: trajectory empty, defaulting to straight.");
            return new StraightProjectileMovement(speed);
        }

        // handle stacked directives like "homing+spiraling"
        if (name.Contains("+"))
        {
            var parts = name.Split('+');
            var moves = new List<ProjectileMovement>();
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    moves.Add(MakeMovement(trimmed, speed));
            }
            return new CompositeProjectileMovement(moves);
        }

        switch (name.ToLower())
        {
            case "straight": return new StraightProjectileMovement(speed);
            case "homing": return new HomingProjectileMovement(speed);
            case "spiraling": return new SpiralingProjectileMovement(speed);
            default:
                Debug.LogWarning($"ProjectileManager: Unknown trajectory '{name}', defaulting to straight.");
                return new StraightProjectileMovement(speed);
        }
    }
}
