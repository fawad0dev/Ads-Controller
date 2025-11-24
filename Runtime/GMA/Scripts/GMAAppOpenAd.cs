using System;
using UnityEngine;
using CustomAds.Core;

#if GMA_DEPENDENCIES_INSTALLED
using GoogleMobileAds.Api;
#endif

namespace CustomAds.GMA {
    /// <summary>
    /// Google Mobile Ads app open ad implementation
    /// </summary>
    public class GMAAppOpenAd : MonoBehaviour, IAppOpenAd {
        [Header("Configuration")]
        [SerializeField] private AdConfig config;

        [Header("App Open Settings")]
        [SerializeField] private float adTimeoutHours = 4f;

        // Events
        public event Action OnClicked;
        public event Action OnImpression;
        public event Action<AdRevenue> OnPaid;
        public event Action OnOpened;
        public event Action OnClosed;

        // Properties
        public string AdUnitId => GetAdUnitId();
        private string GetAdUnitId() {
            if (config.useTestIds) {
#if UNITY_ANDROID
                return "ca-app-pub-3940256099942544/9257395921";
#elif UNITY_IOS
                return "ca-app-pub-3940256099942544/5575463023";
#else
                return null;
#endif
            }
            return config.GetAdUnitId();
        }
        public bool IsReady { get; private set; }
        public bool IsLoading { get; private set; }

#if GMA_DEPENDENCIES_INSTALLED
        private AppOpenAd _appOpenAd;
        private DateTime _expireTime;
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

            if (_appOpenAd != null) {
                Destroy();
            }

            Log("Load()");
            IsLoading = true;
            _loadCallback = onComplete;

            var adRequest = new AdRequest();
            AppOpenAd.Load(AdUnitId, adRequest, HandleAdLoaded);
        }

        public void Show(Action<AdResult> onComplete) {
            if (!IsReady) {
                Log("Show() - Ad not ready");
                onComplete?.Invoke(AdResult.Failed("Ad not ready"));
                return;
            }

            if (DateTime.Now >= _expireTime) {
                Log("Show() - Ad expired");
                IsReady = false;
                onComplete?.Invoke(AdResult.Failed("Ad expired"));
                Destroy();
                return;
            }

            Log("Show()");
            _showCallback = onComplete;
            _appOpenAd.Show();
        }

        public void Destroy() {
            if (_appOpenAd == null) return;

            Log("Destroy()");
            _appOpenAd.Destroy();
            _appOpenAd = null;
            IsReady = false;
        }

        public void LogResponseInfo() {
            if (_appOpenAd != null) {
                var info = _appOpenAd.GetResponseInfo();
                Debug.Log($"[{gameObject.name}] {info}");
            }
        }

        // Load callback
        private void HandleAdLoaded(AppOpenAd ad, LoadAdError error) {
            IsLoading = false;

            if (error != null) {
                Log($"Load failed: {error}");
                _loadCallback?.Invoke(AdResult.Failed(error.GetMessage(), error.GetCode()));
                _loadCallback = null;
                return;
            }

            if (ad == null) {
                Log("Load failed: Ad is null");
                _loadCallback?.Invoke(AdResult.Failed("Ad is null"));
                _loadCallback = null;
                return;
            }

            Log("Ad loaded successfully");
            _appOpenAd = ad;
            _expireTime = DateTime.Now.AddHours(adTimeoutHours);
            IsReady = true;
            RegisterEvents(ad);
            _loadCallback?.Invoke(AdResult.Successful());
            _loadCallback = null;
        }

        // Event registration
        private void RegisterEvents(AppOpenAd ad) {
            ad.OnAdClicked += HandleAdClicked;
            ad.OnAdFullScreenContentOpened += HandleAdOpened;
            ad.OnAdFullScreenContentClosed += HandleAdClosed;
            ad.OnAdFullScreenContentFailed += HandleAdFailed;
            ad.OnAdImpressionRecorded += HandleAdImpression;
            ad.OnAdPaid += HandleAdPaid;
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
