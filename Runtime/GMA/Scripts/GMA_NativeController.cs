using System;
#if GMA_DEPENDENCIES_INSTALLED
using GoogleMobileAds.Api;
#endif
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace CustomAds.GMA {
    public class GMA_NativeController : MonoBehaviour {
        [SerializeField] bool useTestIds = false;
        [SerializeField] string androidNativeID = "ca-app-pub-3940256099942544/2247696110";
        [SerializeField] string iosNativeID = "ca-app-pub-3940256099942544/3986624511";
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
        [SerializeField] Vector2Int customBannerSize = new(320, 50);
        enum NaiveAdSize {
            Banner,
            IABBanner,
            MediumRectangle,
            Leaderboard,
            Custom
        }
        [SerializeField] bool useCustomPosition = false;
        [SerializeField] AdPosition adPosition = AdPosition.Bottom;
        [SerializeField] RectTransform position;
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
#if UNITY_EDITOR
    [CustomEditor(typeof(GMA_NativeController))]
    public class GMA_NativeControllerEditor : Editor {
        private SerializedProperty useTestIds;
        private SerializedProperty androidNativeID;
        private SerializedProperty iosNativeID;
        private SerializedProperty debugLogs;
        private SerializedProperty platformFilter;
        private SerializedProperty mainBackgroundColor;
        private SerializedProperty Option;
        private SerializedProperty nativeTemplateId;
        private SerializedProperty nativeAdSize;
        private SerializedProperty customBannerSize;
        private SerializedProperty useCustomPosition;
        private SerializedProperty adPosition;
        private SerializedProperty position;
        private SerializedProperty onNativeAdLoaded;
        private SerializedProperty onNativeAdLoadFailed;
        private SerializedProperty onAdPaid;
        private SerializedProperty onAdImpressionRecorded;
        private SerializedProperty onAdClicked;
        private SerializedProperty onAdFullScreenContentOpened;
        private SerializedProperty onAdFullScreenContentClosed;

        private void OnEnable() {
            useTestIds = serializedObject.FindProperty("useTestIds");
            androidNativeID = serializedObject.FindProperty("androidNativeID");
            iosNativeID = serializedObject.FindProperty("iosNativeID");
            debugLogs = serializedObject.FindProperty("debugLogs");
            platformFilter = serializedObject.FindProperty("platformFilter");
            mainBackgroundColor = serializedObject.FindProperty("mainBackgroundColor");
            Option = serializedObject.FindProperty("Option");
            nativeTemplateId = serializedObject.FindProperty("nativeTemplateId");
            nativeAdSize = serializedObject.FindProperty("nativeAdSize");
            customBannerSize = serializedObject.FindProperty("customBannerSize");
            useCustomPosition = serializedObject.FindProperty("useCustomPosition");
            adPosition = serializedObject.FindProperty("adPosition");
            position = serializedObject.FindProperty("position");
            onNativeAdLoaded = serializedObject.FindProperty("onNativeAdLoaded");
            onNativeAdLoadFailed = serializedObject.FindProperty("onNativeAdLoadFailed");
            onAdPaid = serializedObject.FindProperty("onAdPaid");
            onAdImpressionRecorded = serializedObject.FindProperty("onAdImpressionRecorded");
            onAdClicked = serializedObject.FindProperty("onAdClicked");
            onAdFullScreenContentOpened = serializedObject.FindProperty("onAdFullScreenContentOpened");
            onAdFullScreenContentClosed = serializedObject.FindProperty("onAdFullScreenContentClosed");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

#if !GMA_DEPENDENCIES_INSTALLED
            EditorGUILayout.HelpBox("GMA_DEPENDENCIES_INSTALLED is not added in Scripting Define Symbols. The GMA will not work to add them goto GMA_AdsController open the Context menu and click on 'Add GMA Dependencies'", MessageType.Warning);
#endif

            EditorGUILayout.LabelField("Ad Unit Ids", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useTestIds);

            if (!useTestIds.boolValue) {
                EditorGUILayout.PropertyField(androidNativeID);
                EditorGUILayout.PropertyField(iosNativeID);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Native Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(debugLogs);
            EditorGUILayout.PropertyField(platformFilter);
            EditorGUILayout.PropertyField(mainBackgroundColor);
            EditorGUILayout.PropertyField(Option);
            EditorGUILayout.PropertyField(nativeTemplateId);
            EditorGUILayout.PropertyField(nativeAdSize);

            if (nativeAdSize.enumValueIndex == 4) { // Custom
                EditorGUILayout.PropertyField(customBannerSize);
            }

            EditorGUILayout.PropertyField(useCustomPosition);

            if (useCustomPosition.boolValue) {
                EditorGUILayout.PropertyField(position);
            } else {
                EditorGUILayout.PropertyField(adPosition);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(onNativeAdLoaded);
            EditorGUILayout.PropertyField(onNativeAdLoadFailed);
            EditorGUILayout.PropertyField(onAdPaid);
            EditorGUILayout.PropertyField(onAdImpressionRecorded);
            EditorGUILayout.PropertyField(onAdClicked);
            EditorGUILayout.PropertyField(onAdFullScreenContentOpened);
            EditorGUILayout.PropertyField(onAdFullScreenContentClosed);

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}