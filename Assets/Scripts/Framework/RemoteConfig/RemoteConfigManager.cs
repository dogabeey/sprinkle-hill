using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;
using UnityEngine;
using Game.Singleton;

namespace Game
{

    public class RemoteConfigManager : SingletonComponent<RemoteConfigManager>
    {
        public bool IsInitialized { get; private set; }

        public event Action OnRemoteConfigLoaded;

        private async void Start()
        {
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

            IsInitialized = true;

            OnRemoteConfigLoaded?.Invoke();
        }


        private void OnDestroy()
        {
            RemoteConfigService.Instance.FetchCompleted -= OnFetchCompleted;
        }

        public float GetFloat(string v, ref float currentValue)
        {
            currentValue = RemoteConfigService.Instance.appConfig.GetFloat(v, currentValue);
            return currentValue;
        }
        public float GetFloat(string v, float defaultValue)
        {
            return RemoteConfigService.Instance.appConfig.GetFloat(v, defaultValue);
        }
        public int GetInt(string v, ref int currentValue)
        {
            currentValue = RemoteConfigService.Instance.appConfig.GetInt(v, currentValue);
            return currentValue;
        }
        public int GetInt(string v, int defaultValue)
        {
            return RemoteConfigService.Instance.appConfig.GetInt(v, defaultValue);
        }
        public string GetString(string v, ref string currentValue)
        {
            currentValue = RemoteConfigService.Instance.appConfig.GetString(v, currentValue);
            return currentValue;
        }
        public string GetString(string v, string defaultValue)
        {
            return RemoteConfigService.Instance.appConfig.GetString(v, defaultValue);
        }
        public bool GetBool(string v, ref bool currentValue)
        {
            currentValue = RemoteConfigService.Instance.appConfig.GetBool(v, currentValue);
            return currentValue;
        }
        public bool GetBool(string v, bool defaultValue)
        {
            return RemoteConfigService.Instance.appConfig.GetBool(v, defaultValue);
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