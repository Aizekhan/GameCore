using UnityEngine;


namespace GameCore.Core
{
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
                CoreLogger.LogWarning("❌ InputSchemeManager не знайдено!");
            }
        }
    }
}