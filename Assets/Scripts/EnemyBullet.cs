using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed = 5f;
    public int damage = 1;
    public float lifeTime = 3f;

    private float lifeTimer;

    public void SetDirection(Vector2 direction)
    {
        Vector2 normalized = direction.normalized;
        float angle = Mathf.Atan2(normalized.y, normalized.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }

    void OnEnable()
    {
        lifeTimer = lifeTime;
    }

    void Update()
    {
        transform.Translate(Vector3.right * speed * Time.deltaTime, Space.Self);

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            ObjectPool.Despawn(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHp = other.GetComponent<PlayerHealth>();
            if (playerHp != null)
            {
                playerHp.TakeDamage(damage);
            }
            ObjectPool.Despawn(gameObject);
        }
        else if (other.CompareTag("Wall"))
        {
            ObjectPool.Despawn(gameObject);
        }
    }
}