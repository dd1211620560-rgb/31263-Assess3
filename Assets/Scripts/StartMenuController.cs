using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private TextMeshProUGUI bestTimeText;

    private void Start()
    {
        int hs = PlayerPrefs.GetInt("HighScore", 0);
        float bt = PlayerPrefs.GetFloat("BestTime", 0f);

        if (highScoreText != null) highScoreText.text = $"High Score: {hs:D6}";
        if (bestTimeText != null)
        {
            int m = Mathf.FloorToInt(bt / 60f);
            int s = Mathf.FloorToInt(bt % 60f);
            int ms = Mathf.FloorToInt((bt * 1000f) % 1000f);
            bestTimeText.text = $"Best Time: {m:00}:{s:00}:{ms:000}";
        }
    }

    public void OnLevel1Clicked() => SceneManager.LoadScene("Level01Scene");
    public void OnLevel2Clicked() => SceneManager.LoadScene("InnovationScene");
    public void OnExitClicked() => Application.Quit();
}