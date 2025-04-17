// Новий файл IResetable.cs
namespace GameCore.Core
{
    /// <summary>
    /// Інтерфейс для компонентів, що потребують спеціального скидання стану
    /// </summary>
    public interface IResetable
    {
        /// <summary>
        /// Відновлює стан об'єкта до початкового
        /// </summary>
        void ResetState();
    }
}