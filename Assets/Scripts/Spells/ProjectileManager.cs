using UnityEngine;
using System;

public class ProjectileManager : MonoBehaviour
{
    public GameObject[] projectiles;

    void Start()
    {
        GameManager.Instance.projectileManager = this;
    }

    void Update() { }

    public void CreateProjectile(int which, string trajectory, Vector3 where, Vector3 direction, float speed, Action<Hittable, Vector3> onHit)
    {
        if (which < 0 || which >= projectiles.Length)
        {
            which = 0;
        }

        GameObject new_projectile = Instantiate(projectiles[which], where + direction.normalized * 1.1f, Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));

        ProjectileMovement movement = MakeMovement(trajectory, speed);
        if (movement == null)
        {
            Debug.LogWarning($"ProjectileManager: Unknown trajectory type '{trajectory}', defaulting to straight.");
            movement = new StraightProjectileMovement(speed);
        }

        ProjectileController controller = new_projectile.GetComponent<ProjectileController>();
        controller.movement = movement;
        controller.OnHit += onHit;
    }

    public void CreateProjectile(int which, string trajectory, Vector3 where, Vector3 direction, float speed, Action<Hittable, Vector3> onHit, float lifetime)
    {
        if (which < 0 || which >= projectiles.Length)
        {
            which = 0;
        }

        GameObject new_projectile = Instantiate(projectiles[which], where + direction.normalized * 1.1f, Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));

        ProjectileMovement movement = MakeMovement(trajectory, speed);
        if (movement == null)
        {
            Debug.LogWarning($"ProjectileManager: Unknown trajectory type '{trajectory}', defaulting to straight.");
            movement = new StraightProjectileMovement(speed);
        }

        ProjectileController controller = new_projectile.GetComponent<ProjectileController>();
        controller.movement = movement;
        controller.OnHit += onHit;
        controller.SetLifetime(lifetime);
    }

    public void CreatePiercingProjectile(int which, string trajectory, Vector3 where, Vector3 direction, float speed, Action<Hittable, Vector3> onHit)
    {
        if (which < 0 || which >= projectiles.Length)
        {
            which = 0;
        }

        GameObject new_projectile = Instantiate(projectiles[which], where + direction.normalized * 1.1f, Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));

        ProjectileMovement movement = MakeMovement(trajectory, speed);
        if (movement == null)
        {
            Debug.LogWarning($"ProjectileManager: Unknown trajectory type '{trajectory}', defaulting to straight.");
            movement = new StraightProjectileMovement(speed);
        }

        ProjectileController controller = new_projectile.GetComponent<ProjectileController>();
        controller.movement = movement;
        controller.OnHit += onHit;
        controller.piercing = true; // <--- mark projectile as piercing
    }

    public ProjectileMovement MakeMovement(string name, float speed)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("ProjectileManager: Trajectory name is empty, defaulting to straight.");
            return new StraightProjectileMovement(speed);
        }

        switch (name.ToLower())
        {
            case "straight": return new StraightProjectileMovement(speed);
            case "homing": return new HomingProjectileMovement(speed);
            case "spiraling": return new SpiralingProjectileMovement(speed);
            default:
                Debug.LogWarning($"ProjectileManager: Unknown trajectory type '{name}', defaulting to straight.");
                return new StraightProjectileMovement(speed);
        }
    }
}