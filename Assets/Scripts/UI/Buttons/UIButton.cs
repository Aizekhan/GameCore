using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


namespace GameCore.Core
{
    /// <summary>
    /// Розширений компонент кнопки з додатковими функціями, звуками та підтримкою навігації.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISubmitHandler
    {
        [Header("Реєстрація")]
        [SerializeField] private string buttonCategory = "Default";

        [Header("Навігація")]
        [SerializeField] private bool isBackButton = false;
        [SerializeField] private string showPanelName = "";

        [Header("Аудіо")]
        [SerializeField] private string clickSoundName = "ButtonClick";
        [SerializeField] private string hoverSoundName = "ButtonHover";
        [SerializeField] private AudioType soundType = AudioType.UI;

        [Header("Анімація")]
        [SerializeField] private bool useHoverAnimation = true;
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float animationSpeed = 10f;

        private Button _button;
        private Vector3 _originalScale;
        private AudioManager _audioManager;
        private UINavigationService _navigationService;
        private UIButtonRegistry _buttonRegistry;

        public Button Button => _button;
        public string ButtonCategory => buttonCategory;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _originalScale = transform.localScale;
            _button.onClick.AddListener(OnButtonClick);
        }

        private void Start()
        {
            LazyInit();
            RegisterInButtonRegistry();
        }

        private void LazyInit()
        {
            _audioManager ??= ServiceLocator.Instance?.GetService<AudioManager>();
            _navigationService ??= ServiceLocator.Instance?.GetService<UINavigationService>();
            _buttonRegistry ??= ServiceLocator.Instance?.GetService<UIButtonRegistry>();
        }

        private void RegisterInButtonRegistry()
        {
            _buttonRegistry?.RegisterButton(this, buttonCategory);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_button.interactable) return;
            LazyInit();

            _audioManager?.PlaySound(hoverSoundName, soundType);

            if (useHoverAnimation)
            {
                LeanTween.cancel(gameObject);
                LeanTween.scale(gameObject, _originalScale * hoverScale, 1f / animationSpeed)
                         .setEase(LeanTweenType.easeOutQuad);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_button.interactable) return;

            if (useHoverAnimation)
            {
                LeanTween.cancel(gameObject);
                LeanTween.scale(gameObject, _originalScale, 1f / animationSpeed)
                         .setEase(LeanTweenType.easeOutQuad);
            }
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (_button != null && _button.interactable)
            {
                OnButtonClick();
            }
        }

        private void OnButtonClick()
        {
            LazyInit();
            _audioManager?.PlaySound(clickSoundName, soundType);

            if (isBackButton)
            {
                _navigationService?.GoBack();
            }
            else if (!string.IsNullOrEmpty(showPanelName))
            {
                EventBus.Emit("UI/ShowPanel", showPanelName);
            }
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnButtonClick);
            LeanTween.cancel(gameObject);
        }

        // Додаткові методи для динамічного налаштування
        public void SetCategory(string category)
        {
            buttonCategory = category;
            RegisterInButtonRegistry();
        }

        public void AddCustomAction(UnityEngine.Events.UnityAction action)
        {
            _button.onClick.AddListener(action);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            string cleanName = gameObject.name;

            // Видалити старі позначки
            cleanName = cleanName.Replace("[BackButton]", "").Trim();
            if (showPanelName != null)
                cleanName = cleanName.Replace($"[{showPanelName}]", "").Trim();

            if (isBackButton)
                gameObject.name = $"[BackButton] {cleanName}";
            else if (!string.IsNullOrEmpty(showPanelName))
                gameObject.name = $"[{showPanelName}] {cleanName}";
            else
                gameObject.name = cleanName;
        }
#endif

    }
}