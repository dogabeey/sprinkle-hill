using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;
using UnityEngine;

namespace Game
{

    public class RemoteConfigManager : MonoBehaviour
    {
        public static RemoteConfigManager Instance { get; private set; }
        public bool IsInitialized { get; private set; }

        public event Action OnRemoteConfigLoaded;

        private async void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                RemoteConfigService.Instance.FetchCompleted += OnFetchCompleted;

                await RemoteConfigService.Instance.FetchConfigsAsync(
                    new UserAttributes(),
                    new AppAttributes()
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"Remote Config initialization failed:\n{e}");
            }
        }

        private void OnFetchCompleted(ConfigResponse response)
        {
            Debug.Log($"Remote Config fetched from: {response.requestOrigin}");

            ApplyAllRemoteConfigs();

            IsInitialized = true;

            OnRemoteConfigLoaded?.Invoke();
        }

        private void ApplyAllRemoteConfigs()
        {
            Assembly assembly = typeof(RemoteConfigManager).Assembly;
            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch
            {
                return;
            }

            foreach (var type in types)
            {
                var fields = type.GetFields(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static
                );

                foreach (var field in fields)
                {
                    var attribute = field.GetCustomAttribute<RemoteConfigAttribute>();

                    if (attribute == null)
                        continue;

                    if (TryGetRemoteValue(out object value, field.FieldType, attribute))
                    {
                        field.SetValue(null, value);
                    }

                    Debug.Log(
                        $"[RemoteConfig] {type.Name}.{field.Name} = {value}"
                    );
                }
            }
        }

        private bool TryGetRemoteValue(out object value, Type fieldType, RemoteConfigAttribute attribute)
        {
            string key = attribute.Key;
            object defaultValue = attribute.DefaultValue;

            var config = RemoteConfigService.Instance.appConfig;

            if (fieldType == typeof(int))
            {
                value = config.GetInt(key, (int)defaultValue);
                return true;
            }

            if (fieldType == typeof(float))
            {
                value = config.GetFloat(key, Convert.ToSingle(defaultValue));
                return true;
            }

            if (fieldType == typeof(bool))
            {
                value = config.GetBool(key, (bool)defaultValue);
                return true;
            }

            if (fieldType == typeof(string))
            {
                value = config.GetString(key, (string)defaultValue);
                return true;
            }

            if (fieldType == typeof(long))
            {
                value = config.GetLong(key, Convert.ToInt64(defaultValue));
                return true;
            }

            Debug.LogWarning(
                $"Unsupported RemoteConfig field type: {fieldType.Name}"
            );

            value = null;
            return false;
        }

        private void OnDestroy()
        {
            RemoteConfigService.Instance.FetchCompleted -= OnFetchCompleted;
        }

        public struct UserAttributes
        {
        }

        public struct AppAttributes
        {
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class RemoteConfigAttribute : Attribute
    {
        public string Key { get; }
        public object DefaultValue { get; }

        public RemoteConfigAttribute(string key, object defaultValue)
        {
            Key = key;
            DefaultValue = defaultValue;
        }
    }
}