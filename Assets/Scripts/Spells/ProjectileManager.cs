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
        // 添加索引安全检查
        if (which < 0 || which >= projectiles.Length)
        {
            Debug.LogWarning($"ProjectileManager: 索引 {which} 超出范围，最大值为 {projectiles.Length-1}，使用索引 0 代替。");
            which = 0; // 使用默认值
        }
        
        GameObject new_projectile = Instantiate(projectiles[which], where + direction.normalized*1.1f, Quaternion.Euler(0,0,Mathf.Atan2(direction.y, direction.x)*Mathf.Rad2Deg));
        
        // 创建移动组件并确保非空
        ProjectileMovement movement = MakeMovement(trajectory, speed);
        if (movement == null)
        {
            Debug.LogWarning($"ProjectileManager: 未知轨迹类型 '{trajectory}'，使用直线轨迹。");
            movement = new StraightProjectileMovement(speed);
        }
        
        // 设置投射物控制器
        ProjectileController controller = new_projectile.GetComponent<ProjectileController>();
        controller.movement = movement;
        controller.OnHit += onHit;
    }

    public void CreateProjectile(int which, string trajectory, Vector3 where, Vector3 direction, float speed, Action<Hittable, Vector3> onHit, float lifetime)
    {
        // 添加索引安全检查
        if (which < 0 || which >= projectiles.Length)
        {
            Debug.LogWarning($"ProjectileManager: 索引 {which} 超出范围，最大值为 {projectiles.Length-1}，使用索引 0 代替。");
            which = 0; // 使用默认值
        }
        
        GameObject new_projectile = Instantiate(projectiles[which], where + direction.normalized * 1.1f, Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
        
        // 创建移动组件并确保非空
        ProjectileMovement movement = MakeMovement(trajectory, speed);
        if (movement == null)
        {
            Debug.LogWarning($"ProjectileManager: 未知轨迹类型 '{trajectory}'，使用直线轨迹。");
            movement = new StraightProjectileMovement(speed);
        }
        
        // 设置投射物控制器
        ProjectileController controller = new_projectile.GetComponent<ProjectileController>();
        controller.movement = movement;
        controller.OnHit += onHit;
        controller.SetLifetime(lifetime);
    }

    public ProjectileMovement MakeMovement(string name, float speed)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("ProjectileManager: 轨迹名称为空，使用直线轨迹。");
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
            default:
                Debug.LogWarning($"ProjectileManager: 未知轨迹类型 '{name}'，使用直线轨迹。");
                return new StraightProjectileMovement(speed);
        }
    }
}