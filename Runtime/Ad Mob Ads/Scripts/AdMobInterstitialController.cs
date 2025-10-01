using System;
using CustomAttributes;
#if ADMOB_DEPENDENCIES_INSTALLED
using GoogleMobileAds.Api;
#endif
using UnityEngine;

public class AdMobInterstitialController : MonoBehaviour {
    [Header("Ad Unit IDs")]
    [SerializeField] bool useTestIds = false;
    [SerializeField, HideIf(nameof(useTestIds))] string androidInterstitialID = "ca-app-pub-3940256099942544/1033173712";
    [SerializeField, HideIf(nameof(useTestIds))] string iosInterstitialID = "ca-app-pub-3940256099942544/4411468910";
    [Header("Interstitial Settings")]
    [SerializeField] bool debugLogs = true;
    [SerializeField] float loadCooldown = 5f; // Time in seconds to wait after a failed load
#if ADMOB_DEPENDENCIES_INSTALLED
    private InterstitialAd _interstitialAd;
    private bool isAdLoading = false;
    private float lastLoadAttemptTime = 0f;
    public Action<AdValue> onAdPaidAction;
    public Action onAdImpressionRecordedAction;
    public Action<AdError> onAdFullScreenContentFailedAction;
    public Action onAdFullScreenContentClosedAction;
    public Action onAdFullScreenContentOpenedAction;
    public Action onAdClickedAction;
    public Action onLoadedSuccess;
    public Action<AdError> onLoadedFailed;
    string InterstitialId {
        get {
            if (AdMobAdsController.IsAndroid)
                return useTestIds ? "ca-app-pub-3940256099942544/1033173712" : androidInterstitialID;
            else if (AdMobAdsController.IsIos)
                return useTestIds ? "ca-app-pub-3940256099942544/4411468910" : iosInterstitialID;
            else
                return null;
        }
    }
    public void Load(
        Action onLoadedSuccess = null,
        Action<AdError> onLoadedFailed = null
    ) {
        if (isAdLoading) {
            this.onLoadedSuccess = onLoadedSuccess;
            this.onLoadedFailed = onLoadedFailed;
            if (debugLogs) Debug.Log($"{gameObject.name} Load() - Ad load already in progress.");

            return;
        }

        if (Time.time - lastLoadAttemptTime < loadCooldown) {
            this.onLoadedSuccess = onLoadedSuccess;
            this.onLoadedFailed = onLoadedFailed;
            if (debugLogs) Debug.Log($"{gameObject.name} Load() - Load cooldown active. Time remaining: {loadCooldown - (Time.time - lastLoadAttemptTime):F2} seconds.");
            return;
        }

        if (!IsAdReady) {
            this.onLoadedSuccess = onLoadedSuccess;
            this.onLoadedFailed = onLoadedFailed;
            var adRequest = new AdRequest();
            isAdLoading = true;
            InterstitialAd.Load(InterstitialId, adRequest, AdLoadCallback);
        } else {
            if (debugLogs) Debug.Log($"{gameObject.name} Load() - Ad is already ready.");
            //onLoadedSuccess?.Invoke();
        }
    }
    public void Destroy() {
        if (_interstitialAd != null) {
            Debug.Log($"{gameObject.name} Destroy()");
            UnregisterEvents(_interstitialAd);
            _interstitialAd.Destroy();
            _interstitialAd = null;
        }
    }
    public bool IsAdReady => _interstitialAd != null && _interstitialAd.CanShowAd();

    public bool IsAdLoading { get => isAdLoading; }

