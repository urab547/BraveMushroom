using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    public int damage = 1; // 撞一下扣多少血

    // Unity自带的碰撞检测函数：当两个Collider碰到时触发
    void OnCollisionEnter2D(Collision2D collision)
    {
        // 如果碰到的是玩家
        if (collision.gameObject.CompareTag("Player"))
        {
            // 尝试获取玩家身上的血量脚本
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();

            // 如果对方真的有血量脚本，就扣血
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }
    }
}