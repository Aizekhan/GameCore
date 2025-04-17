// Assets/Scripts/UI/Components/UIPanel.cs
using System.Threading.Tasks;
using UnityEngine;
using GameCore.Core.EventSystem;
using UnityEngine.UI;

namespace GameCore.Core
{
    /// <summary>
    /// ������� ���� ��� ��� UI �������
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPanel : MonoBehaviour, IUIPanel
    {
        [Header("Animation Settings")]
        [SerializeField] private UIPanelAnimationType showAnimationType = UIPanelAnimationType.Default;
        [SerializeField] private UIPanelAnimationType hideAnimationType = UIPanelAnimationType.Default;
        [SerializeField] private float showDuration = -1f; // -1 = ��������������� ���������� ��������
        [SerializeField] private float hideDuration = -1f; // -1 = ��������������� ���������� ��������
        [SerializeField] private LeanTweenType showEaseType = LeanTweenType.notUsed; // notUsed = ��������������� ���������� ��������
        [SerializeField] private LeanTweenType hideEaseType = LeanTweenType.notUsed; // notUsed = ��������������� ���������� ��������

        protected CanvasGroup canvasGroup;
        protected RectTransform rectTransform;
        protected bool isVisible = false;

        public bool IsVisible => isVisible;

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();

            // ��������� ��������� ������
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            isVisible = false;
        }

        /// <summary>
        /// ³������� ������ � ��������
        /// </summary>
        public virtual async Task Show()
        {
            // ������������, �� ���������� �����������
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

            // ������ ������ �������������
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            // ��������� �������
            if (UIPanelAnimation.Instance != null)
            {
                await UIPanelAnimation.Instance.AnimateShow(
                    rectTransform, canvasGroup,
                    showAnimationType, showDuration, showEaseType);
            }
            else
            {
                // ���� ������� ����, ������ ���������� ������
                canvasGroup.alpha = 1f;
            }

            OnShow();
            isVisible = true;

            // ³���������� ���� ��� ����������� �����
            EventBus.Emit("UI/PanelShown", gameObject.name);
        }

        /// <summary>
        /// ������� ������ � ��������
        /// </summary>
        public virtual async Task Hide()
        {
            // ����������, �� ������ ������
            if (!isVisible)
                return;

            // ��������� �������
            if (UIPanelAnimation.Instance != null)
            {
                await UIPanelAnimation.Instance.AnimateHide(
                    rectTransform, canvasGroup,
                    hideAnimationType, hideDuration, hideEaseType);
            }
            else
            {
                // ���� ������� ����, ������ ��������� ������
                canvasGroup.alpha = 0f;
            }

            // ������ ������ ��������������
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            OnHide();
            isVisible = false;

            // ³���������� ���� ��� ���������� �����
            EventBus.Emit("UI/PanelHidden", gameObject.name);
        }

        /// <summary>
        /// ����������� ���� ����, �� ������ ����� �������
        /// </summary>
        protected virtual void OnShow()
        {
            CoreLogger.Log("UI", $"Panel {gameObject.name} shown");
        }

        /// <summary>
        /// ����������� ���� ����, �� ������ ���� ���������
        /// </summary>
        protected virtual void OnHide()
        {
            CoreLogger.Log("UI", $"Panel {gameObject.name} hidden");
        }

        /// <summary>
        /// ������ ������ � UIPanelRegistry ��� ��������
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
        /// ����� ��� ������� ����� �� ��� ���������
        /// </summary>
        public void SetAnimationType(UIPanelAnimationType showType, UIPanelAnimationType hideType)
        {
            showAnimationType = showType;
            hideAnimationType = hideType;
        }

        /// <summary>
        /// ����� ��������� ������� ����� �� ��� ���������
        /// </summary>
        public void SetAnimationDurations(float showDuration, float hideDuration)
        {
            this.showDuration = showDuration;
            this.hideDuration = hideDuration;
        }
    }
}