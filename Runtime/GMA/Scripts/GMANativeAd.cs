using System;
using UnityEngine;
using CustomAds.Core;

#if GMA_DEPENDENCIES_INSTALLED
using GoogleMobileAds.Api;
#endif

namespace CustomAds.GMA {
    /// <summary>
    /// Google Mobile Ads native ad implementation
    /// </summary>
    public class GMANativeAd : MonoBehaviour, INativeAd {
        [Header("Configuration")]
        [SerializeField] private AdConfig config;

        [Header("Native Ad Settings")]
        [SerializeField] private NativeTemplateType templateType = NativeTemplateType.Medium;
        [SerializeField] private NativeSizeType sizeType = NativeSizeType.MediumRectangle;
        [SerializeField] private NativePositionType positionType = NativePositionType.Custom;
        [SerializeField] private Color backgroundColor = Color.white;

        [Header("Custom Position")]
        [SerializeField] private bool useCustomPosition = false;
        [SerializeField] private RectTransform customPositionTransform;

        [Header("Custom Size")]
        [SerializeField] private Vector2Int customSize = new Vector2Int(320, 250);

        [Header("Platform Filter")]
        [SerializeField]
        private RuntimePlatform[] allowedPlatforms = new RuntimePlatform[] {
            RuntimePlatform.Android,
            RuntimePlatform.IPhonePlayer
        };

        public enum NativeTemplateType {
            Small,
            Medium
        }

        public enum NativeSizeType {
            Banner,
            IABBanner,
            MediumRectangle,
            Leaderboard,
            Custom
        }

        public enum NativePositionType {
            Top,
            Bottom,
            Custom
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
                return "ca-app-pub-3940256099942544/2247696110";
#elif UNITY_IOS
                return "ca-app-pub-3940256099942544/3986624511";
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
        private NativeOverlayAd _nativeAd;
        private bool _initialized;
        private Action<AdResult> _loadCallback;
        private Action<AdResult> _showCallback;

        private void Log(string message) {
            if (config.debugLogs) Debug.Log($"[{gameObject.name}] {message}");
        }

        private void LogWarning(string message) {
            Debug.LogWarning($"[{gameObject.name}] {message}");
        }

        public void Initialize() {
            if (_initialized) return;

            if (!IsAllowedPlatform()) {
                LogWarning("Native ads not allowed on this platform");
                return;
            }

            Log("Initialize()");

            if (string.IsNullOrEmpty(AdUnitId)) {
                Debug.LogError($"[{gameObject.name}] Ad Unit ID is null or empty");
                return;
            }

            _initialized = true;
        }

        public void Load(Action<AdResult> onComplete) {
            if (!_initialized) Initialize();

            if (!IsAllowedPlatform()) {
                LogWarning("Platform not allowed");
                onComplete?.Invoke(AdResult.Failed("Platform not allowed"));
                return;
            }

            if (IsLoading) {
                Log("Already loading");
                onComplete?.Invoke(AdResult.Failed("Already loading"));
                return;
            }

            if (_nativeAd != null) {
                Destroy();
            }

            Log("Load()");
            IsLoading = true;
            _loadCallback = onComplete;

            var adRequest = new AdRequest();
            var options = new NativeAdOptions {
                AdChoicesPlacement = AdChoicesPlacement.TopRightCorner,
                MediaAspectRatio = MediaAspectRatio.Any
            };

            NativeOverlayAd.Load(AdUnitId, adRequest, options, HandleAdLoaded);
        }

        public void Show(Action<AdResult> onComplete = null) {
            if (!IsReady) {
                Log("Show() - Ad not ready");
                onComplete?.Invoke(AdResult.Failed("Ad not ready"));
                return;
            }

            Log("Show()");
            _showCallback = onComplete;

            try {
                RenderAd();
                _nativeAd.Show();
                IsVisible = true;
                onComplete?.Invoke(AdResult.Successful());
            } catch (Exception ex) {
                Debug.LogError($"[{gameObject.name}] Failed to show ad: {ex.Message}");
                onComplete?.Invoke(AdResult.Failed(ex.Message));
            }
        }

        public void Hide() {
            if (_nativeAd == null) return;

            Log("Hide()");
            _nativeAd.Hide();
            IsVisible = false;
        }

        public void Destroy() {
            if (_nativeAd == null) return;

            Log("Destroy()");
            _nativeAd.Destroy();
            _nativeAd = null;
            IsReady = false;
            IsVisible = false;
        }

        public void LogResponseInfo() {
            if (_nativeAd != null) {
                var info = _nativeAd.GetResponseInfo();
                Debug.Log($"[{gameObject.name}] {info}");
            }
        }

        // Load callback
        private void HandleAdLoaded(NativeOverlayAd ad, LoadAdError error) {
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
            _nativeAd = ad;
            IsReady = true;
            RegisterEvents(ad);
            _loadCallback?.Invoke(AdResult.Successful());
            _loadCallback = null;
        }

        // Render the native ad
        private void RenderAd() {
            if (_nativeAd == null) return;

            var style = new NativeTemplateStyle {
                TemplateId = templateType == NativeTemplateType.Small ? "small" : "medium",
                MainBackgroundColor = backgroundColor
            };

            var adSize = GetAdSize();

            if (useCustomPosition && customPositionTransform != null) {
                int x = (int)customPositionTransform.anchoredPosition.x;
                int y = -(int)customPositionTransform.anchoredPosition.y;
                _nativeAd.RenderTemplate(style, adSize, x, y);
            } else {
                var position = positionType == NativePositionType.Top ?
                    AdPosition.Top : AdPosition.Bottom;
                _nativeAd.RenderTemplate(style, adSize, position);
            }
        }

        // Event registration
        private void RegisterEvents(NativeOverlayAd ad) {
            ad.OnAdClicked += HandleAdClicked;
            ad.OnAdImpressionRecorded += HandleAdImpression;
            ad.OnAdPaid += HandleAdPaid;
        }

        // Event handlers
        private void HandleAdClicked() {
            Log("Ad clicked");
            OnClicked?.Invoke();
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

        // Helper methods
        private AdSize GetAdSize() {
            return sizeType switch {
                NativeSizeType.Banner => AdSize.Banner,
                NativeSizeType.IABBanner => AdSize.IABBanner,
                NativeSizeType.MediumRectangle => AdSize.MediumRectangle,
                NativeSizeType.Leaderboard => AdSize.Leaderboard,
                NativeSizeType.Custom => new AdSize(customSize.x, customSize.y),
                _ => AdSize.MediumRectangle
            };
        }

        private bool IsAllowedPlatform() {
            foreach (var platform in allowedPlatforms) {
                if (Application.platform == platform)
                    return true;
            }
            return false;
        }

        
#else
        public void Initialize() { }
        public void Load(Action<AdResult> onComplete) => onComplete?.Invoke(AdResult.Failed("GMA not installed"));
        public void Show(Action<AdResult> onComplete = null) => onComplete?.Invoke(AdResult.Failed("GMA not installed"));
        public void Hide() { }
        public void Destroy() { }
        public void LogResponseInfo() { }

        private bool IsAllowedPlatform() => false;
#endif

        private void OnDestroy() {
            Destroy();
        }
    }
}
