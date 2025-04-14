// Assets/Scripts/Core/Interfaces/IService.cs
using System.Threading.Tasks;

namespace GameCore.Core
{
    /// <summary>
    /// Базовий інтерфейс для всіх сервісів в системі.
    /// Використовується для реєстрації в ServiceLocator.
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// Ініціалізує сервіс. Викликається автоматично при реєстрації в ServiceLocator.
        /// </summary>
        Task Initialize();
    }
}