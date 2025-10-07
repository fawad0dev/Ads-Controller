using UnityEngine;
using UnityEngine.UIElements;

public class AdManagerUIController : MonoBehaviour {
    [SerializeField] private UIDocument uiDocument;

    // UI Elements
    private Button directCallsTabButton;
    private Button automatedCallsTabButton;
    private VisualElement directCallsPanel;
    private VisualElement automatedCallsPanel;

    // Direct Calls Buttons

    private Button initBannerButton;
    private Button loadBannerButton;
    private Button showBannerButton;
    private Button hideBannerButton;
    private Button destroyBannerButton;
    private Button initInterstitialButton;
    private Button loadInterstitialButton;
    private Button showInterstitialButton;
    private Button destroyInterstitialButton;
    private Button initRewardedButton;
    private Button loadRewardedButton;
    private Button showRewardedButton;
    private Button destroyRewardedButton;


    // Automated Calls Buttons
    private Button autoShowBannerButton;
    private Button autoAdditionalBannerButton;
    private Button autoShowInterstitialButton;
    private Button autoAdditionalInterstitialButton;
    private Button autoShowRewardedButton;
    private Button autoAdditionalRewardedButton;
    private Button sendAnalyticsBtn;
    private StyleColor mainBtnColor;


    private void OnEnable() {
        var root = uiDocument.rootVisualElement;
        // Get tab elements
        directCallsTabButton = root.Q<Button>("direct-calls-tab-button");
        automatedCallsTabButton = root.Q<Button>("automated-calls-tab-button");
        directCallsPanel = root.Q("direct-calls-panel");
        automatedCallsPanel = root.Q("automated-calls-panel");

        // Get Direct Calls buttons
        initBannerButton = root.Q<Button>("init-banner-button");
        loadBannerButton = root.Q<Button>("load-banner-button");
        showBannerButton = root.Q<Button>("show-banner-button");
        hideBannerButton = root.Q<Button>("hide-banner-button");
        destroyBannerButton = root.Q<Button>("destroy-banner-button");

        initInterstitialButton = root.Q<Button>("init-interstitial-button");
        loadInterstitialButton = root.Q<Button>("load-interstitial-button");
        showInterstitialButton = root.Q<Button>("show-interstitial-button");
        destroyInterstitialButton = root.Q<Button>("destroy-interstitial-button");

        initRewardedButton = root.Q<Button>("init-rewarded-button");
        loadRewardedButton = root.Q<Button>("load-rewarded-button");
        showRewardedButton = root.Q<Button>("show-rewarded-button");
        destroyRewardedButton = root.Q<Button>("destroy-rewarded-button");

        // Get Automated Calls buttons
        autoShowBannerButton = root.Q<Button>("auto-show-banner-button");
        autoAdditionalBannerButton = root.Q<Button>("auto-additional-banner-button");
        autoShowInterstitialButton = root.Q<Button>("auto-show-interstitial-button");
        autoAdditionalInterstitialButton = root.Q<Button>("auto-additional-interstitial-button");
        autoShowRewardedButton = root.Q<Button>("auto-show-rewarded-button");
        autoAdditionalRewardedButton = root.Q<Button>("auto-additional-rewarded-button");

        // Register tab button callbacks
        directCallsTabButton.clicked += () => SwitchTab(true);
        automatedCallsTabButton.clicked += () => SwitchTab(false);
        mainBtnColor = loadBannerButton.style.backgroundColor;
        loadBannerButton.clicked += () => {
            AdsController.LoadBanner(onLoaded: () => {
                loadBannerButton.style.backgroundColor = new StyleColor(Color.green);
            },
             onLoadFailed: (err) => {
                 loadBannerButton.style.backgroundColor = new StyleColor(Color.red);
             });
        };
        showBannerButton.clicked += () => {
            AdsController.ShowBanner();
        };
        hideBannerButton.clicked += () => {
            AdsController.HideBanner();
        };
        loadInterstitialButton.clicked += () => {
            AdsController.LoadInterstitial(onLoaded: () => {
                loadInterstitialButton.style.backgroundColor = new StyleColor(Color.green);
            },
             onLoadFailed: (err) => {
                 loadInterstitialButton.style.backgroundColor = new StyleColor(Color.red);
                 Debug.LogError("Interstitial load failed: " + err);
             });
        };
        showInterstitialButton.clicked += () => {
            AdsController.ShowInterstitial(onFailed: (err) => {
                showInterstitialButton.style.backgroundColor = new StyleColor(Color.red);
                Debug.LogError("Interstitial show failed: " + err);
            },
             onClose: () => {
                 showInterstitialButton.style.backgroundColor = mainBtnColor;
                 loadInterstitialButton.style.backgroundColor = mainBtnColor;
             });
        };
        loadRewardedButton.clicked += () => {
            AdsController.LoadRewarded(onLoaded: () => {
                loadRewardedButton.style.backgroundColor = new StyleColor(Color.green);
            },
             onLoadFailed: (err) => {
                 loadRewardedButton.style.backgroundColor = new StyleColor(Color.red);
                 Debug.LogError("Rewarded load failed: " + err);
             });
        };
        showRewardedButton.clicked += () => {
            AdsController.ShowRewarded(onShowFailed: (err) => {
                showRewardedButton.style.backgroundColor = new StyleColor(Color.red);
                Debug.LogError("Rewarded show failed: " + err);
            },
             onClosed: () => {
                 showRewardedButton.style.backgroundColor = mainBtnColor;
                 loadRewardedButton.style.backgroundColor = mainBtnColor;
             },
             onReward: (rw) => {
                 showRewardedButton.style.backgroundColor = mainBtnColor;
             });
        };
        autoShowInterstitialButton.clicked += () => {
            AdsController.ShowInterstitial(true, true, true, true);
        };
        autoShowRewardedButton.clicked += () => {
            AdsController.ShowRewarded(true, true, true, true);
        };
        /*sendAnalyticsBtn.clicked += () => {
            AnalyticsManager.LogEvent("test_event", new IAnalyticsManager.Parameter[] {
                new("param1", "value1"),
                new("param2", "123"),
                new("param3", "45.67"),
                new("param4", "true"),
            });
        };*/
    }

    private void SwitchTab(bool showDirectCalls) {
        directCallsTabButton.EnableInClassList("tab-button-active", showDirectCalls);
        automatedCallsTabButton.EnableInClassList("tab-button-active", !showDirectCalls);
        directCallsPanel.style.display = showDirectCalls ? DisplayStyle.Flex : DisplayStyle.None;
        automatedCallsPanel.style.display = showDirectCalls ? DisplayStyle.None : DisplayStyle.Flex;
    }
}
