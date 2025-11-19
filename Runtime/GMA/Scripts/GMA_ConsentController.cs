#if GMA_DEPENDENCIES_INSTALLED
using GoogleMobileAds.Ump.Api;
#endif
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace CustomAds.GMA {
    public class GMA_ConsentController : MonoBehaviour {
        [SerializeField, Tooltip("Button to show user consent and privacy settings.")]
        private Button _privacyButton;
        [SerializeField, Tooltip("GameObject with the error popup.")]
        private GameObject _errorPopup;
        [SerializeField, Tooltip("Error message for the error popup,")]
        private TMP_Text _errorText;
        [SerializeField] bool debugLogs;
#if GMA_DEPENDENCIES_INSTALLED
        [SerializeField] Button _adsInspectorButton;
        [SerializeField] bool tagForUnderAgeOfConsent;
        public bool CanRequestAds => ConsentInformation.CanRequestAds();
        [SerializeField] DebugGeography debugGeography = DebugGeography.Disabled;
        private void Start() {
            if (_errorPopup != null) {
                _errorPopup.SetActive(false);
            }
            _adsInspectorButton.gameObject.SetActive(debugLogs);
            _adsInspectorButton.onClick.AddListener(() => transform.parent.GetComponent<GMA_AdsController>().OpenAdInspector());
        }
        void Log(object message) {
            if (debugLogs) Debug.Log($"{gameObject.name} {message}");
        }

        public void GatherConsent(Action<string> onComplete) {
            Log("GatherConsent()");
            var requestParameters = new ConsentRequestParameters {
                TagForUnderAgeOfConsent = tagForUnderAgeOfConsent,
                ConsentDebugSettings = new ConsentDebugSettings {
                    DebugGeography = debugGeography,
                    TestDeviceHashedIds = GMA_AdsController.TestDeviceIds,
                }
            };
            onComplete = (onComplete == null) ? UpdateErrorPopup : onComplete + UpdateErrorPopup;
            ConsentInformation.Update(requestParameters, updateError => {
                UpdatePrivacyButton();
                if (updateError != null) {
                    onComplete(updateError.Message);
                    return;
                }
                if (CanRequestAds) {
                    onComplete(null);
                    return;
                }
                ConsentForm.LoadAndShowConsentFormIfRequired((FormError showError) => {
                    UpdatePrivacyButton();
                    if (showError != null) {
                        onComplete?.Invoke(showError.Message);
                    } else {
                        onComplete?.Invoke(null);
                    }
                });
            });
        }
        public void ShowPrivacyOptionsForm(Action<string> onComplete) {
            Log("ShowPrivacyOptionsForm()");
            onComplete = (onComplete == null) ? UpdateErrorPopup : onComplete + UpdateErrorPopup;
            ConsentForm.ShowPrivacyOptionsForm(showError => {
                UpdatePrivacyButton();
                if (showError != null) {
                    onComplete?.Invoke(showError.Message);
                } else {
                    onComplete?.Invoke(null);
                }
            });
        }
        public void ResetConsentInformation() {
            ConsentInformation.Reset();
            UpdatePrivacyButton();
        }
        void UpdatePrivacyButton() {
            if (_privacyButton != null) {
                _privacyButton.interactable =
                    ConsentInformation.PrivacyOptionsRequirementStatus ==
                        PrivacyOptionsRequirementStatus.Required;
            }
        }
        void UpdateErrorPopup(string message) {
            if (string.IsNullOrEmpty(message)) {
                return;
            }
            if (_errorText != null) {
                _errorText.text = message;
            }
            if (_errorPopup != null) {
                _errorPopup.SetActive(true);
            }
            if (_privacyButton != null) {
                _privacyButton.interactable = true;
            }
        }
#endif
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(GMA_ConsentController))]
    public class GMA_ConsentControllerEditor : Editor {
        public override void OnInspectorGUI() {
            serializedObject.Update();
#if !GMA_DEPENDENCIES_INSTALLED
            EditorGUILayout.HelpBox("GMA_DEPENDENCIES_INSTALLED is not added in Scripting Define Symbols. The GMA will not work to add them goto GMA_AdsController open the Context menu and click on 'Add GMA Dependencies'", MessageType.Warning);
#endif
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}