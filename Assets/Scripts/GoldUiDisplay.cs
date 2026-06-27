using UnityEngine;
using TMPro;

public class GoldUIDisplay : MonoBehaviour
{
    private TMP_Text goldText;

    void Start()
    {
        goldText = GetComponent<TMP_Text>();
    }

    void Update()
    {
        // 不再只读存档，而是要“总金额”
        if (GameLevelManager.instance != null)
        {
            int totalGold = GameLevelManager.instance.GetTotalCurrentGold();
            goldText.text = ": " + totalGold.ToString();
        }
    }
}