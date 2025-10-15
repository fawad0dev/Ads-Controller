using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace CustomAds {
    public class InterstitialAdDisplayer : MonoBehaviour {
        [SerializeField] int index;
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
            if (AdsController.IsReady) {
                bool IsAdReady = AdsController.IsInterstitialReady(index);
                if (autoLoadIfNotReady && !IsAdReady) {
                    AdsController.ShowWaitingScreen(10, "Loading Ad...");
                    AdsController.LoadInterstitial(index, () => {
                        AdsController.ShowInterstitial(index, (err) => {
                            AdsController.SetWaitingScreen(false);
                            displayFailedEvent.Invoke(err);
                        }, () => {
                            AdsController.SetWaitingScreen(false);
                            displayedEvent.Invoke();
                        }, closedEvent.Invoke);
                    },
                    (error) => {
                        AdsController.SetWaitingScreen(false);
                    });
                } else if (IsAdReady) {
                    AdsController.ShowWaitingScreen(10, "Loading Ad...");
                    AdsController.ShowInterstitial(index, (err) => {
                        AdsController.SetWaitingScreen(false);
                        displayFailedEvent.Invoke(err);
                    }, () => {
                        AdsController.SetWaitingScreen(false);
                        displayedEvent.Invoke();
                    }, closedEvent.Invoke);
                }
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