// Assets/Scripts/UI/Components/UIAnimatedElement.cs
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GameCore.Core
{
    /// <summary>
    /// Анімації для окремих UI елементів (кнопки, зображення, тексти)
    /// </summary>
    public class UIAnimatedElement : MonoBehaviour
    {
        [Header("General Settings")]
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private float startDelay = 0f;
        [SerializeField] private float loopDelay = 0f;
        [SerializeField] private bool loop = false;
        [SerializeField] private bool destroyOnComplete = false;

        [Header("Animation Type")]
        [SerializeField] private bool useScale = false;
        [SerializeField] private bool useRotation = false;
        [SerializeField] private bool usePosition = false;
        [SerializeField] private bool useAlpha = false;
        [SerializeField] private bool useColor = false;

        [Header("Scale Animation")]
        [SerializeField] private Vector3 fromScale = Vector3.zero;
        [SerializeField] private Vector3 toScale = Vector3.one;
        [SerializeField] private float scaleDuration = 0.5f;
        [SerializeField] private LeanTweenType scaleEaseType = LeanTweenType.easeOutBack;

        [Header("Rotation Animation")]
        [SerializeField] private Vector3 fromRotation = Vector3.zero;
        [SerializeField] private Vector3 toRotation = new Vector3(0, 0, 360f);
        [SerializeField] private float rotationDuration = 0.5f;
        [SerializeField] private LeanTweenType rotationEaseType = LeanTweenType.easeOutCubic;

        [Header("Position Animation")]
        [SerializeField] private Vector3 fromPosition = Vector3.zero;
        [SerializeField] private Vector3 toPosition = Vector3.zero;
        [SerializeField] private bool useRelativePosition = true;
        [SerializeField] private float positionDuration = 0.5f;
        [SerializeField] private LeanTweenType positionEaseType = LeanTweenType.easeOutCubic;

        [Header("Alpha Animation")]
        [SerializeField] private float fromAlpha = 0f;
        [SerializeField] private float toAlpha = 1f;
        [SerializeField] private float alphaDuration = 0.5f;
        [SerializeField] private LeanTweenType alphaEaseType = LeanTweenType.easeOutCubic;

        [Header("Color Animation")]
        [SerializeField] private Color fromColor = Color.white;
        [SerializeField] private Color toColor = Color.white;
        [SerializeField] private float colorDuration = 0.5f;
        [SerializeField] private LeanTweenType colorEaseType = LeanTweenType.easeOutCubic;

        [Header("Events")]
        [SerializeField] private UnityEvent onAnimationStart;
        [SerializeField] private UnityEvent onAnimationComplete;

        // Компоненти
        private CanvasGroup canvasGroup;
        private Graphic graphic;
        private RectTransform rectTransform;
        private Vector3 originalScale;
        private Vector3 originalRotation;
        private Vector3 originalPosition;

        // ID твінів для можливості їх зупинки
        private int scaleTweenId = -1;
        private int rotationTweenId = -1;
        private int positionTweenId = -1;
        private int alphaTweenId = -1;
        private int colorTweenId = -1;

        private void Awake()
        {
            // Отримуємо необхідні компоненти
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            graphic = GetComponent<Graphic>();

            // Зберігаємо оригінальні значення
            originalScale = rectTransform.localScale;
            originalRotation = rectTransform.rotation.eulerAngles;
            originalPosition = rectTransform.anchoredPosition3D;
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                PlayAnimation();
            }
        }

        private void OnDisable()
        {
            StopAllAnimations();
        }

        /// <summary>
        /// Запускає анімацію елемента
        /// </summary>
        public void PlayAnimation()
        {
            StopAllAnimations();
            StartCoroutine(PlayAnimationRoutine());
        }

        private IEnumerator PlayAnimationRoutine()
        {
            if (startDelay > 0)
                yield return new WaitForSeconds(startDelay);

            onAnimationStart.Invoke();

            // Встановлюємо початкові значення
            if (useScale)
                rectTransform.localScale = fromScale;

            if (useRotation)
                rectTransform.rotation = Quaternion.Euler(fromRotation);

            if (usePosition)
            {
                if (useRelativePosition)
                    rectTransform.anchoredPosition3D = originalPosition + fromPosition;
                else
                    rectTransform.anchoredPosition3D = fromPosition;
            }

            if (useAlpha && canvasGroup != null)
                canvasGroup.alpha = fromAlpha;

            if (useColor && graphic != null)
                graphic.color = fromColor;

            // Запускаємо анімації
            if (useScale)
            {
                scaleTweenId = LeanTween.scale(gameObject, toScale, scaleDuration)
                    .setEase(scaleEaseType)
                    .setIgnoreTimeScale(true)
                    .id;
            }

            if (useRotation)
            {
                rotationTweenId = LeanTween.rotateLocal(gameObject, toRotation, rotationDuration)
                    .setEase(rotationEaseType)
                    .setIgnoreTimeScale(true)
                    .id;
            }

            if (usePosition)
            {
                Vector3 targetPosition = useRelativePosition ? originalPosition + toPosition : toPosition;
                positionTweenId = LeanTween.move(rectTransform, targetPosition, positionDuration)
                    .setEase(positionEaseType)
                    .setIgnoreTimeScale(true)
                    .id;
            }

            if (useAlpha && canvasGroup != null)
            {
                alphaTweenId = LeanTween.alphaCanvas(canvasGroup, toAlpha, alphaDuration)
                    .setEase(alphaEaseType)
                    .setIgnoreTimeScale(true)
                    .id;
            }

            if (useColor && graphic != null)
            {
                colorTweenId = LeanTween.color(rectTransform, toColor, colorDuration)
                    .setEase(colorEaseType)
                    .setIgnoreTimeScale(true)
                    .id;
            }

            // Чекаємо завершення найтривалішої анімації
            float maxDuration = Mathf.Max(
                useScale ? scaleDuration : 0,
                useRotation ? rotationDuration : 0,
                usePosition ? positionDuration : 0,
                useAlpha ? alphaDuration : 0,
                useColor ? colorDuration : 0
            );

            yield return new WaitForSeconds(maxDuration);

            onAnimationComplete.Invoke();

            if (destroyOnComplete)
            {
                Destroy(gameObject);
                yield break;
            }

            if (loop)
            {
                if (loopDelay > 0)
                    yield return new WaitForSeconds(loopDelay);

                PlayAnimation();
            }
        }

        /// <summary>
        /// Зупиняє всі активні анімації
        /// </summary>
        public void StopAllAnimations()
        {
            if (scaleTweenId != -1)
            {
                LeanTween.cancel(scaleTweenId);
                scaleTweenId = -1;
            }

            if (rotationTweenId != -1)
            {
                LeanTween.cancel(rotationTweenId);
                rotationTweenId = -1;
            }

            if (positionTweenId != -1)
            {
                LeanTween.cancel(positionTweenId);
                positionTweenId = -1;
            }

            if (alphaTweenId != -1)
            {
                LeanTween.cancel(alphaTweenId);
                alphaTweenId = -1;
            }

            if (colorTweenId != -1)
            {
                LeanTween.cancel(colorTweenId);
                colorTweenId = -1;
            }

            StopAllCoroutines();
        }

        /// <summary>
        /// Скидає елемент до початкового стану
        /// </summary>
        public void ResetToOriginal()
        {
            StopAllAnimations();
            rectTransform.localScale = originalScale;
            rectTransform.rotation = Quaternion.Euler(originalRotation);
            rectTransform.anchoredPosition3D = originalPosition;

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            if (graphic != null)
                graphic.color = Color.white;
        }

        /// <summary>
        /// Швидко встановлює завершений стан анімації
        /// </summary>
        public void SetToCompleted()
        {
            StopAllAnimations();

            if (useScale)
                rectTransform.localScale = toScale;

            if (useRotation)
                rectTransform.rotation = Quaternion.Euler(toRotation);

            if (usePosition)
            {
                if (useRelativePosition)
                    rectTransform.anchoredPosition3D = originalPosition + toPosition;
                else
                    rectTransform.anchoredPosition3D = toPosition;
            }

            if (useAlpha && canvasGroup != null)
                canvasGroup.alpha = toAlpha;

            if (useColor && graphic != null)
                graphic.color = toColor;
        }
    }
}