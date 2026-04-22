using UnityEngine;

public class TrafficLightController : MonoBehaviour
{
    public Renderer redLight;
    public Renderer yellowLight;
    public Renderer greenLight;
    public float greenDuration = 12f;
    public float yellowDuration = 3f;
    public float redDuration = 12f;
    public float startOffset = 0f;

    private Material redMaterial;
    private Material yellowMaterial;
    private Material greenMaterial;

    private void Start()
    {
        redMaterial = redLight.material;
        yellowMaterial = yellowLight.material;
        greenMaterial = greenLight.material;
    }

    private void Update()
    {
        float totalTime = greenDuration + yellowDuration + redDuration;
        float timer = (Time.time + startOffset) % totalTime;

        if (timer < greenDuration)
        {
            SetLights(false, false, true);
        }
        else if (timer < greenDuration + yellowDuration)
        {
            SetLights(false, true, false);
        }
        else
        {
            SetLights(true, false, false);
        }
    }

    private void SetLights(bool redOn, bool yellowOn, bool greenOn)
    {
        redMaterial.color = redOn ? Color.red : new Color(0.18f, 0f, 0f);
        yellowMaterial.color = yellowOn ? Color.yellow : new Color(0.18f, 0.15f, 0f);
        greenMaterial.color = greenOn ? Color.green : new Color(0f, 0.15f, 0f);
    }
}
