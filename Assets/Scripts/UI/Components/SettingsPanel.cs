using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GameCore.Core
{
    public class SettingsPanel : UIPanel
    {
        [Header("UI Elements")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Button backButton;

        protected override void Awake()
        {
            base.Awake();
            SetupUI();
        }

        private void SetupUI()
        {
            if (musicSlider != null)
            {
                musicSlider.value = AudioManager.Instance?.GetVolumeForType(AudioType.Music) ?? 1f;
                musicSlider.onValueChanged.AddListener(SetMusicVolume);
            }

            if (sfxSlider != null)
            {
                sfxSlider.value = AudioManager.Instance?.GetVolumeForType(AudioType.SFX) ?? 1f;
                sfxSlider.onValueChanged.AddListener(SetSfxVolume);
            }

            
        }

        private void SetMusicVolume(float value)
        {
            AudioManager.Instance?.SetVolume(AudioType.Music, value);
        }

        private void SetSfxVolume(float value)
        {
            AudioManager.Instance?.SetVolume(AudioType.SFX, value);
        }

      

        public override void Show()
        {
            base.Show();
            CoreLogger.Log("SettingsPanel", "Settings panel shown");
        }

        public override void Hide()
        {
            base.Hide();
            CoreLogger.Log("SettingsPanel", "Settings panel hidden");
        }
    }
}
