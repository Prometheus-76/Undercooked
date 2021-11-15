using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Author: Darcy Matheson
// Purpose: Makes object rotate around Y axis and slowly bob up and down

public class ItemTurntable : MonoBehaviour
{
    public float verticalDistance;
    public float verticalSpeed;
    public float rotationSpeed;

    private Transform itemTransform;
    private Vector3 startPosition;
    private float currentRotation;
    private float seed;

    private void Start()
    {
        itemTransform = GetComponent<Transform>();
        startPosition = itemTransform.position;
        currentRotation = 0f;

        seed = Random.Range(0f, 10f);
    }

    // Update is called once per frame
    void Update()
    {
        // Move up and down with a Cosine wave
        Vector3 newPosition = startPosition;
        newPosition.y = startPosition.y + ((1f - Mathf.Cos(verticalSpeed * Mathf.PI * (Time.time + seed)) / 2f) * verticalDistance);
        itemTransform.position = newPosition;

        // Rotate around the y axis
        currentRotation += Time.deltaTime * rotationSpeed * 360f;
        Quaternion newRotation = Quaternion.Euler(0f, currentRotation + (seed * 360f), 0f);
        itemTransform.rotation = newRotation;
    }
}
