// Clean version of InputSchemeManager.cs — no auto-creation, no Resources.Load, only external setup
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
        [SerializeField] private PlayerInput _playerInput;

        [Header("Current Info (ReadOnly)")]
        [SerializeField] private string currentControlScheme;
        [SerializeField] private string currentActionMap;

        public event Action<string> OnControlSchemeChanged;
        public event Action<string> OnActionMapSwitched;
        public InputActionAsset actions => _playerInput?.actions;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void SetPlayerInput(PlayerInput playerInput)
        {
            if (playerInput == null)
            {
                CoreLogger.LogError("INPUT", "❌ PlayerInput is null!");
                return;
            }

            this._playerInput = playerInput;
            CoreLogger.Log("INPUT", $"✅ PlayerInput linked: {playerInput.gameObject.name}");
        }

        public async Task Initialize()
        {
            CoreLogger.Log("INPUT", "InputSchemeManager initialized via IService");
            await Task.CompletedTask;
        }

        private void OnDestroy()
        {
            if (_playerInput != null)
                _playerInput.onControlsChanged -= OnControlsChanged;
        }

        private void OnControlsChanged(PlayerInput input)
        {
            currentControlScheme = input.currentControlScheme;
            CoreLogger.Log("INPUT", $"🔄 Control scheme changed: {currentControlScheme}");
            OnControlSchemeChanged?.Invoke(currentControlScheme);
        }

        public void SwitchToUI()
        {
            if (_playerInput == null) return;

            if (_playerInput.currentActionMap.name != "UI")
            {
                _playerInput.SwitchCurrentActionMap("UI");
                currentActionMap = "UI";
                CoreLogger.Log("INPUT", "🧭 Switched to UI Input Map");
                OnActionMapSwitched?.Invoke("UI");
            }
        }

        public void SwitchToGameplay()
        {
            if (_playerInput == null) return;

            if (_playerInput.currentActionMap.name != "Gameplay")
            {
                _playerInput.SwitchCurrentActionMap("Gameplay");
                currentActionMap = "Gameplay";
                CoreLogger.Log("INPUT", "🎮 Switched to Gameplay Input Map");
                OnActionMapSwitched?.Invoke("Gameplay");
            }
        }

        public string GetCurrentScheme() => currentControlScheme;
        public string GetCurrentMap() => currentActionMap;
    }
}
