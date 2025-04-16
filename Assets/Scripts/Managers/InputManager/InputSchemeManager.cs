using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Threading.Tasks;

namespace GameCore.Core
{
    public class InputSchemeManager : MonoBehaviour, IService, IInitializable
    {
        public static InputSchemeManager Instance { get; private set; }
        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 40;

        [Header("Player Input Reference")]
        [SerializeField] private PlayerInput playerInput;

        [Header("Current Info (ReadOnly)")]
        [SerializeField] private string currentControlScheme;
        [SerializeField] private string currentActionMap;

        public event Action<string> OnControlSchemeChanged;
        public event Action<string> OnActionMapSwitched;
        public InputActionAsset actions => playerInput?.actions;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (playerInput == null)
                playerInput = FindFirstObjectByType<PlayerInput>();

            if (playerInput != null)
            {
                currentControlScheme = playerInput.currentControlScheme;
                currentActionMap = playerInput.currentActionMap.name;
                playerInput.onControlsChanged += OnControlsChanged;
            }
            else
            {
                CoreLogger.LogError("INPUT", "PlayerInput not assigned or not found.");
            }
        }
        public async Task Initialize()
        {
            CoreLogger.Log("INPUT", "InputSchemeManager initialized via IService");
            await Task.CompletedTask;
        }

        private void OnDestroy()
        {
            if (playerInput != null)
                playerInput.onControlsChanged -= OnControlsChanged;
        }

        private void OnControlsChanged(PlayerInput input)
        {
            currentControlScheme = input.currentControlScheme;
            CoreLogger.Log("INPUT", $"🔄 Control scheme changed: {currentControlScheme}");
            OnControlSchemeChanged?.Invoke(currentControlScheme);
        }

        public void SwitchToUI()
        {
            if (playerInput == null) return;

            if (playerInput.currentActionMap.name != "UI")
            {
                playerInput.SwitchCurrentActionMap("UI");
                currentActionMap = "UI";
                CoreLogger.Log("INPUT", "🧭 Switched to UI Input Map");
                OnActionMapSwitched?.Invoke("UI");
            }
        }

        public void SwitchToGameplay()
        {
            if (playerInput == null) return;

            if (playerInput.currentActionMap.name != "Gameplay")
            {
                playerInput.SwitchCurrentActionMap("Gameplay");
                currentActionMap = "Gameplay";
                CoreLogger.Log("INPUT", "🎮 Switched to Gameplay Input Map");
                OnActionMapSwitched?.Invoke("Gameplay");
            }
        }

        public string GetCurrentScheme() => currentControlScheme;
        public string GetCurrentMap() => currentActionMap;
    }
}
