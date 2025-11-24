using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using CustomAds.Core;

namespace CustomAds {
    /// <summary>
    /// Legacy InterstitialAdDisplayer - Updated to use new AdsManager API
    /// Maintains backward compatibility with existing scenes
    /// </summary>
    public class InterstitialAdDisplayer : MonoBehaviour {
        [SerializeField] string adName = "Main";
        [SerializeField] bool autoLoadIfNotReady;
        [SerializeField] ShowState showState;
        [SerializeField] UnityEvent displayedEvent;
        [SerializeField] UnityEvent<string> displayFailedEvent;
        [SerializeField] UnityEvent closedEvent;
        [Flags]
        enum ShowState {
            None = 0,
            ShowOnEnable = 1 << 0,   // 1
            ShowOnDisable = 1 << 1,   // 2
        }

        public void Show() {
            var interstitial = AdsManager.GetInterstitial(adName);

            if (interstitial == null) {
                displayFailedEvent?.Invoke("Interstitial not configured");
                return;
            }

            // Subscribe to close event
            interstitial.OnClosed += () => closedEvent?.Invoke();

            if (autoLoadIfNotReady && !interstitial.IsReady) {
                AdsManager.LoadInterstitial(adName, loadResult => {
                    if (loadResult.Success) {
                        AdsManager.ShowInterstitial(adName, result => {
                            if (result.Success) {
                                displayedEvent?.Invoke();
                            } else {
                                displayFailedEvent?.Invoke(result.ErrorMessage);
                            }
                        });
                    } else {
                        displayFailedEvent?.Invoke(loadResult.ErrorMessage);
                    }
                });
            } else if (interstitial.IsReady) {
                AdsManager.ShowInterstitial(adName, result => {
                    if (result.Success) {
                        displayedEvent?.Invoke();
                    } else {
                        displayFailedEvent?.Invoke(result.ErrorMessage);
                    }
                });
            } else {
                displayFailedEvent?.Invoke("Ad not ready");
            }
        }
        private void OnEnable() {
            if ((showState & ShowState.ShowOnEnable) != 0) {
                Show();
            }
        }
        private void OnDisable() {
            if ((showState & ShowState.ShowOnDisable) != 0) {
                Show();
            }
        }
    }
}
