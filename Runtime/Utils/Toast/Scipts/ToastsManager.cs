using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CustomAds.Utils {
    public class ToastsManager : MonoBehaviour {
        static ToastsManager instance;

        public static ToastsManager Instance {
            get {
                if (!instance) {
                    instance = FindAnyObjectByType<ToastsManager>();
                }
                return instance;
            }
        }
        void Awake() {
            instance = this;
        }
        [SerializeField] List<Toast> toastsPool;
        public static void ShowToast(object message) {
            var toastsPool = Instance.toastsPool;
            Toast toast = null;
            for (int i = 0; i < toastsPool.Count; i++) {
                if (!toastsPool[i].gameObject.activeSelf) {
                    toast = toastsPool[i];
                    break;
                }
            }
            if (!toast) {
                toast = Instantiate(toastsPool[0], toastsPool[0].transform.parent);
                toastsPool.Add(toast);
            }
            toast.ShowToast(message.ToString());
        }
    }
}