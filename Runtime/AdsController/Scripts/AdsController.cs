using System;
using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
#endif
public class AdsController : MonoBehaviour {
#if UNITY_EDITOR
    public static void AddDefine(string define) {
#if UNITY_6000_0_OR_NEWER
        var buildTarget = NamedBuildTarget.FromBuildTargetGroup(
            EditorUserBuildSettings.selectedBuildTargetGroup
        );
        var defines = PlayerSettings.GetScriptingDefineSymbols(buildTarget);
        if (!defines.Contains(define)) {
            PlayerSettings.SetScriptingDefineSymbols(buildTarget, defines + ";" + define);
        }
#else
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
            EditorUserBuildSettings.selectedBuildTargetGroup
        );
        if (!defines.Contains(define))
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup,
                defines + ";" + define
            );
        }
#endif
    }
    public static void RemoveDefine(string define) {
#if UNITY_6000_0_OR_NEWER
        var buildTarget = NamedBuildTarget.FromBuildTargetGroup(
            EditorUserBuildSettings.selectedBuildTargetGroup
        );
        var defines = PlayerSettings.GetScriptingDefineSymbols(buildTarget);
        if (defines.Contains(define)) {
            PlayerSettings.SetScriptingDefineSymbols(
                buildTarget,
                defines.Replace(";" + define, "").Replace(define + ";", "").Replace(define, "")
            );
            PlayerSettings.SetScriptingDefineSymbols(buildTarget, defines);
        }
#else
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
            EditorUserBuildSettings.selectedBuildTargetGroup
        );
        if (defines.Contains(define))
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup,
                defines.Replace(";" + define, "").Replace(define + ";", "").Replace(define, "")
            );
        }
#endif
    }
    public static void AddScopeRegistry(string name, string url, string scope) {
        string manifestPath = Path.Combine(UnityEngine.Application.dataPath, "..", "Packages", "manifest.json");
        if (!File.Exists(manifestPath)) {
            Debug.LogError("manifest.json not found at: " + manifestPath);
            return;
        }
        try {
            string manifestContent = File.ReadAllText(manifestPath);
            if (manifestContent.Contains($"\"name\": \"{name}\"") &&
                manifestContent.Contains($"\"url\": \"{url}\"") &&
                manifestContent.Contains($"\"{scope}\"")) {
                Debug.Log($"{name} package registry already exists in manifest.json");
                return;
            }
            if (!manifestContent.Contains("\"scopedRegistries\"")) {
                int lastBraceIndex = manifestContent.LastIndexOf('}');
                string registryEntry = $",\n  \"scopedRegistries\": [\n    {{\n      \"name\": \"{name}\",\n      \"url\": \"{url}\",\n      \"scopes\": [\n        \"{scope}\"\n      ]\n    }}\n  ]\n";
                manifestContent = manifestContent.Insert(lastBraceIndex, registryEntry);
            } else {
                string registryEntry = $"    {{\n      \"name\": \"{name}\",\n      \"url\": \"{url}\",\n      \"scopes\": [\n        \"{scope}\"\n      ]\n    }},\n";
                int registriesIndex = manifestContent.IndexOf("\"scopedRegistries\": [") + "\"scopedRegistries\": [".Length;
                manifestContent = manifestContent.Insert(registriesIndex + 1, "\n" + registryEntry);
            }
            File.WriteAllText(manifestPath, manifestContent);
            AssetDatabase.Refresh();
            Debug.Log($"{name} package registry added to manifest.json successfully!");
        } catch (System.Exception e) {
            Debug.LogError($"Failed to add {name} registry to manifest.json: " + e.Message);
        }
    }
    public static void AddPackageByName(string packageName) {
        UnityEditor.PackageManager.Client.Add(packageName);
    }
    [System.Serializable]
    public class ManifestData {
        public System.Collections.Generic.Dictionary<string, string> dependencies;
    }
#endif
#if GMA_DEPENDENCIES_INSTALLED
    [SerializeField] GMA_AdsController adMobAdsController;
