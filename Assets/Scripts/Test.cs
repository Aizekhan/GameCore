using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class Test : MonoBehaviour
{
    void Update()
    {
        if (PlayerInput.all.Count > 0)
        {
            var currentScheme = PlayerInput.all[0].currentControlScheme;
            var currentMap = PlayerInput.all[0].currentActionMap.name;
            Logger.Log($"Current scheme: {currentScheme}, Current map: {currentMap}");
        }

        // Нова система замість Input.GetMouseButtonDown(0)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Logger.Log("Mouse click detected at: " + mousePos);
        }
    }
}
