using Game;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public abstract class UIElement : MonoBehaviour
{
    public GameEvent fireEvent;

    private void OnEnable()
    {
        EventManager.StartListening(fireEvent, OnEvent);
    }
    private void OnDisable()
    {
        EventManager.StopListening(fireEvent, OnEvent);
    }

    public void OnEvent(EventParam e)
    {
        DrawUI();
    }

    public abstract void DrawUI();
}
