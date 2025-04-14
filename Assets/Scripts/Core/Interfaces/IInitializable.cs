// Assets/Scripts/Core/Interfaces/IInitializable.cs
using System.Threading.Tasks;

namespace GameCore.Core
{
    /// <summary>
    /// Інтерфейс для компонентів, які потребують ініціалізації.
    /// Використовується App для асинхронного запуску систем.
    /// </summary>
    public interface IInitializable
    {
        /// <summary>
        /// Асинхронно ініціалізує компонент.
        /// </summary>
        /// <returns>Task, який завершується після ініціалізації</returns>
        Task Initialize();

        /// <summary>
        /// Перевіряє, чи компонент ініціалізований.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Пріоритет ініціалізації. Компоненти з вищим пріоритетом ініціалізуються першими.
        /// </summary>
        int InitializationPriority { get; }
    }
}