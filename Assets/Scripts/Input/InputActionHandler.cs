using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Threading.Tasks;

namespace GameCore.Core
{
    /// <summary>
    /// Універсальний обробник UI-дiй для всіх платформ (геймпад, клавіатура, тач).
    /// </summary>
    public class InputActionHandler : MonoBehaviour, IService
    {
        [Header("Input")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private InputActionAsset inputActions;

        [Header("UI Events")]
        public UnityEvent onSubmit;
        public UnityEvent onCancel;
        public UnityEvent onPause;
        public UnityEvent onMiddleClick;

        private InputAction submitAction;
        private InputAction cancelAction;
        private InputAction pauseAction;
        private InputAction middleClickAction;

        private void Awake()
        {
            if (playerInput == null)
                playerInput = FindFirstObjectByType<PlayerInput>();

            if (inputActions == null && playerInput != null)
                inputActions = playerInput.actions;

            if (inputActions != null)
            {
                submitAction = inputActions.FindAction("Submit");
                cancelAction = inputActions.FindAction("Cancel");
                pauseAction = inputActions.FindAction("Menu");
                middleClickAction = inputActions.FindAction("MiddleClick");

                Bind(submitAction, OnSubmit);
                Bind(cancelAction, OnCancel);
                Bind(pauseAction, OnPause);
                Bind(middleClickAction, OnMiddleClick);
            }
            else
            {
                CoreLogger.LogWarning("INPUT", "❗ InputActions не призначені в InputActionHandler.");
            }
        }
        public async Task Initialize()
        {
            CoreLogger.Log("INPUT", "InputActionHandler initialized via IService");
            await Task.CompletedTask;
        }
        private void OnDestroy()
        {
            Unbind(submitAction, OnSubmit);
            Unbind(cancelAction, OnCancel);
            Unbind(pauseAction, OnPause);
            Unbind(middleClickAction, OnMiddleClick);
        }

        private void Bind(InputAction action, System.Action callback)
        {
            if (action != null)
                action.performed += ctx => callback?.Invoke();
        }

        private void Unbind(InputAction action, System.Action callback)
        {
            if (action != null)
                action.performed -= ctx => callback?.Invoke();
        }

        private void OnSubmit()
        {
            CoreLogger.Log("INPUT", "✅ Submit action");
            onSubmit?.Invoke();
        }

        private void OnCancel()
        {
            CoreLogger.Log("INPUT", "❌ Cancel action");
            onCancel?.Invoke();
        }

        private void OnPause()
        {
            CoreLogger.Log("INPUT", "⏸ Pause/Menu action");
            onPause?.Invoke();
        }

        private void OnMiddleClick()
        {
            CoreLogger.Log("INPUT", "🖱️ MiddleClick action");
            onMiddleClick?.Invoke();
        }
    }
}
