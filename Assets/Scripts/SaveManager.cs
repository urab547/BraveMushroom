using UnityEngine;
using System.IO; // 必须引用 IO 来检查文件是否存在

public static class SaveManager
{
    // 存档路径 (必须与 SaveSystem.cs 中的路径保持一致)
    private static string savePath = Application.persistentDataPath + "/savegame.json";

    // 检查是否存在存档
    public static bool HasSaveFile()
    {
        // 只要 savegame.json 文件存在，就说明有存档
        return File.Exists(savePath);
    }

    // 创建新游戏（初始化数据）
    public static void CreateNewGame()
    {

        // 智能重置逻辑
        // 1. 先读取旧存档，备份音量设置
        // (防止玩家点“新游戏”后，音量突然变回默认值)
        SaveData oldData = SaveSystem.Load();
        float bakMaster = oldData.masterVolume;
        float bakMusic = oldData.musicVolume;
        float bakSfx = oldData.sfxVolume;

        // 2. 物理删除旧存档
        SaveSystem.DeleteSave();

        // 3. 构建全新数据对象
        SaveData newData = new SaveData();

        // 4. 恢复音量设置
        newData.masterVolume = bakMaster;
        newData.musicVolume = bakMusic;
        newData.sfxVolume = bakSfx;

        // 5. 初始化核心游戏数据
        // (SaveData 构造函数默认就是 Level 1, Gold 0，这里显式赋值是为了逻辑清晰)
        newData.currentLevelIndex = 1;
        newData.coins = 0;
        newData.currentWeaponID = ""; // 重置武器
        newData.isInShop = false;     // 重置状态

        // 6. 写入磁盘
        SaveSystem.Save(newData);

        Debug.Log("[SaveManager] 新存档已创建 (已保留音量设置)");
    }

    // 获取当前关卡
    public static int GetCurrentLevel()
    {
        // 从 JSON 读取
        return SaveSystem.Load().currentLevelIndex;
    }
}