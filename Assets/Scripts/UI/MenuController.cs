using System.Collections;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BusSimulator.UI
{
    /// <summary>
    /// Bind the buttons, scroll views, sliders, and TMP texts here.
    /// This controller can work with empty placeholder UI and generates simple row items at runtime.
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        [Serializable]
        public class PlayerProfile
        {
            public string playerName = "Driver";
            public int level = 1;
            public int coins = 5000;
        }

        [Serializable]
        public class BusEntry
        {
            public string busName;
            public int price;
            public float speed;
            public float fuel;
            public float power;
            public bool unlocked;
        }

        [Serializable]
        public class RouteEntry
        {
            public string routeName;
            public string difficulty;
            public float distanceKm;
            public int rewardCoins;
        }

        [Serializable]
        public class ShopEntry
        {
            public string itemName;
            public string description;
            public int price;
        }

        [Header("Manager")]
        [SerializeField] private UIScreenManager screenManager;
        [SerializeField] private UIManager uiManager;

        [Header("Main Menu")]
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text playerLevelText;
        [SerializeField] private TMP_Text playerCoinsText;
        [SerializeField] private TMP_Text eventBannerTitleText;
        [SerializeField] private TMP_Text eventBannerBodyText;

        [Header("Garage")]
        [SerializeField] private Transform garageContent;
        [SerializeField] private TMP_Text garageSelectedNameText;
        [SerializeField] private TMP_Text garageStatsText;
        [SerializeField] private Button garageSelectButton;
        [SerializeField] private Button garageUpgradeButton;

        [Header("Routes")]
        [SerializeField] private Transform routesContent;
        [SerializeField] private TMP_Text routeSelectedNameText;
        [SerializeField] private TMP_Text routeDetailsText;
        [SerializeField] private Button startJourneyButton;

        [Header("Shop")]
        [SerializeField] private Transform shopContent;
        [SerializeField] private TMP_Text shopCurrencyText;
        [SerializeField] private Button purchaseButton;

        [Header("Settings")]
        [SerializeField] private TMP_Dropdown graphicsDropdown;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TMP_InputField forwardKeyInput;
        [SerializeField] private TMP_InputField backKeyInput;
        [SerializeField] private TMP_InputField leftKeyInput;
        [SerializeField] private TMP_InputField rightKeyInput;
        [SerializeField] private TMP_InputField brakeKeyInput;

        [Header("Pause")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button pauseSettingsButton;
        [SerializeField] private Button exitToMainMenuButton;

        [Header("Mobile Menu")]
        [SerializeField] private CanvasGroup loadingScreenRoot;
        [SerializeField] private Slider loadingProgressBar;
        [SerializeField] private TMP_Text loadingProgressText;
        [SerializeField] private CanvasGroup fadeScreenRoot;

        [Header("Audio")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip menuMusicClip;
        [SerializeField] private AudioClip clickSoundClip;

        [Header("Data")]
        [SerializeField] private PlayerProfile profile = new PlayerProfile();
        [SerializeField] private List<BusEntry> buses = new List<BusEntry>
        {
            new BusEntry { busName = "City Standard", price = 0, speed = 82f, fuel = 180f, power = 145f, unlocked = true },
            new BusEntry { busName = "Intercity Premium", price = 12000, speed = 94f, fuel = 210f, power = 168f, unlocked = false },
            new BusEntry { busName = "Mountain Express", price = 26000, speed = 88f, fuel = 240f, power = 192f, unlocked = false }
        };

        [SerializeField] private List<RouteEntry> routes = new List<RouteEntry>
        {
            new RouteEntry { routeName = "City Loop", difficulty = "Easy", distanceKm = 8.4f, rewardCoins = 450 },
            new RouteEntry { routeName = "Hill Pass", difficulty = "Medium", distanceKm = 24.6f, rewardCoins = 1200 },
            new RouteEntry { routeName = "Mountain Express", difficulty = "Hard", distanceKm = 41.2f, rewardCoins = 2400 }
        };

        [SerializeField] private List<ShopEntry> shopItems = new List<ShopEntry>
        {
            new ShopEntry { itemName = "Starter Bus Pack", description = "Unlocks a city-ready bus skin pack.", price = 2500 },
            new ShopEntry { itemName = "Luxury Interior Pack", description = "Adds premium cabin styling.", price = 4500 },
            new ShopEntry { itemName = "Performance Upgrade", description = "Improves handling and climb power.", price = 7000 }
        };

        private int selectedBusIndex;
        private int selectedRouteIndex;
        private int selectedShopIndex;
        private Coroutine loadingRoutine;

        private void Awake()
        {
            if (screenManager == null)
            {
                screenManager = FindObjectOfType<UIScreenManager>();
            }

            if (uiManager == null)
            {
                uiManager = FindObjectOfType<UIManager>();
            }

            if (screenManager != null)
            {
                screenManager.SetAudio(musicSource, clickSoundClip);
            }
        }

        public void BindRuntimeManager(UIScreenManager manager)
        {
            screenManager = manager;
            if (screenManager != null)
            {
                screenManager.SetAudio(musicSource, clickSoundClip);
            }
        }

        public void BindRuntimeUi(
            TMP_Text runtimePlayerNameText,
            TMP_Text runtimePlayerLevelText,
            TMP_Text runtimePlayerCoinsText,
            TMP_Text runtimeEventBannerTitleText,
            TMP_Text runtimeEventBannerBodyText,
            Transform runtimeGarageContent,
            TMP_Text runtimeGarageSelectedNameText,
            TMP_Text runtimeGarageStatsText,
            Button runtimeGarageSelectButton,
            Button runtimeGarageUpgradeButton,
            Transform runtimeRoutesContent,
            TMP_Text runtimeRouteSelectedNameText,
            TMP_Text runtimeRouteDetailsText,
            Button runtimeStartJourneyButton,
            Transform runtimeShopContent,
            TMP_Text runtimeShopCurrencyText,
            Button runtimePurchaseButton,
            TMP_Dropdown runtimeGraphicsDropdown,
            Slider runtimeMasterVolumeSlider,
            Slider runtimeMusicVolumeSlider,
            Slider runtimeSfxVolumeSlider,
            TMP_InputField runtimeForwardKeyInput,
            TMP_InputField runtimeBackKeyInput,
            TMP_InputField runtimeLeftKeyInput,
            TMP_InputField runtimeRightKeyInput,
            TMP_InputField runtimeBrakeKeyInput,
            Button runtimeResumeButton,
            Button runtimeRestartButton,
            Button runtimePauseSettingsButton,
            Button runtimeExitToMainMenuButton)
        {
            playerNameText = runtimePlayerNameText;
            playerLevelText = runtimePlayerLevelText;
            playerCoinsText = runtimePlayerCoinsText;
            eventBannerTitleText = runtimeEventBannerTitleText;
            eventBannerBodyText = runtimeEventBannerBodyText;
            garageContent = runtimeGarageContent;
            garageSelectedNameText = runtimeGarageSelectedNameText;
            garageStatsText = runtimeGarageStatsText;
            garageSelectButton = runtimeGarageSelectButton;
            garageUpgradeButton = runtimeGarageUpgradeButton;
            routesContent = runtimeRoutesContent;
            routeSelectedNameText = runtimeRouteSelectedNameText;
            routeDetailsText = runtimeRouteDetailsText;
            startJourneyButton = runtimeStartJourneyButton;
            shopContent = runtimeShopContent;
            shopCurrencyText = runtimeShopCurrencyText;
            purchaseButton = runtimePurchaseButton;
            graphicsDropdown = runtimeGraphicsDropdown;
            masterVolumeSlider = runtimeMasterVolumeSlider;
            musicVolumeSlider = runtimeMusicVolumeSlider;
            sfxVolumeSlider = runtimeSfxVolumeSlider;
            forwardKeyInput = runtimeForwardKeyInput;
            backKeyInput = runtimeBackKeyInput;
            leftKeyInput = runtimeLeftKeyInput;
            rightKeyInput = runtimeRightKeyInput;
            brakeKeyInput = runtimeBrakeKeyInput;
            resumeButton = runtimeResumeButton;
            restartButton = runtimeRestartButton;
            pauseSettingsButton = runtimePauseSettingsButton;
            exitToMainMenuButton = runtimeExitToMainMenuButton;
        }

        public void InitializeRuntimeUi()
        {
            LoadSettings();
            RefreshProfile();
            PopulateGarage();
            PopulateRoutes();
            PopulateShop();
            ApplySelectedBus(0);
            ApplySelectedRoute(0);
            ApplySelectedShop(0);
            WireButtons();
        }

        private void Start()
        {
            LoadSettings();
            RefreshProfile();
            PopulateGarage();
            PopulateRoutes();
            PopulateShop();
            ApplySelectedBus(0);
            ApplySelectedRoute(0);
            ApplySelectedShop(0);
            WireButtons();
            StartMenuMusic();
        }

        public void BindRuntimeMobileUi(
            CanvasGroup runtimeLoadingScreenRoot,
            Slider runtimeLoadingProgressBar,
            TMP_Text runtimeLoadingProgressText,
            CanvasGroup runtimeFadeScreenRoot,
            AudioSource runtimeMusicSource,
            AudioSource runtimeSfxSource,
            AudioClip runtimeMenuMusicClip,
            AudioClip runtimeClickSoundClip)
        {
            loadingScreenRoot = runtimeLoadingScreenRoot;
            loadingProgressBar = runtimeLoadingProgressBar;
            loadingProgressText = runtimeLoadingProgressText;
            fadeScreenRoot = runtimeFadeScreenRoot;
            musicSource = runtimeMusicSource;
            sfxSource = runtimeSfxSource;
            menuMusicClip = runtimeMenuMusicClip;
            clickSoundClip = runtimeClickSoundClip;
            StartMenuMusic();
        }

        public void OnPlayPressed()
        {
            PlayGame();
        }

        public void OnCareerPressed()
        {
            OpenCareer();
        }

        public void OnGaragePressed()
        {
            OpenGarage();
        }

        public void OnRoutesPressed()
        {
            OpenRoutes();
        }

        public void OnShopPressed()
        {
            OpenShop();
        }

        public void OnSettingsPressed()
        {
            OpenSettings();
        }

        public void OnExitPressed()
        {
            ExitGame();
        }

        public void OnResumePressed()
        {
            PlayClickSound();
            screenManager?.ShowMainMenu();
        }

        public void OnRestartPressed()
        {
            PlayClickSound();
            screenManager?.LoadGameScene("GameScene");
        }

        public void OnPauseSettingsPressed()
        {
            PlayClickSound();
            screenManager?.ShowSettings();
        }

        public void OnExitToMainMenuPressed()
        {
            PlayClickSound();
            screenManager?.ShowMainMenu();
        }

        public void PlayGame()
        {
            Debug.Log("MenuController: Play pressed");
            PlayClickSound();
            if (screenManager != null)
            {
                screenManager.LoadGameScene("SampleScene");
            }
            else
            {
                SceneManager.LoadScene("SampleScene");
            }
        }

        public void OpenCareer()
        {
            Debug.Log("MenuController: Career pressed");
            PlayClickSound();
            screenManager?.ShowMainMenu();
        }

        public void OpenGarage()
        {
            Debug.Log("MenuController: Garage pressed");
            PlayClickSound();
            screenManager?.ShowGarage();
        }

        public void OpenRoutes()
        {
            Debug.Log("MenuController: Routes pressed");
            PlayClickSound();
            screenManager?.ShowRoutes();
        }

        public void OpenShop()
        {
            Debug.Log("MenuController: Shop pressed");
            PlayClickSound();
            screenManager?.ShowShop();
        }

        public void OpenSettings()
        {
            Debug.Log("MenuController: Settings pressed");
            PlayClickSound();
            screenManager?.ShowSettings();
        }

        public void ExitGame()
        {
            Debug.Log("MenuController: Exit pressed");
            PlayClickSound();
            Application.Quit();
        }

        public void BackToMenu()
        {
            Debug.Log("MenuController: Back to menu pressed");
            PlayClickSound();
            SceneManager.LoadScene("MainMenuScene");
        }

        public void UpgradeSelectedBus()
        {
            if (selectedBusIndex < 0 || selectedBusIndex >= buses.Count)
            {
                return;
            }

            BusEntry bus = buses[selectedBusIndex];
            if (profile.coins < bus.price)
            {
                return;
            }

            profile.coins -= bus.price;
            bus.price += 1500;
            bus.speed += 2.5f;
            bus.power += 3.5f;
            bus.fuel += 5f;
            RefreshProfile();
            ApplySelectedBus(selectedBusIndex);
        }

        public void StartSelectedRoute()
        {
            screenManager?.LoadGameScene("GameScene");
        }

        public void PurchaseSelectedItem()
        {
            if (selectedShopIndex < 0 || selectedShopIndex >= shopItems.Count)
            {
                return;
            }

            ShopEntry item = shopItems[selectedShopIndex];
            if (profile.coins < item.price)
            {
                return;
            }

            profile.coins -= item.price;
            item.price += 1000;
            RefreshProfile();
            ApplySelectedShop(selectedShopIndex);
        }

        public void SaveSettings()
        {
            if (graphicsDropdown != null)
            {
                PlayerPrefs.SetInt("bussim.graphics", graphicsDropdown.value);
            }

            if (masterVolumeSlider != null)
            {
                PlayerPrefs.SetFloat("bussim.audio.master", masterVolumeSlider.value);
            }

            if (musicVolumeSlider != null)
            {
                PlayerPrefs.SetFloat("bussim.audio.music", musicVolumeSlider.value);
            }

            if (sfxVolumeSlider != null)
            {
                PlayerPrefs.SetFloat("bussim.audio.sfx", sfxVolumeSlider.value);
            }

            SaveBinding("bussim.key.forward", forwardKeyInput);
            SaveBinding("bussim.key.back", backKeyInput);
            SaveBinding("bussim.key.left", leftKeyInput);
            SaveBinding("bussim.key.right", rightKeyInput);
            SaveBinding("bussim.key.brake", brakeKeyInput);

            PlayerPrefs.Save();
        }

        private void StartMenuMusic()
        {
            if (musicSource == null || menuMusicClip == null)
            {
                return;
            }

            musicSource.clip = menuMusicClip;
            musicSource.loop = true;
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }

        private void PlayClickSound()
        {
            if (sfxSource != null && clickSoundClip != null)
            {
                sfxSource.PlayOneShot(clickSoundClip, 0.85f);
            }
        }

        private void RefreshProfile()
        {
            if (playerNameText != null)
            {
                playerNameText.text = profile.playerName;
            }

            if (playerLevelText != null)
            {
                playerLevelText.text = "Level " + profile.level;
            }

            if (playerCoinsText != null)
            {
                playerCoinsText.text = profile.coins.ToString("N0") + " Coins";
            }

            if (shopCurrencyText != null)
            {
                shopCurrencyText.text = "Balance: " + profile.coins.ToString("N0");
            }
        }

        private void PopulateGarage()
        {
            ClearContainer(garageContent);
            if (garageContent == null)
            {
                return;
            }

            for (int i = 0; i < buses.Count; i++)
            {
                int busIndex = i;
                BusEntry bus = buses[i];
                CreateListRow(
                    garageContent,
                    bus.busName,
                    bus.unlocked ? "Unlocked" : "Locked",
                    () => ApplySelectedBus(busIndex)
                );
            }
        }

        private void PopulateRoutes()
        {
            ClearContainer(routesContent);
            if (routesContent == null)
            {
                return;
            }

            for (int i = 0; i < routes.Count; i++)
            {
                int routeIndex = i;
                RouteEntry route = routes[i];
                CreateListRow(
                    routesContent,
                    route.routeName,
                    route.difficulty + " | " + route.distanceKm.ToString("0.0") + " km",
                    () => ApplySelectedRoute(routeIndex)
                );
            }
        }

        private void PopulateShop()
        {
            ClearContainer(shopContent);
            if (shopContent == null)
            {
                return;
            }

            for (int i = 0; i < shopItems.Count; i++)
            {
                int shopIndex = i;
                ShopEntry item = shopItems[i];
                CreateListRow(
                    shopContent,
                    item.itemName,
                    item.description + " | " + item.price.ToString("N0") + " coins",
                    () => ApplySelectedShop(shopIndex)
                );
            }
        }

        public void ApplySelectedBus(int index)
        {
            if (buses.Count == 0)
            {
                return;
            }

            selectedBusIndex = Mathf.Clamp(index, 0, buses.Count - 1);
            BusEntry bus = buses[selectedBusIndex];

            if (garageSelectedNameText != null)
            {
                garageSelectedNameText.text = bus.busName;
            }

            if (garageStatsText != null)
            {
                garageStatsText.text =
                    "Speed: " + bus.speed.ToString("0") + "\n" +
                    "Fuel: " + bus.fuel.ToString("0") + "\n" +
                    "Power: " + bus.power.ToString("0") + "\n" +
                    "Price: " + bus.price.ToString("N0");
            }
        }

        public void ApplySelectedRoute(int index)
        {
            if (routes.Count == 0)
            {
                return;
            }

            selectedRouteIndex = Mathf.Clamp(index, 0, routes.Count - 1);
            RouteEntry route = routes[selectedRouteIndex];

            if (routeSelectedNameText != null)
            {
                routeSelectedNameText.text = route.routeName;
            }

            if (routeDetailsText != null)
            {
                routeDetailsText.text =
                    "Difficulty: " + route.difficulty + "\n" +
                    "Distance: " + route.distanceKm.ToString("0.0") + " km\n" +
                    "Reward: " + route.rewardCoins.ToString("N0") + " coins";
            }
        }

        public void ApplySelectedShop(int index)
        {
            if (shopItems.Count == 0)
            {
                return;
            }

            selectedShopIndex = Mathf.Clamp(index, 0, shopItems.Count - 1);
            ShopEntry item = shopItems[selectedShopIndex];

            if (purchaseButton != null)
            {
                TMP_Text buttonLabel = purchaseButton.GetComponentInChildren<TMP_Text>();
                if (buttonLabel != null)
                {
                    buttonLabel.text = "Buy " + item.price.ToString("N0");
                }
            }
        }

        private void LoadSettings()
        {
            if (graphicsDropdown != null)
            {
                graphicsDropdown.value = PlayerPrefs.GetInt("bussim.graphics", graphicsDropdown.value);
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = PlayerPrefs.GetFloat("bussim.audio.master", masterVolumeSlider.value);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = PlayerPrefs.GetFloat("bussim.audio.music", musicVolumeSlider.value);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = PlayerPrefs.GetFloat("bussim.audio.sfx", sfxVolumeSlider.value);
            }

            LoadBinding("bussim.key.forward", forwardKeyInput);
            LoadBinding("bussim.key.back", backKeyInput);
            LoadBinding("bussim.key.left", leftKeyInput);
            LoadBinding("bussim.key.right", rightKeyInput);
            LoadBinding("bussim.key.brake", brakeKeyInput);
        }

        private static void SaveBinding(string key, TMP_InputField field)
        {
            if (field != null)
            {
                PlayerPrefs.SetString(key, field.text);
            }
        }

        private static void LoadBinding(string key, TMP_InputField field)
        {
            if (field != null)
            {
                field.text = PlayerPrefs.GetString(key, field.text);
            }
        }

        private void WireButtons()
        {
            WireSceneMenuButtons();

            if (garageSelectButton != null)
            {
                garageSelectButton.onClick.RemoveAllListeners();
                garageSelectButton.onClick.AddListener(() => ApplySelectedBus(selectedBusIndex));
            }

            if (garageUpgradeButton != null)
            {
                garageUpgradeButton.onClick.RemoveAllListeners();
                garageUpgradeButton.onClick.AddListener(UpgradeSelectedBus);
            }

            if (startJourneyButton != null)
            {
                startJourneyButton.onClick.RemoveAllListeners();
                startJourneyButton.onClick.AddListener(StartSelectedRoute);
            }

            if (purchaseButton != null)
            {
                purchaseButton.onClick.RemoveAllListeners();
                purchaseButton.onClick.AddListener(PurchaseSelectedItem);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveAllListeners();
                resumeButton.onClick.AddListener(OnResumePressed);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(OnRestartPressed);
            }

            if (pauseSettingsButton != null)
            {
                pauseSettingsButton.onClick.RemoveAllListeners();
                pauseSettingsButton.onClick.AddListener(OnPauseSettingsPressed);
            }

            if (exitToMainMenuButton != null)
            {
                exitToMainMenuButton.onClick.RemoveAllListeners();
                exitToMainMenuButton.onClick.AddListener(BackToMenu);
            }
        }

        private void WireSceneMenuButtons()
        {
            WireButtonByName("PlayButton", PlayGame);
            WireButtonByName("ExitButton", ExitGame);
            WireButtonByName("BackButton", BackToMenu);
        }

        private static void WireButtonByName(string objectName, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = GameObject.Find(objectName);
            if (buttonObject == null)
            {
                return;
            }

            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private static void ClearContainer(Transform container)
        {
            if (container == null)
            {
                return;
            }

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(container.GetChild(i).gameObject);
            }
        }

        private static Button CreateListRow(Transform parent, string title, string subtitle, Action onClick)
        {
            GameObject row = new GameObject(title + " Row", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            row.transform.SetParent(parent, false);

            Image image = row.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.08f);

            LayoutElement layout = row.GetComponent<LayoutElement>();
            layout.minHeight = 68f;
            layout.preferredHeight = 76f;

            Button button = row.GetComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick.Invoke());
            }

            GameObject textBlock = new GameObject("Text", typeof(RectTransform));
            textBlock.transform.SetParent(row.transform, false);

            RectTransform textRect = textBlock.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18f, 8f);
            textRect.offsetMax = new Vector2(-18f, -8f);

            TMP_Text titleText = CreateTMPText("Title", textBlock.transform, title, 24, FontStyles.Bold);
            TMP_Text subtitleText = CreateTMPText("Subtitle", textBlock.transform, subtitle, 16, FontStyles.Normal);
            subtitleText.color = new Color(1f, 1f, 1f, 0.75f);
            subtitleText.rectTransform.anchorMin = new Vector2(0f, 0f);
            subtitleText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            subtitleText.rectTransform.offsetMin = new Vector2(0f, 0f);
            subtitleText.rectTransform.offsetMax = new Vector2(0f, 0f);
            titleText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleText.rectTransform.offsetMin = new Vector2(0f, 0f);
            titleText.rectTransform.offsetMax = new Vector2(0f, 0f);

            return button;
        }

        private static TMP_Text CreateTMPText(string name, Transform parent, string text, int size, FontStyles style)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            TMP_Text tmp = textObject.GetComponent<TMP_Text>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = Color.white;
            TMP_FontAsset font = GetSafeDefaultFontAsset();
            if (font != null)
            {
                tmp.font = font;
            }

            RectTransform rect = tmp.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return tmp;
        }

        private static TMP_FontAsset GetSafeDefaultFontAsset()
        {
            try
            {
                return TMP_Settings.defaultFontAsset;
            }
            catch
            {
                return null;
            }
        }
    }
}
