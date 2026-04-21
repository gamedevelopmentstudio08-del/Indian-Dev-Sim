using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BusSimulator.UI
{
    /// <summary>
    /// Mobile-friendly button feedback:
    /// - smooth press scale
    /// - optional click handling through the Button component
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MenuButtonAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float pressScale = 0.94f;
        [SerializeField] private float animationSpeed = 16f;

        private RectTransform rectTransform;
        private Vector3 targetScale;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            targetScale = Vector3.one;
        }

        private void Update()
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.unscaledDeltaTime * animationSpeed);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            targetScale = Vector3.one * pressScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            targetScale = Vector3.one;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = Vector3.one;
        }
    }
}
