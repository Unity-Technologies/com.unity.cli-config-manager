#if OCULUS_SDK
using Unity.XR.Oculus;
#endif
using System;
using System.Linq;
using com.unity.xr.test.runtimesettings;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
#endif
using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_VR
using UnityEngine.XR;
#endif

namespace com.unity.cliconfigmanager
{
    public class PlatformSettings
    {
#if UNITY_EDITOR
        public BuildTargetGroup BuildTargetGroup => EditorUserBuildSettings.selectedBuildTargetGroup;
        public BuildTarget BuildTarget => EditorUserBuildSettings.activeBuildTarget;

        public string XrTarget;
        public GraphicsDeviceType PlayerGraphicsApi;

#if OCULUS_SDK
        public OculusSettings.StereoRenderingModeDesktop StereoRenderingModeDesktop;
        public OculusSettings.StereoRenderingModeAndroid StereoRenderingModeAndroid;
#else
        public StereoRenderingPath StereoRenderingPath;
#endif
        public bool MtRendering = true;
        public bool GraphicsJobs;
        public AndroidSdkVersions MinimumAndroidSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        public AndroidSdkVersions TargetAndroidSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        public ScriptingImplementation ScriptingImplementation = ScriptingImplementation.Mono2x;
        public string AppleDeveloperTeamId;
        public string IOsProvisioningProfileId;
        public ColorSpace ColorSpace = ColorSpace.Gamma;
        public string XrsdkRevision;
        public string XrsdkRevisionDate;
        public string XrsdkBranch;

        public string SimulationMode;
        private readonly string ResourceDir = "Assets/Resources";
        private readonly string xrManagementPackage = "com.unity.xr.management";
        private readonly string oculusXrSdkPackage = "com.unity.xr.oculus";

        public void SerializeToAsset()
        {
            var settingsAsset = ScriptableObject.CreateInstance<CurrentSettings>();

            settingsAsset.SimulationMode = SimulationMode;
            settingsAsset.PlayerGraphicsApi = PlayerGraphicsApi.ToString();
            settingsAsset.MtRendering = MtRendering;
            settingsAsset.GraphicsJobs = GraphicsJobs;
            settingsAsset.ColorSpace = ColorSpace.ToString();
            settingsAsset.EnabledXrTarget = XrTarget;
            settingsAsset.XrsdkRevision = GetOculusXrSdkPackageRevision();
            settingsAsset.XrManagementRevision = GetXrManagementPackageRevision();

#if OCULUS_SDK
            settingsAsset.StereoRenderingModeDesktop = StereoRenderingModeDesktop.ToString();
            settingsAsset.StereoRenderingModeAndroid = StereoRenderingModeAndroid.ToString();
#if OCULUS_SDK_PERF
            settingsAsset.PluginVersion = string.Format("OculusPluginVersion|{0}", OculusStats.PluginVersion);
#endif
#else
            settingsAsset.StereoRenderingMode = GetXrStereoRenderingPathMapping(StereoRenderingPath).ToString();
#endif
            CreateAndSaveCurrentSettingsAsset(settingsAsset);
        }

        public string GetXrManagementPackageRevision()
        {
            string packageRevision = string.Empty;

            var listRequest = Client.List(true);
            while (!listRequest.IsCompleted)
            {
            }

            if (listRequest.Result.Any(r => r.name.Equals(xrManagementPackage)))
            {
                var xrManagementPckg =
                    listRequest.Result.First(r => r.name.Equals(xrManagementPackage));
#if UNITY_2020_1_OR_NEWER
                var revision = xrManagementPckg.repository.revision;
#else
                var revision = "unavailable";
#endif
                var version = xrManagementPckg.version;
                packageRevision = string.Format("{0}|{1}|{2}", xrManagementPackage, version, revision);
            }

            return packageRevision;
        }

        public string GetOculusXrSdkPackageRevision()
        {
            string packageRevision = String.Empty;

            var listRequest = Client.List(true);
            while (!listRequest.IsCompleted)
            {
            }

            if (listRequest.Result.Any(r => r.name.Equals(oculusXrSdkPackage)))
            {
                var oculusXrsdkPckg =
                    listRequest.Result.First(r => r.name.Equals(oculusXrSdkPackage));

                var version = oculusXrsdkPckg.version;
                packageRevision = string.Format("{0}|{1}|{2}|{3}|{4}", oculusXrSdkPackage, version, XrsdkRevision, XrsdkRevisionDate, XrsdkBranch);
            }

            return packageRevision;
        }

        private XRSettings.StereoRenderingMode GetXrStereoRenderingPathMapping(StereoRenderingPath stereoRenderingPath)
        {
            switch (stereoRenderingPath)
            {
                case StereoRenderingPath.SinglePass:
                    return XRSettings.StereoRenderingMode.SinglePass;
                case StereoRenderingPath.MultiPass:
                    return XRSettings.StereoRenderingMode.MultiPass;
                case StereoRenderingPath.Instancing:
                    return XRSettings.StereoRenderingMode.SinglePassInstanced;
                default:
                    return XRSettings.StereoRenderingMode.SinglePassMultiview;
            }
        }

        private void CreateAndSaveCurrentSettingsAsset(CurrentSettings settingsAsset)
        {
            if (!System.IO.Directory.Exists(ResourceDir))
            {
                System.IO.Directory.CreateDirectory(ResourceDir);
            }

            AssetDatabase.CreateAsset(settingsAsset, ResourceDir + "/settings.asset");
            AssetDatabase.SaveAssets();
        }
#endif
    }
}