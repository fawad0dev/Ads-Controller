using System;
using UnityEngine;
using UnityEngine.Events;
using CustomAds.Core;

namespace CustomAds {
    /// <summary>
    /// Legacy NativeDisplayer - Updated to use new AdsManager API
    /// Maintains backward compatibility with existing scenes
    /// </summary>
    public class NativeDisplayer : MonoBehaviour {
        [SerializeField] string adName;
        [SerializeField] EnableState enableState;
        [SerializeField] DisableState disableState;
        [SerializeField] UnityEvent displayedEvent;
        [SerializeField] UnityEvent<string> displayedFailedEvent;
        [SerializeField] UnityEvent collapsedEvent;
        private bool disableCalled;

        enum EnableState {
            None,
            Show,
            Hide
        }
        enum DisableState {
            None,
            Show,
            Hide,
            Revert
        }

        bool wasShown;

        public void Show() {
            var native = AdsManager.GetNative(adName);

            if (native == null) {
                displayedFailedEvent?.Invoke("Native ad not configured");
                return;
            }

            if (!native.IsReady) {
                AdsManager.LoadNative(adName, loadResult => {
                    if (loadResult.Success) {
                        if (disableCalled && (disableState == DisableState.Hide || (disableState == DisableState.Revert && !wasShown)))
                            return;

                        AdsManager.ShowNative(adName, showResult => {
                            if (showResult.Success) {
                                displayedEvent?.Invoke();
                            } else {
                                displayedFailedEvent?.Invoke(showResult.ErrorMessage);
                            }
                        });
                    } else {
                        Debug.LogWarning($"Failed to load native ad {adName}: {loadResult.ErrorMessage}");
                        displayedFailedEvent?.Invoke(loadResult.ErrorMessage);
                    }
                });
            } else {
                AdsManager.ShowNative(adName, result => {
                    if (result.Success) {
                        displayedEvent?.Invoke();
                    } else {
                        displayedFailedEvent?.Invoke(result.ErrorMessage);
                    }
                });
            }
        }

        public void Hide() {
            AdsManager.HideNative(adName);
            collapsedEvent?.Invoke();
        }

        public void Render() {
            // Render is now handled automatically by Show() in new architecture
            Show();
        }
        private void OnEnable() {
            disableCalled = false;
            switch (enableState) {
                case EnableState.Show:
                    Show();
                    wasShown = true;
                    break;
                case EnableState.Hide:
                    Hide();
                    wasShown = false;
                    break;
            }
        }

        private void OnDisable() {
            disableCalled = true;
            switch (disableState) {
                case DisableState.Show:
                    Show();
                    break;
                case DisableState.Hide:
                    Hide();
                    break;
                case DisableState.Revert:
                    if (wasShown) {
                        Show();
                    } else {
                        Hide();
                    }
                    break;
            }
        }
    }
}
