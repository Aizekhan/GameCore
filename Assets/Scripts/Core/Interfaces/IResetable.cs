// ����� ���� IResetable.cs
namespace GameCore.Core
{
    /// <summary>
    /// ��������� ��� ����������, �� ���������� ������������ �������� �����
    /// </summary>
    public interface IResetable
    {
        /// <summary>
        /// ³������� ���� ��'���� �� �����������
        /// </summary>
        void ResetState();
    }
}