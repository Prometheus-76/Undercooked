using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Author: Darcy Matheson
// Purpose: Controls a damage number instance, including it's animations and data

public class DamageNumber : MonoBehaviour
{
    #region Variables

    #region Internal

    private Transform mainCameraTransform;
    private Vector3 targetPosition;
    private Vector3 startPosition;

    private float instanceTimer;

    private bool isCritical;

    #endregion

    #region Parameters
    [Header("Parameters")]

    [Tooltip("The reference distance for scaling UI, lower values means larger UI."), Range(1f, 10f)]
    public float scaleRate;

    [Tooltip("The smallest the UI is able to scale down to before disappearing."), Range(0f, 5f)]
    public float minimumScale;

    [Tooltip("The largest the UI is able to scale up to."), Range(0f, 100f)]
    public float maximumScale;

    [Tooltip("The colors that the text boomerangs between when non-critical.")]
    public Gradient boomerangGradientStandard;

    [Tooltip("The colors that the text boomerangs between when critical.")]
    public Gradient boomerangGradientCritical;

    [Tooltip("The time taken for the text UI as it fades in and out before and after the effect respectively."), Range(0f, 1f)]
    public float textFadeTime;

    [Tooltip("The time taken for the text UI to scale up before the effect."), Range(0f, 3f)]
    public float textScaleTime;

    [Tooltip("The time taken for the text UI to move to the target position."), Range(0f, 3f)]
    public float textMoveTime;

    [Tooltip("The total time the text is alive for."), Range(0f, 5f)]
    public float textLifetime;

    [Tooltip("How high above the hit point the text will rise."), Range(0f, 10f)]
    public float heightOffset;

    [Tooltip("The random variance of the text position.")]
    public Vector3 positionVariance;

    [Tooltip("How long it takes for a full cycle of the colour gradient (in seconds)."), Range(0f, 20f)]
    public float colourBoomerangRate;

    [Tooltip("The increased scale of text on critical hits."), Range(0f, 5f)]
    public float criticalScale;
    #endregion

    #region Components
    [Header("Components")]

    public RectTransform textTransform;
    public TextMeshPro damageText;
    #endregion

    #endregion

    private void Start()
    {
        #region Initialisation

        mainCameraTransform = Camera.main.transform;
        instanceTimer = 0f;

        Destroy(gameObject, textLifetime + 1f);

        #endregion
    }

    private void LateUpdate()
    {
        // Calculate distance between the main camera and this grapple point
        float distanceToCamera = Vector3.Distance(textTransform.position, mainCameraTransform.position);
        instanceTimer += Time.deltaTime;

        #region Position

        float positionProgress = Mathf.Clamp01(instanceTimer / textMoveTime);
        textTransform.position = Vector3.Lerp(startPosition, targetPosition, EaseOut(positionProgress));

        #endregion

        #region Colour

        float evaluateProgress = CosWave(instanceTimer * colourBoomerangRate);
        Color newColour = isCritical ? boomerangGradientCritical.Evaluate(evaluateProgress) : boomerangGradientStandard.Evaluate(evaluateProgress);

        float alphaMultiplier = 1f;
        if (instanceTimer < textFadeTime)
        {
            // Fade in
            alphaMultiplier = (instanceTimer / textFadeTime);
        }
        else if (instanceTimer > (textLifetime - textFadeTime))
        {
            // Fade out
            alphaMultiplier = (instanceTimer - textLifetime - textFadeTime) / -textFadeTime;
        }

        newColour.a *= alphaMultiplier;
        damageText.color = newColour;

        #endregion

        #region Scale

        float scaleProgress = 1f;
        if (instanceTimer < textScaleTime)
        {
            // Fade in
            scaleProgress = Mathf.Clamp01(instanceTimer / textScaleTime);
        }
        else if (instanceTimer > (textLifetime - textScaleTime))
        {
            // Fade out
            scaleProgress = Mathf.Clamp01((instanceTimer - textLifetime - textScaleTime) / -textScaleTime);
        }

        #endregion

        #region Orientation and Scaling Relative To Camera

        // Rotate towards camera
        Quaternion rotationToCamera = Quaternion.LookRotation(textTransform.position - mainCameraTransform.position);
        textTransform.rotation = rotationToCamera;

        // Scale with distance
        textTransform.localScale = Vector3.one * Mathf.Clamp((distanceToCamera / scaleRate), minimumScale, maximumScale) * EaseOut(scaleProgress) * (isCritical ? criticalScale : 1f);

        #endregion
    }

    public void SetupDamageNumber(string damageValue, Vector3 hitPoint, bool criticalHit)
    {
        damageText.text = damageValue;

        startPosition = hitPoint;

        targetPosition = hitPoint;
        targetPosition.x += Random.Range(-positionVariance.x, positionVariance.x);
        targetPosition.y += Random.Range(-positionVariance.y, positionVariance.y) + heightOffset;
        targetPosition.z += Random.Range(-positionVariance.z, positionVariance.z);

        isCritical = criticalHit;
    }

    float EaseOut(float time)
    {
        float result = 0f;

        result = 1f - ((time - 1f) * (time - 1f));
        result = Mathf.Clamp01(result);

        return result;
    }

    float CosWave(float time)
    {
        float result = 0f;

        result = (Mathf.Cos(Mathf.PI * (time + 1f)) + 1f) / 2f;
        result = Mathf.Clamp01(result);

        return result;
    }
}
