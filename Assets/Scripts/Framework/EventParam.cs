using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EventParam
{
    public GameObject paramObj;
    public ScriptableObject paramScriptable;
    public int paramInt;
    public float paramFloat;
    public string paramStr;
    public Type paramType;
    public Vector3[] vectorList;
    public bool paramBool;
    public Dictionary<string, object> paramDictionary;

    public EventParam()
    {

    }

    public EventParam(Dictionary<string, object> paramDictionary)
    {
        this.paramDictionary = paramDictionary;
    }

    public EventParam(GameObject paramObj = null, ScriptableObject paramScriptable = null, int paramInt = 0, float paramFloat = 0f, string paramStr = "", Type paramType = null, Dictionary<string, object> paramDictionary = null,
    Vector3[] vectorList = null, bool paramBool = false)
    {
        this.paramObj = paramObj;
        this.paramScriptable = paramScriptable;
        this.paramInt = paramInt;
        this.paramFloat = paramFloat;
        this.paramStr = paramStr;
        this.paramType = paramType;
        this.paramDictionary = paramDictionary;
        this.vectorList = vectorList;
        this.paramBool = paramBool;
    }
}