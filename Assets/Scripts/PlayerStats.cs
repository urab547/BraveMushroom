using UnityEngine;
using System.Collections.Generic;


public class PlayerStats : MonoBehaviour
{
    public static PlayerStats instance;

    [Header("配置：角色的初始白板属性")]
    public List<SaveData.StatBonus> baseStats;

    private Dictionary<StatType, float> baseStatsDict = new Dictionary<StatType, float>();
    private Dictionary<StatType, float> runtimeModifiers = new Dictionary<StatType, float>();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        BuildBaseStatsDict();
        runtimeModifiers.Clear();
        LoadBonusesFromSave();
    }

    void BuildBaseStatsDict()
    {
        baseStatsDict.Clear();
        if (baseStats == null) return;
        foreach (var stat in baseStats)
        {
            if (baseStatsDict.ContainsKey(stat.type))
                baseStatsDict[stat.type] += stat.value;
            else
                baseStatsDict[stat.type] = stat.value;
        }
    }

    void LoadBonusesFromSave()
    {
        SaveData data = SaveSystem.Load();
        if (data.statBonuses == null) return;

        foreach (var bonus in data.statBonuses)
        {
            if (runtimeModifiers.ContainsKey(bonus.type))
                runtimeModifiers[bonus.type] += bonus.value;
            else
                runtimeModifiers[bonus.type] = bonus.value;
        }
    }

    public void ApplyItem(ShopItemData item, SaveData data)
    {
        if (item.modifiers == null) return;

        foreach (var mod in item.modifiers)
        {
            if (runtimeModifiers.ContainsKey(mod.type))
                runtimeModifiers[mod.type] += mod.value;
            else
                runtimeModifiers[mod.type] = mod.value;

            AggregateStatBonus(data, mod.type, mod.value);
        }
    }

    private void AggregateStatBonus(SaveData data, StatType type, float value)
    {
        for (int i = 0; i < data.statBonuses.Count; i++)
        {
            if (data.statBonuses[i].type == type)
            {
                var existing = data.statBonuses[i];
                existing.value += value;
                data.statBonuses[i] = existing;
                return;
            }
        }
        data.statBonuses.Add(new SaveData.StatBonus { type = type, value = value });
    }

    public float GetStat(StatType type)
    {
        float total = 0;

        if (baseStatsDict.TryGetValue(type, out float baseVal))
            total += baseVal;

        if (runtimeModifiers.TryGetValue(type, out float bonus))
            total += bonus;

        return total;
    }
}