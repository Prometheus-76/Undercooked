using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Author: Darcy Matheson
// Purpose: Displays and influences the appearance of the grapple point UI.

public class GrapplePointUI : MonoBehaviour
{
    #region Variables

    #region Internal

    private float pointSelectedBaseAlpha;
    private float radialProgressBaseAlpha;
    private float progressUnderlayBaseAlpha;
    private float fadeUnderlayBaseAlpha;
    private Transform mainCameraTransform;
    private float overallAlphaMultiplier = 1f;
    private float highlightAlphaMultiplier = 1f;
    private bool isDetected = false;

    #endregion

    #region Parameters
    [Header("Parameters")]

    [Tooltip("The reference distance for scaling UI, lower values means larger UI."), Range(1f, 10f)]
    public float scaleRate = 3f;

    [Tooltip("The smallest the UI is able to scale down to before disappearing."), Range(0f, 5f)]
    public float minimumScale = 1f;

    [Tooltip("The time it takes for the UI to fade away when it is no longer valid or within range."), Range(0f, 2f)]
    public float overallFadeTime = 0.3f;

    [Tooltip("The time it takes for the central grapple-selected graphic to fade on and off when valid."), Range(0f, 1f)]
    public float highlightFadeTime = 0.2f;

    [Tooltip("The colour of the outer progress ring when the player is within range and the grapple is ready.")]
    public Color withinRangeColour;

    [Tooltip("The colour gradient of the progress ring as it approaches the maximum viable grapple range.")]
    public Gradient outOfRangeColour;

    [Tooltip("The layers which can block line of sight to the grapple point.")]
    public LayerMask environmentLayers;
    #endregion

    #region Components
    [Header("Components")]

    public Movement playerMovement;
    public RectTransform canvasTransform;
    public Image pointSelectedImage;
    public Image radialProgressImage;
    public Image progressUnderlayImage;
    public Image fadeUnderlayImage;
    #endregion

    #endregion

    // Start is called before the first frame update
    private void Start()
    {
        #region Initialisation

        mainCameraTransform = Camera.main.transform;

        // Records the base level of transparency for each UI component
        pointSelectedBaseAlpha = pointSelectedImage.color.a;
        radialProgressBaseAlpha = radialProgressImage.color.a;
        progressUnderlayBaseAlpha = progressUnderlayImage.color.a;
        fadeUnderlayBaseAlpha = fadeUnderlayImage.color.a;

        playerMovement = GameObject.FindGameObjectWithTag("Player").GetComponent<Movement>();

        #endregion
    }

    // LateUpdate is called each frame after Update
    private void LateUpdate()
    {
        // Calculate distance between the main camera and this grapple point
        float distanceToCamera = Vector3.Distance(canvasTransform.position, mainCameraTransform.position);
        
        // Fade on/off based on viability within player detection
        isDetected = true;

        #region Determine Grapple Viability

        // Must be higher than player by height offset
        if (canvasTransform.position.y < mainCameraTransform.position.y + playerMovement.grappleMinHeightOffset)
        {
            isDetected = false;
        }

        // Must be within detection range
        if (distanceToCamera > playerMovement.grappleDetectionRange || distanceToCamera < playerMovement.grappleMinRange)
        {
            isDetected = false;
        }

        // Check raycast line of sight to point
        if (isDetected && Physics.Raycast(mainCameraTransform.position, (canvasTransform.position - mainCameraTransform.position), distanceToCamera, environmentLayers))
        {
            isDetected = false;
        }

        #endregion

        #region Update Distance UI

        // Update progress ring
        float distanceWithinDetection = Mathf.Clamp((playerMovement.grappleDetectionRange - distanceToCamera), 0f, Mathf.Infinity);
        float detectionRangeDelta = Mathf.Clamp((playerMovement.grappleDetectionRange - playerMovement.grappleMaxRange), 0f, Mathf.Infinity);
        float rangeProgress = Mathf.Clamp((distanceWithinDetection / detectionRangeDelta), 0f, 1f);
        radialProgressImage.fillAmount = rangeProgress;

        // Change colours based on completion
        radialProgressImage.color = (rangeProgress < 1f) ? outOfRangeColour.Evaluate(rangeProgress) : withinRangeColour;

        #endregion

        #region Fade All Grapple UI

        if (isDetected && Movement.grappleUnlocked)
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
        
        newColour = pointSelectedImage.color;
        newColour.a = pointSelectedBaseAlpha * overallAlphaMultiplier;
        pointSelectedImage.color = newColour;

        newColour = radialProgressImage.color;
        newColour.a = overallAlphaMultiplier * ((rangeProgress < 1f) ? outOfRangeColour.Evaluate(rangeProgress).a : withinRangeColour.a);
        radialProgressImage.color = newColour;

        newColour = progressUnderlayImage.color;
        newColour.a = progressUnderlayBaseAlpha * overallAlphaMultiplier;
        progressUnderlayImage.color = newColour;

        newColour = fadeUnderlayImage.color;
        newColour.a = fadeUnderlayBaseAlpha * overallAlphaMultiplier;
        fadeUnderlayImage.color = newColour;

        #endregion

        #region Fade Preferred UI

        // Highlight if closest grapple point
        if (canvasTransform.parent == playerMovement.preferredGrapplePoint && isDetected && Movement.grappleUnlocked && rangeProgress == 1f)
        {
            highlightAlphaMultiplier += (Time.deltaTime / highlightFadeTime);
        }
        else
        {
            highlightAlphaMultiplier -= (Time.deltaTime / highlightFadeTime);
        }

        // The selected alpha of the entire grapple UI, 1 is full opacity and 0 is invisible
        highlightAlphaMultiplier = Mathf.Clamp(highlightAlphaMultiplier, 0f, 1f);

        // Calculate new colour with fade multiplier
        newColour = pointSelectedImage.color;
        newColour.a = pointSelectedBaseAlpha * highlightAlphaMultiplier;
        pointSelectedImage.color = newColour;

        #endregion

        #region Orientation and Scaling Relative To Camera

        // Rotate towards camera
        Quaternion rotationToCamera = Quaternion.LookRotation(mainCameraTransform.position - canvasTransform.position);
        canvasTransform.rotation = rotationToCamera;

        // Scale with distance
        canvasTransform.localScale = Vector3.one * Mathf.Clamp((distanceToCamera / scaleRate), minimumScale, Mathf.Infinity);

        #endregion
    }
}
