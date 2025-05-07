using UnityEngine;
using System;

public class ProjectileManager : MonoBehaviour
{
    public GameObject[] projectiles;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameManager.Instance.projectileManager = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CreateProjectile(int which, string trajectory, Vector3 where, Vector3 direction, float speed, Action<Hittable,Vector3> onHit)
    {
        // Add index safety check
        if (which < 0 || which >= projectiles.Length)
        {
            Debug.LogWarning($"ProjectileManager: Index {which} out of range, max is {projectiles.Length-1}, using index 0 instead.");
            which = 0; // Use default value
        }
        
        GameObject new_projectile = Instantiate(projectiles[which], where + direction.normalized*1.1f, Quaternion.Euler(0,0,Mathf.Atan2(direction.y, direction.x)*Mathf.Rad2Deg));
        
        // Create movement component and ensure non-null
        ProjectileMovement movement = MakeMovement(trajectory, speed);
        if (movement == null)
        {
            Debug.LogWarning($"ProjectileManager: Unknown trajectory type '{trajectory}', using straight trajectory.");
            movement = new StraightProjectileMovement(speed);
        }
        
        // Set projectile controller
        ProjectileController controller = new_projectile.GetComponent<ProjectileController>();
        controller.movement = movement;
        controller.OnHit += onHit;
    }

    public void CreateProjectile(int which, string trajectory, Vector3 where, Vector3 direction, float speed, Action<Hittable, Vector3> onHit, float lifetime)
    {
        // Add index safety check
        if (which < 0 || which >= projectiles.Length)
        {
            Debug.LogWarning($"ProjectileManager: Index {which} out of range, max is {projectiles.Length-1}, using index 0 instead.");
            which = 0; // Use default value
        }
        
        GameObject new_projectile = Instantiate(projectiles[which], where + direction.normalized * 1.1f, Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Deg2Rad));
        
        // Create movement component and ensure non-null
        ProjectileMovement movement = MakeMovement(trajectory, speed);
        if (movement == null)
        {
            Debug.LogWarning($"ProjectileManager: Unknown trajectory type '{trajectory}', using straight trajectory.");
            movement = new StraightProjectileMovement(speed);
        }
        
        // Set projectile controller
        ProjectileController controller = new_projectile.GetComponent<ProjectileController>();
        controller.movement = movement;
        controller.OnHit += onHit;
        controller.SetLifetime(lifetime);
    }

    public ProjectileMovement MakeMovement(string name, float speed)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("ProjectileManager: Trajectory name is empty, using straight trajectory.");
            return new StraightProjectileMovement(speed);
        }
        
        switch (name.ToLower())
        {
            case "straight":
                return new StraightProjectileMovement(speed);
            case "homing":
                return new HomingProjectileMovement(speed);
            case "spiraling":
                return new SpiralingProjectileMovement(speed);
            // You could add a combined homing+spiraling movement type here in the future
            default:
                Debug.LogWarning($"ProjectileManager: Unknown trajectory type '{name}', using straight trajectory.");
                return new StraightProjectileMovement(speed);
        }
    }
}