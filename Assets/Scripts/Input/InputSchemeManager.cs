using UnityEngine;
using UnityEngine.InputSystem;

namespace GameCore.Core
{
    public class InputSchemeManager : MonoBehaviour
    {
        [Header("Player Input Reference")]
        [SerializeField] private PlayerInput playerInput;

        [Header("Current Info (ReadOnly)")]
        [SerializeField] private string currentControlScheme;
        [SerializeField] private string currentActionMap;

        private void Awake()
        {

            if (playerInput == null)
                playerInput = UnityEngine.Object.FindFirstObjectByType<PlayerInput>();


            currentControlScheme = playerInput.currentControlScheme;
            currentActionMap = playerInput.currentActionMap.name;


            playerInput.onControlsChanged += OnControlsChanged;
        }

        private void OnDestroy()
        {
            if (playerInput != null)
                playerInput.onControlsChanged -= OnControlsChanged;
        }

        private void OnControlsChanged(PlayerInput input)
        {
            currentControlScheme = input.currentControlScheme;
            CoreLogger.Log($"🔄 Control scheme changed: {currentControlScheme}");
        }

        public void SwitchToUI()
        {
            if (playerInput.currentActionMap.name != "UI")
            {
                playerInput.SwitchCurrentActionMap("UI");
                currentActionMap = "UI";
                CoreLogger.Log("🧭 Switched to UI Input Map");
            }
        }

        public void SwitchToGameplay()
        {
            if (playerInput.currentActionMap.name != "Gameplay")
            {
                playerInput.SwitchCurrentActionMap("Gameplay");
                currentActionMap = "Gameplay";
                CoreLogger.Log("🎮 Switched to Gameplay Input Map");
            }
        }

        public string GetCurrentScheme() => currentControlScheme;
        public string GetCurrentMap() => currentActionMap;
    }
}