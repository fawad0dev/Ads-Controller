using UnityEngine;
using UnityEngine.Events;
namespace CustomAds {
    public class AppOpenDisplayer : MonoBehaviour {
        [SerializeField] int index;
        [SerializeField] UnityEvent<string> onFailed;
        [SerializeField] UnityEvent onOpen;
        [SerializeField] UnityEvent onClose;
        [SerializeField] UnityEvent onClicked;
        [SerializeField] UnityEvent onImpression;
        [SerializeField] UnityEvent<long> onPaid;
        [SerializeField] DisplayBy displayBy = DisplayBy.OnFocus;
        [SerializeField] float cooldownSeconds = 5f;

        static float lastAdShownTime = float.MinValue;

        enum DisplayBy {
            OnFocus,
            OnPause
        }

        bool CanShowAd() {
            return Time.realtimeSinceStartup - lastAdShownTime >= cooldownSeconds;
        }

        void OnApplicationFocus(bool focus) {
            if (displayBy != DisplayBy.OnFocus) return;
            if (focus && CanShowAd()) {
                lastAdShownTime = Time.realtimeSinceStartup;
                AdsController.ShowAppOpen(index, onFailed.Invoke, onOpen.Invoke, onClose.Invoke, onClicked.Invoke, onImpression.Invoke, onPaid.Invoke);
            }
        }
        void OnApplicationPause(bool pause) {
            if (displayBy != DisplayBy.OnPause) return;
            if (!pause && CanShowAd()) {
                lastAdShownTime = Time.realtimeSinceStartup;
                AdsController.ShowAppOpen(index, onFailed.Invoke, onOpen.Invoke, onClose.Invoke, onClicked.Invoke, onImpression.Invoke, onPaid.Invoke);
            }
        }
    }
}