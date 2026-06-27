using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "BraveMushroom/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("关卡信息")]
    public string levelName;

    [Header("关卡参数")]
    public float levelDuration = 60f;
    public int totalEnemies = 10;
    public float spawnInterval = 2f;

    [Header("敌人配置")]
    public List<GameObject> enemyPrefabs;
}
