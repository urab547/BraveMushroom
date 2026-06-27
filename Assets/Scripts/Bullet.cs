using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage = 10;

    public float speed = 10f;
    public float lifeTime = 3f;

    private float lifeTimer;

    void OnEnable()
    {
        lifeTimer = lifeTime;
    }

    void Update()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime);

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            ObjectPool.Despawn(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
            ObjectPool.Despawn(gameObject);
        }
        else if (other.CompareTag("Wall"))
        {
            ObjectPool.Despawn(gameObject);
        }
    }
}