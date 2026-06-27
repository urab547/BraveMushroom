using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class GameLevelManager : MonoBehaviour
{
    public static GameLevelManager instance;

    [Header("关卡设置")]
    public int totalEnemiesToKill = 10;
    private int currentKills = 0;
    private bool isLevelFinished = false;

    [HideInInspector]
    public int tempGold = 0;

    private int cachedSavedCoins = 0;

    [Header("UI 引用")]
    public GameObject shopPanel;
    public GameObject gameHUDPanel;
    public TMP_Text playerGoldText;
    public TMP_Text levelText;

    [Header("商店组件")]
    public ShopSlot[] shopSlots;
    public GameObject noMoneyPrefab;
    public GameObject refreshButtonObj;

    [Header("商店数据")]
    public List<ShopItemData> allItems;
    public int refreshCost = 5;

    [Header("通关设置")]
    public int finalLevelIndex = 3;
    public GameObject victoryPanel;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        tempGold = 0;
        isLevelFinished = false;
        currentKills = 0;
        Time.timeScale = 1f;


        SaveData data = SaveSystem.Load();
        cachedSavedCoins = data.coins;

        // 1. 刷新关卡UI (从 SaveData 读取)
        UpdateLevelUI(data.currentLevelIndex);

        // 2. 状态恢复：检查是在商店还是战斗
        if (data.isInShop)
        {
            Debug.Log("[系统] 恢复中断的商店会话...");
            isLevelFinished = true;

            // 关掉刷怪
            EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
            if (spawner != null) spawner.enabled = false;

            OpenShop(false); // false 代表不需要再次结算金币
        }
        else
        {
            // 正常战斗状态
            if (shopPanel != null) shopPanel.SetActive(false);
            if (gameHUDPanel != null) gameHUDPanel.SetActive(true);
        }

        // 3. 音乐逻辑
        if (AudioManager.instance != null)
        {
            if (data.currentLevelIndex >= finalLevelIndex)
                AudioManager.instance.PlayBossMusic();
            else
                AudioManager.instance.PlayNormalMusic();
        }

        // 4. 恢复武器 (从 SaveData 读取 ID)
        RestoreWeapon(data.currentWeaponID);
    }

    // --- 战斗逻辑 ---
    public void OnEnemyKilled()
    {
        if (isLevelFinished) return;
        currentKills++;
        if (currentKills >= totalEnemiesToKill)
        {
            StartCoroutine(LevelClearSequence());
        }
    }

    void UpdateLevelUI(int levelIndex)
    {
        if (levelText != null)
        {
            if (levelIndex >= finalLevelIndex)
            {
                levelText.text = "最终决战";
                levelText.color = Color.red;
            }
            else
            {
                levelText.text = "第 " + levelIndex + " 关";
                levelText.color = Color.white;
            }
        }
    }

    public void AddTempGold(int amount)
    {
        tempGold += amount;
        // 这里可以做一些 UI 动效，比如金币飞向角落
    }

    IEnumerator LevelClearSequence()
    {
        isLevelFinished = true;
        if (AudioManager.instance != null) AudioManager.instance.PlaySFX("Victory");

        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null) spawner.enabled = false;

        yield return new WaitForSeconds(4.0f);

        // 读取当前进度
        SaveData data = SaveSystem.Load();

        if (data.currentLevelIndex >= finalLevelIndex)
        {
            ShowVictory();
        }
        else
        {
            OpenShop(true); // true 代表需要结算关卡奖励
        }
    }

    // 参数 settleRewards: 是否需要把临时金币加进存档？
    void OpenShop(bool settleRewards)
    {
        Time.timeScale = 0;

        if (gameHUDPanel != null) gameHUDPanel.SetActive(false);
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);

            if (settleRewards) SettleLevelRewards();

            UpdateGoldUI();
            GenerateRandomShop();
        }
    }

    void ShowVictory()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("EndingScene");
    }

    public void OnRestartClicked()
    {
        // ✅ [替换] 使用 SaveSystem 删除存档，而不是 DeleteAll
        SaveSystem.DeleteSave();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnQuitClicked()
    {
        Application.Quit();
    }

    void SettleLevelRewards()
    {
        SaveData data = SaveSystem.Load();

        if (tempGold > 0)
        {
            data.coins += tempGold;
            tempGold = 0;
        }

        data.isInShop = true;
        cachedSavedCoins = data.coins;
        SaveSystem.Save(data);
    }

    // 刷新商店商品
    public void GenerateRandomShop()
    {
        if (allItems.Count < shopSlots.Length) return;
        List<ShopItemData> tempPool = new List<ShopItemData>(allItems);

        foreach (var slot in shopSlots)
        {
            if (tempPool.Count == 0) break;
            int randomIndex = Random.Range(0, tempPool.Count);
            ShopItemData selectedItem = tempPool[randomIndex];
            slot.Setup(selectedItem, this);
            tempPool.RemoveAt(randomIndex);
        }
    }

    public void OnRefreshClicked()
    {
        SaveData data = SaveSystem.Load();

        if (data.coins >= refreshCost)
        {
            data.coins -= refreshCost;
            cachedSavedCoins = data.coins;
            SaveSystem.Save(data);

            UpdateGoldUI();
            GenerateRandomShop();
        }
        else
        {
            ShowNoMoneyPopup(refreshButtonObj.transform);
        }
    }
    public int GetTotalCurrentGold()
    {
        return cachedSavedCoins + tempGold;
    }


    public void TryBuyItem(ShopItemData item, ShopSlot slotScript)
    {
        SaveData data = SaveSystem.Load();

        if (data.coins >= item.price)
        {
            data.coins -= item.price;
            cachedSavedCoins = data.coins;

            if (item.weaponPrefab != null)
            {
                ApplyWeaponData(item.weaponPrefab);
                data.currentWeaponID = item.weaponPrefab.name;
            }

            if (item.modifiers != null && item.modifiers.Count > 0)
            {
                if (PlayerStats.instance != null)
                    PlayerStats.instance.ApplyItem(item, data);

                PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
                if (ph != null) ph.Heal(0);

                WeaponController wc = FindFirstObjectByType<WeaponController>();
                if (wc != null) wc.UpdateStats();

                PlayerController pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) pc.UpdateMoveSpeed();
            }

            SaveSystem.Save(data);

            UpdateGoldUI();
            slotScript.SetSoldOut();
        }
        else
        {
            ShowNoMoneyPopup(slotScript.transform);
        }
    }

    void ApplyWeaponData(GameObject newWeaponPrefab)
    {
        WeaponController playerWeapon = FindFirstObjectByType<WeaponController>();
        WeaponController newData = newWeaponPrefab.GetComponent<WeaponController>();

        if (playerWeapon == null || newData == null) return;

        playerWeapon.bulletPrefab = newData.bulletPrefab;
        playerWeapon.baseDamage = newData.baseDamage;
        playerWeapon.baseFireInterval = newData.baseFireInterval;
        playerWeapon.baseProjectileCount = newData.baseProjectileCount;
        playerWeapon.spreadAngle = newData.spreadAngle;
        playerWeapon.rotationSpeed = newData.rotationSpeed;
        playerWeapon.attackRange = newData.attackRange;

        if (playerWeapon.gunRenderer != null && newData.gunRenderer != null)
            playerWeapon.gunRenderer.sprite = newData.gunRenderer.sprite;

        playerWeapon.UpdateStats();
    }

    void RestoreWeapon(string weaponID)
    {
        if (string.IsNullOrEmpty(weaponID)) return;

        foreach (var item in allItems)
        {
            if (item.weaponPrefab != null && item.weaponPrefab.name == weaponID)
            {
                ApplyWeaponData(item.weaponPrefab);
                return;
            }
        }
    }

    void UpdateGoldUI()
    {
        if (playerGoldText != null)
        {
            playerGoldText.text = "持有金币: " + cachedSavedCoins;
        }
    }

    void ShowNoMoneyPopup(Transform targetTF)
    {
        if (noMoneyPrefab != null)
        {
            GameObject popup = Instantiate(noMoneyPrefab, targetTF.position, Quaternion.identity, shopPanel.transform);
            popup.GetComponent<FloatingText>().SetText("金币不足!");
        }
    }

    // --- 场景切换 ---
    public void GoToNextLevel()
    {
        SaveData data = SaveSystem.Load();

        // 1. 关卡 +1
        data.currentLevelIndex++;

        // 2. 退出商店状态
        data.isInShop = false;

        SaveSystem.Save(data);

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}