using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI gameTimerText;
    [SerializeField] private TextMeshProUGUI scaredTimerText;
    [SerializeField] private List<GameObject> lifeIcons = new List<GameObject>();

    [Header("Countdown")]
    [SerializeField] private GameObject screenMask;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip wallHitClip;
    [SerializeField] private AudioClip cherryEatClip;
    [SerializeField] private AudioClip ghostEatClip;
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip bgmNormal;
    [SerializeField] private AudioClip bgmScared;
    [SerializeField] private AudioClip bgmDead;

    [Header("PacStudent")]
    [SerializeField] private PacStudentController pacStudent;
    [SerializeField] private Vector3 pacSpawnPos = new Vector3(1.5f, -1.5f, 0f);

    [Header("Ghosts")]
    [SerializeField] private List<GhostController> ghosts = new List<GhostController>();
    [SerializeField] private List<Vector3> ghostSpawnPositions = new List<Vector3>();

    // 内部状态
    private int score = 0;
    private int lives = 3;
    private float gameTime = 0f;
    private bool isGameActive = false;
    private bool isScaredTimerActive = false;
    private float scaredTimer = 0f;
    private bool isGameOver = false;

    public bool IsGameActive => isGameActive;

    private void Start()
    {
        UpdateScoreUI();
        UpdateLivesUI();

        if (screenMask != null) screenMask.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (scaredTimerText != null) scaredTimerText.gameObject.SetActive(false);

        StartCoroutine(StartCountdown());
    }

    private void Update()
    {
        if (!isGameActive || isGameOver) return;

        gameTime += Time.deltaTime;
        UpdateTimerUI();

        if (isScaredTimerActive)
        {
            scaredTimer -= Time.deltaTime;

            if (scaredTimerText != null)
            {
                scaredTimerText.gameObject.SetActive(true);
                scaredTimerText.text = Mathf.CeilToInt(Mathf.Max(scaredTimer, 0f)).ToString();
            }

            // 剩 3 秒进入 Recovering
            if (scaredTimer <= 3f && scaredTimer + Time.deltaTime > 3f)
            {
                SetAllGhostsState(GhostController.GhostState.Recovering);
                PlayBGM(bgmNormal); // 可以换成 Recovering 音效
            }

            if (scaredTimer <= 0f)
            {
                isScaredTimerActive = false;
                scaredTimer = 0f;
                if (scaredTimerText != null) scaredTimerText.gameObject.SetActive(false);
                SetAllGhostsState(GhostController.GhostState.Normal);
                PlayBGM(bgmNormal);
            }
        }
    }

    // ---------------- 分数 / 生命 / 计时 ----------------
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
        for (int i = 0; i < lifeIcons.Count; i++)
            if (lifeIcons[i] != null) lifeIcons[i].SetActive(i < lives);
    }

    private void UpdateTimerUI()
    {
        if (gameTimerText == null) return;
        int m = Mathf.FloorToInt(gameTime / 60f);
        int s = Mathf.FloorToInt(gameTime % 60f);
        int ms = Mathf.FloorToInt((gameTime * 1000f) % 1000f);
        gameTimerText.text = string.Format("{0:00}:{1:00}:{2:000}", m, s, ms);
    }

    // ---------------- 能量豆 ----------------
    public void ActivatePowerPellet()
    {
        isScaredTimerActive = true;
        scaredTimer = 10f;
        if (scaredTimerText != null) scaredTimerText.gameObject.SetActive(true);
        SetAllGhostsState(GhostController.GhostState.Scared);
        PlayBGM(bgmScared);
    }

    private void SetAllGhostsState(GhostController.GhostState state)
    {
        foreach (var g in ghosts)
            if (g != null) g.SetState(state);
    }

    // ---------------- PacStudent 死亡 ----------------
    public void PlayerCaught()
    {
        if (isGameOver) return;

        lives--;
        UpdateLivesUI();

        // 重置鬼状态与位置
        SetAllGhostsState(GhostController.GhostState.Normal);
        for (int i = 0; i < ghosts.Count && i < ghostSpawnPositions.Count; i++)
            if (ghosts[i] != null) ghosts[i].ResetToStart(ghostSpawnPositions[i]);

        if (pacStudent != null) pacStudent.ResetToStart(pacSpawnPos);

        isScaredTimerActive = false;
        scaredTimer = 0f;
        if (scaredTimerText != null) scaredTimerText.gameObject.SetActive(false);

        if (lives <= 0) GameOver();
        else PlayBGM(bgmNormal);
    }

    // ---------------- 音效 ----------------
    public void PlayWallHitSFX()
    {
        if (sfxSource != null && wallHitClip != null) sfxSource.PlayOneShot(wallHitClip);
    }

    public void PlayCherryEatSFX()
    {
        if (sfxSource != null && cherryEatClip != null) sfxSource.PlayOneShot(cherryEatClip);
    }

    private void PlayGhostEatSFX()
    {
        if (sfxSource != null && ghostEatClip != null) sfxSource.PlayOneShot(ghostEatClip);
    }

    private void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null || clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    // ---------------- 倒计时 ----------------
    private IEnumerator StartCountdown()
    {
        isGameActive = false;
        if (screenMask != null) screenMask.SetActive(true);
        if (countdownText != null) countdownText.gameObject.SetActive(true);

        string[] steps = { "3", "2", "1", "GO!" };
        foreach (var s in steps)
        {
            if (countdownText != null) countdownText.text = s;
            yield return new WaitForSeconds(1f);
        }

        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (screenMask != null) screenMask.SetActive(false);

        isGameActive = true;
        PlayBGM(bgmNormal);
    }

    // ---------------- Game Over ----------------
    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        isGameActive = false;

        SaveHighScore();

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverText != null) gameOverText.text = "Game Over";

        StartCoroutine(ReturnToMenuAfter(3f));
    }

    private void SaveHighScore()
    {
        int prevHigh = PlayerPrefs.GetInt("HighScore", 0);
        float prevTime = PlayerPrefs.GetFloat("BestTime", float.MaxValue);

        if (score > prevHigh || (score == prevHigh && gameTime < prevTime))
        {
            PlayerPrefs.SetInt("HighScore", score);
            PlayerPrefs.SetFloat("BestTime", gameTime);
            PlayerPrefs.Save();
        }
    }

    private IEnumerator ReturnToMenuAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene("StartScene");
    }

    public void ExitToMenu()
    {
        SceneManager.LoadScene("StartScene");
    }
}