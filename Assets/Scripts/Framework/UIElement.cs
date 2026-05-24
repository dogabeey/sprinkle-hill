using DG.Tweening;
using Game;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine; using Game.EventManagement;

public abstract class UIElement : MonoBehaviour
{
    public List<GameEvent> initEvents;
    public List<GameEvent> fireEvents;
    public float triggerLatency = 0.2f;

    protected virtual void OnEnable()
    {
        initEvents.ForEach(initEvent => EventManager.StartListening(initEvent, OnInitEvent));
        fireEvents.ForEach(fireEvent => EventManager.StartListening(fireEvent, OnFireEvent));
    }
    protected virtual void OnDisable()
    {
        initEvents.ForEach(initEvent => EventManager.StopListening(initEvent, OnInitEvent));
        fireEvents.ForEach(fireEvent => EventManager.StopListening(fireEvent, OnFireEvent));
    }

    public void OnInitEvent(EventParam e)
    {
        DOVirtual.DelayedCall(triggerLatency, () =>
        {
            InitUI();
        });
    }
    public void OnFireEvent(EventParam e)
    {
        DOVirtual.DelayedCall(triggerLatency, () =>
        {
            DrawUI();
        });
    }

    public abstract void InitUI();
    public abstract void DrawUI();
}
