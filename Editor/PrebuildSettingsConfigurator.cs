#if UNITY_EDITOR
using NDesk.Options;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.Text.RegularExpressions;
using Unity.XR.Oculus;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.unity.cliconfigmanager
{
    public class PrebuildSettingsConfigurator : IPreprocessBuildWithReport
    {
        public int callbackOrder
        {
            get { return -1; }
        }

        public readonly string ConfigManagerSettingsPath = "Assets/XR/Settings/ConfigManagerSettings.asset";

        private readonly Regex customArgRegex = new Regex("-([^=]*)=", RegexOptions.Compiled);
        private bool useCliConfigManager = true;
        private ConfigManagerSettings configManagerSettings;

        public void OnPreprocessBuild(BuildReport report)
        {
            ParseCommandLineArgs();
            SetConfigManagerSettings();
            EnsureScriptingDefineSymbolsConfiguration();

            if (configManagerSettings.EnableConfigManager)
            {
                PlatformSettings.SerializeToAsset();
                ConfigureSettings();
            }
        }

        private static void EnsureScriptingDefineSymbolsConfiguration()
        {
            if (IsXrSdk())
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(
                    EditorUserBuildSettings.selectedBuildTargetGroup,
                    string.Format(
                        "XR_SDK{0}",
                        PlatformSettings.XrTarget.ToLower().Equals("oculusxrsdk") ? ";OCULUS_SDK" : ""));
            }
            else
            {
                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup)
                    .Split(';');
                // Remove XR SDK defines if we know we're using legacy
                var cleanedDefines = defines.Where(d => !d.Equals("XR_SDK") && !d.Equals("OCULUS_SDK"));
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                    string.Join(";", cleanedDefines.ToArray()));
            }
        }

        private void ParseCommandLineArgs()
        {
            var args = Environment.GetCommandLineArgs();
            EnsureOptionsLowerCased(args);
            var optionSet = DefineOptionSet();
            var unParsedArgs = optionSet.Parse(args);
        }

        private void SetConfigManagerSettings()
        {
            if (!System.IO.File.Exists(ConfigManagerSettingsPath))
            {
                if (!System.IO.Directory.Exists(ConfigManagerSettingsPath))
                {
                    System.IO.Directory.CreateDirectory(ConfigManagerSettingsPath);
                    configManagerSettings = ScriptableObject.CreateInstance<ConfigManagerSettings>();

                    // Default to EnableConfigManager to true if this is a new ConfigManagerSettings
                    configManagerSettings.EnableConfigManager = true;
                    AssetDatabase.CreateAsset(configManagerSettings,ConfigManagerSettingsPath);
                }
            }
            else
            {
                configManagerSettings = AssetDatabase.LoadAssetAtPath<ConfigManagerSettings>(ConfigManagerSettingsPath);
            }

            if (configManagerSettings == null)
            {
                throw new ArgumentNullException($"{typeof(ConfigManagerSettings).Name} is null, but shouldn't be.");
            }

            // This handles the case where the command line option passed in is different than the current value of settings.EnableConfigManager. In this
            // case we assume you're running from command line and want to override with this passed in value.
            configManagerSettings.EnableConfigManager = useCliConfigManager || configManagerSettings.EnableConfigManager;

            return configManagerSettings;
        }

        private void EnsureOptionsLowerCased(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (customArgRegex.IsMatch(args[i]))
                {
                    args[i] = customArgRegex.Replace(args[i], customArgRegex.Matches(args[i])[0].ToString().ToLower());
                }
            }
        }

        private void ConfigureSettings()
        {
            if (!string.IsNullOrEmpty(PlatformSettings.XrTarget))
            {
                ConfigureXr();
            }

            if (PlatformSettings.BuildTarget == BuildTarget.Android)
            {
                ConfigureAndroidSettings();
            }

            if (PlatformSettings.PlayerGraphicsApi != GraphicsDeviceType.Null)
            {
                PlayerSettings.SetGraphicsAPIs(PlatformSettings.BuildTarget, new[] { PlatformSettings.PlayerGraphicsApi });
            }
        }

        private static void ConfigureXr()
        {
            if (IsXrSdk())
            {
                SetupXrSdk();
            }
            else
            {
                SetupLegacyXr();
            }
        }

        private static void SetupXrSdk()
        {
            // Need to turn off legacy xr support if using xr sdk. 
            PlayerSettings.virtualRealitySupported = false;
            XrSdkConfigurator.SetupXrSdk();
        }

        private static void SetupLegacyXr()
        {
            PlayerSettings.virtualRealitySupported = true;
            UnityEditorInternal.VR.VREditor.SetVREnabledDevicesOnTargetGroup(PlatformSettings.BuildTargetGroup, new string[] {PlatformSettings.XrTarget});
            PlayerSettings.stereoRenderingPath = PlatformSettings.StereoRenderingPath;
        }

        private static bool IsXrSdk()
        {
            return PlatformSettings.XrTarget.Contains("XRSDK");
        }

        private void ConfigureAndroidSettings()
        {
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            PlayerSettings.Android.minSdkVersion = PlatformSettings.MinimumAndroidSdkVersion;
        }

        private OptionSet DefineOptionSet()
        {
            return new OptionSet()
                .Add("simulationmode=",
                    "Enable Simulation modes for Windows MR in Editor. Values: \r\n\"HoloLens\"\r\n\"WindowsMR\"\r\n\"Remoting\"",
                    simMode => PlatformSettings.SimulationMode = simMode)
                .Add("enabledxrtarget=",
                    "XR target to enable in player settings. Values: " +
                    "\r\n\"Oculus\"\r\n\"OpenVR\"\r\n\"cardboard\"\r\n\"daydream\"\r\n\"MockHMD\"\r\n\"OculusXRSDK\"\r\n\"MagicLeapXRSDK\"\r\n\"WindowsMRXRSDK\"",
                    xrTarget => { PlatformSettings.XrTarget = xrTarget; })
                .Add("playergraphicsapi=", "Graphics API based on GraphicsDeviceType.",
                    graphicsDeviceType => PlatformSettings.PlayerGraphicsApi = TryParse<GraphicsDeviceType>(graphicsDeviceType))
                .Add("colorspace=", "Linear or Gamma color space.",
                    colorSpace => PlayerSettings.colorSpace = TryParse<ColorSpace>(colorSpace))
                .Add("stereorenderingpath=", "Stereo rendering path to enable. SinglePass is default",
                    TrySetStereoRenderingPath)
                .Add("mtRendering",
                    "Enable or disable multithreaded rendering. Enabled is default. Use option to enable, or use option and append '-' to disable.",
                    option => PlatformSettings.MtRendering = option != null)
                .Add("graphicsJobs",
                    "Enable graphics jobs rendering. Disabled is default. Use option to enable, or use option and append '-' to disable.",
                    option => PlatformSettings.GraphicsJobs = option != null)
                .Add("minimumandroidsdkversion=", "Minimum Android SDK Version to use.",
                    minAndroidSdkVersion => PlatformSettings.MinimumAndroidSdkVersion = TryParse<AndroidSdkVersions>(minAndroidSdkVersion))
                .Add("targetandroidsdkversion=", "Target Android SDK Version to use.",
                    trgtAndroidSdkVersion => PlatformSettings.TargetAndroidSdkVersion = TryParse<AndroidSdkVersions>(trgtAndroidSdkVersion))
                .Add("useconfigmanager", "Use CLI Config Manager to parse options and set build and player settings.",
                    option => useCliConfigManager = option != null);
        }

        private void TrySetStereoRenderingPath(string stereoRenderingPath)
        {
            if (IsXrSdk())
            {
#if OCULUS_SDK
                // This allows us to be "backward compatible".
                var srp = stereoRenderingPath.Equals("SinglePass") ? "SinglePassInstanced" : stereoRenderingPath;
                if (PlatformSettings.BuildTarget == BuildTarget.Android)
                {
                    PlatformSettings.StereoRenderingModeAndroid = TryParse<OculusSettings.StereoRenderingMode>(srp);
                }
                else
                {
                    PlatformSettings.StereoRenderingModeDesktop = TryParse<OculusSettings.StereoRenderingMode>(srp);
                }
#endif
            }

            if (!IsXrSdk())
            {
                PlatformSettings.StereoRenderingPath = TryParse<StereoRenderingPath>(stereoRenderingPath);
            }
        }

        private static T TryParse<T>(string stringToParse)
        {
            T thisType;
            try
            {
                thisType = (T) Enum.Parse(typeof(T), stringToParse);
            }
            catch (Exception e)
            {
                throw new ArgumentException(($"Couldn't cast {stringToParse} to {typeof(T)}"), e);
            }

            return thisType;
        }
    }
}
#endif