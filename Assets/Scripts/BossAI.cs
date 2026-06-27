using UnityEngine;
using System.Collections;

public class BossAI : MonoBehaviour
{
    [Header("Boss 移动属性")]
    public float moveSpeed = 2f;      // 平时溜达速度 (慢)
    public float chargeSpeed = 10f;   // 冲锋速度 (快)
    public float stopDistance = 5f;   // 远程射击时的停车距离

    [Header("防止穿模设置 (关键)")]
    public float minContactDistance = 1.6f;

    [Header("伤害数值")]
    public int meleeDamage = 20;
    public int rangedDamage = 10;

    [Header("技能通用设置")]
    public float attackCooldown = 1.5f; // 两次攻击动作之间的间隔

    [Header("节奏控制")]
    public float postShootDelay = 1.2f; // 射击后的发呆时间

    [Header("远程技能配置 (散弹)")]
    public GameObject bulletPrefab;
    public int bulletCount = 5;
    public float spreadAngle = 15f;
    public int shotsBeforeCharge = 2; // 射击几次后切换冲锋？

    [Header("近战技能配置")]
    public float meleeRange = 2.2f;

    [Header("近战攻击微调")]
    [Tooltip("红圈消失后，经过多少秒才真正计算伤害（用于对齐动画攻击帧）")]
    public float meleeDamageDelay = 0.5f;

    // ==========================================
    // 近战前摇视觉设置 
    // ==========================================
    [Header("近战前摇设置 (视觉表现)")]
    [Tooltip("攻击前摇时间，红圈变红需要多久")]
    public float meleePreparationTime = 0.5f;

    [Tooltip("拖入子物体：一个带有圆形SpriteRenderer的对象")]
    public Transform attackRangeIndicator;

    private SpriteRenderer rangeSprite; // 用于控制红圈透明度
    private bool isPreparingMelee = false; // 新状态：是否正在进行攻击前摇？
    private float prepareTimer = 0f;      // 前摇计时器
    // ==========================================

    private Transform player;
    private float timer; // 常规技能冷却计时器
    private bool isDead = false;

    // 僵直状态标记
    private bool isRecovering = false;

    // 状态控制
    private bool wantsToMelee = false;
    private int shotCounter = 0;

    // 组件
    private Animator anim;
    private SpriteRenderer sr; // 用于翻转身体

    void Start()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>(); // 获取 SpriteRenderer

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        wantsToMelee = false;
        timer = attackCooldown;

