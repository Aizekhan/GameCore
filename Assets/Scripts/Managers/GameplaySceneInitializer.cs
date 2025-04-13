using UnityEngine;

public class GameplaySceneInitializer : MonoBehaviour
{
    private void Start()
    {
        var inputManager = FindFirstObjectByType<InputSchemeManager>();
        if (inputManager != null)
        {
            inputManager.SwitchToGameplay();
        }
        else
        {
            Logger.LogWarning("❌ InputSchemeManager не знайдено!");
        }
    }
}