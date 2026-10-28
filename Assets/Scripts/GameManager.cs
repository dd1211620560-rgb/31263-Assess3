using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("HUD - Text")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI gameTimerText;
    [SerializeField] private TextMeshProUGUI scaredTimerText;
    [SerializeField] private TextMeshProUGUI levelNameText;

    [Header("HUD - Lives")]
    [SerializeField] private Transform livesContainer;
    [SerializeField] private GameObject lifeIconPrefab;
    [SerializeField] private int startingLives = 3;

    [Header("HUD - Overlays")]
    [SerializeField] private GameObject screenMask;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip pelletSFX;
    [SerializeField] private AudioClip powerPelletSFX;
    [SerializeField] private AudioClip cherrySFX;
    [SerializeField] private AudioClip wallHitSFX;
    [SerializeField] private AudioClip ghostEatSFX;
    [SerializeField] private AudioClip deathSFX;
    [SerializeField] private AudioClip introMusic;
    [SerializeField] private AudioClip ghostNormalMusic;
    [SerializeField] private AudioClip ghostScaredMusic;
    [SerializeField] private AudioClip ghostDeadMusic;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Ghost References")]
    [SerializeField] private List<GhostController> ghosts;

    // 运行状态
    private int score = 0;
    private int lives;
    private float gameTime = 0f;
    private bool isGameActive = false;

    private bool isScaredTimerActive = false;
    private float scaredTimer = 0f;
    private const float ScaredDuration = 10f;
    private const float RecoveringThreshold = 3f;

    private bool isDeadGhostPlaying = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        lives = startingLives;
        UpdateScoreUI();
        UpdateLivesUI();
        UpdateTimerUI();
        if (levelNameText != null) levelNameText.text = "Level 1";
        if (scaredTimerText != null) scaredTimerText.gameObject.SetActive(false);

        StartCoroutine(RoundStartSequence());
    }

    private void Update()
    {
        if (!isGameActive) return;

        gameTime += Time.deltaTime;
        UpdateTimerUI();

        if (isScaredTimerActive)
        {
            scaredTimer -= Time.deltaTime;
            if (scaredTimerText != null)
            {
                scaredTimerText.gameObject.SetActive(scaredTimer > 0f);
                scaredTimerText.text = Mathf.CeilToInt(Mathf.Max(0f, scaredTimer)).ToString();
            }

            if (scaredTimer <= RecoveringThreshold && scaredTimer > 0f)
            {
                // 进入 Recovering
                foreach (var g in ghosts)
                    if (g != null && g.CurrentState == GhostController.GhostState.Scared)
                        g.SetState(GhostController.GhostState.Recovering);
            }
            else if (scaredTimer <= 0f)
            {
                EndScaredState();
            }
        }
    }

    // ============ 倒计时 ============
    private IEnumerator RoundStartSequence()
    {
        isGameActive = false;
        DisablePlayerAndGhosts(true);

        if (screenMask != null) screenMask.SetActive(true);
        if (countdownText != null) countdownText.gameObject.SetActive(true);

        if (bgmSource != null && introMusic != null)
        {
            bgmSource.clip = introMusic;
            bgmSource.loop = false;
            bgmSource.Play();
        }

        string[] seq = { "3", "2", "1", "GO!" };
        foreach (var s in seq)
        {
            if (countdownText != null) countdownText.text = s;
            yield return new WaitForSeconds(1f);
        }

        if (screenMask != null) screenMask.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(false);

        isGameActive = true;
        DisablePlayerAndGhosts(false);

        if (bgmSource != null && ghostNormalMusic != null)
        {
            bgmSource.clip = ghostNormalMusic;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    private void DisablePlayerAndGhosts(bool disabled)
    {
        var player = FindObjectOfType<PacStudentController>();
        if (player != null) player.enabled = !disabled;
        foreach (var g in ghosts)
            if (g != null) g.enabled = !disabled;
    }

    // ============ UI 更新 ============
    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = score.ToString("D6");
    }

    private void UpdateLivesUI()
    {
        if (livesContainer == null || lifeIconPrefab == null) return;
        foreach (Transform child in livesContainer) Destroy(child.gameObject);
        for (int i = 0; i < startingLives; i++)
        {
            var icon = Instantiate(lifeIconPrefab, livesContainer);
            icon.SetActive(i < lives);
        }
    }

    private void UpdateTimerUI()
    {
        if (gameTimerText == null) return;
        int m = Mathf.FloorToInt(gameTime / 60f);
        int s = Mathf.FloorToInt(gameTime % 60f);
        int ms = Mathf.FloorToInt((gameTime * 1000f) % 1000f);
        gameTimerText.text = $"{m:00}:{s:00}:{ms:000}";
    }

    // ============ 吃豆子/樱桃/能量豆 ============
    public void OnPelletEaten() { PlaySFX(pelletSFX); AddScore(10); }
    public void OnCherryEaten() { PlaySFX(cherrySFX); AddScore(100); }
    public void OnWallHit() { PlaySFX(wallHitSFX); }

    public void OnPowerPelletEaten()
    {
        PlaySFX(powerPelletSFX);
        AddScore(50);

        isScaredTimerActive = true;
        scaredTimer = ScaredDuration;
        if (scaredTimerText != null)
        {
            scaredTimerText.gameObject.SetActive(true);
            scaredTimerText.text = Mathf.CeilToInt(ScaredDuration).ToString();
        }

        foreach (var g in ghosts)
            if (g != null && g.CurrentState != GhostController.GhostState.Dead)
                g.SetState(GhostController.GhostState.Scared);

        if (bgmSource != null && ghostScaredMusic != null)
        {
            bgmSource.clip = ghostScaredMusic;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    public void OnGhostEaten()
    {
        AddScore(300);
        PlaySFX(ghostEatSFX);

        // 如果还有别的死鬼，继续播 dead 音乐
        if (bgmSource != null && ghostDeadMusic != null)
        {
            bgmSource.clip = ghostDeadMusic;
            bgmSource.loop = true;
            bgmSource.Play();
            isDeadGhostPlaying = true;
        }
    }

    private void EndScaredState()
    {
        isScaredTimerActive = false;
        if (scaredTimerText != null) scaredTimerText.gameObject.SetActive(false);

        foreach (var g in ghosts)
            if (g != null && g.CurrentState != GhostController.GhostState.Dead)
                g.SetState(GhostController.GhostState.Normal);

        if (!isDeadGhostPlaying && bgmSource != null && ghostNormalMusic != null)
        {
            bgmSource.clip = ghostNormalMusic;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    // ============ 玩家死亡 ============
    public void OnPacStudentCaught()
    {
        if (!isGameActive) return;
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        isGameActive = false;
        PlaySFX(deathSFX);
        if (bgmSource != null) bgmSource.Stop();

        // 停掉移动
        var player = FindObjectOfType<PacStudentController>();
        if (player != null) player.enabled = false;
        foreach (var g in ghosts) if (g != null) g.enabled = false;

        // 播放死亡动画（PacStudent 的 Animator 有 Dead 状态）
        if (player != null)
        {
            var anim = player.GetComponent<Animator>();
            if (anim != null) anim.Play("Dead");
        }

        yield return new WaitForSeconds(1.5f);

        lives--;
        UpdateLivesUI();

        if (lives <= 0)
        {
            yield return StartCoroutine(GameOverSequence());
            yield break;
        }

        // 复位
        if (player != null)
        {
            player.ResetToStart();
            player.enabled = true;
        }
        foreach (var g in ghosts) { if (g != null) { g.ResetToSpawn(); g.enabled = true; } }

        isGameActive = true;
        if (bgmSource != null && ghostNormalMusic != null)
        {
            bgmSource.clip = ghostNormalMusic;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    // ============ Game Over ============
    public IEnumerator GameOverSequence()
    {
        isGameActive = false;

        if (bgmSource != null) bgmSource.Stop();

        SaveHighScore();

        if (screenMask != null) screenMask.SetActive(true);
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "Game Over";
        }

        yield return new WaitForSeconds(3f);

        SceneManager.LoadScene("StartScene");
    }

    private void SaveHighScore()
    {
        int prevHigh = PlayerPrefs.GetInt("HighScore", 0);
        float prevTime = PlayerPrefs.GetFloat("BestTime", 0f);

        bool newHigh = score > prevHigh;
        bool sameScoreFaster = score == prevHigh && (prevTime <= 0f || gameTime < prevTime);

        if (newHigh || sameScoreFaster)
        {
            PlayerPrefs.SetInt("HighScore", score);
            PlayerPrefs.SetFloat("BestTime", gameTime);
            PlayerPrefs.Save();
        }
    }

    // ============ 工具 ============
    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip);
    }

    public void ExitToMenu() => SceneManager.LoadScene("StartScene");

    public bool IsGameActive => isGameActive;
}