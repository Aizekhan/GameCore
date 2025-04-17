// Assets/Scripts/UI/Animation/UIPanelAnimation.cs
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace GameCore.Core
{
    /// <summary>
    /// Тип анімації для UI панелей
    /// </summary>
    public enum UIPanelAnimationType
    {
        None,
        Default,       // Використовує тип анімації за замовчуванням з UIPanelAnimation
        Fade,
        Scale,
        SlideFromRight,
        SlideFromLeft,
        SlideFromTop,
        SlideFromBottom,
        Rotate,
        FadeAndScale
    }

    /// <summary>
    /// Центральний клас для управління анімаціями UI панелей з використанням LeanTween
    /// </summary>
    public class UIPanelAnimation : MonoBehaviour, IService, IInitializable
    {
        [Header("Default Settings")]
        [SerializeField] private float defaultDuration = 0.3f;
        [SerializeField] private LeanTweenType defaultEaseIn = LeanTweenType.easeOutBack;
        [SerializeField] private LeanTweenType defaultEaseOut = LeanTweenType.easeInBack;
        [SerializeField] private UIPanelAnimationType defaultAnimationType = UIPanelAnimationType.Fade;

        public static UIPanelAnimation Instance { get; private set; }
        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 31;
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async Task Initialize()
        {
            CoreLogger.Log("UI", "UIPanelAnimation initialized!");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Запускає анімацію появи панелі
        /// </summary>
        public async Task AnimateShow(RectTransform panelTransform, CanvasGroup canvasGroup,
            UIPanelAnimationType animationType = UIPanelAnimationType.Default,
            float duration = -1f, LeanTweenType easeType = LeanTweenType.notUsed)
        {
            if (animationType == UIPanelAnimationType.Default)
                animationType = defaultAnimationType;

            if (duration < 0)
                duration = defaultDuration;

            if (easeType == LeanTweenType.notUsed)
                easeType = defaultEaseIn;

            // Зберігаємо початкові значення для повернення при знищенні
            Vector3 originalPosition = panelTransform.anchoredPosition3D;
            Vector3 originalScale = panelTransform.localScale;

            // Завершуємо всі активні твіни на об'єкті перед початком нових
            LeanTween.cancel(panelTransform.gameObject);

            // Налаштовуємо початковий стан
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            switch (animationType)
            {
                case UIPanelAnimationType.None:
                    if (canvasGroup != null)
                        canvasGroup.alpha = 1f;
                    tcs.SetResult(true);
                    break;

                case UIPanelAnimationType.Fade:
                    if (canvasGroup != null)
                    {
                        LeanTween.alphaCanvas(canvasGroup, 1f, duration)
                            .setEase(easeType)
                            .setOnComplete(() => tcs.SetResult(true));
                    }
                    else
                    {
                        tcs.SetResult(true);
                    }
                    break;

                case UIPanelAnimationType.Scale:
                    panelTransform.localScale = Vector3.zero;
                    LeanTween.scale(panelTransform.gameObject, originalScale, duration)
                        .setEase(easeType)
                        .setOnComplete(() => tcs.SetResult(true));

                    if (canvasGroup != null)
                        canvasGroup.alpha = 1f;
                    break;

                case UIPanelAnimationType.SlideFromRight:
                    panelTransform.anchoredPosition = new Vector2(Screen.width, originalPosition.y);
                    LeanTween.moveX(panelTransform.gameObject, originalPosition.x, duration)
                        .setEase(easeType)
                        .setOnComplete(() => tcs.SetResult(true));

                    if (canvasGroup != null)
                        canvasGroup.alpha = 1f;
                    break;

                case UIPanelAnimationType.SlideFromLeft:
                    panelTransform.anchoredPosition = new Vector2(-Screen.width, originalPosition.y);
                    LeanTween.moveX(panelTransform.gameObject, originalPosition.x, duration)
                        .setEase(easeType)
                        .setOnComplete(() => tcs.SetResult(true));

                    if (canvasGroup != null)
                        canvasGroup.alpha = 1f;
                    break;

                case UIPanelAnimationType.SlideFromTop:
                    panelTransform.anchoredPosition = new Vector2(originalPosition.x, Screen.height);
                    LeanTween.moveY(panelTransform.gameObject, originalPosition.y, duration)
                        .setEase(easeType)
                        .setOnComplete(() => tcs.SetResult(true));

                    if (canvasGroup != null)
                        canvasGroup.alpha = 1f;
                    break;

                case UIPanelAnimationType.SlideFromBottom:
                    panelTransform.anchoredPosition = new Vector2(originalPosition.x, -Screen.height);
                    LeanTween.moveY(panelTransform.gameObject, originalPosition.y, duration)
                        .setEase(easeType)
                        .setOnComplete(() => tcs.SetResult(true));

                    if (canvasGroup != null)
                        canvasGroup.alpha = 1f;
                    break;

                case UIPanelAnimationType.Rotate:
                    panelTransform.rotation = Quaternion.Euler(0, 0, 90);
                    LeanTween.rotateZ(panelTransform.gameObject, 0f, duration)
                        .setEase(easeType)
                        .setOnComplete(() => tcs.SetResult(true));

                    if (canvasGroup != null)
                        canvasGroup.alpha = 1f;
                    break;

                case UIPanelAnimationType.FadeAndScale:
                    panelTransform.localScale = Vector3.zero;
                    if (canvasGroup != null)
                    {
                        LeanTween.alphaCanvas(canvasGroup, 1f, duration)
                            .setEase(easeType);
                    }

                    LeanTween.scale(panelTransform.gameObject, originalScale, duration)
                        .setEase(easeType)
                        .setOnComplete(() => tcs.SetResult(true));
                    break;
            }

            await tcs.Task;
        }

        /// <summary>
        /// Запускає анімацію приховання панелі
        /// </summary>
        public async Task AnimateHide(RectTransform panelTransform, CanvasGroup canvasGroup,
            UIPanelAnimationType animationType = UIPanelAnimationType.Default,
            float duration = -1f, LeanTweenType easeType = LeanTweenType.notUsed)
        {
            if (animationType == UIPanelAnimationType.Default)
                animationType = defaultAnimationType;

            if (duration < 0)
                duration = defaultDuration;

            if (easeType == LeanTweenType.notUsed)
                easeType = defaultEaseOut;

            // Завершуємо всі активні твіни на об'єкті перед початком нових
            LeanTween.cancel(panelTransform.gameObject);

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            switch (animationType)
            {
                case UIPanelAnimationType.None:
                    if (canvasGroup != null)
                        canvasGroup.alpha = 0f;
                    tcs.SetResult(true);
                    break;

                case UIPanelAnimationType.Fade:
                    if (canvasGroup != null)
                    {
                        LeanTween.alphaCanvas(canvasGroup, 0f, duration)
                            .setEase(easeType)
                            .setOnComplete(() => tcs.SetResult(true));
                    }
                    else
                    {
                        tcs.SetResult(true);
                    }
                    break;

                case UIPanelAnimationType.Scale:
                    LeanTween.scale(panelTransform.gameObject, Vector3.zero, duration)
                        .setEase(easeType)
                        .setOnComplete(() => tcs.SetResult(true));
                    break;

                case UIPanelAnimationType.SlideFromRight:
                    LeanTween.moveX(panelTransform.gameObject, Screen.width, duration)
                        .setEase(easeType)
                        .setOnComplete(() => tcs.SetResult(true));
                    break;

                case UIPanelAnimationType.SlideFromLeft:
                    LeanTween.moveX(panelTransform.gameObject, -Screen.width, duration)
                        .setEase(easeType)
                        .setOnComplete(() => tcs.SetResult(true));
                    break;

                case UIPanelAnimationType.SlideFromTop:
                    LeanTween.moveY(panelTransform.gameObject, Screen.height, duration)
                        .setEase(easeType)
                        .setOnComplete(() => tcs.SetResult(true));
                    break;

                case UIPanelAnimationType.SlideFromBottom:
                    LeanTween.moveY(panelTransform.gameObject, -Screen.height, duration)
                        .setEase(easeType)
                        .setOnComplete(() => tcs.SetResult(true));
                    break;

                case UIPanelAnimationType.Rotate:
                    LeanTween.rotateZ(panelTransform.gameObject, 90f, duration)
                        .setEase(easeType)
                        .setOnComplete(() => tcs.SetResult(true));
                    break;

                case UIPanelAnimationType.FadeAndScale:
                    if (canvasGroup != null)
                    {
                        LeanTween.alphaCanvas(canvasGroup, 0f, duration)
                            .setEase(easeType);
                    }

                    LeanTween.scale(panelTransform.gameObject, Vector3.zero, duration)
                        .setEase(easeType)
                        .setOnComplete(() => tcs.SetResult(true));
                    break;
            }

            await tcs.Task;
        }
    }
}