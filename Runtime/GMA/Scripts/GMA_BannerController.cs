using System;
#if GMA_DEPENDENCIES_INSTALLED
using GoogleMobileAds.Api;
#endif
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace CustomAds.GMA {
    public class GMA_BannerController : MonoBehaviour {
        [SerializeField] bool useTestIds = false;
        [SerializeField] string androidBannerID = "ca-app-pub-3940256099942544/6300978111";
        [SerializeField] string iosBannerID = "ca-app-pub-3940256099942544/2934735716";
        [SerializeField] bool debugLogs;
        [SerializeField] BannerAdSize adSize = BannerAdSize.Banner;
        [SerializeField] bool collapsable = false;
        enum BannerAdSize {
            Banner,
            IABBanner,
            MediumRectangle,
            Leaderboard,
            Custom
        }
        [SerializeField] Rect customBannerSize = new(0, 0, 320, 50);
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
#if UNITY_EDITOR
    [CustomEditor(typeof(GMA_BannerController))]
    public class GMA_BannerControllerEditor : Editor {
        private SerializedProperty useTestIds;
        private SerializedProperty androidBannerID;
        private SerializedProperty iosBannerID;
        private SerializedProperty debugLogs;
        private SerializedProperty adSize;
        private SerializedProperty customBannerSize;
        private SerializedProperty adPosition;
        private SerializedProperty collapsable;

        private void OnEnable() {
            useTestIds = serializedObject.FindProperty("useTestIds");
            androidBannerID = serializedObject.FindProperty("androidBannerID");
            iosBannerID = serializedObject.FindProperty("iosBannerID");
            debugLogs = serializedObject.FindProperty("debugLogs");
            adSize = serializedObject.FindProperty("adSize");
            customBannerSize = serializedObject.FindProperty("customBannerSize");
            adPosition = serializedObject.FindProperty("adPosition");
            collapsable = serializedObject.FindProperty("collapsable");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
#if !GMA_DEPENDENCIES_INSTALLED
            EditorGUILayout.HelpBox("GMA_DEPENDENCIES_INSTALLED is not added in Scripting Define Symbols. The GMA will not work to add them goto GMA_AdsController open the Context menu and click on 'Add GMA Dependencies'", MessageType.Warning);
#endif

            EditorGUILayout.LabelField("Ad Unit IDs", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useTestIds);

            if (!useTestIds.boolValue) {
                EditorGUILayout.PropertyField(androidBannerID);
                EditorGUILayout.PropertyField(iosBannerID);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Banner Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(debugLogs);
            EditorGUILayout.PropertyField(adSize);

            if (adSize.enumValueIndex == 4) {
                EditorGUILayout.PropertyField(customBannerSize);
            }
#if GMA_DEPENDENCIES_INSTALLED
            EditorGUILayout.PropertyField(adPosition);
#endif
            EditorGUILayout.PropertyField(collapsable);
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}