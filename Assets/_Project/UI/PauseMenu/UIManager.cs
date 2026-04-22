using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BusSimulator.UI
{
    public enum UIScreen
    {
        MainMenu,
        HUD,
        PauseMenu,
        Settings,
        Garage,
        Routes,
        Shop
    }

    [DefaultExecutionOrder(-100)]
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Screen Roots")]
        [SerializeField] private GameObject mainMenuScreen;
        [SerializeField] private GameObject hudScreen;
        [SerializeField] private GameObject pauseMenuScreen;
        [SerializeField] private GameObject settingsScreen;
        [SerializeField] private GameObject garageScreen;
        [SerializeField] private GameObject routesScreen;
        [SerializeField] private GameObject shopScreen;

        [Header("Controllers")]
        [SerializeField] private MonoBehaviour hudControllerBehaviour;
        [SerializeField] private MenuController menuController;

        [Header("Transitions")]
        [SerializeField] private bool useSmoothTransitions = true;
        [SerializeField] private float transitionDuration = 0.18f;
        [SerializeField] private bool pauseTimeWhenInGame = true;
        [SerializeField] private bool openMainMenuOnStart = true;
        [SerializeField] private bool allowEscapePauseToggle = true;

        public UIScreen CurrentScreen { get; private set; } = UIScreen.MainMenu;

        private Coroutine transitionRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CacheCanvasGroups();
        }

        private void Start()
        {
            if (openMainMenuOnStart)
            {
                ShowMainMenu(true);
            }
            else
            {
                ShowHUD(true);
            }
        }

        private void Update()
        {
            if (!allowEscapePauseToggle)
            {
                return;
            }

            if (!Input.GetKeyDown(KeyCode.Escape))
            {
                return;
            }

            if (CurrentScreen == UIScreen.HUD)
            {
                ShowPauseMenu();
            }
            else if (CurrentScreen == UIScreen.PauseMenu)
            {
                ShowHUD();
            }
        }

        public void ShowMainMenu(bool instant = false)
        {
            PauseGame(false);
            SwitchToScreen(UIScreen.MainMenu, instant);
        }

        public void ShowHUD(bool instant = false)
        {
            if (pauseTimeWhenInGame)
            {
                PauseGame(false);
            }

            SwitchToScreen(UIScreen.HUD, instant);
        }

        public void ShowPauseMenu(bool instant = false)
        {
            PauseGame(true);
            SwitchToScreen(UIScreen.PauseMenu, instant);
        }

        public void ShowSettings(bool instant = false)
        {
            SwitchToScreen(UIScreen.Settings, instant);
        }

        public void ShowGarage(bool instant = false)
        {
            SwitchToScreen(UIScreen.Garage, instant);
        }

        public void ShowRoutes(bool instant = false)
        {
            SwitchToScreen(UIScreen.Routes, instant);
        }

        public void ShowShop(bool instant = false)
        {
            SwitchToScreen(UIScreen.Shop, instant);
        }

        public void RestartCurrentScene()
        {
            PauseGame(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void PauseGame(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;
        }

        public void SetHUDController(MonoBehaviour controller)
        {
            hudControllerBehaviour = controller;
        }

        public void SetMenuController(MenuController controller)
        {
            menuController = controller;
        }

        public void BindRuntimeUi(
            GameObject runtimeMainMenu,
            GameObject runtimeHud,
            GameObject runtimePauseMenu,
            GameObject runtimeSettings,
            GameObject runtimeGarage,
            GameObject runtimeRoutes,
            GameObject runtimeShop,
            MonoBehaviour runtimeHudController,
            MenuController runtimeMenuController)
        {
            mainMenuScreen = runtimeMainMenu;
            hudScreen = runtimeHud;
            pauseMenuScreen = runtimePauseMenu;
            settingsScreen = runtimeSettings;
            garageScreen = runtimeGarage;
            routesScreen = runtimeRoutes;
            shopScreen = runtimeShop;
            hudControllerBehaviour = runtimeHudController;
            menuController = runtimeMenuController;
            CacheCanvasGroups();
        }

        private void SwitchToScreen(UIScreen screen, bool instant)
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            CurrentScreen = screen;
            UpdateScreenBindings();

            if (!useSmoothTransitions || instant)
            {
                SetImmediateVisibility(screen);
                return;
            }

            transitionRoutine = StartCoroutine(CrossFadeRoutine(screen));
        }

        private void UpdateScreenBindings()
        {
            if (hudControllerBehaviour != null)
            {
                hudControllerBehaviour.gameObject.SendMessage(
                    "SetVisible",
                    CurrentScreen == UIScreen.HUD,
                    SendMessageOptions.DontRequireReceiver);
            }
        }

        private IEnumerator CrossFadeRoutine(UIScreen targetScreen)
        {
            CanvasGroup[] groups = GetAllScreenGroups();
            CanvasGroup targetGroup = GetGroup(targetScreen);

            for (int i = 0; i < groups.Length; i++)
            {
                CanvasGroup group = groups[i];
                if (group == null)
                {
                    continue;
                }

                group.gameObject.SetActive(true);
                if (group != targetGroup)
                {
                    group.blocksRaycasts = false;
                    group.interactable = false;
                }
            }

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float blend = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, transitionDuration));

                for (int i = 0; i < groups.Length; i++)
                {
                    CanvasGroup group = groups[i];
                    if (group == null)
                    {
                        continue;
                    }

                    if (group == targetGroup)
                    {
                        group.alpha = blend;
                    }
                    else
                    {
                        group.alpha = 1f - blend;
                    }
                }

                yield return null;
            }

            SetImmediateVisibility(targetScreen);
            transitionRoutine = null;
        }

        private void SetImmediateVisibility(UIScreen screen)
        {
            CanvasGroup[] groups = GetAllScreenGroups();
            CanvasGroup targetGroup = GetGroup(screen);

            for (int i = 0; i < groups.Length; i++)
            {
                CanvasGroup group = groups[i];
                if (group == null)
                {
                    continue;
                }

                bool visible = group == targetGroup;
                group.gameObject.SetActive(visible);
                group.alpha = visible ? 1f : 0f;
                group.interactable = visible;
                group.blocksRaycasts = visible;
            }

            UpdateScreenBindings();
        }

        private void CacheCanvasGroups()
        {
            EnsureCanvasGroup(mainMenuScreen);
            EnsureCanvasGroup(hudScreen);
            EnsureCanvasGroup(pauseMenuScreen);
            EnsureCanvasGroup(settingsScreen);
            EnsureCanvasGroup(garageScreen);
            EnsureCanvasGroup(routesScreen);
            EnsureCanvasGroup(shopScreen);
        }

        private CanvasGroup EnsureCanvasGroup(GameObject root)
        {
            if (root == null)
            {
                return null;
            }

            CanvasGroup group = root.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = root.AddComponent<CanvasGroup>();
            }

            return group;
        }

        private CanvasGroup[] GetAllScreenGroups()
        {
            return new[]
            {
                GetGroup(UIScreen.MainMenu),
                GetGroup(UIScreen.HUD),
                GetGroup(UIScreen.PauseMenu),
                GetGroup(UIScreen.Settings),
                GetGroup(UIScreen.Garage),
                GetGroup(UIScreen.Routes),
                GetGroup(UIScreen.Shop)
            };
        }

        private CanvasGroup GetGroup(UIScreen screen)
        {
            switch (screen)
            {
                case UIScreen.MainMenu: return mainMenuScreen != null ? mainMenuScreen.GetComponent<CanvasGroup>() : null;
                case UIScreen.HUD: return hudScreen != null ? hudScreen.GetComponent<CanvasGroup>() : null;
                case UIScreen.PauseMenu: return pauseMenuScreen != null ? pauseMenuScreen.GetComponent<CanvasGroup>() : null;
                case UIScreen.Settings: return settingsScreen != null ? settingsScreen.GetComponent<CanvasGroup>() : null;
                case UIScreen.Garage: return garageScreen != null ? garageScreen.GetComponent<CanvasGroup>() : null;
                case UIScreen.Routes: return routesScreen != null ? routesScreen.GetComponent<CanvasGroup>() : null;
                case UIScreen.Shop: return shopScreen != null ? shopScreen.GetComponent<CanvasGroup>() : null;
                default: return null;
            }
        }
    }
}
