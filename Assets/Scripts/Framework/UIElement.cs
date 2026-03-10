using Game;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public abstract class UIElement : MonoBehaviour
{
    public GameEvent initEvent;
    public GameEvent fireEvent;

    private void OnEnable()
    {
        EventManager.StartListening(initEvent, OnInitEvent);
        EventManager.StartListening(fireEvent, OnFireEvent);
    }
    private void OnDisable()
    {
        EventManager.StopListening(initEvent, OnInitEvent);
        EventManager.StopListening(fireEvent, OnFireEvent);
    }

    public void OnInitEvent(EventParam e)
    {
        InitUI();
    }
    public void OnFireEvent(EventParam e)
    {
        DrawUI();
    }

    public abstract void InitUI();
    public abstract void DrawUI();
}
