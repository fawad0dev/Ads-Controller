using System;
using UnityEngine;
using CustomAds.Core;

#if GMA_DEPENDENCIES_INSTALLED
using GoogleMobileAds.Api;
#endif

namespace CustomAds.GMA {
    /// <summary>
    /// Google Mobile Ads rewarded ad implementation
    /// </summary>
    public class GMARewardedAd : MonoBehaviour, IRewardedAd {
        [Header("Configuration")]
        [SerializeField] private AdConfig config;

        // Events
        public event Action<AdReward> OnUserRewarded;
        public event Action OnClicked;
        public event Action OnImpression;
        public event Action<AdRevenue> OnPaid;
        public event Action OnOpened;
        public event Action OnClosed;

        // Properties
        public string AdUnitId => GetAdUnitId();
        public bool IsReady { get; private set; }
        public bool IsLoading { get; private set; }
        private string GetAdUnitId() {
            if (config.useTestIds) {
#if UNITY_ANDROID
                return "ca-app-pub-3940256099942544/5224354917";
#elif UNITY_IOS
                return "ca-app-pub-3940256099942544/1712485313";
#else
                return null;
#endif
            }
            return config.GetAdUnitId();
        }
#if GMA_DEPENDENCIES_INSTALLED
        private RewardedAd _rewardedAd;
        private float _lastLoadAttemptTime;
        private Action<AdResult> _loadCallback;
        private Action<AdResult> _showCallback;

        private void Log(string message) {
            if (config.debugLogs) Debug.Log($"[{gameObject.name}] {message}");
        }

        public void Initialize() {
            Log("Initialize()");
        }

        public void Load(Action<AdResult> onComplete) {
            if (IsLoading) {
                Log("Already loading");
                onComplete?.Invoke(AdResult.Failed("Already loading"));
                return;
            }

            if (Time.time - _lastLoadAttemptTime < config.loadCooldown) {
                var remaining = config.loadCooldown - (Time.time - _lastLoadAttemptTime);
                Log($"Load cooldown active: {remaining:F1}s remaining");
                onComplete?.Invoke(AdResult.Failed($"Cooldown active: {remaining:F1}s"));
                return;
            }

            if (IsReady) {
                Log("Ad already ready");
                onComplete?.Invoke(AdResult.Successful());
                return;
            }

            Log("Load()");
            IsLoading = true;
            _loadCallback = onComplete;

            var adRequest = new AdRequest();
            RewardedAd.Load(AdUnitId, adRequest, HandleAdLoaded);
        }

        public void Show(Action<AdResult> onComplete) {
            if (!IsReady) {
                Log("Show() - Ad not ready");
                onComplete?.Invoke(AdResult.Failed("Ad not ready"));
                return;
            }

            Log("Show()");
            _showCallback = onComplete;
            _rewardedAd.Show(HandleUserRewarded);
        }

        public void Destroy() {
            if (_rewardedAd == null) return;

            Log("Destroy()");
            UnregisterEvents(_rewardedAd);
            _rewardedAd.Destroy();
            _rewardedAd = null;
            IsReady = false;
        }

        public void LogResponseInfo() {
            if (_rewardedAd != null) {
                var info = _rewardedAd.GetResponseInfo();
                Debug.Log($"[{gameObject.name}] {info}");
            }
        }

        // Load callback
        private void HandleAdLoaded(RewardedAd ad, LoadAdError error) {
            IsLoading = false;

            if (error != null) {
                Log($"Load failed: {error}");
                _lastLoadAttemptTime = Time.time;
                _loadCallback?.Invoke(AdResult.Failed(error.GetMessage(), error.GetCode()));
                _loadCallback = null;
                return;
            }

            if (ad == null) {
                Log("Load failed: Ad is null");
                _lastLoadAttemptTime = Time.time;
                _loadCallback?.Invoke(AdResult.Failed("Ad is null"));
                _loadCallback = null;
                return;
            }

            Log("Ad loaded successfully");
            _rewardedAd = ad;
            IsReady = true;
            RegisterEvents(ad);
            _loadCallback?.Invoke(AdResult.Successful());
            _loadCallback = null;
        }

        // Reward callback
        private void HandleUserRewarded(Reward reward) {
            Log($"User rewarded: {reward.Amount} {reward.Type}");
            OnUserRewarded?.Invoke(new AdReward {
                Amount = reward.Amount,
                Type = reward.Type
            });
        }

        // Event registration
        private void RegisterEvents(RewardedAd ad) {
            ad.OnAdClicked += HandleAdClicked;
            ad.OnAdFullScreenContentOpened += HandleAdOpened;
            ad.OnAdFullScreenContentClosed += HandleAdClosed;
            ad.OnAdFullScreenContentFailed += HandleAdFailed;
            ad.OnAdImpressionRecorded += HandleAdImpression;
            ad.OnAdPaid += HandleAdPaid;
        }

        private void UnregisterEvents(RewardedAd ad) {
            ad.OnAdClicked -= HandleAdClicked;
            ad.OnAdFullScreenContentOpened -= HandleAdOpened;
            ad.OnAdFullScreenContentClosed -= HandleAdClosed;
            ad.OnAdFullScreenContentFailed -= HandleAdFailed;
            ad.OnAdImpressionRecorded -= HandleAdImpression;
            ad.OnAdPaid -= HandleAdPaid;
        }

        // Event handlers
        private void HandleAdClicked() {
            Log("Ad clicked");
            OnClicked?.Invoke();
        }

        private void HandleAdOpened() {
            Log("Ad opened");
            OnOpened?.Invoke();
        }

        private void HandleAdClosed() {
            Log("Ad closed");
            IsReady = false;
            OnClosed?.Invoke();
            _showCallback?.Invoke(AdResult.Successful());
            _showCallback = null;

            // Auto-reload
            Load(null);
        }

        private void HandleAdFailed(AdError error) {
            Log($"Ad failed to show: {error}");
            IsReady = false;
            _showCallback?.Invoke(AdResult.Failed(error.GetMessage(), error.GetCode()));
            _showCallback = null;
        }

        private void HandleAdImpression() {
            Log("Ad impression recorded");
            OnImpression?.Invoke();
        }

        private void HandleAdPaid(AdValue value) {
            Log($"Ad paid: {value.Value} {value.CurrencyCode}");
            OnPaid?.Invoke(new AdRevenue {
                Value = value.Value,
                CurrencyCode = value.CurrencyCode,
                Precision = (int)value.Precision
            });
        }

        
#else
        public void Initialize() { }
        public void Load(Action<AdResult> onComplete) => onComplete?.Invoke(AdResult.Failed("GMA not installed"));
        public void Show(Action<AdResult> onComplete) => onComplete?.Invoke(AdResult.Failed("GMA not installed"));
        public void Destroy() { }
        public void LogResponseInfo() { }
#endif

        private void OnDestroy() {
            Destroy();
        }
    }
}
