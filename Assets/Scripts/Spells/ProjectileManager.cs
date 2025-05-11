// File: Assets/Scripts/ProjectileManager.cs
using UnityEngine;
using System;

public class ProjectileManager : MonoBehaviour
{
    [Tooltip("All your projectile prefabs, by sprite index")]
    public GameObject[] projectiles;

    // ← when non‑null, forces every CreateProjectile call to use this trajectory
    [HideInInspector] public string forcedTrajectory = null;

    void Start()
    {
        GameManager.Instance.projectileManager = this;
    }

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

        // pick real trajectory
        string traj = string.IsNullOrEmpty(forcedTrajectory)
                      ? trajectory
                      : forcedTrajectory;
        proj.GetComponent<ProjectileController>().movement
            = MakeMovement(traj, speed);
        proj.GetComponent<ProjectileController>().OnHit += onHit;
    }

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

    public ProjectileMovement MakeMovement(string name, float speed)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("ProjectileManager: trajectory empty, defaulting to straight.");
            return new StraightProjectileMovement(speed);
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
