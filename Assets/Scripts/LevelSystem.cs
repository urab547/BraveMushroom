using UnityEngine;
using UnityEngine.UI;
using TMPro; // 显示等级文字

public class LevelSystem : MonoBehaviour
{
    public int level = 1;
    public int currentExp = 0;
    public int requiredExp = 10; // 升下一级需要多少经验

    public Slider expBar;

    void Start()
    {
        UpdateUI();
    }

    public void AddExp(int amount)
    {
        currentExp += amount;

        // 检查是否升级
        if (currentExp >= requiredExp)
        {
            LevelUp();
        }

        UpdateUI();
    }

    void LevelUp()
    {
        level++;
        currentExp -= requiredExp; // 扣除升级消耗的经验

        // 下一级需要的经验更多
        requiredExp = (int)(requiredExp * 1.2f);

        Debug.Log("升级了！当前等级: " + level);
        // 这里未来可以弹窗让玩家选技能
    }

    void UpdateUI()
    {
        if (expBar != null)
        {
            // 计算比例：当前经验 / 需要的经验
            expBar.value = (float)currentExp / requiredExp;
        }
    }
}