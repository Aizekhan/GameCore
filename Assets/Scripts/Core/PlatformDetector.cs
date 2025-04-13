using UnityEngine;
using UnityEngine.SceneManagement;

public class PlatformDetector : MonoBehaviour
{
    [Tooltip("Назва сцени, на яку перейти після визначення платформи")]
    public string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        DetectPlatform();
    }

    private void Start()
    {
        // Після визначення — переходимо до головного меню
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void DetectPlatform()
    {
#if UNITY_XBOXONE || UNITY_GAMECORE
        Logger.Log("🎮 Платформа: Xbox");
#elif UNITY_PS4 || UNITY_PS5
        Logger.Log("🕹 Платформа: PlayStation");
#elif UNITY_STANDALONE
        Logger.Log("🖥 Платформа: Windows/macOS/Linux (Standalone)");
#elif UNITY_ANDROID
        Logger.Log("📱 Платформа: Android");
#elif UNITY_IOS
        Logger.Log("🍏 Платформа: iOS");
#elif UNITY_WEBGL
        Logger.Log("🌐 Платформа: WebGL (браузер)");
#else
        Logger.Log("❓ Невідома платформа: " + Application.platform);
#endif
    }
}
