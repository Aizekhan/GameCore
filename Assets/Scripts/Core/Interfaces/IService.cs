// Assets/Scripts/Core/Interfaces/IService.cs
using System.Threading.Tasks;

namespace GameCore.Core
{
    /// <summary>
    /// ������� ��������� ��� ��� ������ � ������.
    /// ��������������� ��� ��������� � ServiceLocator.
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// �������� �����. ����������� ����������� ��� ��������� � ServiceLocator.
        /// </summary>
        Task Initialize();
    }
}