// Assets/Scripts/Core/Interfaces/IUIPanel.cs
using System.Threading.Tasks;

namespace GameCore.Core
{
    /// <summary>
    /// Базовий інтерфейс для всіх UI панелей
    /// </summary>
    public interface IUIPanel
    {
        /// <summary>
        /// Показує панель (з анімацією, якщо доступно)
        /// </summary>
        Task Show();

        /// <summary>
        /// Приховує панель (з анімацією, якщо доступно)
        /// </summary>
        Task Hide();

        /// <summary>
        /// Чи видима панель зараз
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Встановлює тип анімації для показу/приховання
        /// </summary>
        void SetAnimationType(UIPanelAnimationType showType, UIPanelAnimationType hideType);

        /// <summary>
        /// Встановлює тривалості анімацій
        /// </summary>
        void SetAnimationDurations(float showDuration, float hideDuration);
    }
}