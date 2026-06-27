using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("=== 1. 核心引用 (拖拽赋值) ===")]
    [Tooltip("子弹的预制体 (Prefab)")]
    public GameObject bulletPrefab;

    [Tooltip("枪支旋转的轴心 (通常是 GunPivot 空物体)")]
    public Transform gunPivot;

    [Tooltip("子弹生成的枪口位置 (FirePoint)")]
    public Transform firePoint;

    [Tooltip("枪支的图片渲染器 (用来处理翻转)")]
    public SpriteRenderer gunRenderer;

    [Header("=== 2. 武器基础属性 (在 Inspector 设定) ===")]
    [Tooltip("手感核心：基础转向速度 (手枪建议30，重炮建议5)")]
    public float rotationSpeed = 20f;

    [Tooltip("一次发射几发子弹")]
    public int baseProjectileCount = 1;

    [Tooltip("多发子弹之间的夹角 (散布角度)")]
    public float spreadAngle = 15f;

    [Tooltip("基础伤害 (单发子弹伤害)")]
    public int baseDamage = 10;

    [Tooltip("基础射击间隔 (秒)")]
    public float baseFireInterval = 0.5f;

    [Tooltip("索敌范围")]
    public float attackRange = 6f;

    // --- 3. 内部运行时变量 (包含 基础值 + 商店加成) ---
    private float actualFireInterval;
    private int actualDamage;
    private int actualProjectileCount;
    private float currentSpreadAngle;
    private float currentRotationSpeed;

    private float nextFireTime = 0f;
    private Transform target;

    void Start()
    {
        // 游戏开始时，计算一次属性
        UpdateStats();
    }

    /// <summary>
    /// 当玩家升级或购买道具后，调用此方法刷新属性
    /// </summary>
    public void UpdateStats()
    {
        // 数据源重定向
        // 检查 PlayerStats 单例是否存在（防止场景单独测试时报错）
        if (PlayerStats.instance == null)
        {
            Debug.LogWarning("[WeaponController] 未找到 PlayerStats 实例，使用基础属性。");
            actualFireInterval = baseFireInterval;
            actualDamage = baseDamage;
            actualProjectileCount = baseProjectileCount;
            currentSpreadAngle = spreadAngle;
            currentRotationSpeed = rotationSpeed;
            return;
        }

        // --- 1. 计算射速 (除法模型: 射速加成越高，间隔越短) ---

        float speedBonus = PlayerStats.instance.GetStat(StatType.FireRate);
        actualFireInterval = baseFireInterval / (1f + speedBonus);

        // --- 2. 计算伤害 (加法模型) ---
        float damageBonus = PlayerStats.instance.GetStat(StatType.Attack);
        actualDamage = baseDamage + (int)damageBonus;

        // --- 3. 计算子弹数 (基础 + 额外技能) ---
        float extraProj = PlayerStats.instance.GetStat(StatType.ProjectileCount);
        actualProjectileCount = baseProjectileCount + (int)extraProj;

        // --- 4. 计算最终散布 (基础 + 商店修正) ---
        float spreadBonus = PlayerStats.instance.GetStat(StatType.SpreadAngle);
        currentSpreadAngle = Mathf.Max(0f, spreadAngle + spreadBonus);

        // --- 5. 计算最终转向速度 (基础 + 商店修正) ---
        float rotationBonus = PlayerStats.instance.GetStat(StatType.RotationSpeed);
        currentRotationSpeed = Mathf.Max(2f, rotationSpeed + rotationBonus);

        Debug.Log($"[武器系统] 属性同步完毕 | 伤害:{actualDamage} (+{damageBonus}) | 射速间隔:{actualFireInterval:F2}s");
    }

    void Update()
    {
        // 1. 持续寻找最近敌人
        FindNearestEnemy();
        if (target != null)
        {
            // 2. 让枪瞄准敌人 (使用最终转向速度)
            RotateGunToTarget();
            // 3. 射击倒计时
            if (Time.time >= nextFireTime)
            {
                // 二次检查距离 (防止敌人跑出去了枪还在射)
                float dist = Vector2.Distance(transform.position, target.position);
                if (dist <= attackRange)
                {
                    Shoot();
                    // 重置冷却时间
                    nextFireTime = Time.time + actualFireInterval;
                }
            }
        }
    }

    // --- 核心功能：枪支旋转与翻转 ---
    void RotateGunToTarget()
    {
        if (gunPivot == null || target == null) return;

        // 1. 计算目标角度
        Vector2 direction = target.position - gunPivot.position;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);

        // 使用 currentRotationSpeed (计算后的值) 进行插值
        gunPivot.rotation = Quaternion.Slerp(gunPivot.rotation, targetRotation, currentRotationSpeed * Time.deltaTime);

        // 3. 处理翻转 (Flip Y)
        if (gunRenderer != null)
        {
            if (Mathf.Abs(targetAngle) > 90)
                gunRenderer.flipY = true;
            else
                gunRenderer.flipY = false;
        }
    }

    // --- 核心功能：多重射击 ---
    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogError("没有拖拽 BulletPrefab 或 FirePoint！无法射击！");
            return;
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFXWithPitch("Shoot", 0.8f, 1.2f);
        }

        // 获取当前枪口的绝对角度
        float currentAngle = gunPivot.rotation.eulerAngles.z;

        // 计算扇形的起始偏移量
        float startOffset = 0;

        // 使用 currentSpreadAngle (计算后的散布值)
        if (actualProjectileCount > 1)
        {
            startOffset = -(actualProjectileCount - 1) * currentSpreadAngle / 2f;
        }

        // 循环生成每一颗子弹
        for (int i = 0; i < actualProjectileCount; i++)
        {
            // 1. 计算这颗子弹的最终角度
            float angleStep = startOffset + (i * currentSpreadAngle);
            float finalAngle = currentAngle + angleStep;
            Quaternion rotation = Quaternion.Euler(0, 0, finalAngle);

            GameObject bullet = ObjectPool.Spawn(bulletPrefab, firePoint.position, rotation);

            // 3. 注入伤害数值
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.damage = actualDamage;
            }
        }
    }

    void FindNearestEnemy()
    {
        float minDistance = Mathf.Infinity;
        Transform nearest = null;

        foreach (EnemyHealth enemy in EnemyHealth.allEnemies)
        {
            if (enemy == null || enemy.isDead) continue;

            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = enemy.transform;
            }
        }

        target = (nearest != null && minDistance <= attackRange) ? nearest : null;
    }

    // --- 编辑器辅助：画出攻击范围圈 ---
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}