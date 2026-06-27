using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSlot : MonoBehaviour
{
    [Header("UI 组件 (需要手动拖拽)")]
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text descText;
    public TMP_Text priceText;

    [Header("购买按钮")]
    public Button buyButton;

    private ShopItemData currentItem;
    private GameLevelManager manager;


    public void Setup(ShopItemData item, GameLevelManager mgr)
    {
        currentItem = item;
        manager = mgr;

        // 自动寻找按钮组件
        if (buyButton == null) buyButton = GetComponentInChildren<Button>();

        // 自动绑定点击事件
        buyButton.onClick.RemoveAllListeners(); // 先清除旧的，防止重复
        buyButton.onClick.AddListener(OnClickBuy); // 绑定下面的 OnClickBuy 方法

        // 更新 UI 显示
        if (item != null)
        {
            if (iconImage != null) iconImage.sprite = item.icon;
            if (nameText != null) nameText.text = item.itemName;
            if (descText != null) descText.text = item.description;
            if (priceText != null) priceText.text = "$" + item.price;


            buyButton.interactable = true;
            if (buyButton.GetComponentInChildren<TMP_Text>())
                buyButton.GetComponentInChildren<TMP_Text>().text = "购买";
        }
    }

    // 点击按钮时触发
    void OnClickBuy()
    {
        // 调试如果点击有反应
        Debug.Log("点击了购买按钮: " + (currentItem != null ? currentItem.itemName : "空物品"));

        if (currentItem != null && manager != null)
        {
            manager.TryBuyItem(currentItem, this);
        }
        else
        {
            Debug.LogError("购买失败！可能是 Item 数据为空，或者没找到 Manager");
        }
    }

    public void SetSoldOut()
    {
        buyButton.interactable = false;
        if (buyButton.GetComponentInChildren<TMP_Text>())
            buyButton.GetComponentInChildren<TMP_Text>().text = "已售";
    }
}