using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MobileHapticsProFreeEdition
{
    public static class AndroidTapticWave
    {
        public static void Haptic(HapticModes mode)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            {
                long[] pattern = GetHapticPattern(mode);
                if (pattern != null)
                {
                    vibrator.Call("vibrate", pattern, -1);
                }
                else
                {
                    // Default vibration for undefined modes
                    vibrator.Call("vibrate", 50);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("AndroidTaptic error: " + ex.Message);
        }
#else
            Handheld.Vibrate();
#endif
        }

        private static long[] GetHapticPattern(HapticModes mode)
        {
            switch (mode)
            {
                case HapticModes.Select:
                    return new long[] { 0, 20 };

                case HapticModes.Confirm:
                    return new long[] { 0, 30, 50, 50 };

                case HapticModes.Alert:
                    return new long[] { 0, 80, 50, 80 };

                case HapticModes.Failure:
                    return new long[] { 0, 50, 50, 50, 50, 50 };

                default:
                    Handheld.Vibrate();
                    return null;
            }
        }
    }
}