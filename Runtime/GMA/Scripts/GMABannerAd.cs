using System;
using UnityEngine;
using CustomAds.Core;

#if GMA_DEPENDENCIES_INSTALLED
using GoogleMobileAds.Api;
#endif

namespace CustomAds.GMA {
    /// <summary>
    /// Google Mobile Ads banner implementation
    /// </summary>
    public class GMABannerAd : MonoBehaviour, IBannerAd {
        [Header("Configuration")]
        [SerializeField] private AdConfig config;

        [Header("Banner Settings")]
        [SerializeField] private BannerSize bannerSize = BannerSize.Banner;
        [SerializeField] private BannerPosition position = BannerPosition.Bottom;
        [SerializeField] private bool collapsible = false;

        public enum BannerSize {
            Banner,
            IABBanner,
            MediumRectangle,
            Leaderboard
        }

        public enum BannerPosition {
            Top,
            Bottom
        }

        // Events
        public event Action OnClicked;
        public event Action OnImpression;
        public event Action<AdRevenue> OnPaid;

        // Properties
        public string AdUnitId => GetAdUnitId();
        private string GetAdUnitId() {
            if (config.useTestIds) {
#if UNITY_ANDROID
                return "ca-app-pub-3940256099942544/6300978111";
#elif UNITY_IOS
                return "ca-app-pub-3940256099942544/2934735716";
#else
                return null;
#endif
            }
            return config.GetAdUnitId();
        }
        public bool IsReady { get; private set; }
        public bool IsLoading { get; private set; }
        public bool IsVisible { get; private set; }

#if GMA_DEPENDENCIES_INSTALLED
        private BannerView _bannerView;
        private bool _initialized;
        
        private void Log(string message) {
            if (config.debugLogs) Debug.Log($"[{gameObject.name}] {message}");
        }
        
        public void Initialize() {
            if (_initialized) return;
            
            Log("Initialize()");
            
            if (string.IsNullOrEmpty(AdUnitId)) {
                Debug.LogError($"[{gameObject.name}] Ad Unit ID is null or empty");
                return;
            }
            
            var adSize = GetGMAAdSize();
            var adPosition = position == BannerPosition.Top ? AdPosition.Top : AdPosition.Bottom;
            
            _bannerView = new BannerView(AdUnitId, adSize, adPosition);
            
            // Register events
            _bannerView.OnBannerAdLoaded += HandleAdLoaded;
            _bannerView.OnBannerAdLoadFailed += HandleAdLoadFailed;
            _bannerView.OnAdPaid += HandleAdPaid;
            _bannerView.OnAdImpressionRecorded += HandleAdImpression;
            _bannerView.OnAdClicked += HandleAdClicked;
            
            _initialized = true;
        }
        
        public void Load(Action<AdResult> onComplete) {
            if (!_initialized) Initialize();
            
            if (IsLoading) {
                Log("Already loading");
                onComplete?.Invoke(AdResult.Failed("Already loading"));
                return;
            }
            
            Log("Load()");
            IsLoading = true;
            _loadCallback = onComplete;
            
            var adRequest = new AdRequest();
            
            if (collapsible) {
                adRequest.Extras.Add("collapsible", position == BannerPosition.Top ? "top" : "bottom");
            }
            
            _bannerView.LoadAd(adRequest);
        }
        
        private Action<AdResult> _loadCallback;
        private Action<AdResult> _showCallback;
        
        public void Show(Action<AdResult> onComplete = null) {
            if (_bannerView == null) {
                Log("Show() - Banner not initialized");
                onComplete?.Invoke(AdResult.Failed("Banner not initialized"));
                return;
            }
            
            Log("Show()");
            _showCallback = onComplete;
            _bannerView.Show();
            IsVisible = true;
            onComplete?.Invoke(AdResult.Successful());
        }
        
        public void Hide() {
            if (_bannerView == null) return;
            
            Log("Hide()");
            _bannerView.Hide();
            IsVisible = false;
        }
        
        public void Destroy() {
            if (_bannerView == null) return;
            
            Log("Destroy()");
            
            _bannerView.OnBannerAdLoaded -= HandleAdLoaded;
            _bannerView.OnBannerAdLoadFailed -= HandleAdLoadFailed;
            _bannerView.OnAdPaid -= HandleAdPaid;
            _bannerView.OnAdImpressionRecorded -= HandleAdImpression;
            _bannerView.OnAdClicked -= HandleAdClicked;
            
            _bannerView.Destroy();
            _bannerView = null;
            _initialized = false;
            IsReady = false;
            IsVisible = false;
        }
        
        public void LogResponseInfo() {
            if (_bannerView != null) {
                var info = _bannerView.GetResponseInfo();
                Debug.Log($"[{gameObject.name}] {info}");
            }
        }
        
        // Event handlers
        private void HandleAdLoaded() {
            Log("Ad loaded successfully");
            IsLoading = false;
            IsReady = true;
            _loadCallback?.Invoke(AdResult.Successful());
            _loadCallback = null;
        }
        
        private void HandleAdLoadFailed(LoadAdError error) {
            Log($"Ad load failed: {error}");
            IsLoading = false;
            IsReady = false;
            _loadCallback?.Invoke(AdResult.Failed(error.GetMessage(), error.GetCode()));
            _loadCallback = null;
        }
        
        private void HandleAdPaid(AdValue value) {
            Log($"Ad paid: {value.Value} {value.CurrencyCode}");
            OnPaid?.Invoke(new AdRevenue {
                Value = value.Value,
                CurrencyCode = value.CurrencyCode,
                Precision = (int)value.Precision
            });
        }
        
        private void HandleAdImpression() {
            Log("Ad impression recorded");
            OnImpression?.Invoke();
        }
        
        private void HandleAdClicked() {
            Log("Ad clicked");
            OnClicked?.Invoke();
        }
        
        private AdSize GetGMAAdSize() {
            return bannerSize switch {
                BannerSize.Banner => AdSize.Banner,
                BannerSize.IABBanner => AdSize.IABBanner,
                BannerSize.MediumRectangle => AdSize.MediumRectangle,
                BannerSize.Leaderboard => AdSize.Leaderboard,
                _ => AdSize.Banner
            };
        }
        
        
#else
        public void Initialize() { }
        public void Load(Action<AdResult> onComplete) => onComplete?.Invoke(AdResult.Failed("GMA not installed"));
        public void Show(Action<AdResult> onComplete = null) => onComplete?.Invoke(AdResult.Failed("GMA not installed"));
        public void Hide() { }
        public void Destroy() { }
        public void LogResponseInfo() { }
#endif

        private void OnDestroy() {
            Destroy();
        }
    }
}