#endif
    [SerializeField] private DomainConnectivityManager adMobDomain;
    [SerializeField] bool preLoadBannerAd;
    [SerializeField] bool preLoadInterstitialAd;
    [SerializeField] bool preLoadRewardedAd;
    [SerializeField] bool preLoadNativeAd;
    [SerializeField] WaitingScreen waitingScreen;
    [SerializeField] private bool debugLogs;
    void Log(string message) {
        if (debugLogs) Debug.Log("[AdsController] " + message);
    }
    static AdsController instance;
    private bool isAdMobInitialized;
    public static class AdErrors {
        public const string INVALID_BANNER = "Invalid Banner Ad Controller";
        public const string INVALID_INTERSTITIAL = "Invalid Interstitial Ad Controller";
        public const string INVALID_REWARDED = "Invalid Rewarded Ad Controller";
        public const string ADMOB_NOT_ASSIGNED = "AdMobAdsController is not assigned";
        public const string INTERSTITIAL_LOADING = "AdMob interstitial is already loading";
        public const string REWARDED_LOADING = "AdMob rewarded is already loading";
        public const string INTERNET_CONNECTION = "Make sure you have internet connection and try again";
        public const string INVALID_NATIVE_OVERLAY = "Invalid Native Ad Overlay Controller";
        public const string INVALID_APPOPEN = "Invalid App Open Ad Controller";
    }
    static AdsController Instance {
        get {
            if (instance == null) {
                instance = FindAnyObjectByType<AdsController>();
            }
            return instance;
        }
    }
    private void Awake() {
        if (instance == null) {
            instance = this;
        } else {
            Destroy(gameObject);
        }
    }
    public event Action OnAllAdsInitialized;
    public event Action<string> OnAdError;
    object[] BannerAds {
        get {
            object[] ads = null;
#if GMA_DEPENDENCIES_INSTALLED
            ads = adMobAdsController.BannerControllers;
#endif
            return ads;
        }
    }
    object[] InterstitialAds {
        get {
            object[] ads = null;
#if GMA_DEPENDENCIES_INSTALLED
            ads = adMobAdsController.InterstitialControllers;
#endif
            return ads;
        }
    }
    object[] RewardedAds {
        get {
            object[] ads = null;
#if GMA_DEPENDENCIES_INSTALLED
            ads = adMobAdsController.RewardedControllers;
#endif
            return ads;
        }
    }
    object[] NativeAds {
        get {
            object[] ads = null;
#if GMA_DEPENDENCIES_INSTALLED
            ads = adMobAdsController.NativeControllers;
#endif
            return ads;
        }
    }
    object[] AppOpenAds {
        get {
            object[] ads = null;
#if GMA_DEPENDENCIES_INSTALLED
            ads = adMobAdsController.AppOpenControllers;
#endif
            return ads;
        }
    }

    public static bool IsReady => Instance != null;

    IEnumerator Start() {
        bool initializationAttempted = false;
        while (!adMobDomain.IsOnline) {
            if (!initializationAttempted)
                Log("Waiting for internet connection to initialize ads...");
            initializationAttempted = true;
            yield return new WaitForSeconds(1f);
        }
        if (!isAdMobInitialized) {
            InitializeAds();
        }
    }
    private void InitializeAds() {
#if GMA_DEPENDENCIES_INSTALLED
        if (adMobAdsController != null) {
            adMobAdsController.AdsStart(() => {
                InitAdMobAds();
            });
        } else {
            var error = AdErrors.ADMOB_NOT_ASSIGNED;
            Log(error);
            OnAdError?.Invoke(error);
        }
#endif
    }
#if GMA_DEPENDENCIES_INSTALLED
    private void InitAdMobAds() {
        try {
            adMobAdsController.SetApplicationMuted(false);
            adMobAdsController.SetApplicationVolume(1);
            adMobAdsController.InitControllers();
            isAdMobInitialized = true;
            Log("AdMob ads initialized successfully.");
            adMobDomain.onDomainConnectivityLost.AddListener(DestroyAdMobAds);
            adMobDomain.onDomainConnected.RemoveListener(InitAdMobAds);
            PreloadAds();
        } catch (Exception ex) {
            isAdMobInitialized = false;
            Log($"Error initializing AdMob ads: {ex.Message}");
            OnAdError?.Invoke($"AdMob init error: {ex.Message}");
        }
    }
    private void DestroyAdMobAds() {
        isAdMobInitialized = false;
        adMobAdsController.DestroyControllers();
        adMobDomain.onDomainConnectivityLost.RemoveListener(DestroyAdMobAds);
        adMobDomain.onDomainConnected.AddListener(InitAdMobAds);
    }
