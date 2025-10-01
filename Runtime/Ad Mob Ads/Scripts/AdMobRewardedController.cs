using System;
using CustomAttributes;

#if ADMOB_DEPENDENCIES_INSTALLED
using GoogleMobileAds.Api;
#endif
using UnityEngine;

public class AdMobRewardedController : MonoBehaviour {
    [Header("Ad Unit IDs")]
    [SerializeField] bool useTestIds = false;
    [SerializeField, HideIf(nameof(useTestIds))] string androidRewardedAdId = "ca-app-pub-3940256099942544/5224354917";
    [SerializeField, HideIf(nameof(useTestIds))] string iosRewardedAdId = "ca-app-pub-3940256099942544/1712485313";
    [Header("Rewarded Ad Settings")]
    [SerializeField] bool debugLogs;
    [SerializeField] float loadCooldown = 5f;
#if ADMOB_DEPENDENCIES_INSTALLED
    public Action<Reward> userRewardEarnedCallback;
    public Action<AdValue> onAdPaidAction;
    public Action onImpressionRecordedAction;
    public Action<AdError> onAdFullScreenContentFailedAction;
    public Action onAdFullScreenContentOpenedAction;
    public Action onAdFullScreenContentClosedAction;
    public Action onAdClickedAction;
    public Action onAdLoadedAction;
    public Action<LoadAdError> onAdFailedToLoadAction;
    bool IsAndroid => AdMobAdsController.IsAndroid;
    bool IsIos => AdMobAdsController.IsIos;
    public string RewardedAdId {
        get {
            if (IsAndroid)
                return useTestIds ? "ca-app-pub-3940256099942544/5224354917" : androidRewardedAdId;
            else if (IsIos)
                return useTestIds ? "ca-app-pub-3940256099942544/1712485313" : iosRewardedAdId;
            else
                return null;
        }
    }
    private RewardedAd _rewardedAd;
    private bool isAdLoading = false;
    private float lastLoadAttemptTime = 0f;
    void Log(object message) {
        if (debugLogs) Debug.Log($"{gameObject.name} {message}");
    }
    public void Load(
        Action onAdLoadedAction = null,
        Action<LoadAdError> onAdFailedToLoadAction = null
    ) {
        if (isAdLoading) {
            this.onAdLoadedAction = onAdLoadedAction;
            this.onAdFailedToLoadAction = onAdFailedToLoadAction;
            Log("Load() - Ad load already in progress.");
            return;
        }

        if (Time.time - lastLoadAttemptTime < loadCooldown) {
            this.onAdLoadedAction = onAdLoadedAction;
            this.onAdFailedToLoadAction = onAdFailedToLoadAction;
            Log($"Load() - Load cooldown active. Time remaining: {loadCooldown - (Time.time - lastLoadAttemptTime):F2} seconds.");
            return;
        }

        Log($"Load() IsAdReady: {IsAdReady}");
        if (!IsAdReady) {
            this.onAdLoadedAction = onAdLoadedAction;
            this.onAdFailedToLoadAction = onAdFailedToLoadAction;
            var adRequest = new AdRequest();
            Log($"Load() adRequest: {adRequest} RewardedAdId: {RewardedAdId}");
            isAdLoading = true;
            RewardedAd.Load(RewardedAdId, adRequest, AdLoadCallback);
        } else {
            Log($"Load() - Ad is already ready.");
            //onAdLoadedAction?.Invoke(_rewardedAd);
        }
    }
    private void AdLoadCallback(RewardedAd ad, LoadAdError error) {
        isAdLoading = false;
        if (error != null) {
            Log($"AdLoadCallback() {error}");
            onAdFailedToLoadAction?.Invoke(error);
            lastLoadAttemptTime = Time.time;
            return;
        }
        if (ad == null) {
            Log($"AdLoadCallback() ad is null");
            onAdFailedToLoadAction?.Invoke(null);
            lastLoadAttemptTime = Time.time;
            return;
        }
        _rewardedAd = ad;
        onAdLoadedAction?.Invoke();
        Log($"AdLoadCallback() ad loaded");
        RegisterEvents(ad);
    }
    public void Show(
        Action<Reward> userRewardEarnedCallbackAction = null,
        Action<AdError> onAdFullScreenContentFailedAction = null,
        Action onAdFullScreenContentOpenedAction = null,
        Action onAdFullScreenContentClosedAction = null,
        Action onAdClickedAction = null,
        Action onImpressionRecordedAction = null,
        Action<AdValue> onAdPaidAction = null
    ) {
        this.onAdClickedAction = onAdClickedAction;
        this.onAdFullScreenContentClosedAction = onAdFullScreenContentClosedAction;
        this.onAdFullScreenContentOpenedAction = onAdFullScreenContentOpenedAction;
        this.onAdFullScreenContentFailedAction = onAdFullScreenContentFailedAction;
        this.onImpressionRecordedAction = onImpressionRecordedAction;
        this.onAdPaidAction = onAdPaidAction;
        if (IsAdReady) {
            Log("ShowAd()");
            _rewardedAd.Show(reward => {
                Log($"ShowAd() reward: {reward.Amount} {reward.Type}");
                userRewardEarnedCallbackAction?.Invoke(reward);
                userRewardEarnedCallback?.Invoke(reward);
            });
        } else {
            Log("ShowAd() ad not ready");
        }
    }
    public bool IsAdReady => _rewardedAd != null && _rewardedAd.CanShowAd();

