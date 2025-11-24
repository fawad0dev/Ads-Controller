using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomAds.Core {
    /// <summary>
    /// Simplified, modular ads manager with static API
    /// </summary>
    public class AdsManager : MonoBehaviour {
        [Header("Configuration")]
        [SerializeField] private bool debugLogs = false;

        [Header("Auto-Initialize")]
        [SerializeField] private bool autoInitialize = true;

        [Header("SDK Initializers")]
        [SerializeField] private List<MonoBehaviour> sdkInitializers = new List<MonoBehaviour>();

        [Header("Ad Units")]
        [SerializeField] private List<BannerAdUnit> bannerAds = new List<BannerAdUnit>();
        [SerializeField] private List<InterstitialAdUnit> interstitialAds = new List<InterstitialAdUnit>();
        [SerializeField] private List<RewardedAdUnit> rewardedAds = new List<RewardedAdUnit>();
        [SerializeField] private List<AppOpenAdUnit> appOpenAds = new List<AppOpenAdUnit>();
        [SerializeField] private List<NativeAdUnit> nativeAds = new List<NativeAdUnit>();

        [System.Serializable]
        public class BannerAdUnit {
            public string name;
            public MonoBehaviour adComponent;
            public bool autoLoad = true;

            [HideInInspector] public IBannerAd ad;
        }

        [System.Serializable]
        public class InterstitialAdUnit {
            public string name;
            public MonoBehaviour adComponent;
            public bool autoLoad = true;

            [HideInInspector] public IInterstitialAd ad;
        }

        [System.Serializable]
        public class RewardedAdUnit {
            public string name;
            public MonoBehaviour adComponent;
            public bool autoLoad = true;

            [HideInInspector] public IRewardedAd ad;
        }

        [System.Serializable]
        public class AppOpenAdUnit {
            public string name;
            public MonoBehaviour adComponent;
            public bool autoLoad = true;

            [HideInInspector] public IAppOpenAd ad;
        }

        [System.Serializable]
        public class NativeAdUnit {
            public string name;
            public MonoBehaviour adComponent;
            public bool autoLoad = true;

            [HideInInspector] public INativeAd ad;
        }

        // Singleton
        private static AdsManager _instance;

        // Events
        public event Action OnInitialized;
        public event Action<string> OnError;

        // Properties
        public bool IsInitialized { get; private set; }

        private void Awake() {
            if (_instance == null) {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            } else if (_instance != this) {
                Destroy(gameObject);
                return;
            }

            if (autoInitialize) {
                Initialize();
            }
        }

        private void Log(string message) {
            if (debugLogs) Debug.Log($"[AdsManager] {message}");
        }

        private void LogError(string message) {
            Debug.LogError($"[AdsManager] {message}");
            OnError?.Invoke(message);
        }

        private static AdsManager GetInstance() {
            if (_instance == null) {
                _instance = FindFirstObjectByType<AdsManager>();
            }
            return _instance;
        }

        /// <summary>
        /// Initialize all ad units
        /// </summary>
        public void Initialize() {
            if (IsInitialized) {
                Log("Already initialized");
                return;
            }

            Log("Initializing...");

            // Initialize SDK providers first
            InitializeSDKs(() => {
                InitializeAdUnits();
            });
        }

        /// <summary>
        /// Initialize all SDK providers (GMA, Unity Ads, etc.)
        /// </summary>
        private void InitializeSDKs(Action onComplete) {
            if (sdkInitializers == null || sdkInitializers.Count == 0) {
                Log("No SDK initializers configured");
                onComplete?.Invoke();
                return;
            }

            int initializersCompleted = 0;
            int totalInitializers = sdkInitializers.Count;

            foreach (var initializer in sdkInitializers) {
                if (initializer == null) {
                    initializersCompleted++;
                    if (initializersCompleted >= totalInitializers) {
                        onComplete?.Invoke();
                    }
                    continue;
                }

                // Try to cast to GMAInitializer
                if (initializer is CustomAds.GMA.GMAInitializer gmaInit) {
                    Log($"Initializing {initializer.GetType().Name}");
                    gmaInit.Initialize(() => {
                        initializersCompleted++;
                        Log($"SDK initialized: {initializer.GetType().Name}");

                        if (initializersCompleted >= totalInitializers) {
                            onComplete?.Invoke();
                        }
                    });
                } else {
                    // Unknown initializer type, skip
                    LogError($"Unknown initializer type: {initializer.GetType().Name}");
                    initializersCompleted++;
                    if (initializersCompleted >= totalInitializers) {
                        onComplete?.Invoke();
                    }
                }
            }
        }

        /// <summary>
        /// Initialize all ad unit components
        /// </summary>
        private void InitializeAdUnits() {
            Log("Initializing ad units...");

            // Initialize banners
            foreach (var unit in bannerAds) {
                if (unit.adComponent is IBannerAd banner) {
                    unit.ad = banner;
                    banner.Initialize();
                    if (unit.autoLoad) {
                        banner.Load(result => {
                            if (!result.Success) {
                                LogError($"Failed to load banner '{unit.name}': {result.ErrorMessage}");
                            }
                        });
                    }
                } else {
                    LogError($"Banner '{unit.name}' component does not implement IBannerAd");
                }
            }

            // Initialize interstitials
            foreach (var unit in interstitialAds) {
                if (unit.adComponent is IInterstitialAd interstitial) {
                    unit.ad = interstitial;
                    interstitial.Initialize();
                    if (unit.autoLoad) {
                        interstitial.Load(result => {
                            if (!result.Success) {
                                LogError($"Failed to load interstitial '{unit.name}': {result.ErrorMessage}");
                            }
                        });
                    }
                } else {
                    LogError($"Interstitial '{unit.name}' component does not implement IInterstitialAd");
                }
            }

            // Initialize rewarded
            foreach (var unit in rewardedAds) {
                if (unit.adComponent is IRewardedAd rewarded) {
                    unit.ad = rewarded;
                    rewarded.Initialize();
                    if (unit.autoLoad) {
                        rewarded.Load(result => {
                            if (!result.Success) {
                                LogError($"Failed to load rewarded '{unit.name}': {result.ErrorMessage}");
                            }
                        });
                    }
                } else {
                    LogError($"Rewarded '{unit.name}' component does not implement IRewardedAd");
                }
            }

            // Initialize app open
            foreach (var unit in appOpenAds) {
                if (unit.adComponent is IAppOpenAd appOpen) {
                    unit.ad = appOpen;
                    appOpen.Initialize();
                    if (unit.autoLoad) {
                        appOpen.Load(result => {
                            if (!result.Success) {
                                LogError($"Failed to load app open '{unit.name}': {result.ErrorMessage}");
                            }
                        });
                    }
                } else {
                    LogError($"App open '{unit.name}' component does not implement IAppOpenAd");
                }
            }

            // Initialize native
            foreach (var unit in nativeAds) {
                if (unit.adComponent is INativeAd native) {
                    unit.ad = native;
                    native.Initialize();
                    if (unit.autoLoad) {
                        native.Load(result => {
                            if (!result.Success) {
                                LogError($"Failed to load native '{unit.name}': {result.ErrorMessage}");
                            }
                        });
                    }
                } else {
                    LogError($"Native '{unit.name}' component does not implement INativeAd");
                }
            }

            IsInitialized = true;
            Log("Initialization complete");
            OnInitialized?.Invoke();
        }

        #region Banner Ads

        /// <summary>
        /// Get a banner ad by name (empty name = first available)
        /// </summary>
        public static IBannerAd GetBanner(string name = "") {
            var instance = GetInstance();
            if (instance == null) return null;

            if (string.IsNullOrEmpty(name)) {
                return instance.bannerAds.Count > 0 ? instance.bannerAds[0].ad : null;
            }

            var unit = instance.bannerAds.Find(b => b.name == name);
            return unit?.ad;
        }

        /// <summary>
        /// Show a banner ad (empty name = first available)
        /// </summary>
        public static void ShowBanner(string name = "", Action<AdResult> onComplete = null) {
            var instance = GetInstance();
            var banner = GetBanner(name);
            if (banner == null) {
                instance?.LogError($"Banner '{name}' not found");
                onComplete?.Invoke(AdResult.Failed("Banner not found"));
                return;
            }

            if (!banner.IsReady) {
                instance?.Log($"Banner '{name}' not ready, loading first...");
                banner.Load(loadResult => {
                    if (loadResult.Success) {
                        banner.Show(onComplete);
                    } else {
                        onComplete?.Invoke(loadResult);
                    }
                });
            } else {
                banner.Show(onComplete);
            }
        }

        /// <summary>
        /// Hide a banner ad (empty name = first available)
        /// </summary>
        public static void HideBanner(string name = "") {
            var instance = GetInstance();
            var banner = GetBanner(name);
            if (banner == null) {
                instance?.LogError($"Banner '{name}' not found");
                return;
            }
            banner.Hide();
        }

        #endregion

        #region Interstitial Ads

        /// <summary>
        /// Get an interstitial ad by name (empty name = first available)
        /// </summary>
        public static IInterstitialAd GetInterstitial(string name = "") {
            var instance = GetInstance();
            if (instance == null) return null;

            if (string.IsNullOrEmpty(name)) {
                return instance.interstitialAds.Count > 0 ? instance.interstitialAds[0].ad : null;
            }

            var unit = instance.interstitialAds.Find(i => i.name == name);
            return unit?.ad;
        }

        /// <summary>
        /// Show an interstitial ad (empty name = first available)
        /// </summary>
        public static void ShowInterstitial(string name = "", Action<AdResult> onComplete = null) {
            var instance = GetInstance();
            var interstitial = GetInterstitial(name);
            if (interstitial == null) {
                instance?.LogError($"Interstitial '{name}' not found");
                onComplete?.Invoke(AdResult.Failed("Interstitial not found"));
                return;
            }

            if (!interstitial.IsReady) {
                instance?.LogError($"Interstitial '{name}' not ready");
                onComplete?.Invoke(AdResult.Failed("Ad not ready"));
                return;
            }

            interstitial.Show(onComplete);
        }

        /// <summary>
        /// Load an interstitial ad (empty name = first available)
        /// </summary>
        public static void LoadInterstitial(string name = "", Action<AdResult> onComplete = null) {
            var instance = GetInstance();
            var interstitial = GetInterstitial(name);
            if (interstitial == null) {
                instance?.LogError($"Interstitial '{name}' not found");
                onComplete?.Invoke(AdResult.Failed("Interstitial not found"));
                return;
            }

            interstitial.Load(onComplete);
        }

        #endregion

        #region Rewarded Ads

        /// <summary>
        /// Get a rewarded ad by name (empty name = first available)
        /// </summary>
        public static IRewardedAd GetRewarded(string name = "") {
            var instance = GetInstance();
            if (instance == null) return null;

            if (string.IsNullOrEmpty(name)) {
                return instance.rewardedAds.Count > 0 ? instance.rewardedAds[0].ad : null;
            }

            var unit = instance.rewardedAds.Find(r => r.name == name);
            return unit?.ad;
        }

        /// <summary>
        /// Show a rewarded ad (empty name = first available)
        /// </summary>
        public static void ShowRewarded(string name = "", Action<AdResult> onComplete = null) {
            var instance = GetInstance();
            var rewarded = GetRewarded(name);
            if (rewarded == null) {
                instance?.LogError($"Rewarded '{name}' not found");
                onComplete?.Invoke(AdResult.Failed("Rewarded ad not found"));
                return;
            }

            if (!rewarded.IsReady) {
                instance?.LogError($"Rewarded '{name}' not ready");
                onComplete?.Invoke(AdResult.Failed("Ad not ready"));
                return;
            }

            rewarded.Show(onComplete);
        }

        /// <summary>
        /// Load a rewarded ad (empty name = first available)
        /// </summary>
        public static void LoadRewarded(string name = "", Action<AdResult> onComplete = null) {
            var instance = GetInstance();
            var rewarded = GetRewarded(name);
            if (rewarded == null) {
                instance?.LogError($"Rewarded '{name}' not found");
                onComplete?.Invoke(AdResult.Failed("Rewarded ad not found"));
                return;
            }

            rewarded.Load(onComplete);
        }

        #endregion

        #region App Open Ads

        /// <summary>
        /// Get an app open ad by name (empty name = first available)
        /// </summary>
        public static IAppOpenAd GetAppOpen(string name = "") {
            var instance = GetInstance();
            if (instance == null) return null;

            if (string.IsNullOrEmpty(name)) {
                return instance.appOpenAds.Count > 0 ? instance.appOpenAds[0].ad : null;
            }

            var unit = instance.appOpenAds.Find(a => a.name == name);
            return unit?.ad;
        }

        /// <summary>
        /// Show an app open ad (empty name = first available)
        /// </summary>
        public static void ShowAppOpen(string name = "", Action<AdResult> onComplete = null) {
            var instance = GetInstance();
            var appOpen = GetAppOpen(name);
            if (appOpen == null) {
                instance?.LogError($"App open '{name}' not found");
                onComplete?.Invoke(AdResult.Failed("App open ad not found"));
                return;
            }

            if (!appOpen.IsReady) {
                instance?.LogError($"App open '{name}' not ready");
                onComplete?.Invoke(AdResult.Failed("Ad not ready"));
                return;
            }

            appOpen.Show(onComplete);
        }

        /// <summary>
        /// Load an app open ad (empty name = first available)
        /// </summary>
        public static void LoadAppOpen(string name = "", Action<AdResult> onComplete = null) {
            var instance = GetInstance();
            var appOpen = GetAppOpen(name);
            if (appOpen == null) {
                instance?.LogError($"App open '{name}' not found");
                onComplete?.Invoke(AdResult.Failed("App open ad not found"));
                return;
            }

            appOpen.Load(onComplete);
        }

        #endregion

        #region Native Ads

        /// <summary>
        /// Get a native ad by name (empty name = first available)
        /// </summary>
        public static INativeAd GetNative(string name = "") {
            var instance = GetInstance();
            if (instance == null) return null;

            if (string.IsNullOrEmpty(name)) {
                return instance.nativeAds.Count > 0 ? instance.nativeAds[0].ad : null;
            }

            var unit = instance.nativeAds.Find(n => n.name == name);
            return unit?.ad;
        }

        /// <summary>
        /// Show a native ad (empty name = first available)
        /// </summary>
        public static void ShowNative(string name = "", Action<AdResult> onComplete = null) {
            var instance = GetInstance();
            var native = GetNative(name);
            if (native == null) {
                instance?.LogError($"Native '{name}' not found");
                onComplete?.Invoke(AdResult.Failed("Native ad not found"));
                return;
            }

            if (!native.IsReady) {
                instance?.Log($"Native '{name}' not ready, loading first...");
                native.Load(loadResult => {
                    if (loadResult.Success) {
                        native.Show(onComplete);
                    } else {
                        onComplete?.Invoke(loadResult);
                    }
                });
            } else {
                native.Show(onComplete);
            }
        }

        /// <summary>
        /// Hide a native ad (empty name = first available)
        /// </summary>
        public static void HideNative(string name = "") {
            var instance = GetInstance();
            var native = GetNative(name);
            if (native == null) {
                instance?.LogError($"Native '{name}' not found");
                return;
            }
            native.Hide();
        }

        /// <summary>
        /// Load a native ad (empty name = first available)
        /// </summary>
        public static void LoadNative(string name = "", Action<AdResult> onComplete = null) {
            var instance = GetInstance();
            var native = GetNative(name);
            if (native == null) {
                instance?.LogError($"Native '{name}' not found");
                onComplete?.Invoke(AdResult.Failed("Native ad not found"));
                return;
            }

            native.Load(onComplete);
        }

        #endregion
    }
}
