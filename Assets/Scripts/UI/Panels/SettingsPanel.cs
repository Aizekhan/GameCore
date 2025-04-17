using UnityEngine;
using GameCore.Core;

namespace GameCore.Core
{
    public class SettingsPanel : UIPanel
    {
        private AudioManager _audioManager;

        protected override void Awake()
        {
            base.Awake();
            _audioManager = ServiceLocator.Instance.GetService<AudioManager>();
        }

        public void SetMusicVolume(float value)
        {
            _audioManager?.SetVolume(AudioType.Music, value);
        }

        public void SetSfxVolume(float value)
        {
            _audioManager?.SetVolume(AudioType.SFX, value);
        }
    }
}
