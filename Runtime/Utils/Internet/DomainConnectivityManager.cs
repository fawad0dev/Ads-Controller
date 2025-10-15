using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
namespace AdsControllerNS.Utils {
    public class DomainConnectivityManager : MonoBehaviour {
        [SerializeField] private float checkInterval = 5f;
        [SerializeField] private float connectionTimeout = 3f;
        [SerializeField] private string domainUrl = "https://admob.google.com/";
        public UnityEvent onDomainConnected;
        public UnityEvent onDomainConnectivityLost;
        private bool _isOnline = false;
        public bool IsOnline {
            get { return _isOnline; }
            private set {
                if (_isOnline != value) {
                    _isOnline = value;
                    if (_isOnline) {
                        onDomainConnected?.Invoke();
                        Debug.Log("Domain connection established");
                    } else {
                        onDomainConnectivityLost?.Invoke();
                        Debug.Log("Domain connection lost");
                    }
                }
            }
        }
        public float LastConnectionTime { get; private set; }
        public int ConsecutiveFailures { get; private set; }
        public float ConnectionLatency { get; private set; }
        private Coroutine connectivityCheckRoutine;
        private void Awake() {
            onDomainConnected ??= new UnityEvent();
            onDomainConnectivityLost ??= new UnityEvent();
            _isOnline = false;
            ConsecutiveFailures = 0;
        }
        private void OnEnable() {
            if (connectivityCheckRoutine == null)
                connectivityCheckRoutine = StartCoroutine(CheckConnectivityRoutine());
        }
        private void OnDisable() {
            if (connectivityCheckRoutine != null) {
                StopCoroutine(connectivityCheckRoutine);
                connectivityCheckRoutine = null;
            }
        }
        private IEnumerator CheckConnectivityRoutine() {
            while (true) {
                yield return CheckDomainConnection();
                yield return new WaitForSeconds(checkInterval);
            }
        }
        private IEnumerator CheckDomainConnection() {
            float startTime = Time.time;
            bool connectionSuccessful = false;
            string fullUrl = domainUrl;
            using (UnityWebRequest request = UnityWebRequest.Get(fullUrl)) {
                request.timeout = (int)connectionTimeout;
                yield return request.SendWebRequest();
                float elapsedTime = Time.time - startTime;
                ConnectionLatency = elapsedTime * 1000;
                if (request.result == UnityWebRequest.Result.Success) {
                    connectionSuccessful = true;
                    ConsecutiveFailures = 0;
                    LastConnectionTime = Time.time;
                } else {
                    connectionSuccessful = false;
                    ConsecutiveFailures++;
                    Debug.LogWarning($"Domain connection failed: {request.error}. Consecutive failures: {ConsecutiveFailures}");
                }
            }
            IsOnline = connectionSuccessful;
        }
        public void ForceConnectionCheck() {
            StartCoroutine(CheckDomainConnection());
        }
        public void ChangeDomain(string newDomainUrl) {
            domainUrl = newDomainUrl;
            ForceConnectionCheck();
        }
        public string GetConnectionStatusReport() {
            string status = IsOnline ? "Connected" : "Disconnected";
            return $"Domain: {domainUrl}\nStatus: {status}\nLatency: {ConnectionLatency:F2}ms\n" +
                   $"Last Connected: {(LastConnectionTime > 0 ? (Time.time - LastConnectionTime).ToString("F1") + "s ago" : "Never")}\n" +
                   $"Consecutive Failures: {ConsecutiveFailures}";
        }
    }
}