// Assets/Scripts/UI/Components/UIPanel.cs
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

namespace GameCore.Core
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIPanel : MonoBehaviour, IUIPanel
    {
        [SerializeField] protected string panelName;
        protected CanvasGroup canvasGroup;

        // IUIPanel implementation
        public string PanelName => string.IsNullOrEmpty(panelName) ? gameObject.name : panelName;
        public bool IsActive => canvasGroup.alpha > 0 && canvasGroup.interactable;

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();

            // якщо панель не маЇ ≥мен≥, використовуЇмо ≥м'€ GameObject
            if (string.IsNullOrEmpty(panelName))
            {
                panelName = gameObject.name;
            }
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            CoreLogger.Log("UI", $"Panel shown: {PanelName}");

            // —пов≥щаЇмо про зм≥ну стану панел≥
            EventBus.Emit("UI/PanelShown", PanelName);
        }

        public virtual void Hide()
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            CoreLogger.Log("UI", $"Panel hidden: {PanelName}");

            // —пов≥щаЇмо про зм≥ну стану панел≥
            EventBus.Emit("UI/PanelHidden", PanelName);
        }

        public virtual async Task ShowAnimated(float duration = 0.25f)
        {
            gameObject.SetActive(true);
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            float startTime = Time.time;
            canvasGroup.alpha = 0;

            while (Time.time - startTime < duration)
            {
                float normalizedTime = (Time.time - startTime) / duration;
                canvasGroup.alpha = normalizedTime;
                await Task.Yield();
            }

            canvasGroup.alpha = 1;

            CoreLogger.Log("UI", $"Panel shown with animation: {PanelName}");

            // —пов≥щаЇмо про зм≥ну стану панел≥
            EventBus.Emit("UI/PanelShown", PanelName);
        }

        public virtual async Task HideAnimated(float duration = 0.25f)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            float startTime = Time.time;
            canvasGroup.alpha = 1;

            while (Time.time - startTime < duration)
            {
                float normalizedTime = 1 - (Time.time - startTime) / duration;
                canvasGroup.alpha = normalizedTime;
                await Task.Yield();
            }

            canvasGroup.alpha = 0;

            CoreLogger.Log("UI", $"Panel hidden with animation: {PanelName}");

            // —пов≥щаЇмо про зм≥ну стану панел≥
            EventBus.Emit("UI/PanelHidden", PanelName);
        }
    }
}