#if OCULUS_SDK
using Unity.XR.Oculus;
#endif

using com.unity.xr.test.runtimesettings;
#if UNITY_EDITOR
using UnityEditor;
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
        public StereoRenderingPath StereoRenderingPath;
#if OCULUS_SDK
        public OculusSettings.StereoRenderingModeDesktop StereoRenderingModeDesktop;
        public OculusSettings.StereoRenderingModeAndroid StereoRenderingModeAndroid;
#endif
        public bool MtRendering = true;
        public bool GraphicsJobs;
        public AndroidSdkVersions MinimumAndroidSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        public AndroidSdkVersions TargetAndroidSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        public ScriptingImplementation ScriptingImplementation = ScriptingImplementation.IL2CPP;
        public string AppleDeveloperTeamId;
        public string IOsProvisioningProfileId;
        public ColorSpace ColorSpace = ColorSpace.Gamma;

        public string SimulationMode;
        private readonly string ResourceDir = "Assets/Resources";

        public void SerializeToAsset()
        {
            var settingsAsset = ScriptableObject.CreateInstance<CurrentSettings>();

            settingsAsset.PlayerGraphicsApi = PlayerGraphicsApi.ToString();
            settingsAsset.MtRendering = MtRendering;
            settingsAsset.GraphicsJobs = GraphicsJobs;
            settingsAsset.ColorSpace = ColorSpace.ToString();

            settingsAsset.EnabledXrTarget = XrTarget;
            settingsAsset.StereoRenderingMode = GetXrStereoRenderingPathMapping(StereoRenderingPath).ToString();
#if OCULUS_SDK
            settingsAsset.StereoRenderingModeDesktop = StereoRenderingModeDesktop.ToString();
            settingsAsset.StereoRenderingModeAndroid = StereoRenderingModeAndroid.ToString();
#endif
            CreateAndSaveCurrentSettingsAsset(settingsAsset);
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