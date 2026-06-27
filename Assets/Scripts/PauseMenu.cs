using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI; // 必须引用，用于控制滑条

public class PauseMenu : MonoBehaviour
{
    [Header("UI 组件")]
    public GameObject pauseMenuUI;

    [Header("音量滑条 (可选 - 拖拽暂停面板里的滑条)")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    // 记录当前游戏是否暂停
    public static bool GameIsPaused = false;

    void Start()
    {
        GameIsPaused = false;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
    }

    void Update()
    {
        // 监听 ESC 键
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    // === 功能 1: 恢复游戏 ===
    public void Resume()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
    }

    // === 功能 2: 暂停游戏 ===
    void Pause()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        GameIsPaused = true;

        // 打开菜单时，立即同步滑条位置
        // 这样玩家看到的音量就是当前真实的音量
        InitSliders();
    }

    // 从 SaveData 读取当前音量并更新滑条视觉
    void InitSliders()
    {
        SaveData data = SaveSystem.Load();

        if (masterSlider != null) masterSlider.value = data.masterVolume;
        if (musicSlider != null) musicSlider.value = data.musicVolume;
        if (sfxSlider != null) sfxSlider.value = data.sfxVolume;
    }

    // === 功能 3: 返回主菜单 ===
    public void LoadMenu()
    {
        Time.timeScale = 1f;
        GameIsPaused = false; // 记得重置暂停状态，否则下次进游戏可能出bug

        Debug.Log("加载主菜单...");
        // 确保这里的场景名字和 Build Settings 里的一致
        SceneManager.LoadScene("MainMenu");
    }

    // === 功能 4: 退出游戏 ===
    public void QuitGame()
    {
        Debug.Log("退出游戏！");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ========================================================
    // 音量控制接口 (绑定给滑条的 OnValueChanged)
    // ========================================================

    public void SetMasterVolume(float value)
    {
        if (AudioManager.instance != null) AudioManager.instance.SetAndSaveMasterVolume(value);
    }

    public void SetMusicVolume(float value)
    {
        if (AudioManager.instance != null) AudioManager.instance.SetAndSaveMusicVolume(value);
    }

    public void SetSFXVolume(float value)
    {
        if (AudioManager.instance != null) AudioManager.instance.SetAndSaveSFXVolume(value);
    }
}