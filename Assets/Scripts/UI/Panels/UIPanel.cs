// Assets/Scripts/UI/Components/UIPanel.cs
using System.Threading.Tasks;
using UnityEngine;
using GameCore.Core.EventSystem;
using UnityEngine.UI;

namespace GameCore.Core
{
    /// <summary>
    /// Базовий клас для всіх UI панелей
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPanel : MonoBehaviour, IUIPanel
    {
        [Header("Animation Settings")]
        [SerializeField] private UIPanelAnimationType showAnimationType = UIPanelAnimationType.Default;
        [SerializeField] private UIPanelAnimationType hideAnimationType = UIPanelAnimationType.Default;
        [SerializeField] private float showDuration = -1f; // -1 = використовувати стандартне значення
        [SerializeField] private float hideDuration = -1f; // -1 = використовувати стандартне значення
        [SerializeField] private LeanTweenType showEaseType = LeanTweenType.notUsed; // notUsed = використовувати стандартне значення
        [SerializeField] private LeanTweenType hideEaseType = LeanTweenType.notUsed; // notUsed = використовувати стандартне значення

        protected CanvasGroup canvasGroup;
        protected RectTransform rectTransform;
        protected bool isVisible = false;

        public bool IsVisible => isVisible;

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();

            // Початково приховуємо панель
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            isVisible = false;
        }

        /// <summary>
        /// Відображає панель з анімацією
        /// </summary>
        public virtual async Task Show()
        {
            // Переконуємося, що компоненти ініціалізовані
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

            // Робимо панель інтерактивною
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            // Запускаємо анімацію
            if (UIPanelAnimation.Instance != null)
            {
                await UIPanelAnimation.Instance.AnimateShow(
                    rectTransform, canvasGroup,
                    showAnimationType, showDuration, showEaseType);
            }
            else
            {
                // Якщо анімацій немає, просто відображаємо панель
                canvasGroup.alpha = 1f;
            }

            OnShow();
            isVisible = true;

            // Відправляємо подію про відображення панелі
            EventBus.Emit("UI/PanelShown", gameObject.name);
        }

        /// <summary>
        /// Приховує панель з анімацією
        /// </summary>
        public virtual async Task Hide()
        {
            // Перевіряємо, чи панель видима
            if (!isVisible)
                return;

            // Запускаємо анімацію
            if (UIPanelAnimation.Instance != null)
            {
                await UIPanelAnimation.Instance.AnimateHide(
                    rectTransform, canvasGroup,
                    hideAnimationType, hideDuration, hideEaseType);
            }
            else
            {
                // Якщо анімацій немає, просто приховуємо панель
                canvasGroup.alpha = 0f;
            }

            // Робимо панель неінтерактивною
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            OnHide();
            isVisible = false;

            // Відправляємо подію про приховання панелі
            EventBus.Emit("UI/PanelHidden", gameObject.name);
        }

        /// <summary>
        /// Викликається після того, як панель стала видимою
        /// </summary>
        protected virtual void OnShow()
        {
            CoreLogger.Log("UI", $"Panel {gameObject.name} shown");
        }

        /// <summary>
        /// Викликається після того, як панель була прихована
        /// </summary>
        protected virtual void OnHide()
        {
            CoreLogger.Log("UI", $"Panel {gameObject.name} hidden");
        }

        /// <summary>
        /// Реєструє панель у UIPanelRegistry при створенні
        /// </summary>
        private void Start()
        {
            if (ServiceLocator.Instance != null && ServiceLocator.Instance.HasService<UIPanelRegistry>())
            {
                var registry = ServiceLocator.Instance.GetService<UIPanelRegistry>();
                registry.RegisterPanel(gameObject.name, gameObject);
            }
        }

        /// <summary>
        /// Змінює тип анімації панелі під час виконання
        /// </summary>
        public void SetAnimationType(UIPanelAnimationType showType, UIPanelAnimationType hideType)
        {
            showAnimationType = showType;
            hideAnimationType = hideType;
        }

        /// <summary>
        /// Змінює тривалості анімацій панелі під час виконання
        /// </summary>
        public void SetAnimationDurations(float showDuration, float hideDuration)
        {
            this.showDuration = showDuration;
            this.hideDuration = hideDuration;
        }
    }
}