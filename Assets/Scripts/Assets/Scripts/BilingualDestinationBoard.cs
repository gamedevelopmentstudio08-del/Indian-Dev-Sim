using UnityEngine;
using TMPro;
using System.Collections.Generic;

// Indian Bus Simulator — v1.0
// Document ID: IBS-ART-001 | Section 8.3 & 9
// Purpose: Manages the scrolling bilingual (English/Local) destination board using emissive scrolling text logic.

public class BilingualDestinationBoard : MonoBehaviour
{
    [Header("Text Configuration")]
    public string destinationNameEN = "NEW DELHI";
    public string destinationNameLocal = "नई दिल्ली"; // Hindi Example
    public float scrollSpeed = 2.0f;
    public float transitionDelay = 3.0f;

    [Header("Components")]
    public TextMeshPro textMesh;
    public Material boardMaterial;

    private int currentLanguageIndex = 0;
    private float nextTransitionTime;
    private string[] languages;

    void Start()
    {
        languages = new string[] { destinationNameEN, destinationNameLocal };
        nextTransitionTime = Time.time + transitionDelay;
        
        if (textMesh != null)
        {
            textMesh.text = languages[0];
            // Set Emissive Color (Art Bible Page 2: Amber LED)
            textMesh.color = new Color(1.0f, 0.75f, 0.0f); 
        }
    }

    void Update()
    {
        HandleScrolling();
        HandleLanguageToggle();
    }

    private void HandleScrolling()
    {
        // HLSL Pseudocode logic from Art Bible Page 10
        // Simulates the 'ScrollOffset' found in the shader
        float offset = Mathf.Repeat(Time.time * scrollSpeed, 1.0f);
        
        if (boardMaterial != null)
        {
            boardMaterial.SetVector("_MainTex_ST", new Vector4(1, 1, -offset, 0));
        }
    }

    private void HandleLanguageToggle()
    {
        if (Time.time >= nextTransitionTime)
        {
            currentLanguageIndex = (currentLanguageIndex + 1) % languages.Length;
            textMesh.text = languages[currentLanguageIndex];
            nextTransitionTime = Time.time + transitionDelay;
            
            // Log for Dev Lab Dashboard
            Debug.Log($"Destination Board Toggle: {languages[currentLanguageIndex]}");
        }
    }

    public void UpdateDestination(string en, string local)
    {
        destinationNameEN = en;
        destinationNameLocal = local;
        languages[0] = en;
        languages[1] = local;
        textMesh.text = en;
    }
}
