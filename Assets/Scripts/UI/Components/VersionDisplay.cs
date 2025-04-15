using GameCore.Core.Interfaces;
using TMPro;
using UnityEngine;

namespace GameCore.Core
{
    public class VersionDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI versionText;

        private void Start()
        {
            var version = Application.version;
            var detector = ServiceLocator.Instance.GetService<IPlatformService>();

            if (detector != null)
                version += $" ({detector.CurrentPlatform})";

            if (versionText != null)
                versionText.text = $"Версія: {version}";
        }
    }
}
