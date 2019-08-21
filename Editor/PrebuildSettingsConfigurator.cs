#if UNITY_EDITOR
using NDesk.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.Text.RegularExpressions;
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
        private readonly OculusSettingsConfigurator oculusSettingsConfigurator = new OculusSettingsConfigurator();
        private readonly Regex customArgRegex = new Regex("-([^=]*)=", RegexOptions.Compiled);
        private bool useCliConfigManager = true;
        private ConfigManagerSettings configManagerSettings;
        private string packageId;
        private static readonly string XrSdkDefine = "XR_SDK";
        private static readonly string OculusSdkDefine = "OCULUS_SDK";

        private static readonly List<string> XrSdkDefines = new List<string>
        {
            "XR_SDK",
            "OCULUS_SDK"
        };

        public void OnPreprocessBuild(BuildReport report)
        {
            SetScriptingDefinesAndUpdatePackages();
            ParseCommandLineArgs();
            SetConfigManagerSettings();

            if (configManagerSettings.EnableConfigManager)
            {
                PlatformSettings.SerializeToAsset();
                ConfigureSettings();
            }
        }

        private static void SetScriptingDefinesAndUpdatePackages()
        {
            var args = Environment.GetCommandLineArgs();
            var defines = PlayerSettings
                .GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup)
                .Split(';')
                .ToList();
            var cleanedDefines = defines.ToList().Except(XrSdkDefines).ToList();
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                string.Empty);
            WaitForDomainReload();
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                string.Join(";", cleanedDefines.ToArray()));
            WaitForDomainReload();

            var xrSdkPackageHandler = new XrSdkPackageHandler();
            xrSdkPackageHandler.RemoveXrPackages();
            WaitForDomainReload();

            if (IsOculusXrSdk())
            {
                // Need to turn off legacy xr support if using xr sdk. 
                PlayerSettings.virtualRealitySupported = false;

                // TODO refactor so we can pass this in?
                var oculusPackage = "file:../com.unity.xr.oculus";
                xrSdkPackageHandler.AddPackage("com.unity.xr.management");
                xrSdkPackageHandler.AddPackage(oculusPackage);

                defines.Add(XrSdkDefine);
                defines.Add(OculusSdkDefine);

                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                    string.Join(";", defines.ToArray()));
            }
            else
            {
                // Remove XR SDK defines if we know we're using legacy
                PlayerSettings.virtualRealitySupported = true;
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


        private void ConfigureXr()
        {

            if (IsOculusXrSdk())
            {
                oculusSettingsConfigurator.ConfigureXr();
            }
            else
            {
                PlayerSettings.virtualRealitySupported = true;
                UnityEditorInternal.VR.VREditor.SetVREnabledDevicesOnTargetGroup(PlatformSettings.BuildTargetGroup, new string[] { PlatformSettings.XrTarget });
                PlayerSettings.stereoRenderingPath = PlatformSettings.StereoRenderingPath;
            }
        }

        private bool IsXrSdk()
        {
            var args = Environment.GetCommandLineArgs();
            return args.Any(a => a.ToLower().Contains("xrsdk"));
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
                .Add("enabledxrtarget|enabledxrtargets=",
                    "XR target to enable in player settings. Values: " +
                    "\r\n\"Oculus\"\r\n\"OpenVR\"\r\n\"cardboard\"\r\n\"daydream\"\r\n\"MockHMD\"\r\n\"OculusXRSDK\"\r\n\"MagicLeapXRSDK\"\r\n\"WindowsMRXRSDK\"",
                    Action)
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

        private void Action(string xrTarget)
        {
            PlatformSettings.XrTarget = xrTarget;
        }

        private void TrySetStereoRenderingPath(string stereoRenderingPath)
        {
            if (IsOculusXrSdk())
            {
                oculusSettingsConfigurator.TrySetOculusXrSdkStereoRenderingPath(stereoRenderingPath);
            }

            if (!IsXrSdk())
            {
                PlatformSettings.StereoRenderingPath = TryParse<StereoRenderingPath>(stereoRenderingPath);
            }
        }

        public static T TryParse<T>(string stringToParse)
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

        private static bool IsOculusXrSdk()
        {
            var args = Environment.GetCommandLineArgs();
            return args.Any(a => a.ToLower().Contains("oculusxrsdk"));
        }

        private static void WaitForDomainReload()
        {
            while (EditorApplication.isCompiling)
            {
            }
        }
    }
}
#endif