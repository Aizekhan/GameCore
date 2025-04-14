// Assets/Scripts/Core/Interfaces/IUIPanel.cs
using System.Threading.Tasks;

namespace GameCore.Core
{
    /// <summary>
    /// Інтерфейс для всіх UI панелей. Визначає базові методи управління панеллю.
    /// </summary>
    public interface IUIPanel
    {
        /// <summary>
        /// Унікальне ім'я панелі
        /// </summary>
        string PanelName { get; }

        /// <summary>
        /// Визначає, чи панель активна (видима)
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Показує панель негайно
        /// </summary>
        void Show();

        /// <summary>
        /// Приховує панель негайно
        /// </summary>
        void Hide();

        /// <summary>
        /// Показує панель з анімацією
        /// </summary>
        Task ShowAnimated(float duration = 0.25f);

        /// <summary>
        /// Приховує панель з анімацією
        /// </summary>
        Task HideAnimated(float duration = 0.25f);
    }
}