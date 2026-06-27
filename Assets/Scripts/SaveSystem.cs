using UnityEngine;
using System.IO;

public static class SaveSystem
{
    // 存档路径：C:/Users/AppData/LocalLow/.../savegame.json
    private static string path = Application.persistentDataPath + "/savegame.json";

    public static void Save(SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
        // Debug.Log("已保存: " + path);
    }

    public static SaveData Load()
    {
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<SaveData>(json);
        }
        return new SaveData(); // 没存档就返回新的
    }

    // 用于重置/测试
    public static void DeleteSave()
    {
        if (File.Exists(path)) File.Delete(path);
    }
}