#endif
    private void PreloadAds() {
        StartCoroutine(PreloadAdsSequentially());
    }
    private IEnumerator PreloadAdsSequentially() {
        if (preLoadBannerAd) {
            LoadBanner_();
            yield return new WaitForSeconds(0.5f);
        }
        if (preLoadInterstitialAd) {
            LoadInterstitial_();
            yield return new WaitForSeconds(0.5f);
        }
        if (preLoadRewardedAd) {
            LoadRewarded_();
            yield return new WaitForSeconds(0.5f);
        }
        if (preLoadNativeAd) {
            LoadNative_();
            yield return new WaitForSeconds(0.5f);
        }
        if (preLoadAppOpenAd) {
            LoadAppOpen_();
            yield return new WaitForSeconds(0.5f);
        }
        OnAllAdsInitialized?.Invoke();
    }
    #region Banner
    public static void LoadBanner(int index = 0, Action onLoaded = null, Action<string> onLoadFailed = null) {
        Instance.LoadBanner_(index, onLoaded, onLoadFailed);
    }
    public static void ShowBanner(int index = 0, Action<string> onFailed = null, Action onClicked = null, Action onImpression = null, Action<long> onPaid = null) {
        Instance.ShowBanner_(index, onFailed, onClicked, onImpression, onPaid);
    }
    public static void HideBanner(int index = 0) {
        Instance.HideBanner_(index);
    }
    void LoadBanner_(int index = 0, Action onLoaded = null, Action<string> onLoadFailed = null) {
        if (!ValidateController(index, BannerAds, nameof(BannerAds))) {
            onLoadFailed?.Invoke(AdErrors.INVALID_BANNER);
            return;
        }
        switch (BannerAds[index]) {
#if GMA_DEPENDENCIES_INSTALLED
            case GMA_BannerController bannerController:
                bannerController.Load(
                    () => {
                        onLoaded?.Invoke();
                    },
                    (error) => {
                        string errorMsg = $"AdMob banner load failed: {error.GetMessage()}";
                        Log(errorMsg);
                        onLoadFailed?.Invoke(errorMsg);
                        OnAdError?.Invoke(errorMsg);
                    }
                );
                break;
#endif
            default:
                onLoadFailed?.Invoke(AdErrors.INVALID_BANNER);
                break;
        }
    }
    void ShowBanner_(int index = 0, Action<string> onFailed = null, Action onClicked = null, Action onImpression = null, Action<long> onPaid = null) {
        if (!ValidateController(index, BannerAds, nameof(BannerAds))) {
            onFailed?.Invoke(AdErrors.INVALID_BANNER);
            return;
        }
        switch (BannerAds[index]) {
#if GMA_DEPENDENCIES_INSTALLED
            case GMA_BannerController bannerController:
                bannerController.Show(
                    null, null,
                    () => {
                        onClicked?.Invoke();
                    },
                    () => {
                        onImpression?.Invoke();
                    },
                    (value) => {
                        onPaid?.Invoke(value.Value);
                    }
                );
                break;
#endif
            default:
                onFailed?.Invoke(AdErrors.INVALID_BANNER);
                break;
        }
    }
    void HideBanner_(int index = 0) {
        if (!ValidateController(index, BannerAds, nameof(BannerAds))) {
            return;
        }
        switch (BannerAds[index]) {
#if GMA_DEPENDENCIES_INSTALLED
            case GMA_BannerController bannerController:
                bannerController.Hide();
                break;
#endif
            default:
                break;
        }
    }
    #endregion
    #region NativeOverlay
    public static void LoadNative(int index = 0, Action onLoaded = null, Action<string> onLoadFailed = null) {
        Instance.LoadNative_(index, onLoaded, onLoadFailed);
    }
    public static bool IsNativeReady(int index = 0) {
        return Instance.IsNativeReady_(index);
    }
    public static void ShowNative(int nativeIndex = 0, Action displayedEventAction = null, Action<string> displayFailedEventAction = null) {
        Instance.ShowNative_(nativeIndex, displayedEventAction, displayFailedEventAction);
    }
    public static void HideNative(int nativeIndex = 0, Action closedEventAction = null) {
        Instance.HideNative_(nativeIndex, closedEventAction);
    }
    public static void DestroyNativeAd(int nativeIndex = 0) {
        Instance.DestroyNativeAd_(nativeIndex);
    }
    public static void RenderNative(int nativeIndex = 0) {
        Instance.RenderNative_(nativeIndex);
    }
    void LoadNative_(int index = 0, Action onLoaded = null, Action<string> onLoadFailed = null) {
        if (!ValidateController(index, NativeAds, nameof(NativeAds))) {
            onLoadFailed?.Invoke(AdErrors.INVALID_NATIVE_OVERLAY);
            return;
        }
        switch (NativeAds[index]) {
#if GMA_DEPENDENCIES_INSTALLED
            case GMA_NativeController nativeController:
                nativeController.Load(
                    (ad) => {
                        onLoaded?.Invoke();
                    },
                    (ad, error) => {
                        string errorMsg = $"AdMob native load failed: {error.GetMessage()}";
                        Log(errorMsg);
                        onLoadFailed?.Invoke(errorMsg);
                        OnAdError?.Invoke(errorMsg);
                    }
                );
                break;
#endif
            default:
                onLoadFailed?.Invoke(AdErrors.INVALID_NATIVE_OVERLAY);
                break;
        }
    }
    bool IsNativeReady_(int index = 0) {
        if (!ValidateController(index, NativeAds, nameof(NativeAds))) {
            return false;
        }
        switch (NativeAds[index]) {
#if GMA_DEPENDENCIES_INSTALLED
            case GMA_NativeController nativeController:
                return nativeController.IsAdReady();
#endif
            default:
                return false;
        }
    }
    void ShowNative_(int nativeIndex = 0, Action displayedEventAction = null, Action<string> displayFailedEventAction = null) {
        if (!ValidateController(nativeIndex, NativeAds, nameof(NativeAds))) {
            displayFailedEventAction?.Invoke(AdErrors.INVALID_NATIVE_OVERLAY);
            return;
        }
        switch (NativeAds[nativeIndex]) {
#if GMA_DEPENDENCIES_INSTALLED
            case GMA_NativeController nativeController:
                if (nativeController.IsAdReady()) {
                    nativeController.Show();
                    displayedEventAction?.Invoke();
                } else {
                    displayFailedEventAction?.Invoke(AdErrors.INVALID_NATIVE_OVERLAY);
                }
                break;
#endif
            default:
                displayFailedEventAction?.Invoke(AdErrors.INVALID_NATIVE_OVERLAY);
                break;
        }
    }
    void HideNative_(int nativeIndex = 0, Action closedEventAction = null) {
        if (!ValidateController(nativeIndex, NativeAds, nameof(NativeAds))) {
            return;
        }
        switch (NativeAds[nativeIndex]) {
#if GMA_DEPENDENCIES_INSTALLED
            case GMA_NativeController nativeController:
                nativeController.Hide();
                closedEventAction?.Invoke();
                break;
#endif
            default:
                break;
        }
    }
    void DestroyNativeAd_(int nativeIndex = 0) {
        if (!ValidateController(nativeIndex, NativeAds, nameof(NativeAds))) {
            return;
        }
        switch (NativeAds[nativeIndex]) {
#if GMA_DEPENDENCIES_INSTALLED
            case GMA_NativeController nativeController:
                nativeController.Destroy();
                break;
#endif
            default:
                break;
        }
    }
    void RenderNative_(int nativeIndex = 0) {
        if (!ValidateController(nativeIndex, NativeAds, nameof(NativeAds))) {
            return;
        }
        switch (NativeAds[nativeIndex]) {
#if GMA_DEPENDENCIES_INSTALLED
            case GMA_NativeController nativeController:
                nativeController.Render();
                break;
#endif
            default:
                break;
        }
    }
    #endregion
    #region Interstitial
    public static void LoadInterstitial(int index = 0, Action onLoaded = null, Action<string> onLoadFailed = null) {
        Instance.LoadInterstitial_(index, onLoaded, onLoadFailed);
    }
    public static bool IsInterstitialReady(int index = 0) {
        return Instance.IsInterstitialReady_(index);
    }
    public static void ShowInterstitial(int index = 0, Action<string> onFailed = null, Action onOpen = null, Action onClose = null, Action onClicked = null, Action onImpression = null, Action<long> onPaid = null) {
        Instance.ShowInterstitial_(index, onFailed, onOpen, onClose, onClicked, onImpression, onPaid);
    }
    public static void ShowInterstitial(bool autoLoadIfNotLoaded, bool autoLoadIfClosed = true, bool autoLoadIfLoadFailed = true, bool showWaitingScreen = true, int index = 0, Action onLoaded = null, Action<string> onLoadFailed = null, Action<string> onFailed = null, Action onOpen = null, Action onClose = null, Action onClicked = null, Action onImpression = null, Action<long> onPaid = null) {
        Instance.ShowInterstitial_(autoLoadIfNotLoaded, autoLoadIfClosed, autoLoadIfLoadFailed, showWaitingScreen, index, onLoaded, onLoadFailed, onFailed, onOpen, onClose, onClicked, onImpression, onPaid);
    }
    void LoadInterstitial_(int index = 0, Action onLoaded = null, Action<string> onLoadFailed = null) {
        if (!ValidateController(index, InterstitialAds, nameof(InterstitialAds))) {
            onLoadFailed?.Invoke(AdErrors.INVALID_INTERSTITIAL);
            Log(AdErrors.INVALID_INTERSTITIAL);
            return;
        }
        switch (InterstitialAds[index]) {
#if GMA_DEPENDENCIES_INSTALLED
            case GMA_InterstitialController interstitialController:
                interstitialController.Load(
                    () => {
                        onLoaded?.Invoke();
                    },
                    (error) => {
                        string errorMsg = $"AdMob interstitial load failed: {error.GetMessage()}";
                        Log(errorMsg);
                        onLoadFailed?.Invoke(errorMsg);
                        OnAdError?.Invoke(errorMsg);
                    }
                );
                break;
#endif
            default:
                onLoadFailed?.Invoke(AdErrors.INVALID_INTERSTITIAL);
                break;
        }
    }
    bool IsInterstitialReady_(int index = 0) {
        if (!ValidateController(index, InterstitialAds, nameof(InterstitialAds))) {
            return false;
        }
        switch (InterstitialAds[index]) {
#if GMA_DEPENDENCIES_INSTALLED
            case GMA_InterstitialController interstitialController:
                return interstitialController.IsAdReady;
#endif
            default:
                return false;
        }
    }
    void ShowInterstitial_(int index = 0, Action<string> onFailed = null, Action onOpen = null, Action onClose = null, Action onClicked = null, Action onImpression = null, Action<long> onPaid = null) {
        if (!ValidateController(index, InterstitialAds, nameof(InterstitialAds))) {
            onFailed?.Invoke(AdErrors.INVALID_INTERSTITIAL);
            return;
        }
        switch (InterstitialAds[index]) {
#if GMA_DEPENDENCIES_INSTALLED
            case GMA_InterstitialController interstitialController:
                interstitialController.Show(
                    (error) => {
                        onFailed?.Invoke(error.GetMessage());
                        string errorMsg = $"AdMob interstitial show failed: {error}";
                        Log(errorMsg);
                        OnAdError?.Invoke(errorMsg);
                    },
                    () => {
                        onOpen?.Invoke();
                    },
                    () => {
                        onClose?.Invoke();
                    },
                    () => {
                        onClicked?.Invoke();
                    },
                    () => {
                        onImpression?.Invoke();
                    },
                    (value) => {
                        onPaid?.Invoke(value.Value);
                    }
                );
                break;
#endif
            default:
                onFailed?.Invoke(AdErrors.INVALID_INTERSTITIAL);
                break;
        }
    }
    void ShowInterstitial_(bool autoLoadIfNotLoaded, bool autoLoadIfClosed = true, bool autoLoadIfLoadFailed = true, bool showWaitingScreen = true, int index = 0, Action onLoaded = null, Action<string> onLoadFailed = null, Action<string> onFailed = null, Action onOpen = null, Action onClose = null, Action onClicked = null, Action onImpression = null, Action<long> onPaid = null) {
        if (showWaitingScreen) {
            ShowWaitingScreen_(10, "Loading Ad...");
        }
        if (IsInterstitialReady_(index)) {
            ShowInterstitial_(
                index,
                (err) => {
                    SetWaitingScreen_(false);
                    onFailed?.Invoke(err);
                },
                onOpen,
                () => {
                    SetWaitingScreen_(false);
                    if (autoLoadIfClosed) {
                        LoadInterstitial_(index);
                    }
                    onClose?.Invoke();
                },
                onClicked,
                onImpression,
                onPaid
            );
        } else {
            if (autoLoadIfNotLoaded) {
                LoadInterstitial_(
                    index,
                    () => {
                        onLoaded?.Invoke();
                        ShowInterstitial_(
                            index,
                            (err) => {
                                SetWaitingScreen_(false);
                                onFailed?.Invoke(err);
                            },
                            onOpen,
                            () => {
                                SetWaitingScreen_(false);
                                onClose?.Invoke();
                                if (autoLoadIfClosed) {
                                    LoadInterstitial_(index);
                                }
                            },
                            onClicked,
                            onImpression,
                            onPaid
                        );
                    },
                    (err) => {
                        if (autoLoadIfLoadFailed) {
                            LoadInterstitial_(
                                index,
                                () => {
                                    onLoaded?.Invoke();
                                    ShowInterstitial_(
                                        index,
                                        (err) => {
                                            SetWaitingScreen_(false);
                                            onFailed?.Invoke(err);
                                        },
                                        onOpen,
                                        () => {
                                            SetWaitingScreen_(false);
                                            onClose?.Invoke();
                                            if (autoLoadIfClosed) {
                                                LoadInterstitial_(index);
                                            }
                                        },
                                        onClicked,
                                        onImpression,
                                        onPaid
                                    );
                                },
                                (err) => {
                                    SetWaitingScreen_(false);
                                    onLoadFailed?.Invoke(err);
                                    ToastsManager.ShowToast(AdErrors.INTERNET_CONNECTION);
                                }
                            );
                        } else {
                            SetWaitingScreen_(false);
                            onLoadFailed?.Invoke(err);
                        }
                    }
                );
            } else {
                SetWaitingScreen_(false);
            }
        }
    }
    #endregion
    #region Rewarded
    public class Reward {
        public string Type;
        public double Amount;
        public Reward(string type, double amount) {
            Type = type;
            Amount = amount;
        }
    }
    public static void LoadRewarded(int index = 0, Action onLoaded = null, Action<string> onLoadFailed = null) {
        Instance.LoadRewarded_(index, onLoaded, onLoadFailed);
    }
    public static bool IsRewardedReady(int index = 0) {
        return Instance.IsRewardedReady_(index);
    }
    public static void ShowRewarded(int index = 0, Action<Reward> onReward = null, Action<string> onShowFailed = null, Action onOpen = null, Action onClosed = null, Action onClicked = null, Action onImpression = null, Action<long> onPaid = null) {
        Instance.ShowRewarded_(index, onReward, onShowFailed, onOpen, onClosed, onClicked, onImpression, onPaid);
    }
    public static void ShowRewarded(bool autoLoadIfNotLoaded, bool autoLoadIfClosed = true, bool autoLoadIfLoadFailed = true, bool showWaitingScreen = true, int index = 0, Action onLoaded = null, Action<string> onLoadFailed = null, Action<Reward> onReward = null, Action<string> onShowFailed = null, Action onOpen = null, Action onClosed = null, Action onClicked = null, Action onImpression = null, Action<long> onPaid = null) {
        Instance.ShowRewarded_(autoLoadIfNotLoaded, autoLoadIfClosed, autoLoadIfLoadFailed, showWaitingScreen, index, onLoaded, onLoadFailed, onReward, onShowFailed, onOpen, onClosed, onClicked, onImpression, onPaid);
    }
    void LoadRewarded_(int index = 0, Action onLoaded = null, Action<string> onLoadFailed = null) {
        if (!ValidateController(index, RewardedAds, nameof(RewardedAds))) {
            onLoadFailed?.Invoke(AdErrors.INVALID_REWARDED);
            return;
        }
        switch (RewardedAds[index]) {
#if GMA_DEPENDENCIES_INSTALLED
            case GMA_RewardedController rewardedController:
                rewardedController.Load(
                    () => {
                        onLoaded?.Invoke();
                    },
                    (error) => {
                        string errorMsg = $"AdMob rewarded load failed: {error.GetMessage()}";
                        Log(errorMsg);
                        onLoadFailed?.Invoke(errorMsg);
                        OnAdError?.Invoke(errorMsg);
                    }
                );
                break;
#endif
            default:
                onLoadFailed?.Invoke(AdErrors.INVALID_REWARDED);
                break;
        }
    }
    bool IsRewardedReady_(int index = 0) {
        if (!ValidateController(index, RewardedAds, nameof(RewardedAds))) {
            return false;
        }
        switch (RewardedAds[index]) {
#if GMA_DEPENDENCIES_INSTALLED
            case GMA_RewardedController rewardedController:
                return rewardedController.IsAdReady;
#endif
            default:
                return false;
        }
    }
    void ShowRewarded_(int index = 0, Action<Reward> onReward = null, Action<string> onShowFailed = null, Action onOpen = null, Action onClosed = null, Action onClicked = null, Action onImpression = null, Action<long> onPaid = null) {
        if (!ValidateController(index, RewardedAds, nameof(RewardedAds))) {
            onShowFailed?.Invoke(AdErrors.INVALID_REWARDED);
            return;
        }
        switch (RewardedAds[index]) {
#if GMA_DEPENDENCIES_INSTALLED
            case GMA_RewardedController rewardedController:
                rewardedController.Show(
                    (reward) => {
                        onReward?.Invoke(new Reward(reward.Type, reward.Amount));
                    },
                    (error) => {
                        string errorMsg = $"AdMob rewarded show failed: {error.GetMessage()}";
                        Log(errorMsg);
                        onShowFailed?.Invoke(errorMsg);
                        OnAdError?.Invoke(errorMsg);
                    },
                    () => {
                        onOpen?.Invoke();
                    },
                    () => {
                        onClosed?.Invoke();
                    },
                    () => {
                        onClicked?.Invoke();
                    },
                    () => {
                        onImpression?.Invoke();
                    },
                    (value) => {
                        onPaid?.Invoke(value.Value);
                    }
                );
                break;
#endif
            default:
                onShowFailed?.Invoke(AdErrors.INVALID_REWARDED);
                break;
        }
    }
    void ShowRewarded_(bool autoLoadIfNotLoaded, bool autoLoadIfClosed = true, bool autoLoadIfLoadFailed = true, bool showWaitingScreen = true, int index = 0, Action onLoaded = null, Action<string> onLoadFailed = null, Action<Reward> onReward = null, Action<string> onShowFailed = null, Action onOpen = null, Action onClosed = null, Action onClicked = null, Action onImpression = null, Action<long> onPaid = null) {
        if (showWaitingScreen) {
            ShowWaitingScreen_(10, "Loading Ad...");
        }
        if (IsRewardedReady_(index)) {
            ShowRewarded_(
                index,
                onReward,
                (err) => {
                    SetWaitingScreen_(false);
                    onShowFailed?.Invoke(err);
                },
                onOpen,
                () => {
                    SetWaitingScreen_(false);
                    if (autoLoadIfClosed) {
                        LoadRewarded_(index);
                    }
                    onClosed?.Invoke();
                },
                onClicked,
                onImpression,
                onPaid
            );
        } else {
            if (autoLoadIfNotLoaded) {
                LoadRewarded_(
                    index,
                    () => {
                        onLoaded?.Invoke();
                        ShowRewarded_(
                            index,
                            onReward,
                            (err) => {
                                SetWaitingScreen_(false);
                                onShowFailed?.Invoke(err);
                            },
                            onOpen,
                            () => {
                                SetWaitingScreen_(false);
                                onClosed?.Invoke();
                                if (autoLoadIfClosed) {
                                    LoadRewarded_(index);
                                }
                            },
                            onClicked,
                            onImpression,
                            onPaid
                        );
                    },
                    (err) => {
                        if (autoLoadIfLoadFailed) {
                            LoadRewarded_(
                                index,
                                () => {
                                    onLoaded?.Invoke();
                                    ShowRewarded_(
                                        index,
                                        onReward,
                                        (err) => {
                                            SetWaitingScreen_(false);
                                            onShowFailed?.Invoke(err);
                                        },
                                        onOpen,
                                        () => {
                                            SetWaitingScreen_(false);
                                            onClosed?.Invoke();
                                            if (autoLoadIfClosed) {
                                                LoadRewarded_(index);
                                            }
                                        },
                                        onClicked,
                                        onImpression,
                                        onPaid
                                    );
                                },
                                (err) => {
                                    SetWaitingScreen_(false);
                                    onLoadFailed?.Invoke(err);
                                    ToastsManager.ShowToast(AdErrors.INTERNET_CONNECTION);
                                }
                            );
                        }
                    }
                );
            }
        }
    }
    #endregion
    #region AppOpen
    [SerializeField] bool preLoadAppOpenAd;
    private Coroutine waitingScreenCoroutine;
    public static void LoadAppOpen(int index = 0, Action onLoaded = null, Action<string> onLoadFailed = null) {
        Instance.LoadAppOpen_(index, onLoaded, onLoadFailed);
    }
    public static bool IsAppOpenReady(int index = 0) {
        return Instance.IsAppOpenReady_(index);
    }
    public static void ShowAppOpen(int index = 0, Action<string> onFailed = null, Action onOpen = null, Action onClose = null, Action onClicked = null, Action onImpression = null, Action<long> onPaid = null) {
        Instance.ShowAppOpen_(index, onFailed, onOpen, onClose, onClicked, onImpression, onPaid);
    }
    void LoadAppOpen_(int index = 0, Action onLoaded = null, Action<string> onLoadFailed = null) {
        if (!ValidateController(index, AppOpenAds, nameof(AppOpenAds))) {
            onLoadFailed?.Invoke(AdErrors.INVALID_APPOPEN);
            return;
        }
        switch (AppOpenAds[index]) {
#if GMA_DEPENDENCIES_INSTALLED
            case GMA_AppOpenController appOpenController:
                appOpenController.Load(
                    () => {
                        onLoaded?.Invoke();
                    },
                    (error) => {
                        string errorMsg = $"AdMob app open load failed: {error.GetMessage()}";
                        Log(errorMsg);
                        onLoadFailed?.Invoke(errorMsg);
                        OnAdError?.Invoke(errorMsg);
                    }
                );
                break;
#endif
            default:
                onLoadFailed?.Invoke(AdErrors.INVALID_APPOPEN);
                break;
        }
    }
    bool IsAppOpenReady_(int index = 0) {
        if (!ValidateController(index, AppOpenAds, nameof(AppOpenAds))) {
            return false;
        }
        switch (AppOpenAds[index]) {
#if GMA_DEPENDENCIES_INSTALLED
            case GMA_AppOpenController appOpenController:
                return appOpenController.IsAdReady;
#endif
            default:
                return false;
        }
    }
    void ShowAppOpen_(int index = 0, Action<string> onFailed = null, Action onOpen = null, Action onClose = null, Action onClicked = null, Action onImpression = null, Action<long> onPaid = null) {
        if (!ValidateController(index, AppOpenAds, nameof(AppOpenAds))) {
            onFailed?.Invoke(AdErrors.INVALID_APPOPEN);
            return;
        }
        switch (AppOpenAds[index]) {
#if GMA_DEPENDENCIES_INSTALLED
            case GMA_AppOpenController appOpenController:
                appOpenController.Show(
                    (error) => {
                        string errorMsg = $"AdMob app open show failed: {error.GetMessage()}";
                        onFailed?.Invoke(errorMsg);
                        Log(errorMsg);
                        OnAdError?.Invoke(errorMsg);
                    },
                    () => { onOpen?.Invoke(); },
                    () => { onClose?.Invoke(); },
                    () => { onClicked?.Invoke(); },
                    () => { onImpression?.Invoke(); },
                    (value) => { onPaid?.Invoke(value.Value); }
                );
                break;
#endif
            default:
                onFailed?.Invoke(AdErrors.INVALID_APPOPEN);
                break;
        }
    }
    #endregion
    private void ValidateAllControllers() {
#if GMA_DEPENDENCIES_INSTALLED
        adMobAdsController.ValidateControllers();
#endif
    }
    private bool ValidateController(int index, object[] controllers, string adType) {
        if (controllers == null || controllers.Length == 0) {
            Log($"No {adType} controllers found");
            return false;
        }
        if (index < 0 || index >= controllers.Length) {
            Log($"Invalid {adType} index: {index}");
            return false;
        }
        bool isInit = false;
        switch (controllers[index]) {
#if GMA_DEPENDENCIES_INSTALLED
            case GMA_RewardedController or GMA_InterstitialController or GMA_NativeController or GMA_BannerController or GMA_AppOpenController:
                isInit = isAdMobInitialized;
                break;
#endif
            default:
                Log($"Invalid {adType} controller at index: {index} it is of type {controllers[index].GetType().Name}");
                break;
        }
        return isInit;
    }
    public static void SetWaitingScreen(bool show, string waitingText = "") {
        Instance.SetWaitingScreen_(show, waitingText);
    }
    public static void ShowWaitingScreen(float displayTime, string waitingText = "Loading...") {
        Instance.ShowWaitingScreen_(displayTime, waitingText);
    }
    public static void ShowWaitingScreen(Func<bool> hideCondition, string waitingText = "Loading...") {
        Instance.ShowWaitingScreen_(hideCondition, waitingText);
    }
    void SetWaitingScreen_(bool show, string waitingText = "") {
        waitingScreen.Show(show, waitingText);
        if (!show && waitingScreenCoroutine != null) {
            StopCoroutine(waitingScreenCoroutine);
            waitingScreenCoroutine = null;
        }
    }
    void ShowWaitingScreen_(float displayTime, string waitingText = "Loading...") {
        SetWaitingScreen_(true, waitingText);
        waitingScreenCoroutine = StartCoroutine(ExecuteAfter(displayTime, () => SetWaitingScreen_(false)));
    }
    void ShowWaitingScreen_(Func<bool> hideCondition, string waitingText = "Loading...") {
        SetWaitingScreen_(true, waitingText);
        waitingScreenCoroutine = StartCoroutine(ExecuteAfter(hideCondition, () => SetWaitingScreen_(false)));
    }
    IEnumerator ExecuteAfter(float seconds, Action action, bool realTime = true) {
        yield return realTime ? new WaitForSecondsRealtime(seconds) : new WaitForSeconds(seconds);
        action?.Invoke();
    }
    IEnumerator ExecuteAfter(Func<bool> condition, Action action) {
        while (!condition())
            yield return null;
        action?.Invoke();
    }
}
