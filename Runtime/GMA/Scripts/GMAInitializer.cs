using System;
using System.Collections.Generic;
using UnityEngine;

#if GMA_DEPENDENCIES_INSTALLED
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
#endif

namespace CustomAds.GMA {
    /// <summary>
    /// Handles Google Mobile Ads SDK initialization with consent management
    /// </summary>
    public class GMAInitializer : MonoBehaviour {
        [Header("iOS Settings")]
        [SerializeField] private bool iOSAppPauseOnBackground = true;

        [Header("Threading")]
        [SerializeField] private bool raiseAdEventsOnUnityMainThread = true;

        [Header("Consent")]
        [SerializeField] private bool initWithoutConsent = false;
        [SerializeField] private bool debugGeography = false;
        [SerializeField] private DebugGeographyType debugGeographyType = DebugGeographyType.EEA;

        [Header("Test Devices")]
        [SerializeField]
        private List<string> testDeviceIds = new(){
#if GMA_DEPENDENCIES_INSTALLED
            AdRequest.TestDeviceSimulator,
#endif
#if UNITY_IPHONE
            "96e23e80653bb28980d3f40beb58915c",
#elif UNITY_ANDROID
            "702815ACFC14FF222DA1DC767672A573",
            "1e955e734a6c29f9876109b7c70d8ddc",
            "3DBEA5A2A8E53D02B12FCC454188A254"  // Your test device
#endif
        };

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        public enum DebugGeographyType {
            Disabled,
            EEA,
            NotEEA
        }

        // Events
        public event Action OnInitialized;
        public event Action<string> OnInitializationFailed;

        // Properties
        public bool IsInitialized { get; private set; }
        public bool CanRequestAds { get; private set; }

        private void Log(string message) {
            if (debugLogs) Debug.Log($"[GMAInitializer] {message}");
        }

#if GMA_DEPENDENCIES_INSTALLED

        /// <summary>
        /// Initialize Google Mobile Ads SDK
        /// </summary>
        public void Initialize(Action onComplete = null) {
            if (IsInitialized) {
                Log("Already initialized");
                onComplete?.Invoke();
                return;
            }

            Log($"Initializing GMA SDK...");
            Log($"Device ID: {SystemInfo.deviceUniqueIdentifier}");

#if UNITY_IOS
            MobileAds.SetiOSAppPauseOnBackground(iOSAppPauseOnBackground);
#endif

            MobileAds.RaiseAdEventsOnUnityMainThread = raiseAdEventsOnUnityMainThread;

#if UNITY_EDITOR
            // UMP SDK doesn't work in Unity Editor - skip consent and initialize directly
            Log("Running in Unity Editor - skipping consent check");
            CanRequestAds = true;
            InitializeGMA(onComplete);
#elif UNITY_IOS || UNITY_ANDROID
            if (initWithoutConsent) {
                Log("Initializing without consent check");
                InitializeGMA(onComplete);
            } else {
                Log("Checking consent status");
                GatherConsent(() => {
                    if (CanRequestAds) {
                        InitializeGMA(onComplete);
                    } else {
                        Log("Cannot request ads - consent not obtained");
                        OnInitializationFailed?.Invoke("Consent not obtained");
                        onComplete?.Invoke();
                    }
                });
            }
#else
            Log("Platform not supported for mobile ads");
            onComplete?.Invoke();
#endif
        }

