// Assets/Scripts/UI/Utils/UIScreenUtils.cs
using UnityEngine;

namespace GameCore.Core
{
    /// <summary>
    /// ������ ��� ������ � ������� UI, ���������� �� �����������
    /// </summary>
    public static class UIScreenUtils
    {
        private static Canvas mainCanvas;
        private static RectTransform mainCanvasRect;

        /// <summary>
        /// ������ ����� ������ � ������� �����������
        /// </summary>
        public static Vector2 GetScreenSize()
        {
            return new Vector2(Screen.width, Screen.height);
        }

        /// <summary>
        /// ������ ����� ������ � �������� �������
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
        /// ���������� ����� ���������� � ���������� �������
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
        /// ��������, �� � ������������ ����� ������ ����������
        /// </summary>
        public static bool IsPortraitOrientation()
        {
            return Screen.height > Screen.width;
        }

        /// <summary>
        /// ������ ������� ������������ ����� ������
        /// </summary>
        public static float GetAspectRatio()
        {
            return (float)Screen.width / Screen.height;
        }

        /// <summary>
        /// ��������, �� ��������� ������� �������� (�� ����� ������ ������ �� ���������)
        /// </summary>
        public static bool IsMobileDevice()
        {
#if UNITY_ANDROID || UNITY_IOS
            return true;
#else
            // �� ��-�������� ���������� ���������� ����� ������
            return Screen.width <= 1280 && Screen.height <= 800;
#endif
        }

        /// <summary>
        /// ������� ��� �������� �� ����� ������������ ����� �� ������
        /// </summary>
        public static DeviceType GetDeviceType()
        {
            float aspectRatio = GetAspectRatio();

            if (aspectRatio >= 1.7f) // ������� �����
            {
                if (Screen.width >= 1920)
                    return DeviceType.Desktop;
                else if (Screen.width >= 1280)
                    return DeviceType.Tablet;
                else
                    return DeviceType.Phone;
            }
            else // ����� ���������� �����
            {
                if (Screen.width >= 1024)
                    return DeviceType.Desktop;
                else
                    return DeviceType.Tablet;
            }
        }

        /// <summary>
        /// ���������� ������� dp (density-independent pixels) � ������� �����
        /// </summary>
        public static float DpToPixels(float dp)
        {
            // ��������� ������������ (�� VERY ���������)
            float dpi = Screen.dpi > 0 ? Screen.dpi : 96f;
            return dp * (dpi / 160f);
        }
    }

    /// <summary>
    /// ���� �������� ��� ��������� UI
    /// </summary>
    public enum DeviceType
    {
        Phone,
        Tablet,
        Desktop
    }
}