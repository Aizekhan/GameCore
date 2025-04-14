using UnityEngine;
using UnityEngine.SceneManagement;
namespace GameCore.Core
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string gameSceneName = "GameScene";
        [SerializeField] private string loadingSceneName = "LoadingScene";

        // Викликається при натисканні кнопки Start
        public void OnStartGamePressed()
        {
            SceneLoader.sceneToLoad = gameSceneName;
            SceneManager.LoadScene(loadingSceneName);
        }

        // Викликається при натисканні кнопки Exit
        public void OnExitPressed()
        {
            CoreLogger.Log("Exit button pressed. Quitting the game...");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        }
    }
}