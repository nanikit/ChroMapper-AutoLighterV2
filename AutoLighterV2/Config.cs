// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Jonas00000

using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace AutoLighterV2
{
    public class AutoLightV2Config
    {
        public bool UseWalls { get; set; } = false;
        public float MinWallLength { get; set; } = 0.75f;
        public float AntiFlickerThreshold { get; set; } = 0.25f;
        public float LaserSpeedMulti { get; set; } = 0.8f;
        public bool WallStrobes { get; set; } = true;
        public bool StrobesCenterOnly { get; set; } = true;
        public bool WallSprinkles { get; set; } = true;
        public int ColorMode { get; set; } = 2; // 0=random per event, 1=alternating, 2=override by bars
        public int ColorSwitchBeats { get; set; } = 16;
        public bool LaserColorFade { get; set; } = true;
        public float LaserFadeOutLength { get; set; } = 0.5f;
        public bool ResetLongLaserSpeeds { get; set; } = false;
        public bool UseMapIntensityForBrightness { get; set; } = true;
        public float MinBrightness { get; set; } = 0.9f;
        public float MaxBrightness { get; set; } = 1.2f;
        public int RotationInterval { get; set; } = 8;
        public int ZoomInterval { get; set; } = 16;
        public bool DoubleAtIntenseSections { get; set; } = true;
        public int BoostMode { get; set; } = 1; // 0=off, 1=from intensity, 2=periodic, 3=keep existing
        public float BoostPercent { get; set; } = 0.3f;
        public int MinBoostLength { get; set; } = 8;
    }

    public static class ConfigManager
    {
        private static readonly string FileName = "autolighter_v2_config.json";
        public static AutoLightV2Config Data { get; private set; } = new AutoLightV2Config();

        private static string GetConfigPath()
        {
            try
            {
                var asmPath = Assembly.GetExecutingAssembly().Location;
                var dir = Path.GetDirectoryName(asmPath);
                return Path.Combine(dir ?? ".", FileName);
            }
            catch
            {
                return FileName;
            }
        }

        public static void Load()
        {
            try
            {
                var path = GetConfigPath();
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var cfg = JsonConvert.DeserializeObject<AutoLightV2Config>(json);
                    if (cfg != null)
                        Data = cfg;
                }
            }
            catch
            {
                // Use default config on error
            }
        }

        public static void Save()
        {
            try
            {
                var json = JsonConvert.SerializeObject(Data, Formatting.Indented);
                File.WriteAllText(GetConfigPath(), json);
            }
            catch
            {
                // Config save error is non-critical
            }
        }
    }
}