        /// <summary>
        /// Gather user consent (GDPR/COPPA)
        /// </summary>
        private void GatherConsent(Action onComplete) {
            var requestParameters = new ConsentRequestParameters {
                TagForUnderAgeOfConsent = false
            };

            // Set debug geography if enabled
            if (debugGeography) {
                var debugSettings = new ConsentDebugSettings {
                    DebugGeography = debugGeographyType == DebugGeographyType.EEA ?
                        DebugGeography.EEA : DebugGeography.Disabled
                };

                // Add test device IDs
                if (testDeviceIds.Count > 0) {
                    debugSettings.TestDeviceHashedIds = testDeviceIds;
                }

                requestParameters.ConsentDebugSettings = debugSettings;
            }

            Debug.Log("[GMAInitializer] Calling ConsentInformation.Update...");

            // Add timeout in case callback never fires
            bool callbackInvoked = false;
            StartCoroutine(ConsentTimeoutCoroutine(10f, () => {
                if (!callbackInvoked) {
                    Debug.LogWarning("[GMAInitializer] Consent update timeout - proceeding anyway");
                    callbackInvoked = true;
                    CanRequestAds = true;
                    onComplete?.Invoke();
                }
            }));

            ConsentInformation.Update(requestParameters, error => {
                if (callbackInvoked) {
                    Debug.LogWarning("[GMAInitializer] Consent callback fired after timeout");
                    return;
                }
                callbackInvoked = true;

                Debug.Log("[GMAInitializer] ConsentInformation.Update callback called");

                if (error != null) {
                    Debug.LogError($"[GMAInitializer] Consent update failed: {error}");
                    CanRequestAds = true;
                    onComplete?.Invoke();
                    return;
                }

                Debug.Log($"[GMAInitializer] Consent status: {ConsentInformation.ConsentStatus}");

                Debug.Log("[GMAInitializer] Loading consent form...");
                ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) => {
                    Debug.Log("[GMAInitializer] ConsentForm callback called");
                    if (formError != null) {
                        Debug.LogError($"[GMAInitializer] Consent form error: {formError}");
                    }
                    CanRequestAds = ConsentInformation.CanRequestAds();
                    Debug.Log($"[GMAInitializer] Can request ads: {CanRequestAds}");
                    onComplete?.Invoke();
                });
            });
        }

        private System.Collections.IEnumerator ConsentTimeoutCoroutine(float timeout, Action onTimeout) {
            yield return new WaitForSeconds(timeout);
            onTimeout?.Invoke();
        }

        /// <summary>
        /// Initialize the Google Mobile Ads SDK
        /// </summary>
        private void InitializeGMA(Action onComplete) {
            Log("Initializing Mobile Ads SDK");

            MobileAds.Initialize(status => {
                // Set test device IDs
                var requestConfig = new RequestConfiguration {
                    TestDeviceIds = GetTestDeviceIds()
                };
                MobileAds.SetRequestConfiguration(requestConfig);

                // Log adapter status
                var adapterMap = status.getAdapterStatusMap();
                foreach (var adapter in adapterMap) {
                    var adapterStatus = adapter.Value;
                    Log($"Adapter: {adapter.Key} - State: {adapterStatus.InitializationState} - Description: {adapterStatus.Description}");
                }

                IsInitialized = true;
                CanRequestAds = true;
                Log("GMA SDK initialized successfully");

                OnInitialized?.Invoke();
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// Get test device IDs including the simulator
        /// </summary>
        private List<string> GetTestDeviceIds() {
            var ids = new List<string> { AdRequest.TestDeviceSimulator };

            if (testDeviceIds != null && testDeviceIds.Count > 0) {
                ids.AddRange(testDeviceIds);
            }

            return ids;
        }

        /// <summary>
        /// Set application volume (0.0 to 1.0)
        /// </summary>
        public void SetApplicationVolume(float volume) {
            MobileAds.SetApplicationVolume(Mathf.Clamp01(volume));
            Log($"Application volume set to: {volume}");
        }

        /// <summary>
        /// Set application mute state
        /// </summary>
        public void SetApplicationMuted(bool muted) {
            MobileAds.SetApplicationMuted(muted);
            Log($"Application muted: {muted}");
        }

        /// <summary>
        /// Open ad inspector for debugging
        /// </summary>
        public void OpenAdInspector() {
            MobileAds.OpenAdInspector(error => {
                if (error != null) {
                    Debug.LogError($"[GMAInitializer] Ad Inspector error: {error}");
                } else {
                    Log("Ad Inspector closed");
                }
            });
        }

        /// <summary>
        /// Reset consent information (for testing)
        /// </summary>
        public void ResetConsentInformation() {
            ConsentInformation.Reset();
            Log("Consent information reset");
        }

#else
        public void Initialize(Action onComplete = null) {
            Log("GMA SDK not installed");
            onComplete?.Invoke();
        }

        public void SetApplicationVolume(float volume) { }
        public void SetApplicationMuted(bool muted) { }
        public void OpenAdInspector() { }
        public void ResetConsentInformation() { }
#endif
    }
}
