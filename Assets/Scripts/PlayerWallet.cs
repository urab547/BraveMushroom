using UnityEngine;
using TMPro;

public class PlayerWallet : MonoBehaviour
{

    public TMP_Text goldText;

    void Update()
    {
        // 每一帧刷新 UI
        // 这样无论是捡了钱，还是商店花了钱，UI 都会自动同步，不需要手动通知
        UpdateUI();
    }


    public void AddGold(int amount)
    {
        if (GameLevelManager.instance != null)
        {
            // 转交核心管理器处理
            GameLevelManager.instance.AddTempGold(amount);
        }
    }

    void UpdateUI()
    {
        if (goldText != null)
        {
            // 1. 默认显示 0
            int total = 0;

            // 2. 如果管理器存在，获取【存档金币 + 本局临时金币】的总和
            if (GameLevelManager.instance != null)
            {
                // 调用我们刚才在 GameLevelManager 里补全的方法
                total = GameLevelManager.instance.GetTotalCurrentGold();
            }

            // 3. 显示
            goldText.text = ": " + total.ToString();
        }
    }
}