// Assets/Scripts/Core/Interfaces/IUIPanel.cs
using System.Threading.Tasks;

namespace GameCore.Core
{
    /// <summary>
    /// ��������� ��� ��� UI �������. ������� ����� ������ ��������� �������.
    /// </summary>
    public interface IUIPanel
    {
        /// <summary>
        /// �������� ��'� �����
        /// </summary>
        string PanelName { get; }

        /// <summary>
        /// �������, �� ������ ������� (������)
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// ������ ������ �������
        /// </summary>
        void Show();

        /// <summary>
        /// ������� ������ �������
        /// </summary>
        void Hide();

        /// <summary>
        /// ������ ������ � ��������
        /// </summary>
        Task ShowAnimated(float duration = 0.25f);

        /// <summary>
        /// ������� ������ � ��������
        /// </summary>
        Task HideAnimated(float duration = 0.25f);
    }
}