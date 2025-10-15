using System;
#if GMA_DEPENDENCIES_INSTALLED
using GoogleMobileAds.Api;
#endif
using UnityEngine;
using CustomAttributes;
namespace AdsControllerNS.GMA {
    public class GMA_BannerController : MonoBehaviour {
        [Header("Ad Unit IDs")]
        [SerializeField] bool useTestIds = false;
        [SerializeField, HideIf(nameof(useTestIds))] string androidBannerID = "ca-app-pub-3940256099942544/6300978111";
        [SerializeField, HideIf(nameof(useTestIds))] string iosBannerID = "ca-app-pub-3940256099942544/2934735716";
        [Header("Banner Settings")]
        [SerializeField] bool debugLogs;
        [SerializeField] BannerAdSize adSize = BannerAdSize.Banner;
        enum BannerAdSize {
            Banner,
            IABBanner,
            MediumRectangle,
            Leaderboard,
            Custom
        }
        [SerializeField, ShowIf(nameof(adSize), BannerAdSize.Custom)]
        Rect customBannerSize = new(0, 0, 320, 50);
#if GMA_DEPENDENCIES_INSTALLED
        bool IsAndroid => GMA_AdsController.IsAndroid;
        bool IsIos => GMA_AdsController.IsIos;
        string BannerID {
            get {
                if (IsAndroid)
                    return useTestIds ? "ca-app-pub-3940256099942544/6300978111" : androidBannerID;
                else if (IsIos)
                    return useTestIds ? "ca-app-pub-3940256099942544/2934735716" : iosBannerID;
                else
                    return null;
            }
        }

        [SerializeField] AdPosition adPosition = AdPosition.Bottom;
        [SerializeField] bool collapsable = false;
        public Action onBannerAdLoadedAction;
        public Action<LoadAdError> onBannerAdLoadFailedAction;
        public Action<AdValue> onAdPaidAction;
        public Action onAdImpressionRecordedAction;
        public Action onAdClickedAction;
        public Action onAdFullScreenContentOpenedAction;
        public Action onAdFullScreenContentClosedAction;

        private AdSize bannerAdSize;
        BannerView _bannerView;
        private bool initialized;
        public void Log(object message) {
            if (debugLogs) Debug.Log($"{gameObject.name} {message}");
        }
        public void Init() {
            if (!initialized) {
                Log("Init()");
                if (BannerID == null) return;
                if (adSize == BannerAdSize.Custom) {
                    _bannerView = new BannerView(BannerID, new AdSize((int)customBannerSize.width, (int)customBannerSize.height), (int)customBannerSize.x, (int)customBannerSize.y);
                } else {
                    bannerAdSize = adSize switch {
                        BannerAdSize.Banner => AdSize.Banner,
                        BannerAdSize.IABBanner => AdSize.IABBanner,
                        BannerAdSize.MediumRectangle => AdSize.MediumRectangle,
                        BannerAdSize.Leaderboard => AdSize.Leaderboard,
                        _ => AdSize.Banner
                    };
                    _bannerView = new BannerView(BannerID, bannerAdSize, adPosition);
                }
                _bannerView.OnBannerAdLoaded += OnBannerAdLoaded;
                _bannerView.OnBannerAdLoadFailed += OnBannerAdLoadFailed;
                _bannerView.OnAdPaid += OnAdPaid;
                _bannerView.OnAdImpressionRecorded += OnAdImpressionRecorded;
                _bannerView.OnAdClicked += OnAdClicked;
                _bannerView.OnAdFullScreenContentOpened += OnAdFullScreenContentOpened;
                _bannerView.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
                initialized = true;
            }
        }

        private void OnAdFullScreenContentClosed() {
            Log("OnAdFullScreenContentClosed()");
            onAdFullScreenContentClosedAction?.Invoke();
        }
        private void OnAdFullScreenContentOpened() {
            Log("OnAdFullScreenContentOpened()");
            onAdFullScreenContentOpenedAction?.Invoke();
        }

        private void OnAdClicked() {
            Log("OnAdClicked()");
            onAdClickedAction?.Invoke();
        }

        private void OnAdImpressionRecorded() {
            Log("OnAdImpressionRecorded()");
            onAdImpressionRecordedAction?.Invoke();
        }

        private void OnAdPaid(AdValue value) {
            Log($"OnAdPaid({value})");
            onAdPaidAction?.Invoke(value);
        }

        private void OnBannerAdLoadFailed(LoadAdError error) {
            Log($"OnBannerAdLoadFailed({error})");
            onBannerAdLoadFailedAction?.Invoke(error);
        }

        private void OnBannerAdLoaded() {
            Log("OnBannerAdLoaded()");
            onBannerAdLoadedAction?.Invoke();
        }

        public void Load(
            Action onBannerAdLoadedAction = null,
            Action<LoadAdError> onBannerAdLoadFailedAction = null
        ) {
            Log("Load()");
            this.onBannerAdLoadedAction = onBannerAdLoadedAction;
            this.onBannerAdLoadFailedAction = onBannerAdLoadFailedAction;
            var adRequest = new AdRequest();

            if (collapsable) {
                switch (adPosition) {
                    case AdPosition.Top:
                        adRequest.Extras.Add("collapsible", "top");
                        break;
                    case AdPosition.Bottom:
                        adRequest.Extras.Add("collapsible", "bottom");
                        break;
                }
            }
            _bannerView.LoadAd(adRequest);
        }
        public void Destroy() {
            Log("Destroy()");
            if (_bannerView != null) {
                _bannerView.OnBannerAdLoaded -= OnBannerAdLoaded;
                _bannerView.OnBannerAdLoadFailed -= OnBannerAdLoadFailed;
                _bannerView.OnAdPaid -= OnAdPaid;
                _bannerView.OnAdImpressionRecorded -= OnAdImpressionRecorded;
                _bannerView.OnAdClicked -= OnAdClicked;
                _bannerView.OnAdFullScreenContentOpened -= OnAdFullScreenContentOpened;
                _bannerView.OnAdFullScreenContentClosed -= OnAdFullScreenContentClosed;
                _bannerView.Destroy();
                _bannerView = null;
            }
            initialized = false;
        }
        public bool WasAdShowing { get; private set; } = false;
        public void Show(
            Action onAdFullScreenContentOpenedAction = null,
            Action onAdFullScreenContentClosedAction = null,
            Action onAdClickedAction = null,
            Action onAdImpressionRecordedAction = null,
            Action<AdValue> onAdPaidAction = null
        ) {
            this.onAdPaidAction = onAdPaidAction;
            this.onAdImpressionRecordedAction = onAdImpressionRecordedAction;
            this.onAdClickedAction = onAdClickedAction;
            this.onAdFullScreenContentOpenedAction = onAdFullScreenContentOpenedAction;
            this.onAdFullScreenContentClosedAction = onAdFullScreenContentClosedAction;
            Log("Show()");
            if (_bannerView != null) {
                _bannerView.Show();
                WasAdShowing = true;
            }
        }
        public void Hide() {
            Log("Hide()");
            if (_bannerView != null) {
                _bannerView.Hide();
                WasAdShowing = false;
            }
        }
        public void LogResponseInfo() {
            if (_bannerView != null) {
                var responseInfo = _bannerView.GetResponseInfo();
                if (responseInfo != null) {
                    Debug.Log(responseInfo);
                }
            }
        }

#endif
    }
}