    public bool IsAdLoading { get => isAdLoading; }

    private void RegisterEvents(RewardedAd ad) {
        ad.OnAdClicked += OnAdClicked;
        ad.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
        ad.OnAdFullScreenContentOpened += OnAdFullScreenContentOpened;
        ad.OnAdFullScreenContentFailed += OnAdFullScreenContentFailed;
        ad.OnAdImpressionRecorded += OnImpressionRecorded;
        ad.OnAdPaid += OnAdPaid;
    }
    public void Destroy() {
        if (_rewardedAd != null) {
            Log($"DestroyAd()");
            UnregisterEvents(_rewardedAd);
            _rewardedAd.Destroy();
            _rewardedAd = null;
        }
    }

    private void UnregisterEvents(RewardedAd ad) {
        ad.OnAdClicked -= OnAdClicked;
        ad.OnAdFullScreenContentClosed -= OnAdFullScreenContentClosed;
        ad.OnAdFullScreenContentOpened -= OnAdFullScreenContentOpened;
        ad.OnAdFullScreenContentFailed -= OnAdFullScreenContentFailed;
        ad.OnAdImpressionRecorded -= OnImpressionRecorded;
        ad.OnAdPaid -= OnAdPaid;
    }

    private void OnAdPaid(AdValue value) {
        Log($"OnAdPaid({value})");
        onAdPaidAction?.Invoke(value);
    }
    private void OnImpressionRecorded() {
        Log($"OnImpressionRecorded()");
        onImpressionRecordedAction?.Invoke();
    }
    private void OnAdFullScreenContentFailed(AdError error) {
        Log($"OnAdFullScreenContentFailed({error})");
        onAdFullScreenContentFailedAction?.Invoke(error);
    }
    private void OnAdFullScreenContentOpened() {
        Log($"OnAdFullScreenContentOpened()");
        onAdFullScreenContentOpenedAction?.Invoke();
    }
    private void OnAdFullScreenContentClosed() {
        Log($"OnAdFullScreenContentClosed()");
        onAdFullScreenContentClosedAction?.Invoke();
    }
    private void OnAdClicked() {
        Log($"OnAdClicked()");
        onAdClickedAction?.Invoke();
    }
    public void SetAllEventsEmpty() {
        onAdClickedAction = null;
        onAdFullScreenContentClosedAction = null;
        onAdFullScreenContentOpenedAction = null;
        onAdFullScreenContentFailedAction = null;
        onImpressionRecordedAction = null;
        onAdPaidAction = null;
        onAdLoadedAction = null;
        onAdFailedToLoadAction = null;
    }
#endif
}