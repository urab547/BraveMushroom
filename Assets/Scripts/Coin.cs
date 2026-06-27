using UnityEngine;

public class CoinController : MonoBehaviour
{
    [Header("金币设置")]
    public int goldAmount = 1;
    public float flySpeed = 5f;
    public float acceleration = 10f;
    public float waitTime = 0.5f;

    private Transform playerTransform;
    private bool canFly = false;
    private float initialFlySpeed;

    void Awake()
    {
        initialFlySpeed = flySpeed;
    }

    void OnEnable()
    {
        canFly = false;
        flySpeed = initialFlySpeed;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        CancelInvoke();
        Invoke("StartFlying", waitTime);
    }

    void StartFlying()
    {
        canFly = true;
    }

    void Update()
    {
        if (!canFly || playerTransform == null) return;

        transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, flySpeed * Time.deltaTime);
        flySpeed += acceleration * Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            CollectCoin();
        }
    }

    void CollectCoin()
    {
        if (GameLevelManager.instance != null)
        {
            GameLevelManager.instance.AddTempGold(goldAmount);
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFXWithPitch("Coin", 0.9f, 1.2f);
        }

        ObjectPool.Despawn(gameObject);
    }
}