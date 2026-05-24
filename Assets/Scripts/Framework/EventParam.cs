using System;
using System.Collections.Generic;
using UnityEngine; using Game.EventManagement;

namespace Game.EventManagement
{
    [System.Serializable]
    public class EventParam
    {
        public static class Keys
        {
            public const string GameObject = "gameObject";
            public const string Value = "value";
            public const string Scriptable = "scriptable";
            public const string Int = "int";
            public const string Float = "float";
            public const string String = "string";
            public const string Type = "type";
            public const string VectorList = "vectorList";
            public const string Bool = "bool";
        }

        public GameObject paramObj;
        public object paramValue;
        public ScriptableObject paramScriptable;
        public int paramInt;
        public float paramFloat;
        public string paramStr;
        public Type paramType;
        public Vector3[] vectorList;
        public bool paramBool;
        public Dictionary<string, object> paramDictionary;

        private readonly Dictionary<string, object> payload = new Dictionary<string, object>(StringComparer.Ordinal);

        public Dictionary<string, object> Payload => payload;

        public EventParam()
        {
            paramDictionary = new Dictionary<string, object>();
        }

        public EventParam(Dictionary<string, object> paramDictionary)
        {
            this.paramDictionary = paramDictionary != null
                ? new Dictionary<string, object>(paramDictionary)
                : new Dictionary<string, object>();

            MergeDictionaryIntoPayload();
        }

        public EventParam(GameObject paramObj = null, ScriptableObject paramScriptable = null, int paramInt = 0, float paramFloat = 0f, string paramStr = "", Type paramType = null, Dictionary<string, object> paramDictionary = null,
        Vector3[] vectorList = null, bool paramBool = false, object paramValue = null)
        {
            this.paramObj = paramObj;
            this.paramScriptable = paramScriptable;
            this.paramInt = paramInt;
            this.paramFloat = paramFloat;
            this.paramStr = paramStr;
            this.paramType = paramType;
            this.paramDictionary = paramDictionary != null
                ? new Dictionary<string, object>(paramDictionary)
                : new Dictionary<string, object>();
            this.vectorList = vectorList;
            this.paramBool = paramBool;
            this.paramValue = paramValue;

            MergeDictionaryIntoPayload();
            MergeLegacyFieldsIntoPayload();
        }

        public EventParam With<T>(string key, T value)
        {
            Set(key, value);
            return this;
        }

        public void Set<T>(string key, T value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

            payload[key] = value;

            if (paramDictionary == null)
                paramDictionary = new Dictionary<string, object>();

            paramDictionary[key] = value;
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                value = default;
                return false;
            }

            if (!payload.TryGetValue(key, out object raw) && (paramDictionary == null || !paramDictionary.TryGetValue(key, out raw)))
            {
                value = default;
                return false;
            }

            if (raw is T typed)
            {
                value = typed;
                return true;
            }

            try
            {
                if (raw != null)
                {
                    value = (T)Convert.ChangeType(raw, typeof(T));
                    return true;
                }
            }
            catch
            {
            }

            value = default;
            return false;
        }

        public T Get<T>(string key, T fallback = default)
        {
            return TryGet<T>(key, out T value) ? value : fallback;
        }

        internal void PrepareForDispatch()
        {
            MergeDictionaryIntoPayload();
            MergeLegacyFieldsIntoPayload();
        }

        private void MergeDictionaryIntoPayload()
        {
            if (paramDictionary == null)
                return;

            foreach (KeyValuePair<string, object> kvp in paramDictionary)
                payload[kvp.Key] = kvp.Value;
        }

        private void MergeLegacyFieldsIntoPayload()
        {
            if (paramObj != null) payload[Keys.GameObject] = paramObj;
            if (paramValue != null) payload[Keys.Value] = paramValue;
            if (paramScriptable != null) payload[Keys.Scriptable] = paramScriptable;
            payload[Keys.Int] = paramInt;
            payload[Keys.Float] = paramFloat;
            payload[Keys.String] = paramStr;
            if (paramType != null) payload[Keys.Type] = paramType;
            if (vectorList != null) payload[Keys.VectorList] = vectorList;
            payload[Keys.Bool] = paramBool;
        }
    }
}