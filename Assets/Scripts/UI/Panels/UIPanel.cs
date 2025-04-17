using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;


namespace GameCore.Core
{
    public class UIPanel : MonoBehaviour, IUIPanel
    {
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected string panelId; // ������������� �����

        protected UIPanelAnimationType showAnimationType = UIPanelAnimationType.Fade;
        protected UIPanelAnimationType hideAnimationType = UIPanelAnimationType.Fade;
        protected float showAnimationDuration = 0.3f;
        protected float hideAnimationDuration = 0.3f;
        public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0.01f;
        public string PanelId => string.IsNullOrEmpty(panelId) ? GetType().Name : panelId;

        protected virtual void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // ��������� ��������� ������
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public virtual async Task Show()
        {
            gameObject.SetActive(true);

            UIPanelAnimation animation = ServiceLocator.Instance?.GetService<UIPanelAnimation>();

            if (animation != null)
            {
                await animation.AnimateShow(GetComponent<RectTransform>(), canvasGroup,
                    showAnimationType, showAnimationDuration);
            }
            else
            {
                // ������ �������, ���� UIPanelAnimation ����������
                canvasGroup.alpha = 1;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            OnPanelShown();
        }

        public virtual async Task Hide()
        {
            UIPanelAnimation animation = ServiceLocator.Instance?.GetService<UIPanelAnimation>();

            if (animation != null)
            {
                await animation.AnimateHide(GetComponent<RectTransform>(), canvasGroup,
                    hideAnimationType, hideAnimationDuration);
            }
            else
            {
                // ������ �������, ���� UIPanelAnimation ����������
                canvasGroup.alpha = 0;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            OnPanelHidden();
        }

        /// <summary>
        /// ����� ���� ����� ��� ���������� ������������ � ����
        /// </summary>
        public virtual void Reset()
        {
            // ������� ���� CanvasGroup
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // ������� ���� ���� �����
            var inputFields = GetComponentsInChildren<TMP_InputField>(true);
            foreach (var input in inputFields)
            {
                input.text = string.Empty;
            }

            // ������� ���� �����������
            var toggles = GetComponentsInChildren<UnityEngine.UI.Toggle>(true);
            foreach (var toggle in toggles)
            {
                toggle.isOn = false;
            }

            // ������� ���� ��������
              var sliders = GetComponentsInChildren<UnityEngine.UI.Slider>(true);
            foreach (var slider in sliders)
            {
                slider.value = slider.minValue;
            }

            // ������� ����������, �� ��������� IResetable
            var resetables = GetComponentsInChildren<IResetable>(true);
            foreach (var resetable in resetables)
            {
                resetable.ResetState();
            }
        }

        public void SetAnimationType(UIPanelAnimationType showType, UIPanelAnimationType hideType)
        {
            showAnimationType = showType;
            hideAnimationType = hideType;
        }

        public void SetAnimationDurations(float showDuration, float hideDuration)
        {
            showAnimationDuration = showDuration;
            hideAnimationDuration = hideDuration;
        }

        protected virtual void OnPanelShown() { }
        protected virtual void OnPanelHidden() { }
    }

}