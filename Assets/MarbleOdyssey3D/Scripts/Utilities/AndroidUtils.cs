using UnityEngine;

namespace MarbleOdyssey
{
    public enum DevicePerformanceTier { Low, Medium, High }

    public static class AndroidUtils
    {
        public static DevicePerformanceTier GetDevicePerformanceTier()
        {
            int ram = SystemInfo.systemMemorySize;
            int cores = SystemInfo.processorCount;
            int gpuMem = SystemInfo.graphicsMemorySize;

            if (ram >= 6000 && cores >= 6 && gpuMem >= 3000)
                return DevicePerformanceTier.High;
            if (ram >= 3000 && cores >= 4)
                return DevicePerformanceTier.Medium;
            return DevicePerformanceTier.Low;
        }

        public static void SetQualityForDevice()
        {
            var tier = GetDevicePerformanceTier();
            int qualityLevel = tier switch
            {
                DevicePerformanceTier.High => 2,
                DevicePerformanceTier.Medium => 1,
                _ => 0
            };
            QualitySettings.SetQualityLevel(qualityLevel, true);
        }

        public static bool IsLowMemoryDevice()
        {
            return SystemInfo.systemMemorySize < 3000;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        public static float GetBatteryLevel()
        {
            return SystemInfo.batteryLevel;
        }

        public static void VibrateShort()
        {
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                vibrator?.Call("vibrate", 30L);
            }
            catch (System.Exception) { }
        }

        public static void VibrateMedium()
        {
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                vibrator?.Call("vibrate", 80L);
            }
            catch (System.Exception) { }
        }
#else
        public static float GetBatteryLevel() => 1f;
        public static void VibrateShort() { }
        public static void VibrateMedium() { }
#endif

        public static void SetTargetFrameRate(DevicePerformanceTier tier)
        {
            Application.targetFrameRate = tier switch
            {
                DevicePerformanceTier.High => 60,
                DevicePerformanceTier.Medium => 60,
                _ => 30
            };
        }
    }
}
