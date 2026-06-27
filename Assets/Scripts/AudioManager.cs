using UnityEngine;
using System.Collections.Generic;
using System.Collections; 

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [System.Serializable]
    public class SoundEffect
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 2f)]
        public float volume = 1.0f;
    }

    [Header("组件引用")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("过渡设置")]
    public float fadeDuration = 1.0f; 
    private Coroutine musicFadeCoroutine; 

    [Header("音乐素材")]
    public AudioClip menuBgm;
    public AudioClip normalBgm;
    public AudioClip bossBgm;

    [Header("音效库")]
    public List<SoundEffect> sfxList;

    private Dictionary<string, SoundEffect> sfxDictionary = new Dictionary<string, SoundEffect>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitDictionary();
            
            // 初始化音量 (从存档读取)
            InitVolumeFromSave();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 初始化音量
    private void InitVolumeFromSave()
    {
        // 这里可以直接读 JSON，或者信任 MainMenuController 的初始化
        // 为了保险，这里再读一次 JSON
        SaveData data = SaveSystem.Load();

        if (bgmSource != null) bgmSource.volume = data.musicVolume;
        if (sfxSource != null) sfxSource.volume = data.sfxVolume;
    }

    private void InitDictionary()
    {
        sfxDictionary.Clear();
        foreach (var sfx in sfxList)
        {
            if (!string.IsNullOrEmpty(sfx.name) && !sfxDictionary.ContainsKey(sfx.name))
            {
                sfxDictionary.Add(sfx.name, sfx);
            }
        }
    }

    // --- 背景音乐控制 (带淡入淡出) ---

    public void PlayMenuMusic() => SwitchMusic(menuBgm);
    public void PlayNormalMusic() => SwitchMusic(normalBgm);
    public void PlayBossMusic() => SwitchMusic(bossBgm);

    private void SwitchMusic(AudioClip newClip)
    {
        if (bgmSource == null || newClip == null) return;
        if (bgmSource.clip == newClip && bgmSource.isPlaying) return;

        if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);

        musicFadeCoroutine = StartCoroutine(FadeMusicRoutine(newClip));
    }

    IEnumerator FadeMusicRoutine(AudioClip newClip)
    {
     
        // 从 SaveSystem 读取当前的音乐音量设置
        float targetVolume = SaveSystem.Load().musicVolume; 

        // 1. 淡出当前音乐
        if (bgmSource.isPlaying)
        {
            float startVolume = bgmSource.volume;
            // 这里的判断是为了防止 startVolume 已经是 0 了还淡出个没完
            if (startVolume > 0)
            {
                for (float t = 0; t < fadeDuration; t += Time.unscaledDeltaTime) 
                {
                    bgmSource.volume = Mathf.Lerp(startVolume, 0, t / fadeDuration);
                    yield return null;
                }
            }
            bgmSource.volume = 0;
            bgmSource.Stop();
        }

        // 2. 切换音频并淡入
        bgmSource.clip = newClip;
        bgmSource.Play();

        // 只有当目标音量 > 0 时才淡入，否则静音播放
        if (targetVolume > 0)
        {
            for (float t = 0; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
                bgmSource.volume = Mathf.Lerp(0, targetVolume, t / fadeDuration);
                yield return null;
            }
        }
        
        // 确保最终音量精准
        bgmSource.volume = targetVolume;
    }

    // --- 音效逻辑保持不变 ---

    public void PlaySFX(string sfxName)
    {
        if (sfxDictionary.TryGetValue(sfxName, out SoundEffect sfx))
        {
            // 最终音量 = sfxSource.volume * sfx.volume
            sfxSource.PlayOneShot(sfx.clip, sfx.volume);
        }
    }

    public void PlaySFXWithPitch(string sfxName, float minPitch = 0.9f, float maxPitch = 1.1f)
    {
        if (sfxDictionary.TryGetValue(sfxName, out SoundEffect sfx))
        {
            sfxSource.pitch = Random.Range(minPitch, maxPitch);
            sfxSource.PlayOneShot(sfx.clip, sfx.volume);
            
            StartCoroutine(ResetPitchNextFrame());
        }
    }

    IEnumerator ResetPitchNextFrame()
    {
        yield return null; 
        sfxSource.pitch = 1.0f;
    }

    // --- 音量控制统一接口 ---

    public void SetAndSaveMasterVolume(float value)
    {
        AudioListener.volume = value;
        SaveData data = SaveSystem.Load();
        data.masterVolume = value;
        SaveSystem.Save(data);
    }

    public void SetAndSaveMusicVolume(float value)
    {
        if (bgmSource != null) bgmSource.volume = value;
        SaveData data = SaveSystem.Load();
        data.musicVolume = value;
        SaveSystem.Save(data);
    }

    public void SetAndSaveSFXVolume(float value)
    {
        if (sfxSource != null) sfxSource.volume = value;
        SaveData data = SaveSystem.Load();
        data.sfxVolume = value;
        SaveSystem.Save(data);
    }

    public IEnumerator PlaySFXAndResetPitch(string sfxName, float targetDuration)
    {
        if (sfxDictionary.TryGetValue(sfxName, out SoundEffect sfx))
        {
            float requiredPitch = sfx.clip.length / targetDuration;
            sfxSource.pitch = requiredPitch;
            sfxSource.PlayOneShot(sfx.clip, sfx.volume); // 记得加上 volume

            yield return new WaitForSeconds(targetDuration);
            
            sfxSource.pitch = 1.0f;
        }
    }
}