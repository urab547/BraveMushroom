using UnityEngine;
using System.Collections.Generic;

// 1. 定义属性类型 (包含了手感、弹道)
public enum StatType
{
    Attack,         // 攻击力
    FireRate,       // 射速 (百分比)
    Health,         // 血量上限
    MoveSpeed,      // 移速
    ProjectileCount,// 子弹数量 (散弹/多重射击)
    SpreadAngle,    // 散射角度 (精准度)
    RotationSpeed   // 枪支手感 (转向速度)
}

// 2. 属性修饰器结构体
[System.Serializable]
public struct StatModifier
{
    [Tooltip("要修改什么属性")]
    public StatType type;
    [Tooltip("修改数值 (支持负数)")]
    public float value;
}

// 3. 统一的物品数据类
[CreateAssetMenu(fileName = "NewShopItem", menuName = "Shop/Unified Item")]
public class ShopItemData : ScriptableObject
{
    [Header("UI 显示信息")]
    public string itemName;
    public Sprite icon;
    public int price;
    [TextArea] public string description;

    [Header("核心配置 (留空代表没有此功能)")]

    [Tooltip("如果拖入了武器预制体，购买时会更换主角武器")]
    public GameObject weaponPrefab; // 可选：可以是空 (null)

    [Tooltip("属性修改列表：购买时会应用列表里所有的属性变化")]
    public List<StatModifier> modifiers; // 可选：可以是空列表
}