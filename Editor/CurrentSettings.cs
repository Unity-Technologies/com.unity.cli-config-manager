using Unity.XR.Oculus;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace com.unity.cliconfigmanager
{
    public class CurrentSettings : ScriptableObject
    {
        public string EnabledXrTarget;
        public GraphicsDeviceType PlayerGraphicsApi;

        public XRSettings.StereoRenderingMode StereoRenderingMode;

#if OCULUS_SDK
        public OculusSettings.StereoRenderingMode StereoRenderingModeDesktop;
        public OculusSettings.StereoRenderingMode StereoRenderingModeAndroid;

        public string StereoRenderingModeDesktopToString => StereoRenderingModeDesktop.ToString();
        public string StereoRenderingModeAndroidToString => StereoRenderingModeAndroid.ToString();
#endif

        public bool MtRendering = true;
        public bool GraphicsJobs;

        public string SimulationMode;
    }
}