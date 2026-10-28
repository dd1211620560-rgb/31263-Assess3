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

    [Header("HUD - Lives")]
    [SerializeField] private Transform livesContainer;
    [SerializeField] private GameObject lifeIconPrefab;
    [SerializeField] private int startingLives = 3;

    [Header("HUD - Overlays")]
    [SerializeField] private GameObject screenMask;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Audio")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip pelletSFX;
    [SerializeField] private AudioClip cherrySFX;
    [SerializeField] private AudioClip powerPelletSFX;
    [SerializeField] private AudioClip ghostEatSFX;
    [SerializeField] private AudioClip deathSFX;
    [SerializeField] private AudioClip introMusic;
    [SerializeField] private AudioClip ghostNormalMusic;
    [SerializeField] private AudioClip ghostScaredMusic;

    [Header("Ghosts")]
    [SerializeField] private List<GhostController> ghosts;

    private int score = 0;
    private int lives;
    private float gameTime = 0f;
    private bool isGameActive = false;
    private bool isScaredTimerActive = false;
    private float scaredTimer = 0f;

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

            if (scaredTimer <= 3f && scaredTimer > 0f)
            {
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

    private IEnumerator RoundStartSequence()
    {
        isGameActive = false;
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

        if (bgmSource != null && ghostNormalMusic != null)
        {
            bgmSource.clip = ghostNormalMusic;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

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

    public void OnPelletEaten() { PlaySFX(pelletSFX); AddScore(10); }
    public void OnCherryEaten() { PlaySFX(cherrySFX); AddScore(100); }

    public void OnPowerPelletEaten()
    {
        PlaySFX(powerPelletSFX);
        AddScore(50);
        isScaredTimerActive = true;
        scaredTimer = 10f;

        if (scaredTimerText != null)
        {
            scaredTimerText.gameObject.SetActive(true);
            scaredTimerText.text = "10";
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
    }

    private void EndScaredState()
    {
        isScaredTimerActive = false;
        if (scaredTimerText != null) scaredTimerText.gameObject.SetActive(false);

        foreach (var g in ghosts)
            if (g != null && g.CurrentState != GhostController.GhostState.Dead)
                g.SetState(GhostController.GhostState.Normal);

        if (bgmSource != null && ghostNormalMusic != null)
        {
            bgmSource.clip = ghostNormalMusic;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

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

        var player = FindObjectOfType<PacStudentController>();
        if (player != null)
        {
            player.enabled = false;
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

    private IEnumerator GameOverSequence()
    {
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
        if (score > prevHigh || (score == prevHigh && (prevTime <= 0f || gameTime < prevTime)))
        {
            PlayerPrefs.SetInt("HighScore", score);
            PlayerPrefs.SetFloat("BestTime", gameTime);
            PlayerPrefs.Save();
        }
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip);
    }

    public void ExitToMenu() => SceneManager.LoadScene("StartScene");
    public bool IsGameActive => isGameActive;
}