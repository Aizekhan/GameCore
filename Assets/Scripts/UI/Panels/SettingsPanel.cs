using UnityEngine;
using GameCore.Core;

namespace GameCore.Core
{
    public class SettingsPanel : UIPanel
    {
        // Метод, який викликається UI-слайдером для зміни музики
        public void SetMusicVolume(float value)
        {
            AudioManager.Instance?.SetVolume(AudioType.Music, value);
        }

        // Метод, який викликається UI-слайдером для зміни ефектів
        public void SetSfxVolume(float value)
        {
            AudioManager.Instance?.SetVolume(AudioType.SFX, value);
        }
    }
}
