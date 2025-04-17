// Assets/Scripts/Core/Interfaces/IUIPanel.cs
using System.Threading.Tasks;

namespace GameCore.Core
{
    /// <summary>
    /// ������� ��������� ��� ��� UI �������
    /// </summary>
    public interface IUIPanel
    {
        /// <summary>
        /// ������ ������ (� ��������, ���� ��������)
        /// </summary>
        Task Show();

        /// <summary>
        /// ������� ������ (� ��������, ���� ��������)
        /// </summary>
        Task Hide();

        /// <summary>
        /// �� ������ ������ �����
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// ���������� ��� ������� ��� ������/����������
        /// </summary>
        void SetAnimationType(UIPanelAnimationType showType, UIPanelAnimationType hideType);

        /// <summary>
        /// ���������� ��������� �������
        /// </summary>
        void SetAnimationDurations(float showDuration, float hideDuration);
    }
}