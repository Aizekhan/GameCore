// Assets/Scripts/UI/Attributes/UIPanelAutoRegisterAttribute.cs
using System;

namespace GameCore.Core
{
    /// <summary>
    /// ������� ��� ����������� ��������� UI �������.
    /// ��������� ��� ������� �� ����� UI ������� ��� ����������� ���������
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class UIPanelAutoRegisterAttribute : Attribute
    {
        public string CustomName { get; private set; }
        public string Category { get; private set; }

        /// <summary>
        /// ������������� ������� �� ������'�������� ��'�� �� �������� ��� ���������
        /// </summary>
        /// <param name="customName">������'������ ��'� ����� (���� �� �������, ��������������� ��'� ����-��'����)</param>
        /// <param name="category">������'������ �������� ��� ���������� �������</param>
        public UIPanelAutoRegisterAttribute(string customName = null, string category = "Default")
        {
            CustomName = customName;
            Category = category;
        }
    }
}