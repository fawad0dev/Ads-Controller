using System;
using CustomAttributes;
#if GMA_DEPENDENCIES_INSTALLED
using GoogleMobileAds.Api;
#endif
using UnityEngine;
using UnityEngine.Events;
namespace AdsControllerNS.GMA {
    public class GMA_NativeController : MonoBehaviour {
        [Header("Ad Unit Ids")]
        [SerializeField] bool useTestIds = false;
        [SerializeField, HideIf(nameof(useTestIds))] string androidNativeID = "ca-app-pub-3940256099942544/2247696110";
        [SerializeField, HideIf(nameof(useTestIds))] string iosNativeID = "ca-app-pub-3940256099942544/3986624511";
        [Header("Native Settings")]
        [SerializeField] bool debugLogs;
        [SerializeField]
        RuntimePlatform[] platformFilter = new RuntimePlatform[]
        {
        RuntimePlatform.IPhonePlayer
        };
        [SerializeField] Color mainBackgroundColor = Color.white;
#if GMA_DEPENDENCIES_INSTALLED
        bool IsAndroid => GMA_AdsController.IsAndroid;
        bool IsIos => GMA_AdsController.IsIos;
        string NativeID {
            get {
                if (IsAndroid)
                    return useTestIds ? "ca-app-pub-3940256099942544/2247696110" : androidNativeID;
                else if (IsIos)
                    return useTestIds ? "ca-app-pub-3940256099942544/3986624511" : iosNativeID;
                else
                    return null;
            }
        }
        [SerializeField]
        NativeAdOptions Option = new NativeAdOptions {
            AdChoicesPlacement = AdChoicesPlacement.TopRightCorner,
            MediaAspectRatio = MediaAspectRatio.Any,
        };
        private NativeOverlayAd _nativeOverlayAd;
        NativeTemplateStyle style;
        [SerializeField] NativeTemplateIdEnum nativeTemplateId = NativeTemplateIdEnum.Medium;
        enum NativeTemplateIdEnum {
            Small,
            Medium
        }
        [SerializeField] NaiveAdSize nativeAdSize = NaiveAdSize.Banner;
        [SerializeField, ShowIf(nameof(nativeAdSize), NaiveAdSize.Custom)]
        Vector2Int customBannerSize = new(320, 50);
        enum NaiveAdSize {
            Banner,
            IABBanner,
            MediumRectangle,
            Leaderboard,
            Custom
        }
        [SerializeField] bool useCustomPosition = false;
        [SerializeField, HideIf(nameof(useCustomPosition))]
        AdPosition adPosition = AdPosition.Bottom;
        [SerializeField, ShowIf(nameof(useCustomPosition))]
        RectTransform position;
        public UnityEvent<NativeOverlayAd> onNativeAdLoaded;
        public UnityEvent<NativeOverlayAd, LoadAdError> onNativeAdLoadFailed;
        public UnityEvent<NativeOverlayAd, AdValue> onAdPaid;
        public UnityEvent<NativeOverlayAd> onAdImpressionRecorded;
        public UnityEvent<NativeOverlayAd> onAdClicked;
        public UnityEvent<NativeOverlayAd> onAdFullScreenContentOpened;
        public UnityEvent<NativeOverlayAd> onAdFullScreenContentClosed;

        private bool hasLoaded = false;
        private AdSize adSize;

