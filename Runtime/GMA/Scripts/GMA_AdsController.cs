using System;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if GMA_DEPENDENCIES_INSTALLED
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
#endif
using UnityEngine;
namespace CustomAds.GMA {
    public class GMA_AdsController : MonoBehaviour {
        [SerializeField] bool iOSAppPauseOnBackground = true;
        [SerializeField] bool raiseAdEventsOnUnityMainThread;
        [SerializeField] bool initWithoutConsent = false;
        [SerializeField] GMA_BannerController[] bannerControllers;
        [SerializeField] GMA_InterstitialController[] interstitialControllers;
        [SerializeField] GMA_RewardedController[] rewardedControllers;
        [SerializeField] GMA_NativeController[] nativeControllers;
        [SerializeField] GMA_AppOpenController[] appOpenControllers;
        [SerializeField] GMA_ConsentController consentController;
#if UNITY_EDITOR
        [ContextMenu("Add GMA Define")]
        void AddGMADefine() => AdsController.AddDefine("GMA_DEPENDENCIES_INSTALLED");
        [ContextMenu("Remove GMA Define")]
        void RemoveGMADefine() => AdsController.RemoveDefine("GMA_DEPENDENCIES_INSTALLED");
        [ContextMenu("Add Google Package Registry")]
        void AddGooglePackageRegistry() => AdsController.AddScopeRegistry("Google", "https://package.openupm.com/", "com.google");
        [ContextMenu("Add Google Mobile Ads Package")]
        void AddGoogleMobileAdsPackage() => AdsController.AddPackageByName("com.google.ads.mobile");
#endif
        public static bool IsAndroid {
            get {
#if UNITY_ANDROID
                return true;
#else
            return false;
#endif
            }
        }
        public static bool IsIos {
            get {
#if UNITY_IOS
            return true;
#else
                return false;
#endif
            }
        }
#if GMA_DEPENDENCIES_INSTALLED
        public GMA_BannerController[] BannerControllers { get => bannerControllers; }
        public GMA_InterstitialController[] InterstitialControllers { get => interstitialControllers; }
        public GMA_RewardedController[] RewardedControllers { get => rewardedControllers; }
        public GMA_NativeController[] NativeControllers { get => nativeControllers; }
        public GMA_AppOpenController[] AppOpenControllers { get => appOpenControllers; }
        // https://developers.google.com/GMA/unity/test-ads
        public static List<string> TestDeviceIds = new()
            {
            AdRequest.TestDeviceSimulator,
#if UNITY_IPHONE
            "96e23e80653bb28980d3f40beb58915c",
#elif UNITY_ANDROID
            "702815ACFC14FF222DA1DC767672A573",
            "1e955e734a6c29f9876109b7c70d8ddc"
#endif
    };
        public bool debugLogs;
        void Log(string message) {
            if (debugLogs) Debug.Log("[GMAAdsController] " + message);
        }
        public void AdsStart(Action onInitComplete) {
            Log("Device Unique Identifier: " + SystemInfo.deviceUniqueIdentifier);
#if UNITY_IOS
        MobileAds.SetiOSAppPauseOnBackground(iOSAppPauseOnBackground);
#endif
#if UNITY_IOS || UNITY_ANDROID
            if (initWithoutConsent) {
                InitializeGMA(onInitComplete);
            } else if (consentController.CanRequestAds) {
                InitializeGMA(onInitComplete);
            }
            InitializeGMAConsent(onInitComplete);
#endif
            MobileAds.RaiseAdEventsOnUnityMainThread = raiseAdEventsOnUnityMainThread;
        }
        private void InitializeGMAConsent(Action onInitComplete) {
            Log("InitializeGMAConsent()");
            consentController.GatherConsent(error => {
                if (error != null) {
                    Debug.LogError("Failed to gather consent with error: " + error);
                } else {
                    Log("Google Mobile Ads consent updated: " + ConsentInformation.ConsentStatus);
                }
                if (consentController.CanRequestAds) {
                    InitializeGMA(onInitComplete);
                }
            });
        }
        private void InitializeGMA(Action onInitComplete) {
            MobileAds.Initialize(status => {
                MobileAds.SetRequestConfiguration(new RequestConfiguration { TestDeviceIds = TestDeviceIds });

                var adapterMap = status.getAdapterStatusMap();
                foreach (var adapterStatus in adapterMap) {
                    if (adapterStatus.Value.InitializationState == AdapterState.NotReady) {
                        Log($"{adapterStatus.Key} not ready: {adapterStatus.Value.Description}");
                    } else {
                        Log(adapterStatus.Key + " ready");
                    }
                }
                onInitComplete();
            });
        }
        public void SetApplicationVolume(float volume) {
            MobileAds.SetApplicationVolume(volume);
        }
        public void SetApplicationMuted(bool muted) {
            MobileAds.SetApplicationMuted(muted);
        }
        public void InitControllers() {
            InitBannerAd();
#if GMA_DEPENDENCIES_INSTALLED
            if (AppOpenControllers != null) {
                foreach (var c in AppOpenControllers) {
                    if (c != null) c.Init();
                }
            }
#endif
        }
        public void InitBannerAd(int index = -1) {
            if (index < 0) {
                for (int i = 0; i < bannerControllers.Length; i++) {
                    bannerControllers[i].Init();
                }
            } else {
                bannerControllers[index].Init();
            }
        }
        public void DestroyBannerAd(int index = -1) {
            if (index < 0) {
                for (int i = 0; i < bannerControllers.Length; i++) {
                    bannerControllers[i].Destroy();
                }
            } else {
                bannerControllers[index].Destroy();
            }
        }
        public void DestroyInterstitialAds(int index = -1) {
            if (index < 0) {
                for (int i = 0; i < interstitialControllers.Length; i++) {
                    interstitialControllers[i].Destroy();
                }
            } else {
                interstitialControllers[index].Destroy();
            }
        }
        public void DestroyRewardedAds(int index = -1) {
            if (index < 0) {
                for (int i = 0; i < rewardedControllers.Length; i++) {
                    rewardedControllers[i].Destroy();
                }
            } else {
                rewardedControllers[index].Destroy();
            }
        }

        public void OpenPrivacyOptions() {
            consentController.ShowPrivacyOptionsForm((string error) => {
                if (error != null) {
                    Debug.LogError("Failed to show consent privacy form with error: " +
                        error);
                } else {
                    Log("Privacy form opened successfully.");
                }
            });
        }
        public void OpenAdInspector() {
            Log("Opening ad Inspector.");
            MobileAds.OpenAdInspector((AdInspectorError error) => {
                if (error != null) {
                    Debug.LogError("Ad Inspector failed to open with error: " + error);
                    return;
                }
                Log("Ad Inspector opened successfully.");
            });
        }

        public void DestroyControllers() {
            DestroyBannerAd();
            DestroyInterstitialAds();
            DestroyRewardedAds();
#if GMA_DEPENDENCIES_INSTALLED
            if (AppOpenControllers != null) {
                foreach (var c in AppOpenControllers) {
                    if (c != null) c.Destroy();
                }
            }
#endif
        }

        internal void ValidateControllers() {

        }
#endif
    }
}