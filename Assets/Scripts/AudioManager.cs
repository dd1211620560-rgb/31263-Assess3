using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips")]
    [SerializeField] private AudioClip introClip;
    [SerializeField] private AudioClip ghostNormalClip;

    [Header("Timing")]
    [SerializeField] private float introMaxDuration = 3f;

    private void Start()
    {
        PlayIntroThenSwitch();
    }

    /// <summary>
    /// 按规格：先播 Intro；当 Intro 播完 或 3 秒（取较早者）→ 切 Ghost Normal 循环
    /// </summary>
    private void PlayIntroThenSwitch()
    {
        if (introClip == null || ghostNormalClip == null || bgmSource == null)
        {
            Debug.LogWarning("AudioManager: clips or bgmSource not assigned.");
            return;
        }

        bgmSource.Stop();
        bgmSource.clip = introClip;
        bgmSource.loop = false;
        bgmSource.Play();

        // 取 "intro 长度" 和 "3 秒" 中较早者
        float waitTime = Mathf.Min(introClip.length, introMaxDuration);

        // 帧率无关的计时（不用 Invoke，更清晰；也可用 StartCoroutine）
        StartCoroutine(SwitchToGhostNormalAfter(waitTime));
    }

    private System.Collections.IEnumerator SwitchToGhostNormalAfter(float delay)
    {
        yield return new WaitForSeconds(delay);

        bgmSource.Stop();
        bgmSource.clip = ghostNormalClip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    // 供后续使用（吃豆 / 吃鬼 / 撞墙 / 死亡）
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }
    public void PlayGhostScaredMusic() { /* 切到 GhostScared_Music，loop */ }
    public void PlayGhostDeadMusic() { /* 切到 GhostDead_Music，loop */ }
    public void PlayStartSceneMusic() { /* 换场景后使用 */ }
}