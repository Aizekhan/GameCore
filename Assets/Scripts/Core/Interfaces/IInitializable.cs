// Assets/Scripts/Core/Interfaces/IInitializable.cs
using System.Threading.Tasks;

namespace GameCore.Core
{
    /// <summary>
    /// ��������� ��� ����������, �� ���������� �����������.
    /// ��������������� App ��� ������������ ������� ������.
    /// </summary>
    public interface IInitializable
    {
        /// <summary>
        /// ���������� �������� ���������.
        /// </summary>
        /// <returns>Task, ���� ����������� ���� �����������</returns>
        Task Initialize();

        /// <summary>
        /// ��������, �� ��������� �������������.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// �������� �����������. ���������� � ����� ���������� ������������� �������.
        /// </summary>
        int InitializationPriority { get; }
    }
}