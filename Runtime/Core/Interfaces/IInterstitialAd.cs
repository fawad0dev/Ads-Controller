using System;

namespace CustomAds.Core {
    /// <summary>
    /// Interface for interstitial ad operations
    /// </summary>
    public interface IInterstitialAd : IAdUnit {
        /// <summary>
        /// Show the interstitial ad
        /// </summary>
        void Show(Action<AdResult> onComplete);
        
        /// <summary>
        /// Event triggered when ad is clicked
        /// </summary>
        event Action OnClicked;
        
        /// <summary>
        /// Event triggered when impression is recorded
        /// </summary>
        event Action OnImpression;
        
        /// <summary>
        /// Event triggered when user earns revenue from this ad
        /// </summary>
        event Action<AdRevenue> OnPaid;
        
        /// <summary>
        /// Event triggered when ad is opened
        /// </summary>
        event Action OnOpened;
        
        /// <summary>
        /// Event triggered when ad is closed
        /// </summary>
        event Action OnClosed;
    }
}
