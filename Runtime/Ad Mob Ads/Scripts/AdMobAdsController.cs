using System;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ADMOB_DEPENDENCIES_INSTALLED
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
#endif
using UnityEngine;
public class AdMobAdsController : MonoBehaviour {
    [SerializeField] bool iOSAppPauseOnBackground = true;
    [SerializeField] bool raiseAdEventsOnUnityMainThread;
    [SerializeField] bool initWithoutConsent = false;
    [SerializeField] AdMobBannerController[] bannerControllers;
    [SerializeField] AdMobInterstitialController[] interstitialControllers;
    [SerializeField] AdMobRewardedController[] rewardedControllers;
    [SerializeField] AdMobNativeController[] nativeControllers;
    [SerializeField] AdMobAppOpenController[] appOpenControllers;
    [SerializeField] AdMobConsentController consentController;
#if UNITY_EDITOR
    [ContextMenu("Add AdMob Define")]
    void AddADMobDefine() => AdsController.AddDefine("ADMOB_DEPENDENCIES_INSTALLED");
    [ContextMenu("Remove AdMob Define")]
    void RemoveADMobDefine() => AdsController.RemoveDefine("ADMOB_DEPENDENCIES_INSTALLED");
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
#if ADMOB_DEPENDENCIES_INSTALLED
    public AdMobBannerController[] BannerControllers { get => bannerControllers; }
    public AdMobInterstitialController[] InterstitialControllers { get => interstitialControllers; }
    public AdMobRewardedController[] RewardedControllers { get => rewardedControllers; }
    public AdMobNativeController[] NativeControllers { get => nativeControllers; }
    public AdMobAppOpenController[] AppOpenControllers { get => appOpenControllers; }
    // https://developers.google.com/admob/unity/test-ads
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
        if (debugLogs) Debug.Log("[AdMobAdsController] " + message);
    }
    public void AdsStart(Action onInitComplete) {
        Log("Device Unique Identifier: " + SystemInfo.deviceUniqueIdentifier);
#if UNITY_IOS
        MobileAds.SetiOSAppPauseOnBackground(iOSAppPauseOnBackground);
#endif
#if UNITY_EDITOR
        InitializeAdMob(onInitComplete);
#elif UNITY_IOS || UNITY_ANDROID
        if(initWithoutConsent) {
            InitializeAdMob(onInitComplete);
        }
        else if (consentController.CanRequestAds) {
            InitializeAdMob(onInitComplete);
        }
        InitializeAdMobConsent(onInitComplete);
#endif
        MobileAds.RaiseAdEventsOnUnityMainThread = raiseAdEventsOnUnityMainThread;
    }
    private void InitializeAdMobConsent(Action onInitComplete) {
        Log("InitializeAdMobConsent()");
        consentController.GatherConsent(error => {
            if (error != null) {
                Debug.LogError("Failed to gather consent with error: " + error);
            } else {
                Log("Google Mobile Ads consent updated: " + ConsentInformation.ConsentStatus);
            }
            if (consentController.CanRequestAds) {
                InitializeAdMob(onInitComplete);
            }
        });
    }
    private void InitializeAdMob(Action onInitComplete) {
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
#if ADMOB_DEPENDENCIES_INSTALLED
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
#if ADMOB_DEPENDENCIES_INSTALLED
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
