using UnityEngine;

using GameCore.Core;
using UnityEngine.InputSystem;
public class GameplayPanel : UIPanel
{
    [Header("GameplayPanel Settings")]
    [SerializeField] private bool closeOnEscape = false;

    private void Update()
    {
        if (closeOnEscape && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Hide(); // метод з UIPanel
        }
    }

    public override void Show()
    {
        base.Show();
        // Можна додати анімацію відкриття, звук, ініціалізацію
        Debug.Log("GameplayPanel shown");
    }

    public override void Hide()
    {
        base.Hide();
        // Можна додати анімацію закриття, звук
        Debug.Log("GameplayPanel hidden");
    }
}
