using UnityEngine;
using System;

public class ProjectileManager : MonoBehaviour
{
    public GameObject[] projectiles;
    
    // New override fields
    public string trajectoryOverride = null;
    public float speedMultiplier = 1.0f;
    public bool piercingOverride = false;
    public Action<Hittable, Vector3> onHitWrapper = null;

    void Start()
    {
        GameManager.Instance.projectileManager = this;
    }

    void Update() {}

    public void CreateProjectile(int which, string trajectory, Vector3 where, Vector3 direction, float speed, Action<Hittable,Vector3> onHit)
    {
        if (which < 0 || which >= projectiles.Length)
        {
            Debug.LogWarning($"ProjectileManager: Index {which} is out of bounds. Max = {projectiles.Length-1}, using index 0 instead.");
            which = 0;
        }

        // Apply overrides if present
        string effectiveTrajectory = trajectoryOverride ?? trajectory;
        float effectiveSpeed = speed * speedMultiplier;
        
        // Apply onHit wrapper if present
        Action<Hittable, Vector3> effectiveOnHit = onHit;
        if (onHitWrapper != null)
        {
            effectiveOnHit = (hit, pos) => {
                onHit(hit, pos);
                onHitWrapper(hit, pos);
            };
        }

        GameObject new_projectile = Instantiate(projectiles[which], where + direction.normalized*1.1f, Quaternion.Euler(0,0,Mathf.Atan2(direction.y, direction.x)*Mathf.Rad2Deg));

        ProjectileMovement movement = MakeMovement(effectiveTrajectory, effectiveSpeed);
        if (movement == null)
        {
            Debug.LogWarning($"ProjectileManager: Unknown trajectory type '{effectiveTrajectory}', defaulting to straight.");
            movement = new StraightProjectileMovement(effectiveSpeed);
        }

        ProjectileController controller = new_projectile.GetComponent<ProjectileController>();
        controller.movement = movement;
        controller.OnHit += effectiveOnHit;
        
        // Apply piercing override if set
        if (piercingOverride)
            controller.piercing = true;
    }

    public void CreateProjectile(int which, string trajectory, Vector3 where, Vector3 direction, float speed, Action<Hittable, Vector3> onHit, float lifetime)
    {
        if (which < 0 || which >= projectiles.Length)
        {
            Debug.LogWarning($"ProjectileManager: Index {which} is out of bounds. Max = {projectiles.Length-1}, using index 0 instead.");
            which = 0;
        }

        // Apply overrides if present
        string effectiveTrajectory = trajectoryOverride ?? trajectory;
        float effectiveSpeed = speed * speedMultiplier;
        
        // Apply onHit wrapper if present
        Action<Hittable, Vector3> effectiveOnHit = onHit;
        if (onHitWrapper != null)
        {
            effectiveOnHit = (hit, pos) => {
                onHit(hit, pos);
                onHitWrapper(hit, pos);
            };
        }

        GameObject new_projectile = Instantiate(projectiles[which], where + direction.normalized * 1.1f, Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Deg2Rad));

        ProjectileMovement movement = MakeMovement(effectiveTrajectory, effectiveSpeed);
        if (movement == null)
        {
            Debug.LogWarning($"ProjectileManager: Unknown trajectory type '{effectiveTrajectory}', defaulting to straight.");
            movement = new StraightProjectileMovement(effectiveSpeed);
        }

        ProjectileController controller = new_projectile.GetComponent<ProjectileController>();
        controller.movement = movement;
        controller.OnHit += effectiveOnHit;
        controller.SetLifetime(lifetime);
        
        // Apply piercing override if set
        if (piercingOverride)
            controller.piercing = true;
    }

    public void CreatePiercingProjectile(int which, string trajectory, Vector3 where, Vector3 direction, float speed, Action<Hittable, Vector3> onHit)
    {
        if (which < 0 || which >= projectiles.Length)
        {
            Debug.LogWarning($"ProjectileManager: Index {which} is out of bounds. Max = {projectiles.Length-1}, using index 0 instead.");
            which = 0;
        }

        // Apply overrides if present
        string effectiveTrajectory = trajectoryOverride ?? trajectory;
        float effectiveSpeed = speed * speedMultiplier;
        
        // Apply onHit wrapper if present
        Action<Hittable, Vector3> effectiveOnHit = onHit;
        if (onHitWrapper != null)
        {
            effectiveOnHit = (hit, pos) => {
                onHit(hit, pos);
                onHitWrapper(hit, pos);
            };
        }

        GameObject new_projectile = Instantiate(projectiles[which], where + direction.normalized * 1.1f, Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Deg2Rad));

        ProjectileMovement movement = MakeMovement(effectiveTrajectory, effectiveSpeed);
        if (movement == null)
        {
            Debug.LogWarning($"ProjectileManager: Unknown trajectory type '{effectiveTrajectory}', defaulting to straight.");
            movement = new StraightProjectileMovement(effectiveSpeed);
        }

        ProjectileController controller = new_projectile.GetComponent<ProjectileController>();
        controller.movement = movement;
        controller.OnHit += effectiveOnHit;
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