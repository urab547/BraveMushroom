using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    // 这种结构体用于解决 JSON 不能直接存字典的问题
    [System.Serializable]
    public struct StatBonus
    {
        public StatType type; // 属性类型
        public float value;   // 累积增加的数值 (比如 +105)

    }

    public List<StatBonus> statBonuses; // 列表：存所有属性的加成
    public int coins; // 金币
    public int currentLevelIndex; // 当前关卡索引

    // 记录当前是否在商店里 (防止误退出后重开丢失进度)
    public bool isInShop;

    // 记录当前拿着什么武器 (存 ID 或 Name)
    public string currentWeaponID;

    public float masterVolume = 1.0f;
    public float musicVolume = 0.5f;
    public float sfxVolume = 1.0f;

    // 构造函数：初始化空列表
    public SaveData()
    {
        statBonuses = new List<StatBonus>();
        coins = 0;
        currentLevelIndex = 1;
        isInShop = false;
        currentWeaponID = ""; // 默认为空

        masterVolume = 1.0f;
        musicVolume = 0.5f;
        sfxVolume = 1.0f;
    }
}