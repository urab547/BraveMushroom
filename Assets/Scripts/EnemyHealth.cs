using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class EnemyHealth : MonoBehaviour
{
    public static readonly List<EnemyHealth> allEnemies = new List<EnemyHealth>();

    [Header("基础属性")]
    public int maxHealth = 20;
    private int currentHealth;

    [Header("Boss 专用设置")]
    public bool isBoss = false;
    public bool isDead = false;

    [Header("掉落设置")]
    public GameObject coinPrefab;
    [Range(0f, 1f)]
    public float dropChance = 0.5f;

    // 内部变量
    private Slider bossSlider;
    private Coroutine healthUpdateCoroutine;

    // 组件引用
    private Animator anim;
    private Collider2D col;

    void OnEnable()
    {
        allEnemies.Add(this);
    }

    void OnDisable()
    {
        allEnemies.Remove(this);
    }

    void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();

        if (isBoss)
        {
            GameObject canvas = GameObject.Find("Canvas");
            Transform uiTr = null;

            if (canvas != null)
            {
                uiTr = canvas.transform.Find("BossHealthUI");
            }

            if (uiTr != null)
            {
                bossSlider = uiTr.GetComponent<Slider>();
                bossSlider.gameObject.SetActive(true);
                bossSlider.maxValue = maxHealth;
                bossSlider.value = currentHealth;

                CanvasGroup group = bossSlider.GetComponent<CanvasGroup>();
                if (group == null) group = bossSlider.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 1f;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        // 死了就别扣血了，防止鞭尸
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        // === 1. 动态血条减少 ===
        if (isBoss && bossSlider != null)
        {
            if (healthUpdateCoroutine != null) StopCoroutine(healthUpdateCoroutine);
            healthUpdateCoroutine = StartCoroutine(SmoothUpdateHealth());
        }

        if (currentHealth <= 0)
        {
            if (gameObject.activeSelf)
            {
                Die();
            }
        }
    }

    IEnumerator SmoothUpdateHealth()
    {
        float speed = 5f;
        while (Mathf.Abs(bossSlider.value - currentHealth) > 0.1f)
        {
            bossSlider.value = Mathf.Lerp(bossSlider.value, currentHealth, Time.deltaTime * speed);
            yield return null;
        }
        bossSlider.value = currentHealth;
    }

    void Die()
    {
        // 防止多次触发
        if (isDead) return;
        isDead = true;

        // 1. 播放音效
        if (AudioManager.instance != null)
        {
            // AudioManager.instance.PlaySFXWithPitch("EnemyDeath", 0.9f, 1.1f);
        }

        // 2. 触发死亡动画
        if (anim != null) anim.SetTrigger("Die");

        // 3. 禁用除了本脚本以外的所有 MonoBehaviour
        // 这会一键关掉 EnemyAI, EnemyAttack, BossAI 等所有逻辑脚本
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            // 排除 EnemyHealth (自己) 和 Animator (要播动画)
            if (script != this)
            {
                script.enabled = false;
            }
        }

        // 4. 停止该物体上所有正在运行的协程 (比如攻击倒计时、以及未完成的血条动画)
        // 注意：这一步会打断 SmoothUpdateHealth，导致血条卡在半路
        StopAllCoroutines();

        // 5. 禁用碰撞体
        if (col != null) col.enabled = false;

        // 6. 停止物理模拟 (防止尸体滑行)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        if (GameLevelManager.instance != null) GameLevelManager.instance.OnEnemyKilled();

        // 掉落金币逻辑
        if (coinPrefab != null)
        {
            if (Random.value <= dropChance)
                ObjectPool.Spawn(coinPrefab, transform.position, Quaternion.identity);
        }

        // === 2. 死亡处理 ===
        // 因为 StopAllCoroutines 停止了 UI 动画，我们需要重启死亡序列
        if (isBoss && bossSlider != null)
        {
            // 既然死了，强制将 UI 归零
            bossSlider.value = 0f;

            StartCoroutine(BossDeathSequence());
        }
        else
        {
            StartCoroutine(NormalEnemyDeathSequence());
        }
    }

    // === 普通怪物死亡序列 ===
    IEnumerator NormalEnemyDeathSequence()
    {
        yield return new WaitForSeconds(0.5f);

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFXWithPitch("Explode", 0.8f, 1.0f);
        }

        Destroy(gameObject);
    }

    IEnumerator BossDeathSequence()
    {
        yield return new WaitForSeconds(2.0f);

        // 血条淡出逻辑
        if (bossSlider != null)
        {
            CanvasGroup group = bossSlider.GetComponent<CanvasGroup>();
            if (group == null) group = bossSlider.gameObject.AddComponent<CanvasGroup>();

            float fadeDuration = 1.0f;
            float timer = 0f;
            // 这里的 startVal 肯定是 0，因为我们在 Die() 里强制设为 0 了
            // 但保留 Lerp 逻辑可以让 Alpha 淡出更自然
            float startVal = bossSlider.value;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / fadeDuration;
                group.alpha = Mathf.Lerp(1f, 0f, progress);
                // 确保值始终锁定在 0
                bossSlider.value = 0f;
                yield return null;
            }

            group.alpha = 0f;
            bossSlider.gameObject.SetActive(false);
        }

        Destroy(gameObject);
    }
}