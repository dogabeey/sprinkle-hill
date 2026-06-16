using Game.Singleton;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;
using UnityEditor;
using UnityEngine;

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

        public float GetFloat(string v, ref float currentValue, string path)
        {
            currentValue = RemoteConfigService.Instance.appConfig.GetFloat(v, currentValue);
            DeployValue(v, currentValue, path);
            return currentValue;
        }
        public float GetFloat(string v, float defaultValue, string path)
        {
            float value = RemoteConfigService.Instance.appConfig.GetFloat(v, defaultValue);
            DeployValue(v, value, path);
            return value;
        }
        public int GetInt(string v, ref int currentValue, string path)
        {
            currentValue = GetIntWithFloatFallback(v, currentValue);
            DeployValue(v, currentValue, path);
            return currentValue;
        }
        public int GetInt(string v, int defaultValue, string path)
        {
            int value = GetIntWithFloatFallback(v, defaultValue);
            DeployValue(v, value, path);
            return value;
        }
        public string GetString(string v, ref string currentValue, string path)
        {
            currentValue = RemoteConfigService.Instance.appConfig.GetString(v, currentValue);
            DeployValue(v, currentValue, path);
            return currentValue;
        }
        public string GetString(string v, string defaultValue, string path)
        {
            string value = RemoteConfigService.Instance.appConfig.GetString(v, defaultValue);
            DeployValue(v, value, path);
            return value;
        }
        public bool GetBool(string v, ref bool currentValue, string path)
        {
            currentValue = RemoteConfigService.Instance.appConfig.GetBool(v, currentValue);
            DeployValue(v, currentValue, path);
            return currentValue;
        }
        public bool GetBool(string v, bool defaultValue, string path)
        {
            bool value = RemoteConfigService.Instance.appConfig.GetBool(v, defaultValue);
            DeployValue(v, value, path);
            return value;
        }

        public struct UserAttributes
        {
        }

        public struct AppAttributes
        {
        }
        [Serializable]
        public class RemoteConfigFile
        {
            [JsonProperty("$schema")]
            public string schema = "https://ugs-config-schemas.unity3d.com/v1/remote-config.schema.json";

            public Dictionary<string, object> entries = new();
            public Dictionary<string, string> types = new();
        }

        public string DeployValue(string key, object value, string path)
        {
            #if UNITY_EDITOR
            try
            {
                RemoteConfigFile config;

                if (File.Exists(path))
                {
                    string existingJson = File.ReadAllText(path);

                    config = JsonConvert.DeserializeObject<RemoteConfigFile>(existingJson);

                    if (config == null)
                        config = new RemoteConfigFile();
                }
                else
                {
                    config = new RemoteConfigFile();
                }
                config.entries[key] = value switch
                {
                    float f => new JValue(f),
                    int i => new JValue((float)i),
                    bool b => new JValue(b),
                    long l => new JValue(l),
                    string s => new JValue(s),
                    _ => JToken.FromObject(value)
                };
                config.types[key] = GetRemoteConfigType(value);

                string json = JsonConvert.SerializeObject(
                    config,
                    Formatting.Indented
                );

                File.WriteAllText(path, json);

                AssetDatabase.Refresh();

                Debug.Log($"Remote Config updated:\n{key} = {value}");
                return path;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to deploy Remote Config value:\n{e}");
                return null;
            }
            #endif
            return null;
        }

        public void DeployRemoteConfigFile(string path)
        {
            try
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();

                process.StartInfo.FileName = "cmd.exe";

                process.StartInfo.Arguments =
                    $"/c unity-services deployment deploy \"{path}\"";

                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;

                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                {
                    Debug.Log(output);
                }

                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError(error);
                }

                Debug.Log("Remote Config deployed successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Remote Config deployment failed:\n{e}");
            }
        }

    private string GetRemoteConfigType(object value)
        {
            return value switch
            {
                string => "STRING",
                int => "FLOAT", // It still act as float in Remote Config due an error in deploying int values, so we use FLOAT as type for both int and float
                bool => "BOOL",
                float => "FLOAT",
                long => "LONG",
                _ => "JSON"
            };
        }

        private int GetIntWithFloatFallback(string key, int fallbackValue)
        {
            try
            {
                return RemoteConfigService.Instance.appConfig.GetInt(key, fallbackValue);
            }
            catch
            {
                float floatValue = RemoteConfigService.Instance.appConfig.GetFloat(key, fallbackValue);
                return Mathf.RoundToInt(floatValue);
            }
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