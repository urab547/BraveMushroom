using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.Video;

public class MainMenuController : MonoBehaviour
{
    [Header("=== UI 面板 (拖拽赋值) ===")]
    public GameObject mainMenuPanel;
    public GameObject confirmPanel;
    public GameObject settingsPanel;
    public GameObject introPanel;

    [Header("=== UI 组件 (拖拽赋值) ===")]
    public TMP_Text skipHintText;
    public Button continueButton;

    [Header("=== 音量滑条 (对应设置面板) ===")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("=== 视频组件 ===")]
    public VideoPlayer introVideoPlayer;

    [Header("=== 游戏设置 ===")]
    public string firstLevelSceneName = "Level_1";

    [Header("=== 视觉效果 ===")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 1.0f;

    private bool isPlayingIntro = false;
    private bool isSkipPromptShown = false;
    private float skipTimer = 0f;
    private const float SKIP_CONFIRM_WINDOW = 2.0f;

    void Start()
    {
        // 1. 初始化面板显示状态
        ShowPanel(mainMenuPanel);
        if (skipHintText) skipHintText.gameObject.SetActive(false);


        // 读取存档以检查是否有进度
        SaveData data = SaveSystem.Load();

        // 如果关卡大于1，或者有金币，说明玩过，可以继续
        bool hasProgress = data.currentLevelIndex > 1 || data.coins > 0 || !string.IsNullOrEmpty(data.currentWeaponID);
        continueButton.interactable = hasProgress;

        // 3.从 JSON 初始化并同步音量 
        InitAndSyncVolumes(data);

        // 4. 播放菜单音乐
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayMenuMusic();
        }
    }

    // 传入 data 避免重复读取
    private void InitAndSyncVolumes(SaveData data)
    {
        // --- 主音量 (Master) ---
        if (masterSlider != null) masterSlider.value = data.masterVolume;
        AudioListener.volume = data.masterVolume;

        // --- 音乐音量 (Music) ---
        if (musicSlider != null) musicSlider.value = data.musicVolume;
        if (AudioManager.instance != null && AudioManager.instance.bgmSource != null)
        {
            AudioManager.instance.bgmSource.volume = data.musicVolume;
        }

        // --- 音效音量 (SFX) ---
        if (sfxSlider != null) sfxSlider.value = data.sfxVolume;
        if (AudioManager.instance != null && AudioManager.instance.sfxSource != null)
        {
            AudioManager.instance.sfxSource.volume = data.sfxVolume;
        }
    }

    void Update()
    {
        if (isPlayingIntro)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                if (!isSkipPromptShown) ShowSkipPrompt();
                else EnterGameLevel();
            }

            if (isSkipPromptShown)
            {
                skipTimer -= Time.deltaTime;
                if (skipTimer <= 0)
                {
                    isSkipPromptShown = false;
                    skipHintText.gameObject.SetActive(false);
                }
            }
        }
    }

    #region --- 按钮与滑条事件绑定 ---

    public void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.instance != null) AudioManager.instance.SetAndSaveMasterVolume(value);
    }

    public void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.instance != null) AudioManager.instance.SetAndSaveMusicVolume(value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.instance != null) AudioManager.instance.SetAndSaveSFXVolume(value);
    }

    public void OnNewGameClicked()
    {
        // 检查是否有实质性进度
        SaveData data = SaveSystem.Load();
        bool hasProgress = data.currentLevelIndex > 1 || data.coins > 0;

        if (hasProgress) ShowPanel(confirmPanel);
        else StartNewGameProcess();
    }

    public void OnConfirmOverwrite() => StartNewGameProcess();
    public void OnCancelOverwrite() => ShowPanel(mainMenuPanel);

    public void OnContinueClicked()
    {
        Time.timeScale = 1f;
        // 读取存档，看看应该进哪一关
        SaveData data = SaveSystem.Load();


        string sceneToLoad = "Level_" + data.currentLevelIndex;

        SceneManager.LoadScene(1);
    }

    public void OnSettingsClicked() => ShowPanel(settingsPanel);
    public void OnSettingsBackClicked()
    {
        // 滑条变化时已经实时保存了，这里只需要切面板
        ShowPanel(mainMenuPanel);
    }

    public void OnQuitClicked() => Application.Quit();

    #endregion

    #region --- 游戏流程逻辑 ---

    private void StartNewGameProcess()
    {
        SaveManager.CreateNewGame();
        PlayIntroSequence();
    }

    private void PlayIntroSequence()
    {
        ShowPanel(introPanel);
        isPlayingIntro = true;
        if (introVideoPlayer != null)
        {
            introVideoPlayer.gameObject.SetActive(true);
            introVideoPlayer.Play();
            introVideoPlayer.loopPointReached += OnVideoFinished;
        }
        else EnterGameLevel();
    }

    void OnVideoFinished(VideoPlayer vp) => EnterGameLevel();

    private void ShowSkipPrompt()
    {
        isSkipPromptShown = true;
        skipTimer = SKIP_CONFIRM_WINDOW;
        if (skipHintText) skipHintText.gameObject.SetActive(true);
    }

    private void EnterGameLevel()
    {
        if (!isPlayingIntro) return;
        isPlayingIntro = false;
        if (introVideoPlayer != null)
        {
            introVideoPlayer.Stop();
            introVideoPlayer.loopPointReached -= OnVideoFinished;
        }
        StartCoroutine(FadeOutAndLoadLevel());
    }

    IEnumerator FadeOutAndLoadLevel()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true;
            fadeCanvasGroup.gameObject.SetActive(true);
            float t = 0;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                fadeCanvasGroup.alpha = t / fadeDuration;
                yield return null;
            }
        }
        SceneManager.LoadScene(firstLevelSceneName);
    }

    private void ShowPanel(GameObject panelToShow)
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (confirmPanel) confirmPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (introPanel) introPanel.SetActive(false);
        if (panelToShow) panelToShow.SetActive(true);
    }

    #endregion
}