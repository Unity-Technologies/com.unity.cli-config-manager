using NDesk.Options;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.unity.cliconfigmanager
{
    public class CliConfigManager
    {
        public string StereoRenderingMode;
        private readonly Regex customArgRegex = new Regex("-([^=]*)=", RegexOptions.Compiled);
        private readonly PlatformSettings platformSettings = new PlatformSettings();

        public void ConfigureFromCmdlineArgs()
        {
#if UNITY_EDITOR
            ParseCommandLineArgs();
            ConfigureSettings();
#endif
        }
#if UNITY_EDITOR
        private void ParseCommandLineArgs()
        {
            var args = Environment.GetCommandLineArgs();
            EnsureOptionsLowerCased(args);
            var optionSet = DefineOptionSet();
            var unParsedArgs = optionSet.Parse(args);
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
            // Setup all-inclusive player settings
            ConfigureCrossplatformSettings();

            // If Android, setup Android player settings
            if (platformSettings.BuildTarget == BuildTarget.Android)
            {
                ConfigureAndroidSettings();
            }

            // If iOS, setup iOS player settings
            if (EditorUserBuildSettings.selectedBuildTargetGroup == BuildTargetGroup.iOS)
            {
                ConfigureIosSettings();
            }

            if (!string.IsNullOrEmpty(platformSettings.XrTarget))
            {
                var xrConfigurator = new XrConfigurator(platformSettings);
                xrConfigurator.ConfigureXr();
            }
        }

        private void ConfigureIosSettings()
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS,
                string.Format("com.unity3d.{0}", PlayerSettings.productName));
            PlayerSettings.iOS.appleDeveloperTeamID = platformSettings.AppleDeveloperTeamId;
            PlayerSettings.iOS.appleEnableAutomaticSigning = false;
            PlayerSettings.iOS.iOSManualProvisioningProfileID = platformSettings.IOsProvisioningProfileId;
            PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Development;
        }

        private void ConfigureCrossplatformSettings()
        {
			PlayerSettings.virtualRealitySupported = false;
            PlayerSettings.colorSpace = platformSettings.ColorSpace;

            if (platformSettings.PlayerGraphicsApi != GraphicsDeviceType.Null)
            {
                PlayerSettings.SetGraphicsAPIs(platformSettings.BuildTarget, new[] {platformSettings.PlayerGraphicsApi});
            }

            PlayerSettings.SetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup,
                platformSettings.ScriptingImplementation);
        }

        private void ConfigureAndroidSettings()
        {
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            PlayerSettings.Android.minSdkVersion = platformSettings.MinimumAndroidSdkVersion;
            PlayerSettings.Android.targetSdkVersion = platformSettings.TargetAndroidSdkVersion;
        }

        private OptionSet DefineOptionSet()
        {
            var optionsSet = new OptionSet()
                .Add("scriptingbackend=",
                            "Scripting backend to use. IL2CPP is default. Values: IL2CPP, Mono",
                            ParseScriptingBackend)
                .Add("simulationmode=",
                    "Enable Simulation modes for Windows MR in Editor. Values: \r\n\"HoloLens\"\r\n\"WindowsMR\"\r\n\"Remoting\"",
                    simMode => platformSettings.SimulationMode = simMode)
                .Add("enabledxrtarget|enabledxrtargets=",
                    "XR target to enable in player settings. Values: " +
                    "\r\n\"Oculus\"\r\n\"OpenVR\"\r\n\"cardboard\"\r\n\"daydream\"\r\n\"MockHMD\"\r\n\"OculusXRSDK\"\r\n\"MagicLeapXRSDK\"\r\n\"WindowsMRXRSDK\"",
                    Action)
                .Add("playergraphicsapi=", "Graphics API based on GraphicsDeviceType.",
                    graphicsDeviceType => platformSettings.PlayerGraphicsApi = TryParse<GraphicsDeviceType>(graphicsDeviceType))
                .Add("colorspace=", "Linear or Gamma color space.",
                    colorSpace => platformSettings.ColorSpace = TryParse<ColorSpace>(colorSpace))
                .Add("stereorenderingpath=", "Stereo rendering path to enable. SinglePass is default",
                    srm => StereoRenderingMode = srm)
                .Add("mtRendering",
                    "Enable or disable multithreaded rendering. Enabled is default. Use option to enable, or use option and append '-' to disable.",
                    option => platformSettings.MtRendering = option != null)
                .Add("graphicsJobs",
                    "Enable graphics jobs rendering. Disabled is default. Use option to enable, or use option and append '-' to disable.",
                    option => platformSettings.GraphicsJobs = option != null)
                .Add("minimumandroidsdkversion=", "Minimum Android SDK Version to use.",
                    minAndroidSdkVersion => platformSettings.MinimumAndroidSdkVersion = TryParse<AndroidSdkVersions>(minAndroidSdkVersion))
                .Add("targetandroidsdkversion=", "Target Android SDK Version to use.",
                    trgtAndroidSdkVersion => platformSettings.TargetAndroidSdkVersion = TryParse<AndroidSdkVersions>(trgtAndroidSdkVersion))
                .Add("appleDeveloperTeamID=",
                    "Apple Developer Team ID. Use for deployment and running tests on iOS device.",
                    appleTeamId => platformSettings.AppleDeveloperTeamId = appleTeamId)
                .Add("iOSProvisioningProfileID=",
                    "iOS Provisioning Profile ID. Use for deployment and running tests on iOS device.",
                    id => platformSettings.IOsProvisioningProfileId = id);

            platformSettings.SerializeToAsset();

            return optionsSet;
        }

        private void Action(string xrTarget)
        {
            platformSettings.XrTarget = xrTarget;
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

        private void ParseScriptingBackend(string scriptingBackend)
        {
            var sb = scriptingBackend.ToLower();
            if (sb.Equals("mono"))
            {
                platformSettings.ScriptingImplementation = ScriptingImplementation.Mono2x;
            }
            else if (sb.Equals("il2cpp"))
            {
                platformSettings.ScriptingImplementation = ScriptingImplementation.IL2CPP;
            }
            else
            {
                throw new ArgumentException(string.Format(
                    "Unrecognized scripting backend {0}. Valid options are Mono or IL2CPP", scriptingBackend));
            }
        }
#endif
    }
}