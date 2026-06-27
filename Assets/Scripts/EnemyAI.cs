using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    // 定义枚举：做一个下拉菜单，选它是近战还是远程
    public enum AttackType { Melee, Ranged }

    [Header("类型设置")]
    public AttackType enemyType; // Melee(近战) 或 Ranged(远程)

    [Header("移动设置")]
    public float moveSpeed = 2f;
    public float stopDistance = 0.5f; // 离主角多远停下来？

    [Header("攻击设置")]
    public int damage = 1;
    public float attackRange = 1f;    // 攻击范围
    public float attackCooldown = 2f; // 几秒打一次？

    [Header("远程专用")]
    public GameObject bulletPrefab;   // 把子弹预制体拖到这里

    private Transform player;
    private float attackTimer;

    // [新增] 动画组件引用
    private Animator anim;

    void Start()
    {
        // [新增] 获取动画组件
        anim = GetComponent<Animator>();

        // 自动寻找主角 
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        attackTimer = attackCooldown;
    }

    void Update()
    {
        if (player == null) return;

        // 计算怪物和主角的距离
        float distance = Vector2.Distance(transform.position, player.position);

        // 1. --- 移动逻辑 ---
        if (distance > stopDistance)
        {
            transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);

            // [新增] 切换为走路动画
            if (anim != null) anim.SetBool("isWalking", true);
        }
        else
        {
            // [新增] 切换为待机动画
            if (anim != null) anim.SetBool("isWalking", false);
        }

        // 2. --- 攻击逻辑 ---
        if (distance <= attackRange)
        {
            attackTimer += Time.deltaTime;

            if (attackTimer >= attackCooldown)
            {
                // [新增] 触发攻击动画
                if (anim != null) anim.SetTrigger("Attack");

                PerformAttack(); // 执行攻击数值计算
                attackTimer = 0;
            }
        }
    }

    void PerformAttack()
    {
        // 根据类型执行不同的攻击
        if (enemyType == AttackType.Melee)
        {
            // === 近战：直接扣血 ===
            PlayerHealth playerHp = player.GetComponent<PlayerHealth>();
            if (playerHp != null)
            {
                playerHp.TakeDamage(damage);
                Debug.Log("近战怪咬了你一口！");
            }
        }
        else if (enemyType == AttackType.Ranged)
        {
            // === 远程：发射子弹 ===
            if (bulletPrefab != null)
            {
                GameObject bullet = ObjectPool.Spawn(bulletPrefab, transform.position, Quaternion.identity);
                Vector2 dir = player.position - transform.position;
                bullet.GetComponent<EnemyBullet>().SetDirection(dir);
                Debug.Log("远程怪射出了一发子弹！");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}