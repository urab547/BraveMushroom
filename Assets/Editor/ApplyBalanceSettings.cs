using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class ApplyBalanceSettings
{
    [MenuItem("Tools/Apply Balance Settings")]
    public static void Apply()
    {
        ApplyGunPrefabs();
        ApplyEnemyPrefabs();
        ApplyShopItems();

        AssetDatabase.SaveAssets();
        Debug.Log("[Balance] ✓ All balance settings applied and saved.");
    }

    // ─────────────────────────────────────────────
    // Gun Prefabs → WeaponController
    // ─────────────────────────────────────────────
    static void ApplyGunPrefabs()
    {
        SetWeapon("Assets/Prefabs/Gun/M92.prefab",  baseDamage: 3, fireInterval: 0.6f, projCount: 1);
        SetWeapon("Assets/Prefabs/Gun/MP5.prefab",  baseDamage: 2, fireInterval: 0.4f, projCount: 2);
        SetWeapon("Assets/Prefabs/Gun/AK47.prefab", baseDamage: 5, fireInterval: 0.5f, projCount: 3);
    }

    static void SetWeapon(string path, int baseDamage, float fireInterval, int projCount)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError($"[Balance] Prefab not found: {path}");
            return;
        }

        WeaponController wc = prefab.GetComponent<WeaponController>();
        if (wc == null)
        {
            Debug.LogError($"[Balance] WeaponController missing on {path}");
            return;
        }

        Undo.RecordObject(wc, "Apply Balance – WeaponController");
        wc.baseDamage          = baseDamage;
        wc.baseFireInterval    = fireInterval;
        wc.baseProjectileCount = projCount;
        EditorUtility.SetDirty(wc);

        Debug.Log($"[Balance] {prefab.name} → baseDamage={baseDamage}, baseFireInterval={fireInterval}, baseProjectileCount={projCount}");
    }

    // ─────────────────────────────────────────────
    // Enemy Prefabs → EnemyHealth
    // ─────────────────────────────────────────────
    static void ApplyEnemyPrefabs()
    {
        SetEnemy("Assets/Prefabs/Enemy/Enemy.prefab",      maxHealth: 6);
        SetEnemy("Assets/Prefabs/Enemy/Enemy_Bat.prefab",  maxHealth: 15);
        SetEnemy("Assets/Prefabs/Enemy/Enemy_Boss.prefab", maxHealth: 400);
    }

    static void SetEnemy(string path, int maxHealth)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError($"[Balance] Prefab not found: {path}");
            return;
        }

        EnemyHealth eh = prefab.GetComponent<EnemyHealth>();
        if (eh == null)
        {
            Debug.LogError($"[Balance] EnemyHealth missing on {path}");
            return;
        }

        Undo.RecordObject(eh, "Apply Balance – EnemyHealth");
        eh.maxHealth = maxHealth;
        EditorUtility.SetDirty(eh);

        Debug.Log($"[Balance] {prefab.name} → maxHealth={maxHealth}");
    }

    // ─────────────────────────────────────────────
    // ShopItemData ScriptableObjects
    // ─────────────────────────────────────────────
    static void ApplyShopItems()
    {
        // Armour: Price=20, Health=15
        SetShopItem("Assets/ShopItems/Armour.asset", price: 20,
            (StatType.Health, 15f));

        // Boot: Price=15, MoveSpeed=1.5
        SetShopItem("Assets/ShopItems/Boot.asset", price: 15,
            (StatType.MoveSpeed, 1.5f));

        // Potion: Price=25, FireRate=0.3, Attack=2, MoveSpeed=0.5
        SetShopItem("Assets/ShopItems/Potion.asset", price: 25,
            (StatType.FireRate, 0.3f),
            (StatType.Attack, 2f),
            (StatType.MoveSpeed, 0.5f));

        // AK47: Price=35 (modifiers untouched)
        SetShopItemPriceOnly("Assets/ShopItems/AK47.asset", price: 35);

        // MP5: Price=20 (modifiers untouched)
        SetShopItemPriceOnly("Assets/ShopItems/MP5.asset", price: 20);
    }

    static void SetShopItem(string path, int price, params (StatType type, float value)[] modifiers)
    {
        ShopItemData item = AssetDatabase.LoadAssetAtPath<ShopItemData>(path);
        if (item == null)
        {
            Debug.LogError($"[Balance] ShopItemData not found: {path}");
            return;
        }

        Undo.RecordObject(item, "Apply Balance – ShopItemData");
        item.price = price;

        // Rebuild the modifiers list: update matching StatType entries,
        // preserve any entries whose type is not listed, add missing ones.
        List<StatModifier> list = item.modifiers ?? new List<StatModifier>();

        foreach (var (type, value) in modifiers)
        {
            bool found = false;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].type == type)
                {
                    list[i] = new StatModifier { type = type, value = value };
                    found = true;
                    break;
                }
            }
            if (!found)
                list.Add(new StatModifier { type = type, value = value });
        }

        item.modifiers = list;
        EditorUtility.SetDirty(item);

        string modLog = "";
        foreach (var (type, value) in modifiers)
            modLog += $" {type}={value}";
        Debug.Log($"[Balance] {item.name} → price={price},{modLog}");
    }

    static void SetShopItemPriceOnly(string path, int price)
    {
        ShopItemData item = AssetDatabase.LoadAssetAtPath<ShopItemData>(path);
        if (item == null)
        {
            Debug.LogError($"[Balance] ShopItemData not found: {path}");
            return;
        }

        Undo.RecordObject(item, "Apply Balance – ShopItemData price");
        item.price = price;
        EditorUtility.SetDirty(item);

        Debug.Log($"[Balance] {item.name} → price={price}");
    }
}
