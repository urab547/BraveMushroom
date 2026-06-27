using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("状态监测")]
    // 实际的 MaxHealth 数值由 PlayerStats 管理。
    public float currentHealth;

    // 使用属性 (Property) 动态获取最大血量
    // 每次调用 MaxHealth 时，都会去问 PlayerStats 要最新的值
    public float MaxHealth
    {
        get
        {
            if (PlayerStats.instance != null)
                return PlayerStats.instance.GetStat(StatType.Health);
            return 10f; // 防报错默认值
        }
    }

    [Header("UI 绑定")]
    public Slider healthSlider;
    public TMP_Text hpText;
    public GameObject youDiedUI;

    [Header("动画组件")]
    public Animator playerAnimator;

    private bool isDead = false;

    void Start()
    {

        // 游戏开始时，将当前血量设为最大值
        currentHealth = MaxHealth;

        UpdateHealthUI();

        if (youDiedUI != null) youDiedUI.SetActive(false);
    }

    // 购买逻辑现在由 PlayerStats.ApplyItem() 负责，
    // 它会自动更新数值并保存，本脚本只需要通过 MaxHealth 属性读取结果即可。

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFXWithPitch("PlayerHurt", 2.0f, 3.0f);
        }

        if (currentHealth < 0) currentHealth = 0;

        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth += amount;

        // 使用动态的 MaxHealth 进行上限钳制
        if (currentHealth > MaxHealth) currentHealth = MaxHealth;

        UpdateHealthUI();
    }

    void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            // 确保 Slider 也是用最新的最大值
            healthSlider.maxValue = MaxHealth;
            healthSlider.value = currentHealth;
        }

        if (hpText != null)
        {
            // 文本显示
            hpText.text = currentHealth + " / " + MaxHealth;
        }
    }

    // 死亡处理
    void Die()
    {
        if (isDead) return;
        isDead = true;
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFXWithPitch("PlayerDeath", 4.0f, 5.0f);
        }

        PlayerController moveScript = GetComponent<PlayerController>();
        if (moveScript != null) moveScript.enabled = false;

        WeaponController weaponScript = GetComponent<WeaponController>();
        if (weaponScript != null) weaponScript.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        FreezeAllEnemies();

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("Die");
        }
        else
        {
            // 如果没有动画，简单的旋转倒下
            transform.Rotate(0, 0, 90);
        }

        yield return new WaitForSeconds(1.0f);

        if (youDiedUI != null)
        {
            youDiedUI.SetActive(true);
        }

        yield return new WaitForSeconds(1.0f);

        SceneManager.LoadScene("GameOverScene");
    }

    void FreezeAllEnemies()
    {
        foreach (EnemyHealth enemy in EnemyHealth.allEnemies)
        {
            if (enemy == null) continue;

            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            if (ai != null) ai.enabled = false;

            BossAI boss = enemy.GetComponent<BossAI>();
            if (boss != null) boss.enabled = false;

            EnemyDamage dmg = enemy.GetComponent<EnemyDamage>();
            if (dmg != null) dmg.enabled = false;

            Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                enemyRb.linearVelocity = Vector2.zero;
                enemyRb.simulated = false;
            }

            Animator enemyAnim = enemy.GetComponent<Animator>();
            if (enemyAnim != null) enemyAnim.enabled = false;
        }
    }
}