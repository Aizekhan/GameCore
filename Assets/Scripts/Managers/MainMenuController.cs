using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string loadingSceneName = "LoadingScene";

    // ����������� ��� ��������� ������ Start
    public void OnStartGamePressed()
    {
        SceneLoader.sceneToLoad = gameSceneName;
        SceneManager.LoadScene(loadingSceneName);
    }

    // ����������� ��� ��������� ������ Exit
    public void OnExitPressed()
    {
        Logger.Log("Exit button pressed. Quitting the game...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
