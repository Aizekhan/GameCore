// Assets/Scripts/UI/Utils/UIScreenUtils.cs
using UnityEngine;

namespace GameCore.Core
{
    /// <summary>
    /// Утиліти для роботи з екраном UI, адаптацією та пропорціями
    /// </summary>
    public static class UIScreenUtils
    {
        private static Canvas mainCanvas;
        private static RectTransform mainCanvasRect;

        /// <summary>
        /// Отримує розмір екрану у світових координатах
        /// </summary>
        public static Vector2 GetScreenSize()
        {
            return new Vector2(Screen.width, Screen.height);
        }

        /// <summary>
        /// Отримує розмір екрану в одиницях канвасу
        /// </summary>
        public static Vector2 GetCanvasSize()
        {
            if (mainCanvas == null)
            {
                mainCanvas = Object.FindObjectOfType<Canvas>();
                if (mainCanvas != null)
                    mainCanvasRect = mainCanvas.GetComponent<RectTransform>();
            }

            if (mainCanvasRect != null)
                return mainCanvasRect.sizeDelta;

            return Vector2.zero;
        }

        /// <summary>
        /// Перетворює світові координати у координати канвасу
        /// </summary>
        public static Vector2 WorldToCanvasPosition(Vector3 worldPosition, Camera camera)
        {
            if (mainCanvas == null)
            {
                mainCanvas = Object.FindObjectOfType<Canvas>();
                if (mainCanvas == null) return Vector2.zero;
            }

            if (camera == null)
                camera = Camera.main;

            if (camera == null) return Vector2.zero;

            Vector2 viewportPosition = camera.WorldToViewportPoint(worldPosition);
            RectTransform canvasRect = mainCanvas.GetComponent<RectTransform>();

            return new Vector2(
                (viewportPosition.x * canvasRect.sizeDelta.x) - (canvasRect.sizeDelta.x * 0.5f),
                (viewportPosition.y * canvasRect.sizeDelta.y) - (canvasRect.sizeDelta.y * 0.5f)
            );
        }

        /// <summary>
        /// Перевіряє, чи є співвідношення сторін екрану портретним
        /// </summary>
        public static bool IsPortraitOrientation()
        {
            return Screen.height > Screen.width;
        }

        /// <summary>
        /// Отримує поточне співвідношення сторін екрану
        /// </summary>
        public static float GetAspectRatio()
        {
            return (float)Screen.width / Screen.height;
        }

        /// <summary>
        /// Перевіряє, чи вважається пристрій мобільним (на основі розміру екрану та платформи)
        /// </summary>
        public static bool IsMobileDevice()
        {
#if UNITY_ANDROID || UNITY_IOS
            return true;
#else
            // На не-мобільних платформах перевіряємо розмір екрану
            return Screen.width <= 1280 && Screen.height <= 800;
#endif
        }

        /// <summary>
        /// Визначає тип пристрою на основі співвідношення сторін та розміру
        /// </summary>
        public static DeviceType GetDeviceType()
        {
            float aspectRatio = GetAspectRatio();

            if (aspectRatio >= 1.7f) // Широкий екран
            {
                if (Screen.width >= 1920)
                    return DeviceType.Desktop;
                else if (Screen.width >= 1280)
                    return DeviceType.Tablet;
                else
                    return DeviceType.Phone;
            }
            else // Більш квадратний екран
            {
                if (Screen.width >= 1024)
                    return DeviceType.Desktop;
                else
                    return DeviceType.Tablet;
            }
        }

        /// <summary>
        /// Перетворює одиниці dp (density-independent pixels) в фактичні пікселі
        /// </summary>
        public static float DpToPixels(float dp)
        {
            // Приблизне перетворення (це VERY приблизно)
            float dpi = Screen.dpi > 0 ? Screen.dpi : 96f;
            return dp * (dpi / 160f);
        }
    }

    /// <summary>
    /// Типи пристроїв для адаптації UI
    /// </summary>
    public enum DeviceType
    {
        Phone,
        Tablet,
        Desktop
    }
}