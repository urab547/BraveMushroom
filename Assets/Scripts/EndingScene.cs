using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingController : MonoBehaviour
{
    [Header("配置")]
    public float animationDuration = 5f; 
    public GameObject storyPanel;        
    public GameObject victoryPanel;      

    private float timer;
    private bool isFinished = false;

    void Start()
    {
        Time.timeScale = 1f;

        if (storyPanel != null) storyPanel.SetActive(true);
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    void Update()
    {
        if (isFinished) return;

        timer += Time.deltaTime;

        if (timer >= animationDuration)
        {
            ShowVictory();
        }
    }

    // --- 按钮功能区 ---

    public void OnSkipClicked()
    {
        if (!isFinished)
        {
            ShowVictory();
        }
    }

    // 2. 重玩 / 回到菜单按钮
    public void OnBackToMenuClicked()
    {

        // 只物理删除 JSON 存档文件 (重置血量/金币)
        SaveSystem.DeleteSave();

    
        // 2. 恢复时间流动
        Time.timeScale = 1f;

        // 3. 加载主菜单场景 (确保 Build Settings 里菜单是 0 号)
        SceneManager.LoadScene(0);
    }

    public void OnQuitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 方便在编辑器里退出
#endif
    }

    void ShowVictory()
    {
        isFinished = true;
        if (storyPanel != null) storyPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(true);
    }
}