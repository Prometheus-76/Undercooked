using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Author: Darcy Matheson
// Purpose: Generic enemy health bar script, controls display of information, enemy name and other GUI effects for the health bar

public class HealthBarUI : MonoBehaviour
{
    #region Variables

    #region Internal

    private float cameraForwardSimilarity; // The dot product between the main camera and the canvas transform forward
    private float damageLingerTimer; // The timer float used for lingering the health bar after the enemy takes damage
    private int lastHealth; // The enemy's health on the last frame
    private float overallAlphaMultiplier; // The multiplier applied to the alpha component of all UI elements in the health bar

    #endregion

    #region Parameters

    #region Scale and Rotation
    [Header("Scale and Rotation")]
    public float scaleRate = 1f;
    public float minimumScale = 1f;
    public float maximumScale = 1f;

    #endregion

    #region Appearance
    [Header("Appearance")]

    [Tooltip("The gradient that controls bar colour as the enemy takes damage.")]
    public Gradient healthBarGradient;

    [Tooltip("The rate at which the lingering underlay of the health bar depletes."), Range(1f, 10f)]
    public float delayedDecayRate = 1f;
    #endregion

    #region Fade In/Out
    [Header("Fade In/Out")]

    [Tooltip("How closely the player should be look at the health bar for it to appear."), Range(0.5f, 0.95f)]
    public float minimumLookSimilarity = 0.9f;

    [Tooltip("The length of time that the bar lingers after the enemy takes damage."), Range(1f , 10f)]
    public float damageLingerDuration = 5f;

    [Tooltip("The time it takes for the bar to fade in/out."), Range(0.1f, 1f)]
    public float overallFadeTime = 0.2f;
    #endregion

    #endregion

    #region Components
    [Header("Components")]

    public Image currentFillImage;
    public Image delayedFillImage;
    public Image borderImage;
    public Image backgroundImage;
    public TextMeshProUGUI enemyNameText;

    private RectTransform canvasTransform;
    private Transform mainCameraTransform;
    private Enemy enemyScript;
    #endregion

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        canvasTransform = GetComponent<RectTransform>();
        mainCameraTransform = Camera.main.transform;
        enemyScript = canvasTransform.parent.GetComponent<Enemy>();

        overallAlphaMultiplier = 0f;

        damageLingerTimer = 0f;
        lastHealth = enemyScript.currentHealth;

        // Assign the enemy's name
        enemyNameText.text = enemyScript.enemyName;
    }

    // Update is called once per frame
    void Update()
    {
        #region Rotate Towards Camera

        // Rotate towards camera
        Quaternion rotationToCamera = Quaternion.LookRotation(canvasTransform.position - mainCameraTransform.position);
        canvasTransform.rotation = rotationToCamera;

        // Scale with distance
        canvasTransform.localScale = Vector3.one * Mathf.Clamp((Vector3.Distance(mainCameraTransform.position, canvasTransform.position) / scaleRate), minimumScale, maximumScale);

        // Determine if the player is looking at the canvas
        cameraForwardSimilarity = Vector3.Dot(canvasTransform.forward, mainCameraTransform.forward);

        #endregion

        #region Current and Delayed Fill

        currentFillImage.fillAmount = Mathf.Clamp01((float)enemyScript.currentHealth / (float)enemyScript.maxHealth);

        // Update delayed fill
        if (currentFillImage.fillAmount < delayedFillImage.fillAmount)
        {
            // Delay rate is faster when the difference between current and delayed is higher
            float newFillAmount = Mathf.Clamp01(delayedFillImage.fillAmount - ((delayedFillImage.fillAmount - currentFillImage.fillAmount) * delayedDecayRate * Time.deltaTime));
            delayedFillImage.fillAmount = (newFillAmount - currentFillImage.fillAmount > 0.005f) ? newFillAmount : currentFillImage.fillAmount;
        }

        #endregion

        #region Determine Recent Damage

        // Refresh the timer
        if (enemyScript.currentHealth < lastHealth || enemyScript.currentHealth <= 0)
        {
            damageLingerTimer = damageLingerDuration;
        }

        // Assign the enemy health last frame
        lastHealth = enemyScript.currentHealth;

        // Adjust timer if damage was recently taken
        if (damageLingerTimer > 0f)
        {
            damageLingerTimer -= Time.deltaTime;
        }
        else
        {
            damageLingerTimer = 0f;
        }

        #endregion

        #region Fade All Grapple UI

        // Fade in/out based on various factors
        if (Physics.Linecast(mainCameraTransform.position, canvasTransform.position, enemyScript.environmentLayers) == false && (damageLingerTimer > 0f || cameraForwardSimilarity >= minimumLookSimilarity) && enemyScript.currentHealth > 0)
        {
            overallAlphaMultiplier += (Time.deltaTime / overallFadeTime);
        }
        else
        {
            overallAlphaMultiplier -= (Time.deltaTime / overallFadeTime);
        }

        // The overall alpha of the entire grapple UI, 1 is full opacity and 0 is invisible
        overallAlphaMultiplier = Mathf.Clamp(overallAlphaMultiplier, 0f, 1f);

        // Calculate new colours with fade multiplier
        Color newColour = Color.white;

        newColour = healthBarGradient.Evaluate(currentFillImage.fillAmount);
        newColour.a = 1f * overallAlphaMultiplier;
        currentFillImage.color = newColour;

        newColour = delayedFillImage.color;
        newColour.a = 1f * overallAlphaMultiplier;
        delayedFillImage.color = newColour;

        newColour = borderImage.color;
        newColour.a = 1f * overallAlphaMultiplier;
        borderImage.color = newColour;

        newColour = backgroundImage.color;
        newColour.a = 1f * overallAlphaMultiplier;
        backgroundImage.color = newColour;

        newColour = enemyNameText.color;
        newColour.a = 1f * overallAlphaMultiplier;
        enemyNameText.color = newColour;

        #endregion
    }
}
