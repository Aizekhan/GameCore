// Assets/Scripts/UI/Components/UIPanel.cs
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using GameCore.Core.EventSystem;

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

        [Header("Panel Settings")]
        [SerializeField] private string panelId; // Для ідентифікації в пулі

        protected CanvasGroup canvasGroup;
        protected RectTransform rectTransform;
        protected bool isVisible = false;

        public string PanelId => string.IsNullOrEmpty(panelId) ? gameObject.name : panelId;
        public bool IsVisible => isVisible;

        protected virtual void Awake()
        {
            // Переконуємося, що компоненти ініціалізовані
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
                CoreLogger.LogWarning("UI", $"CanvasGroup not found on {gameObject.name}, creating one.");
            }

            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
                CoreLogger.LogWarning("UI", $"RectTransform not found on {gameObject.name}, creating one.");
            }

            // Початково приховуємо панель
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            isVisible = false;
        }

        protected virtual void OnEnable()
        {
            // Якщо компоненти ще не ініціалізовані (наприклад, якщо це інстанціюється з пулу),
            // ініціалізуємо їх знову
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
        }

        /// <summary>
        /// Відображає панель з анімацією
        /// </summary>
        public virtual async Task Show()
        {
            // Переконуємося, що компоненти ініціалізовані
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

            // Активуємо GameObject, якщо вона неактивна
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            // Робимо панель інтерактивною
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            // Запускаємо анімацію
            UIPanelAnimation animationService = null;
            if (ServiceLocator.Instance != null)
            {
                animationService = ServiceLocator.Instance.GetService<UIPanelAnimation>();
            }
            else
            {
                animationService = UIPanelAnimation.Instance;
            }

            if (animationService != null)
            {
                await animationService.AnimateShow(
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

            // Переконуємося, що компоненти ініціалізовані
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

            // Запускаємо анімацію
            UIPanelAnimation animationService = null;
            if (ServiceLocator.Instance != null)
            {
                animationService = ServiceLocator.Instance.GetService<UIPanelAnimation>();
            }
            else
            {
                animationService = UIPanelAnimation.Instance;
            }

            if (animationService != null)
            {
                await animationService.AnimateHide(
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

        /// <summary>
        /// Скидає стан панелі до початкового
        /// </summary>
        public virtual void Reset()
        {
            // Переконуємося, що компоненти ініціалізовані
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

            // Скидаємо налаштування відображення
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            isVisible = false;

            // Скидаємо позицію і масштаб (якщо були змінені під час анімації)
            rectTransform.localScale = Vector3.one;

            // Очищаємо будь-які анімації
            LeanTween.cancel(gameObject);

            // Викликаємо віртуальний метод для додаткового скидання у дочірніх класах
            OnReset();
        }

        /// <summary>
        /// Додаткові дії при скиданні
        /// (перевизначається в дочірніх класах)
        /// </summary>
        protected virtual void OnReset()
        {
            // Можна перевизначити в дочірніх класах для очищення даних
        }

        /// <summary>
        /// Підготовка панелі до повернення в пул
        /// </summary>
        public virtual void PrepareForPool()
        {
            // Скидаємо стан панелі
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            isVisible = false;

            // Додаткова підготовка перед поверненням до пулу
            OnPrepareForPool();
        }

        /// <summary>
        /// Додаткові дії при підготовці до повернення в пул 
        /// (перевизначається в дочірніх класах)
        /// </summary>
        protected virtual void OnPrepareForPool()
        {
            // Можна перевизначити в дочірніх класах для очищення даних
        }
    }
}