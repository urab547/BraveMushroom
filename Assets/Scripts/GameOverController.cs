using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverController : MonoBehaviour
{
    [Header("配置")]
    public float animationDuration = 5f; // 死亡动画/文字播放多久
    public GameObject deathAnimPanel;    // 放“你死了”文字或动画的容器
    public GameObject gameOverMenuPanel; // 最后的菜单面板（回主菜单/退出）

    private float timer;
    private bool isFinished = false;

    void Start()
    {
        // 1. 确保时间流动 (防止之前死的时候暂停了)
        Time.timeScale = 1f;

        // 2. 初始化状态：显示动画，隐藏菜单
        if (deathAnimPanel != null) deathAnimPanel.SetActive(true);
        if (gameOverMenuPanel != null) gameOverMenuPanel.SetActive(false);
    }

    void Update()
    {
        if (isFinished) return;

        // 计时
        timer += Time.deltaTime;

        // 时间到了，自动切换到菜单
        if (timer >= animationDuration)
        {
            ShowMenu();
        }
    }

    // --- 按钮功能区 ---

    // 1. 跳过按钮调用
    public void OnSkipClicked()
    {
        if (!isFinished)
        {
            ShowMenu();
        }
    }

    // 2. 回到主菜单按钮调用
    public void OnBackToMenuClicked()
    {

        // 存档清理逻辑
        // 物理删除 JSON 存档文件 (重置血量/金币/进度)
        SaveSystem.DeleteSave();

        Time.timeScale = 1f;
        // 确保 Build Settings 中主菜单场景的索引确实是 0
        SceneManager.LoadScene(0);
    }

    // 3. 退出按钮调用
    public void OnQuitClicked()
    {
        Application.Quit();
        Debug.Log("退出游戏");
    }

    // 内部逻辑：显示菜单
    void ShowMenu()
    {
        isFinished = true;
        Debug.Log("动画结束，显示结算菜单");

        // 关掉动画，打开菜单
        if (deathAnimPanel != null) deathAnimPanel.SetActive(false);
        if (gameOverMenuPanel != null) gameOverMenuPanel.SetActive(true);
    }
}