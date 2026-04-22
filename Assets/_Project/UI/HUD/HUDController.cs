using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BusSimulator.UI
{
    /// <summary>
    /// Wire these fields in the HUD panel.
    /// Works with placeholder Canvas + TextMeshPro elements, no external art needed.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Readouts")]
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private TMP_Text gearText;
        [SerializeField] private TMP_Text fuelText;
        [SerializeField] private TMP_Text passengersText;
        [SerializeField] private TMP_Text distanceText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text miniMapLabel;
        [SerializeField] private TMP_Text warningText;

        [Header("Panels")]
        [SerializeField] private CanvasGroup hudRoot;
        [SerializeField] private CanvasGroup warningRoot;
        [SerializeField] private Image miniMapPlaceholder;

        [Header("Animation")]
        [SerializeField] private float warningPulseSpeed = 3.25f;

        private readonly List<string> warnings = new List<string>();

        public void BindRuntimeUi(
            CanvasGroup runtimeHudRoot,
            CanvasGroup runtimeWarningRoot,
            Image runtimeMiniMapPlaceholder,
            TMP_Text runtimeSpeedText,
            TMP_Text runtimeGearText,
            TMP_Text runtimeFuelText,
            TMP_Text runtimePassengersText,
            TMP_Text runtimeDistanceText,
            TMP_Text runtimeTimeText,
            TMP_Text runtimeMiniMapLabel,
            TMP_Text runtimeWarningText)
        {
            hudRoot = runtimeHudRoot;
            warningRoot = runtimeWarningRoot;
            miniMapPlaceholder = runtimeMiniMapPlaceholder;
            speedText = runtimeSpeedText;
            gearText = runtimeGearText;
            fuelText = runtimeFuelText;
            passengersText = runtimePassengersText;
            distanceText = runtimeDistanceText;
            timeText = runtimeTimeText;
            miniMapLabel = runtimeMiniMapLabel;
            warningText = runtimeWarningText;
        }

        private void Update()
        {
            if (warningRoot != null && warnings.Count > 0)
            {
                float pulse = 0.55f + Mathf.Sin(Time.unscaledTime * warningPulseSpeed) * 0.18f;
                warningRoot.alpha = Mathf.Clamp01(pulse);
            }
            else if (warningRoot != null)
            {
                warningRoot.alpha = 0f;
            }
        }

        public void SetVisible(bool visible)
        {
            if (hudRoot == null)
            {
                return;
            }

            hudRoot.gameObject.SetActive(visible);
            hudRoot.alpha = visible ? 1f : 0f;
            hudRoot.interactable = visible;
            hudRoot.blocksRaycasts = visible;
        }

        public void SetSpeed(float kmh)
        {
            if (speedText != null)
            {
                speedText.text = Mathf.Max(0f, kmh).ToString("0") + " km/h";
            }
        }

        public void SetGear(string gear)
        {
            if (gearText != null)
            {
                gearText.text = "Gear: " + gear;
            }
        }

        public void SetFuel(float percent)
        {
            if (fuelText != null)
            {
                fuelText.text = "Fuel: " + Mathf.Clamp(percent, 0f, 100f).ToString("0") + "%";
            }
        }

        public void SetPassengers(int currentPassengers, int capacity)
        {
            if (passengersText != null)
            {
                passengersText.text = "Passengers: " + currentPassengers + "/" + capacity;
            }
        }

        public void SetDistanceAndTime(float distanceKm, float minutesElapsed)
        {
            if (distanceText != null)
            {
                distanceText.text = "Distance: " + distanceKm.ToString("0.0") + " km";
            }

            if (timeText != null)
            {
                int minutes = Mathf.Max(0, Mathf.RoundToInt(minutesElapsed));
                timeText.text = "Time: " + minutes + " min";
            }
        }

        public void SetMiniMapLabel(string label)
        {
            if (miniMapLabel != null)
            {
                miniMapLabel.text = label;
            }
        }

        public void SetMiniMapPlaceholderAlpha(float alpha)
        {
            if (miniMapPlaceholder != null)
            {
                Color color = miniMapPlaceholder.color;
                color.a = Mathf.Clamp01(alpha);
                miniMapPlaceholder.color = color;
            }
        }

        public void SetWarnings(bool lowFuel, bool doorOpen, bool engineDamage)
        {
            warnings.Clear();

            if (lowFuel)
            {
                warnings.Add("LOW FUEL");
            }

            if (doorOpen)
            {
                warnings.Add("DOOR OPEN");
            }

            if (engineDamage)
            {
                warnings.Add("ENGINE DAMAGE");
            }

            if (warningText != null)
            {
                warningText.text = warnings.Count == 0 ? string.Empty : string.Join("\n", warnings);
            }
        }
    }
}
