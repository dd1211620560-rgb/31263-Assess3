using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
    public void OnLevel1Clicked()
    {
        // 加载复刻关卡（确保在 Build Settings 中名字拼写正确）
        SceneManager.LoadScene("Level01Scene");
    }

    public void OnLevel2Clicked()
    {
        // 如果还没做创新关卡，暂时留空或加载占位
        // SceneManager.LoadScene("InnovationScene");
        Debug.Log("Level 2 not implemented yet.");
    }

    public void OnExitClicked()
    {
        Application.Quit();
    }
}