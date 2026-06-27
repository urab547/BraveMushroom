using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("音频混音器引用 (可选)")]
    // 如果你不想用 Mixer，可以留空，代码会自动降级使用 AudioListener
    public AudioMixer audioMixer;

    [Header("Slider 引用")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    // Mixer 中的参数名称 (必须与 Unity 编辑器里 Expose 的参数一致)
    private const string MIXER_MASTER = "MasterVolume";
    private const string MIXER_MUSIC = "MusicVolume";
    private const string MIXER_SFX = "SFXVolume";

    void Start()
    {
        // 1. 从 JSON 读取存档
        SaveData data = SaveSystem.Load();

        // 2. 初始化滑条位置 (视觉同步)
        if (masterSlider != null) masterSlider.value = data.masterVolume;
        if (musicSlider != null) musicSlider.value = data.musicVolume;
        if (sfxSlider != null) sfxSlider.value = data.sfxVolume;

        // 3. 初始化实际音量 (听觉同步)
        SetMasterVolume(data.masterVolume);
        SetMusicVolume(data.musicVolume);
        SetSFXVolume(data.sfxVolume);
    }

    public void SetMasterVolume(float sliderValue)
    {
        if (audioMixer != null)
        {
            float db = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20;
            audioMixer.SetFloat(MIXER_MASTER, db);
        }
        if (AudioManager.instance != null) AudioManager.instance.SetAndSaveMasterVolume(sliderValue);
    }

    public void SetMusicVolume(float sliderValue)
    {
        if (audioMixer != null)
        {
            float db = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20;
            audioMixer.SetFloat(MIXER_MUSIC, db);
        }
        if (AudioManager.instance != null) AudioManager.instance.SetAndSaveMusicVolume(sliderValue);
    }

    public void SetSFXVolume(float sliderValue)
    {
        if (audioMixer != null)
        {
            float db = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20;
            audioMixer.SetFloat(MIXER_SFX, db);
        }
        if (AudioManager.instance != null) AudioManager.instance.SetAndSaveSFXVolume(sliderValue);
    }
}