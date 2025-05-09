using UnityEngine;
using System;

public class ProjectileManager : MonoBehaviour
{
    // 1) Singleton instance
    public static ProjectileManager Instance { get; private set; }

    // All your projectile prefabs
    public GameObject[] projectiles;

    // Holds a one‑time override for the next shot’s trajectory
    private string nextOverrideTrajectory;

    void Awake()
    {
        // Enforce singleton
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    void Start()
    {
        // Optional: still register on your GameManager if you use that elsewhere
        GameManager.Instance.projectileManager = this;
    }

    void Update() { }

    // 2) Called by HomingModifier (or anyone) to override the next shot’s path
    public void OverrideTrajectory(string trajectory)
    {
        nextOverrideTrajectory = trajectory;
    }

    public void CreateProjectile(int which,
                                 string trajectory,
                                 Vector3 where,
                                 Vector3 direction,
                                 float speed,
                                 Action<Hittable, Vector3> onHit)
    {
        // pick up any override, then clear it
        string traj = !string.IsNullOrEmpty(nextOverrideTrajectory)
                      ? nextOverrideTrajectory
                      : trajectory;
        nextOverrideTrajectory = null;

        // bounds‑check
        if (which < 0 || which >= projectiles.Length)
        {
            Debug.LogWarning(
              $"ProjectileManager: Index {which} is out of bounds. " +
              $"Max = {projectiles.Length - 1}, using index 0 instead.");
            which = 0;
        }

        // spawn
        GameObject proj = Instantiate(
            projectiles[which],
            where + direction.normalized * 1.1f,
            Quaternion.Euler(
                0, 0,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg
            )
        );

        // movement
        var movement = MakeMovement(traj, speed);
        if (movement == null)
        {
            Debug.LogWarning(
              $"ProjectileManager: Unknown trajectory '{traj}', defaulting to straight.");
            movement = new StraightProjectileMovement(speed);
        }

        // hook it up
        var controller = proj.GetComponent<ProjectileController>();
        controller.movement = movement;
        controller.OnHit += onHit;
    }

    public void CreateProjectile(int which,
                                 string trajectory,
                                 Vector3 where,
                                 Vector3 direction,
                                 float speed,
                                 Action<Hittable, Vector3> onHit,
                                 float lifetime)
    {
        // reuse the override logic
        string traj = !string.IsNullOrEmpty(nextOverrideTrajectory)
                      ? nextOverrideTrajectory
                      : trajectory;
        nextOverrideTrajectory = null;

        if (which < 0 || which >= projectiles.Length)
        {
            Debug.LogWarning(
              $"ProjectileManager: Index {which} is out of bounds. " +
              $"Max = {projectiles.Length - 1}, using index 0 instead.");
            which = 0;
        }

        GameObject proj = Instantiate(
            projectiles[which],
            where + direction.normalized * 1.1f,
            Quaternion.Euler(
                0, 0,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg
            )
        );

        var movement = MakeMovement(traj, speed);
        if (movement == null)
        {
            Debug.LogWarning(
              $"ProjectileManager: Unknown trajectory '{traj}', defaulting to straight.");
            movement = new StraightProjectileMovement(speed);
        }

        var controller = proj.GetComponent<ProjectileController>();
        controller.movement = movement;
        controller.OnHit += onHit;
        controller.SetLifetime(lifetime);
    }

    public void CreatePiercingProjectile(int which,
                                         string trajectory,
                                         Vector3 where,
                                         Vector3 direction,
                                         float speed,
                                         Action<Hittable, Vector3> onHit)
    {
        // same override logic
        string traj = !string.IsNullOrEmpty(nextOverrideTrajectory)
                      ? nextOverrideTrajectory
                      : trajectory;
        nextOverrideTrajectory = null;

        if (which < 0 || which >= projectiles.Length)
        {
            Debug.LogWarning(
              $"ProjectileManager: Index {which} is out of bounds. " +
              $"Max = {projectiles.Length - 1}, using index 0 instead.");
            which = 0;
        }

        GameObject proj = Instantiate(
            projectiles[which],
            where + direction.normalized * 1.1f,
            Quaternion.Euler(
                0, 0,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg
            )
        );

        var movement = MakeMovement(traj, speed);
        if (movement == null)
        {
            Debug.LogWarning(
              $"ProjectileManager: Unknown trajectory '{traj}', defaulting to straight.");
            movement = new StraightProjectileMovement(speed);
        }

        var controller = proj.GetComponent<ProjectileController>();
        controller.movement = movement;
        controller.OnHit += onHit;
        controller.piercing = true;
    }

    public ProjectileMovement MakeMovement(string name, float speed)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning(
              "ProjectileManager: Trajectory name is empty, defaulting to straight.");
            return new StraightProjectileMovement(speed);
        }

        switch (name.ToLower())
        {
            case "straight": return new StraightProjectileMovement(speed);
            case "homing": return new HomingProjectileMovement(speed);
            case "spiraling": return new SpiralingProjectileMovement(speed);
            default:
                Debug.LogWarning(
                  $"ProjectileManager: Unknown trajectory type '{name}', defaulting to straight.");
                return new StraightProjectileMovement(speed);
        }
    }
}
