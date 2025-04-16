// PlatformDetector.cs — реалізація IPlatformService
using UnityEngine;
using System.Threading.Tasks;
using GameCore.Core;
using GameCore.Core.Interfaces;
using GameCore.Core.EventSystem;
namespace GameCore.Core
{
    public class PlatformDetector : MonoBehaviour, IService, IInitializable, IPlatformService
    {
        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 90;
        public enum PlatformType
        {
            Unknown,
            Windows,
            Mac,
            Linux,
            Android,
            iOS,
            WebGL,
            PS4,
            PS5,
            Xbox,
            Editor
        }
        public async Task Initialize()
        {
            DetectPlatform();
            EventBus.Emit("Platform/Detected", CurrentPlatform);

            IsInitialized = true;
            await Task.CompletedTask;
        }
        public PlatformType CurrentPlatform { get; private set; } = PlatformType.Unknown;
        public string PlatformName => CurrentPlatform.ToString();
        public bool SupportsCloud => !IsConsolePlatform() && CurrentPlatform != PlatformType.WebGL;

        public bool IsMobilePlatform() =>
            CurrentPlatform == PlatformType.Android || CurrentPlatform == PlatformType.iOS;

        public bool IsConsolePlatform() =>
            CurrentPlatform == PlatformType.PS4 || CurrentPlatform == PlatformType.PS5 || CurrentPlatform == PlatformType.Xbox;

        public bool IsDesktopPlatform() =>
            CurrentPlatform == PlatformType.Windows ||
            CurrentPlatform == PlatformType.Mac ||
            CurrentPlatform == PlatformType.Linux;

       
     

        public void ExecutePlatformSpecificAction(string actionName)
        {
            switch (actionName)
            {
                case "OpenStore":
                    if (IsMobilePlatform())
                    {
#if UNITY_ANDROID
                        Application.OpenURL("https://play.google.com/store");
#elif UNITY_IOS
                        Application.OpenURL("https://apps.apple.com/");
#else
                        CoreLogger.Log("PLATFORM", "Store не підтримується на цій платформі.");
#endif
                    }
                    break;

                case "RateUs":
                    CoreLogger.Log("PLATFORM", $"⭐ Оцінити гру на {PlatformName}");
                    break;

                default:
                    CoreLogger.LogWarning("PLATFORM", $"⚠️ Невідома дія: {actionName}");
                    break;
            }
        }

        private void DetectPlatform()
        {
#if UNITY_EDITOR
            CurrentPlatform = PlatformType.Editor;
#elif UNITY_STANDALONE_WIN
            CurrentPlatform = PlatformType.Windows;
#elif UNITY_STANDALONE_OSX
            CurrentPlatform = PlatformType.Mac;
#elif UNITY_STANDALONE_LINUX
            CurrentPlatform = PlatformType.Linux;
#elif UNITY_ANDROID
            CurrentPlatform = PlatformType.Android;
#elif UNITY_IOS
            CurrentPlatform = PlatformType.iOS;
#elif UNITY_WEBGL
            CurrentPlatform = PlatformType.WebGL;
#elif UNITY_PS5
            CurrentPlatform = PlatformType.PS5;
#elif UNITY_PS4
            CurrentPlatform = PlatformType.PS4;
#elif UNITY_XBOXONE
            CurrentPlatform = PlatformType.Xbox;
#else
            CurrentPlatform = PlatformType.Unknown;
#endif
            CoreLogger.Log("PLATFORM", $"📱 Поточна платформа: {CurrentPlatform}");
        }
    }
}
