using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class SceneUiHelper
{
    public static void EnsureEventSystem()
    {
        EventSystem[] systems = Object.FindObjectsOfType<EventSystem>(true);
        EventSystem current = systems.Length > 0 ? systems[0] : null;

        for (int i = 1; i < systems.Length; i++)
        {
            if (systems[i] != null)
            {
                Object.Destroy(systems[i].gameObject);
            }
        }

        if (current == null)
        {
            GameObject go = new GameObject("EventSystem");
            current = go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
            return;
        }

        if (current.GetComponent<StandaloneInputModule>() == null)
        {
            current.gameObject.AddComponent<StandaloneInputModule>();
        }

        TouchInputModule touch = current.GetComponent<TouchInputModule>();
        if (touch != null)
        {
            Object.Destroy(touch);
        }
    }

    public static Canvas EnsureOverlayCanvas(string canvasName)
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject(canvasName);
            canvas = canvasObject.AddComponent<Canvas>();
        }

        canvas.gameObject.SetActive(true);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 1f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        return canvas;
    }
}
