using UnityEngine;

public class ExpOrb : MonoBehaviour
{

    public float magnetRange = 3f; // 磁铁范围
    public float flySpeed = 10f;   // 飞行速度
    private Transform player;
    public int expAmount = 5; // 一个球给多少经验


    void Start()
    {
        // 找玩家
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player != null)
        {
            // 计算距离
            float distance = Vector2.Distance(transform.position, player.position);

            // ?1?7?1?7?1?7?1?7?1?0?1?7?1?7?1?7?1?7?1?7Χ?1?7?1?7
            if (distance < magnetRange)
            {
                // 飞向玩家 (Lerp 插值或者 MoveTowards)
                transform.position = Vector2.MoveTowards(transform.position, player.position, flySpeed * Time.deltaTime);
            }
        }
    }

    // 当有人走进触发器
    void OnTriggerEnter2D(Collider2D other)
    {
        // 如果是玩家
        if (other.CompareTag("Player"))
        {
            // 获取玩家身上的升级系统
            LevelSystem levelSystem = other.GetComponent<LevelSystem>();

            if (levelSystem != null)
            {
                // 加经验
                levelSystem.AddExp(expAmount);
                // 销毁球
                Destroy(gameObject);
            }
        }
    }
}