using System;
using UnityEngine;
using CustomAttributes;
#if ADMOB_DEPENDENCIES_INSTALLED
using GoogleMobileAds.Api;
#endif
public class AdMobAppOpenController : MonoBehaviour {
    [Header("Ad Unit Ids")]
    [SerializeField] private bool useTestIds = false;
    [SerializeField, HideIf(nameof(useTestIds))] private string androidAdUnitId = "ca-app-pub-3940256099942544/9257395921";
    [SerializeField, HideIf(nameof(useTestIds))] private string iOSAdUnitId = "ca-app-pub-3940256099942544/5575463023";
    [Header("Settings")]
    [SerializeField] private bool debugLogs = false;
    void Log(object message) {
        if (debugLogs) Debug.Log($"{gameObject.name} {message}");
    }
    void LogWarning(object message) {
        Debug.LogWarning($"{gameObject.name} {message}");
    }
    void LogError(object message) {
        Debug.LogError($"{gameObject.name} {message}");
    }
    string AppOpenID {
        get {
            if (AdMobAdsController.IsAndroid)
                return useTestIds ? "ca-app-pub-3940256099942544/9257395921" : androidAdUnitId;
            else if (AdMobAdsController.IsIos)
                return useTestIds ? "ca-app-pub-3940256099942544/5575463023" : iOSAdUnitId;
            else
                return null;
        }
    }
#if ADMOB_DEPENDENCIES_INSTALLED
    private AppOpenAd _ad;
    public bool IsAdReady => _ad != null;
    private readonly TimeSpan TIMEOUT = TimeSpan.FromHours(4);
    private DateTime _expireTime;
    private Action onOpen;
    private Action onClose;
    private Action onClicked;
    private Action onImpression;
    private Action<AdValue> onPaid;
    private Action<AdError> onFailed;

    public void Init() {
    }
    /*private void OnAppStateChanged(AppState state) {
        Log("App State changed to : " + state);
        if (state == AppState.Foreground) {
            Show(null, null, null, null, null, null);
        }
    }
    void Awake() {
        AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
    }
    void OnDestroy() {
        AppStateEventNotifier.AppStateChanged -= OnAppStateChanged;
    }*/
    public void Destroy() {
        _ad?.Destroy();
        _ad = null;
        Log("Destroy()");
    }
    public void Load(Action onLoaded, Action<LoadAdError> onLoadFailed) {
        if (_ad != null) {
            Destroy();
        }
        Log("Loading app open ad.");
        var adRequest = new AdRequest();
        AppOpenAd.Load(AppOpenID, adRequest, (ad, error) => {
            if (error != null) {
                LogError("App open ad failed to load an ad with error : " + error);
                onLoadFailed?.Invoke(error);
                return;
            }
            if (ad == null) {
                LogError("Unexpected error: App open ad load event fired with " + " null ad and null error.");
                onLoadFailed?.Invoke(null);
                return;
            }
            Log("App open ad loaded with response : " + ad.GetResponseInfo());
            _ad = ad;
            _expireTime = DateTime.Now + TIMEOUT;
            RegisterEventHandlers(ad);
            onLoaded?.Invoke();
        });
    }
    public void LogResponseInfo() {
        if (_ad != null) {
            var responseInfo = _ad.GetResponseInfo();
            UnityEngine.Debug.Log(responseInfo);
        }
    }
    private void RegisterEventHandlers(AppOpenAd ad) {
        ad.OnAdPaid += adValue => {
            Log(string.Format("App open ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
            onPaid?.Invoke(adValue);
        };
        ad.OnAdImpressionRecorded += () => {
            Log("App open ad recorded an impression.");
            onImpression?.Invoke();
        };
        ad.OnAdClicked += () => {
            Log("App open ad was clicked.");
            onClicked?.Invoke();
        };
        ad.OnAdFullScreenContentOpened += () => {
            Log("App open ad full screen content opened.");
            onOpen?.Invoke();
        };
        ad.OnAdFullScreenContentClosed += () => {
            Log("App open ad full screen content closed.");
            onClose?.Invoke();
        };
        ad.OnAdFullScreenContentFailed += error => {
            LogError("App open ad failed to open full screen content with error : " + error);
            onFailed?.Invoke(error);
        };
    }
    public void Show(
        Action<AdError> onFailed,
        Action onOpen,
        Action onClose,
        Action onClicked,
        Action onImpression,
        Action<AdValue> onPaid
    ) {
        this.onOpen = onOpen;
        this.onClose = onClose;
        this.onClicked = onClicked;
        this.onImpression = onImpression;
        this.onPaid = onPaid;
        this.onFailed = onFailed;
        if (_ad != null && _ad.CanShowAd() && DateTime.Now < _expireTime) {
            _ad.Show();
            Log("Ad Shown");
        } else {
            LogError("App open ad is not ready yet.");
            onFailed?.Invoke(null);
        }
    }
#endif
}