using UnityEngine;

public class TitleMenu : MonoBehaviour
{
    public void GoToMainScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
    }
}
