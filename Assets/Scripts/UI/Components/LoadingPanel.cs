using UnityEngine;

using GameCore.Core;
using UnityEngine.InputSystem;
public class LoadingPanel : UIPanel
{
    [Header("LoadingPanel Settings")]
    [SerializeField] private bool closeOnEscape = false;

    private void Update()
    {
        if (closeOnEscape && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Hide(); // ����� � UIPanel
        }
    }

    public override void Show()
    {
        base.Show();
        // ����� ������ ������� ��������, ����, �����������
        Debug.Log("LoadingPanel shown");
    }

    public override void Hide()
    {
        base.Hide();
        // ����� ������ ������� ��������, ����
        Debug.Log("LoadingPanel hidden");
    }
}
