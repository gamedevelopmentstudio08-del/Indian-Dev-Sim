using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BusSimulator.UI
{
    /// <summary>
    /// Strict one-screen-at-a-time UI manager for mobile menus.
    /// Keeps inactive screens fully non-interactive so they cannot block raycasts.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class UIScreenManager : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] private CanvasGroup mainMenuScreen;
        [SerializeField] private CanvasGroup garageScreen;
        [SerializeField] private CanvasGroup routesScreen;
        [SerializeField] private CanvasGroup shopScreen;
        [SerializeField] private CanvasGroup settingsScreen;
        [SerializeField] private CanvasGroup pauseMenuScreen;

        [Header("Transition")]
        [SerializeField] private bool useFadeTransition = true;
        [SerializeField] private float fadeDuration = 0.18f;
        [SerializeField] private float transitionDelay = 0.2f;

        [Header("Audio")]
        [SerializeField] private AudioSource clickSource;
        [SerializeField] private AudioClip clickClip;

        public enum ScreenId
        {
            MainMenu,
            Garage,
            Routes,
            Shop,
            Settings,
            Pause
        }

        public ScreenId CurrentScreen { get; private set; } = ScreenId.MainMenu;

        private Coroutine transitionRoutine;

        private void Awake()
        {
            DisableAllScreens();
        }

        private void Start()
        {
            ShowMainMenu();
        }

        public void SetScreens(
            CanvasGroup runtimeMainMenu,
            CanvasGroup runtimeGarage,
            CanvasGroup runtimeRoutes,
            CanvasGroup runtimeShop,
            CanvasGroup runtimeSettings,
            CanvasGroup runtimePauseMenu)
        {
            mainMenuScreen = runtimeMainMenu;
            garageScreen = runtimeGarage;
            routesScreen = runtimeRoutes;
            shopScreen = runtimeShop;
            settingsScreen = runtimeSettings;
            pauseMenuScreen = runtimePauseMenu;
            DisableAllScreens();
        }

        public void SetAudio(AudioSource source, AudioClip clip)
        {
            clickSource = source;
            clickClip = clip;
        }

        public void ShowMainMenu()
        {
            SwitchTo(ScreenId.MainMenu, "MainMenu");
        }

        public void ShowGarage()
        {
            SwitchTo(ScreenId.Garage, "Garage");
        }

        public void ShowRoutes()
        {
            SwitchTo(ScreenId.Routes, "Routes");
        }

        public void ShowShop()
        {
            SwitchTo(ScreenId.Shop, "Shop");
        }

        public void ShowSettings()
        {
            SwitchTo(ScreenId.Settings, "Settings");
        }

        public void ShowPause()
        {
            SwitchTo(ScreenId.Pause, "Pause");
        }

        public void LoadGameScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        public void DisableAllScreens()
        {
            SetScreenState(mainMenuScreen, false);
            SetScreenState(garageScreen, false);
            SetScreenState(routesScreen, false);
            SetScreenState(shopScreen, false);
            SetScreenState(settingsScreen, false);
            SetScreenState(pauseMenuScreen, false);
        }

        private void SwitchTo(ScreenId target, string label)
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            transitionRoutine = StartCoroutine(SwitchRoutine(target, label));
        }

        private IEnumerator SwitchRoutine(ScreenId target, string label)
        {
            DisableAllScreens();
            yield return new WaitForSecondsRealtime(transitionDelay);

            CanvasGroup targetGroup = GetGroup(target);
            if (targetGroup == null)
            {
                Debug.LogWarning("UIScreenManager: missing screen " + label);
                transitionRoutine = null;
                yield break;
            }

            targetGroup.gameObject.SetActive(true);
            targetGroup.alpha = useFadeTransition ? 0f : 1f;
            targetGroup.interactable = true;
            targetGroup.blocksRaycasts = true;
            CurrentScreen = target;

            Debug.Log("UIScreenManager: active screen = " + label);

            if (useFadeTransition)
            {
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    targetGroup.alpha = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, fadeDuration));
                    yield return null;
                }
            }

            targetGroup.alpha = 1f;
            transitionRoutine = null;
        }

        private void SetScreenState(CanvasGroup group, bool visible)
        {
            if (group == null)
            {
                return;
            }

            group.gameObject.SetActive(visible);
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        private CanvasGroup GetGroup(ScreenId target)
        {
            switch (target)
            {
                case ScreenId.MainMenu: return mainMenuScreen;
                case ScreenId.Garage: return garageScreen;
                case ScreenId.Routes: return routesScreen;
                case ScreenId.Shop: return shopScreen;
                case ScreenId.Settings: return settingsScreen;
                case ScreenId.Pause: return pauseMenuScreen;
                default: return null;
            }
        }

    }
}