    public void Show(
        Action<AdError> onAdFullScreenContentFailedAction = null,
        Action onAdFullScreenContentOpenedAction = null,
        Action onAdFullScreenContentClosedAction = null,
        Action onAdClickedAction = null,
        Action onAdImpressionRecordedAction = null,
        Action<AdValue> onAdPaidAction = null
    ) {
        this.onAdFullScreenContentFailedAction = onAdFullScreenContentFailedAction;
        this.onAdFullScreenContentClosedAction = onAdFullScreenContentClosedAction;
        this.onAdFullScreenContentOpenedAction = onAdFullScreenContentOpenedAction;
        this.onAdClickedAction = onAdClickedAction;
        this.onAdImpressionRecordedAction = onAdImpressionRecordedAction;
        this.onAdPaidAction = onAdPaidAction;
        if (IsAdReady) {
            _interstitialAd.Show();
        } else {
            if (debugLogs) Debug.Log($"{gameObject.name} ShowAd() Ad is not ready");
        }
    }
    public void LogResponseInfo() {
        if (_interstitialAd != null) {
            var responseInfo = _interstitialAd.GetResponseInfo();
            Debug.Log(responseInfo);
        }
    }
    private void AdLoadCallback(InterstitialAd ad, LoadAdError error) {
        isAdLoading = false;
        if (error != null) {
            if (debugLogs) Debug.Log($"{gameObject.name} AdLoadCallback() {error}");
            onLoadedFailed?.Invoke(error);
            lastLoadAttemptTime = Time.time;
            return;
        }
        if (ad == null) {
            string errorMessage = $"{gameObject.name} AdLoadCallback() Unexpected error: Interstitial load event fired with null ad and null error.";
            if (debugLogs) Debug.Log(errorMessage);
            onLoadedFailed?.Invoke(null);
            lastLoadAttemptTime = Time.time;
            return;
        }
        if (debugLogs) Debug.Log($"{gameObject.name} AdLoadCallback() Loaded with Response: {ad.GetResponseInfo()}");
        _interstitialAd = ad;
        onLoadedSuccess?.Invoke();
        RegisterEvents(ad);
    }

    private void RegisterEvents(InterstitialAd ad) {
        ad.OnAdClicked += OnAdClicked;
        ad.OnAdFullScreenContentOpened += OnAdFullScreenContentOpened;
        ad.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
        ad.OnAdFullScreenContentFailed += OnAdFullScreenContentFailed;
        ad.OnAdImpressionRecorded += OnAdImpressionRecorded;
        ad.OnAdPaid += OnAdPaid;
    }

    private void UnregisterEvents(InterstitialAd ad) {
        ad.OnAdClicked -= OnAdClicked;
        ad.OnAdFullScreenContentOpened -= OnAdFullScreenContentOpened;
        ad.OnAdFullScreenContentClosed -= OnAdFullScreenContentClosed;
        ad.OnAdFullScreenContentFailed -= OnAdFullScreenContentFailed;
        ad.OnAdImpressionRecorded -= OnAdImpressionRecorded;
        ad.OnAdPaid -= OnAdPaid;
    }

    private void OnAdPaid(AdValue value) {
        if (debugLogs) Debug.Log($"{gameObject.name} OnAdPaid({value.Value} {value.CurrencyCode})");
        onAdPaidAction?.Invoke(value);
    }

    private void OnAdImpressionRecorded() {
        if (debugLogs) Debug.Log($"{gameObject.name} OnAdImpressionRecorded()");
        onAdImpressionRecordedAction?.Invoke();
    }

    private void OnAdFullScreenContentFailed(AdError error) {
        if (debugLogs) Debug.Log($"{gameObject.name} OnAdFullScreenContentFailed({error})");
        onAdFullScreenContentFailedAction?.Invoke(error);
    }

    private void OnAdFullScreenContentClosed() {
        if (debugLogs) Debug.Log($"{gameObject.name} OnAdFullScreenContentClosed()");
        onAdFullScreenContentClosedAction?.Invoke();
    }

    private void OnAdFullScreenContentOpened() {
        if (debugLogs) Debug.Log($"{gameObject.name} OnAdFullScreenContentOpened()");
        onAdFullScreenContentOpenedAction?.Invoke();
    }

    private void OnAdClicked() {
        if (debugLogs) Debug.Log($"{gameObject.name} OnAdClicked()");
        onAdClickedAction?.Invoke();
    }
#endif
}