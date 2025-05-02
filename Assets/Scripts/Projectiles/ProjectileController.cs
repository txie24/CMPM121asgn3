using UnityEngine;
using System;
using System.Collections;

public class ProjectileController : MonoBehaviour
{
    public float lifetime;
    public event Action<Hittable, Vector3> OnHit;
    public ProjectileMovement movement;

    void Start()
    {
        // Automatically assign movement if not assigned manually
        if (movement == null)
        {
            movement = GetComponent<ProjectileMovement>();
            if (movement == null)
            {
                Debug.LogWarning("ProjectileController: Missing ProjectileMovement component.");
            }
        }

        // Start the expiration timer
        if (lifetime > 0)
        {
            StartCoroutine(Expire(lifetime));
        }
    }

    void Update()
    {
        if (movement != null)
        {
            movement.Movement(transform);
        }
        else
        {
            Debug.LogWarning("ProjectileController: Cannot move projectile, movement is null.");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("projectile")) return;

        if (collision.gameObject.CompareTag("unit"))
        {
            var ec = collision.gameObject.GetComponent<EnemyController>();
            if (ec != null && OnHit != null)
            {
                OnHit.Invoke(ec.hp, transform.position);
            }
            else
            {
                var pc = collision.gameObject.GetComponent<PlayerController>();
                if (pc != null && OnHit != null)
                {
                    OnHit.Invoke(pc.hp, transform.position);
                }
            }
        }

        Destroy(gameObject);
    }

    public void SetLifetime(float lifetime)
    {
        this.lifetime = lifetime;
        StartCoroutine(Expire(lifetime));
    }

    IEnumerator Expire(float lifetime)
    {
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject);
    }
}