        // 初始化红圈
        if (attackRangeIndicator != null)
        {
            rangeSprite = attackRangeIndicator.GetComponent<SpriteRenderer>();
            if (rangeSprite != null)
            {
                Color c = Color.red;
                c.a = 0f;
                rangeSprite.color = c;
                float targetDiameter = meleeRange * 2f;
                float parentX = transform.lossyScale.x != 0 ? transform.lossyScale.x : 1;
                float parentY = transform.lossyScale.y != 0 ? transform.lossyScale.y : 1;
                attackRangeIndicator.localScale = new Vector3(targetDiameter / Mathf.Abs(parentX), targetDiameter / Mathf.Abs(parentY), 1f);
                attackRangeIndicator.gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (player == null || isDead) return;

        // 1. 优先处理前摇状态 (禁止移动和转向)
        if (isPreparingMelee)
        {
            if (anim != null) anim.SetBool("isWalking", false);
            HandleMeleePreparation();
            return;
        }

        // 2. 优先处理僵直状态 (禁止移动和转向)
        if (isRecovering)
        {
            if (anim != null) anim.SetBool("isWalking", false);
            return;
        }

        // 只有在非攻击、非僵直状态下，Boss才应该看向玩家
        FacePlayer();

        float distance = Vector2.Distance(transform.position, player.position);
        float currentTargetDist = wantsToMelee ? minContactDistance : stopDistance;

        // 3. 移动逻辑
        bool isMoving = false;
        if (distance > currentTargetDist)
        {
            float actualSpeed = wantsToMelee ? chargeSpeed : moveSpeed;
            transform.position = Vector2.MoveTowards(transform.position, player.position, actualSpeed * Time.deltaTime);
            isMoving = true;
        }
        if (anim != null) anim.SetBool("isWalking", isMoving);

        // 4. 攻击循环判定
        timer += Time.deltaTime;
        if (timer >= attackCooldown)
        {
            if (wantsToMelee && distance <= meleeRange)
            {
                StartMeleePreparation();
            }
            else if (!wantsToMelee)
            {
                PerformShotgunAttack();
                timer = 0;
            }
        }
    }

    // ⚡ 控制 Boss 面朝方向
    void FacePlayer()
    {
        if (player == null || sr == null) return;

        // 如果玩家在 Boss 右边 (x > Boss.x)，Boss 应该不翻转 
        if (player.position.x > transform.position.x)
        {
            sr.flipX = false; // 朝右
        }
        else if (player.position.x < transform.position.x)
        {
            sr.flipX = true;  // 朝左
        }
    }

    // ==========================================
    // 处理前摇
    // ==========================================
    void StartMeleePreparation()
    {
        isPreparingMelee = true;
        prepareTimer = 0f;

        if (AudioManager.instance != null)
        {
            StartCoroutine(AudioManager.instance.PlaySFXAndResetPitch("BossCharge", meleePreparationTime));
        }

        if (attackRangeIndicator != null)
        {
            attackRangeIndicator.gameObject.SetActive(true);
            if (rangeSprite != null)
            {
                Color c = Color.red; c.a = 0f; rangeSprite.color = c;
            }
        }
    }

    void HandleMeleePreparation()
    {
        prepareTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(prepareTimer / meleePreparationTime);
        if (rangeSprite != null)
        {
            Color c = rangeSprite.color;
            c.a = Mathf.Lerp(0f, 0.6f, progress);
            rangeSprite.color = c;
        }
        if (prepareTimer >= meleePreparationTime)
        {
            PerformMeleeAttack();
        }
    }

    // 发起攻击动作，但伤害延迟判定
    void PerformMeleeAttack()
    {
        Debug.Log("Boss：开始攻击动作！");

        // 1. 触发近战动画
        if (anim != null) anim.SetTrigger("Melee");

        // 2. 启动协程：在 0.5s 后判定伤害
        StartCoroutine(MeleeDamageRoutine());

        // 3. 立即清理红圈和前摇状态
        if (attackRangeIndicator != null) attackRangeIndicator.gameObject.SetActive(false);
        isPreparingMelee = false;

        // 4. 重置战术逻辑
        wantsToMelee = false;
        shotCounter = 0;
        timer = 0;
    }

    // 延迟伤害判定协程
    IEnumerator MeleeDamageRoutine()
    {
        // 先播挥舞音效（如果有）
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX("BossMelee");
        }

        // 等待设定的延迟时间 (例如 0.5s)
        yield return new WaitForSeconds(meleeDamageDelay);

        Debug.Log("伤害判定生效！");

        // 进行距离检测并扣血
        if (player != null && Vector2.Distance(transform.position, player.position) <= meleeRange)
        {
            PlayerHealth playerHp = player.GetComponent<PlayerHealth>();
            if (playerHp != null)
            {
                playerHp.TakeDamage(meleeDamage);
            }
        }
    }

    void PerformShotgunAttack()
    {
        if (bulletPrefab == null) return;

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX("BossShoot");
        }

        if (anim != null) anim.SetTrigger("Ranged");

        Vector2 dirToPlayer = player.position - transform.position;
        float baseAngle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
        float startOffset = -(spreadAngle * (bulletCount - 1)) / 2f;

        for (int i = 0; i < bulletCount; i++)
        {
            float currentOffset = startOffset + (spreadAngle * i);
            Quaternion rotation = Quaternion.AngleAxis(baseAngle + currentOffset, Vector3.forward);
            GameObject bullet = ObjectPool.Spawn(bulletPrefab, transform.position, rotation);
            EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
            if (bulletScript != null)
            {
                bulletScript.SetDirection(rotation * Vector3.right);
                bulletScript.SetDamage(rangedDamage);
            }
        }

        StartCoroutine(RecoverRoutine());
        shotCounter++;
        if (shotCounter >= shotsBeforeCharge)
        {
            wantsToMelee = true;
            timer = attackCooldown;
        }
    }

    IEnumerator RecoverRoutine()
    {
        isRecovering = true;
        yield return new WaitForSeconds(postShootDelay);
        isRecovering = false;
    }
}