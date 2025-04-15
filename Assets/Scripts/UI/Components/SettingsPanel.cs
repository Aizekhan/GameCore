using UnityEngine;
using GameCore.Core;

namespace GameCore.Core
{
    public class SettingsPanel : UIPanel
    {
        // �����, ���� ����������� UI-��������� ��� ���� ������
        public void SetMusicVolume(float value)
        {
            AudioManager.Instance?.SetVolume(AudioType.Music, value);
        }

        // �����, ���� ����������� UI-��������� ��� ���� ������
        public void SetSfxVolume(float value)
        {
            AudioManager.Instance?.SetVolume(AudioType.SFX, value);
        }
    }
}
