using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("关卡配置列表")]
    public List<LevelConfig> allLevels;

    [Header("生成通用设置")]
    public Transform player;       
    public float spawnRadius = 10f; 

    [Header("防卡墙设置")]
    public LayerMask obstacleLayer; 
    public float checkRadius = 0.5f; 

    // --- 内部变量 ---
    private float timer;
    private int currentLevelIndex;
    private int enemiesSpawnedSoFar = 0;    
    private LevelConfig currentConfig;

    void Start()
    {
      
        
        // 从统一的 JSON 存档读取关卡进度
        SaveData data = SaveSystem.Load();
        currentLevelIndex = data.currentLevelIndex;

        // 安全校验：防止配了3关，结果存档里是第100关，导致数组越界
        if (currentLevelIndex > allLevels.Count)
        {
            // 策略：如果通关了，就循环最后一关，或者显示通关
            currentLevelIndex = allLevels.Count; 
        }

        // 2. 初始化关卡数据
        SetupLevel();
    }

    void SetupLevel()
    {
        // 数组索引是从0开始，关卡是从1开始，所以要减1
        int configIndex = currentLevelIndex - 1;

        if (allLevels.Count > 0)
        {
            // 钳制索引，防止报错
            configIndex = Mathf.Clamp(configIndex, 0, allLevels.Count - 1);

            currentConfig = allLevels[configIndex];

            // 告诉管理器目标
            if (GameLevelManager.instance != null)
            {
                GameLevelManager.instance.totalEnemiesToKill = currentConfig.totalEnemies;
            }

            Debug.Log($"[关卡系统] 加载关卡 {currentLevelIndex}: {currentConfig.levelName} (目标: {currentConfig.totalEnemies})");
        }
        else
        {
            Debug.LogError("错误：关卡配置列表为空！");
        }
    }

    void Update()
    {
        // 逻辑保护：没配置、刷完了、主角死了 -> 都不刷怪
        if (allLevels.Count == 0 || 
            enemiesSpawnedSoFar >= currentConfig.totalEnemies || 
            player == null)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= currentConfig.spawnInterval)
        {
            SpawnEnemy();
            timer = 0;
        }
    }

    void SpawnEnemy()
    {
        if (currentConfig.enemyPrefabs == null || currentConfig.enemyPrefabs.Count == 0) return;

        GameObject randomEnemy = currentConfig.enemyPrefabs[Random.Range(0, currentConfig.enemyPrefabs.Count)];

        // ================================================================
        // 优化：寻找位置逻辑 
        // ================================================================
        Vector2 spawnPos = Vector2.zero;
        bool validPositionFound = false;
        int maxAttempts = 15; 
        int currentAttempt = 0;

        while (!validPositionFound && currentAttempt < maxAttempts)
        {
            currentAttempt++;
            // 在圆环边缘随机取点
            spawnPos = (Vector2)player.position + Random.insideUnitCircle.normalized * spawnRadius;

            Collider2D hit = Physics2D.OverlapCircle(spawnPos, checkRadius, obstacleLayer);
            if (hit == null)
            {
                validPositionFound = true;
            }
        }

        if (validPositionFound)
        {
            Instantiate(randomEnemy, spawnPos, Quaternion.identity);
            enemiesSpawnedSoFar++;
        }
    }

    // 辅助显示范围
    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.position, spawnRadius);
        }
    }
}