        public bool HasLoaded => hasLoaded;
        bool IsAllowedPlatform {
            get {
                foreach (var platform in platformFilter) {
                    if (Application.platform == platform)
                        return true;
                }
                return false;
            }
        }
        void Log(object message) {
            if (debugLogs) Debug.Log($"{gameObject.name} {message}");
        }
        void LogWarning(object message) {
            Debug.LogWarning($"{gameObject.name} {message}");
        }
        void LogError(object message) {
            Debug.LogError($"{gameObject.name} {message}");
        }
        public void Init() {
            if (!IsAllowedPlatform) {
                LogWarning("is not allowed on this platform.");
                return;
            }
            Log("Init()");
            if (NativeID == null) {
                LogWarning("NativeID is null. Initialization aborted.");
                return;
            }
            if (_nativeOverlayAd != null) {
                Destroy();
            }
            style = new NativeTemplateStyle {
                TemplateId = nativeTemplateId switch {
                    NativeTemplateIdEnum.Small => "small",
                    NativeTemplateIdEnum.Medium => "medium",
                    _ => "medium"
                },
                MainBackgroundColor = mainBackgroundColor,
            };
        }
        public void Load(
            Action<NativeOverlayAd> onNativeAdLoadedAction = null,
            Action<NativeOverlayAd, LoadAdError> onNativeAdLoadFailedAction = null
        ) {
            if (!IsAllowedPlatform) {
                LogWarning("is not allowed on this platform.");
                return;
            }
            Log("Load()");
            if (NativeID == null) {
                LogWarning("NativeID is null. Load aborted.");
                onNativeAdLoadFailedAction?.Invoke(null, null);
                onNativeAdLoadFailed?.Invoke(null, null);
                return;
            }
            var adRequest = new AdRequest();
            hasLoaded = false;
            NativeOverlayAd.Load(NativeID, adRequest, Option,
                (ad, error) => {
                    if (error != null) {
                        LogError($"ad failed to load an ad with error : {error}");
                        onNativeAdLoadFailedAction?.Invoke(_nativeOverlayAd, error);
                        onNativeAdLoadFailed?.Invoke(_nativeOverlayAd, error);
                        return;
                    }
                    if (ad == null) {
                        LogError($"Unexpected error: ad load event fired with null ad and null error.");
                        onNativeAdLoadFailedAction?.Invoke(_nativeOverlayAd, null);
                        onNativeAdLoadFailed?.Invoke(_nativeOverlayAd, null);
                        return;
                    }
                    Log($"ad loaded with response : {ad.GetResponseInfo()}");
                    _nativeOverlayAd = ad;
                    onNativeAdLoadedAction?.Invoke(_nativeOverlayAd);
                    onNativeAdLoaded?.Invoke(_nativeOverlayAd);
                    RegisterEventHandlers(ad);
                    hasLoaded = true;
                });
        }
        public void Load() {
            Load(null, null);
        }
        private void RegisterEventHandlers(NativeOverlayAd ad) {
            ad.OnAdPaid += adValue => {
                Log($"ad paid {adValue.Value} {adValue.CurrencyCode}.");
                onAdPaid?.Invoke(ad, adValue);
            };
            ad.OnAdImpressionRecorded += () => {
                Log($"ad recorded an impression.");
                onAdImpressionRecorded?.Invoke(ad);
            };
            ad.OnAdClicked += () => {
                Log($"ad was clicked.");
                onAdClicked?.Invoke(ad);
            };
            ad.OnAdFullScreenContentOpened += () => {
                Log($"ad full screen content opened.");
                onAdFullScreenContentOpened?.Invoke(ad);
            };
            ad.OnAdFullScreenContentClosed += () => {
                Log($"ad full screen content closed.");
                onAdFullScreenContentClosed?.Invoke(ad);
            };
        }
        public bool IsAdReady() {
            Log($"IsAdReady()");
            return _nativeOverlayAd != null && hasLoaded;
        }
        public void Destroy() {
            Log($"Destroy()");
            if (_nativeOverlayAd != null) {
                _nativeOverlayAd.Destroy();
                _nativeOverlayAd = null;
                hasLoaded = false;
            }
        }
        public void Show() {
            if (!IsAllowedPlatform) {
                LogWarning($"is not allowed on this platform.");
                return;
            }
            Log($"Show()");
            if (_nativeOverlayAd != null) {
                Log($"Showing ad.");
                _nativeOverlayAd.Show();
            } else {
                LogWarning($"Show() called but ad is not loaded.");
            }
        }
        public void Hide() {
            Log($"Hide()");
            if (_nativeOverlayAd != null) {
                Log($"Hiding ad.");
                _nativeOverlayAd.Hide();
            } else {
                LogWarning($"Hide() called but ad is not loaded.");
            }
        }
        public void LogResponseInfo() {
            if (_nativeOverlayAd != null) {
                var responseInfo = _nativeOverlayAd.GetResponseInfo();
                if (responseInfo != null) {
                    Log(responseInfo);
                }
            } else {
                LogWarning($"LogResponseInfo() called but ad is not loaded.");
            }
        }
        public void Render() {
            if (_nativeOverlayAd != null) {
                Log($"Rendering ad.");
                adSize = nativeAdSize switch {
                    NaiveAdSize.Banner => AdSize.Banner,
                    NaiveAdSize.IABBanner => AdSize.IABBanner,
                    NaiveAdSize.MediumRectangle => AdSize.MediumRectangle,
                    NaiveAdSize.Leaderboard => AdSize.Leaderboard,
                    NaiveAdSize.Custom => new AdSize((int)customBannerSize.x, (int)customBannerSize.y),
                    _ => AdSize.Banner
                };
                if (useCustomPosition) {
                    _nativeOverlayAd.RenderTemplate(style, adSize, (int)position.anchoredPosition.x, -(int)position.anchoredPosition.y);
                } else {
                    _nativeOverlayAd.RenderTemplate(style, adSize, adPosition);
                }
            } else {
                LogWarning($"RenderAd() called but ad is not loaded.");
            }
        }
#endif
    }
}