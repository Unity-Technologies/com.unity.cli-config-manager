using NDesk.Options;
using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.unity.cliconfigmanager
{
    public class PrebuildSettingsConfigurator
    {
        private readonly OculusSettingsConfigurator oculusSettingsConfigurator = new OculusSettingsConfigurator();
        private readonly Regex customArgRegex = new Regex("-([^=]*)=", RegexOptions.Compiled);
        private string packageId;
        private static readonly string XrSdkDefine = "XR_SDK";
        private static readonly string OculusSdkDefine = "OCULUS_SDK";

        private static readonly List<string> XrSdkDefines = new List<string>
        {
            "XR_SDK",
            "OCULUS_SDK"
        };

        private static readonly string OculusXrSdkPackageId = "com.unity.xr.oculus";
        private static readonly string XrManagementPackageId = "com.unity.xr.management";

        public void ConfigureFromCmdlineArgs()
        {
#if UNITY_EDITOR
            SetScriptingDefinesAndUpdatePackages();
            ParseCommandLineArgs();
            PlatformSettings.SerializeToAsset();
            ConfigureSettings();
#endif
        }
#if UNITY_EDITOR
        private static void SetScriptingDefinesAndUpdatePackages()
        {
            var nonXrSdkDefines = GetNonXrSdkDefines();
            var xrSdkPackageHandler = new XrSdkPackageHandler();
            xrSdkPackageHandler.RemoveXrSdkPackages();
            WaitForDomainReload();
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                string.Join(";", nonXrSdkDefines.ToArray()));
            WaitForDomainReload();
            AssetDatabase.SaveAssets();

            if (IsOculusXrSdk())
            {
                PlayerSettings.virtualRealitySupported = false;

                xrSdkPackageHandler.AddPackage(XrManagementPackageId);
                xrSdkPackageHandler.AddPackage(OculusXrSdkPackageId);

                List<string> defines = new List<string> {XrSdkDefine, OculusSdkDefine};

                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                    string.Join(";", defines.ToArray()));
            }
            else
            {
                PlayerSettings.virtualRealitySupported = true;
            }
        }

        private static List<string> GetNonXrSdkDefines()
        {
            var defines = PlayerSettings
                .GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup)
                .Split(';')
                .ToList();
            var nonXrSdkDefines = defines.ToList().Except(XrSdkDefines).ToList();
            return nonXrSdkDefines;
        }

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
            ConfigureAllInclusiveSettings();

            // If XR target, configure XR
            if (!string.IsNullOrEmpty(PlatformSettings.XrTarget))
            {
                ConfigureXr();
            }

            // If Android, setup Android player settings
            if (PlatformSettings.BuildTarget == BuildTarget.Android)
            {
                ConfigureAndroidSettings();
            }

            // If iOS, setup iOS player settings
            if (EditorUserBuildSettings.selectedBuildTargetGroup == BuildTargetGroup.iOS)
            {
                ConfigureIosSettings();
            }
        }

        private static void ConfigureIosSettings()
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS,
                string.Format("com.unity3d.{0}", PlayerSettings.productName));
            PlayerSettings.iOS.appleDeveloperTeamID = PlatformSettings.AppleDeveloperTeamId;
            PlayerSettings.iOS.appleEnableAutomaticSigning = false;
            PlayerSettings.iOS.iOSManualProvisioningProfileID = PlatformSettings.IOsProvisioningProfileId;
            PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Development;
        }

        private static void ConfigureAllInclusiveSettings()
        {
            if (PlatformSettings.PlayerGraphicsApi != GraphicsDeviceType.Null)
            {
                PlayerSettings.SetGraphicsAPIs(PlatformSettings.BuildTarget, new[] {PlatformSettings.PlayerGraphicsApi});
            }

            PlayerSettings.SetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup,
                PlatformSettings.ScriptingImplementation);
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
            PlayerSettings.Android.targetSdkVersion = PlatformSettings.TargetAndroidSdkVersion;
        }

        private OptionSet DefineOptionSet()
        {
            return new OptionSet()
                .Add("scriptingbackend=",
                            "Scripting backend to use. IL2CPP is default. Values: IL2CPP, Mono",
                            ParseScriptingBackend)
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
                .Add("appleDeveloperTeamID=",
                    "Apple Developer Team ID. Use for deployment and running tests on iOS device.",
                    appleTeamId => PlatformSettings.AppleDeveloperTeamId = appleTeamId)
                .Add("iOSProvisioningProfileID=",
                    "iOS Provisioning Profile ID. Use for deployment and running tests on iOS device.",
                    id => PlatformSettings.IOsProvisioningProfileId = id);
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

        private void ParseScriptingBackend(string scriptingBackend)
        {
            var sb = scriptingBackend.ToLower();
            if (sb.Equals("mono"))
            {
                PlatformSettings.ScriptingImplementation = ScriptingImplementation.Mono2x;
            }
            else if (sb.Equals("il2cpp"))
            {
                PlatformSettings.ScriptingImplementation = ScriptingImplementation.IL2CPP;
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