using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Look Rotation")]
    [Range(0.1f, 10f)]
    public float mouseSensitivity = 3f;

    [Header("Camera Follow")]
    [Range(0f, 50f)]
    public float followStrength = 25f;

    private Vector2 lookRotation = Vector2.zero;

    [Header("Components")]
    public Transform playerTransform;
    public Transform headTransform;
    public Transform cameraTransform;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        //Note: deltaTime is NOT required for this, as GetAxis refers to the distance moved this frame
        //Update Rotation (Left/Right - Turret)
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * (1f / Time.timeScale);
        lookRotation.y += mouseX;

        //Update Rotation (Up/Down - Gun)
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * (1f / Time.timeScale);
        lookRotation.x -= mouseY;
        lookRotation.x = Mathf.Clamp(lookRotation.x, -85f, 85f);

        //Set Rotation (All)
        playerTransform.localEulerAngles = new Vector3(0f, lookRotation.y, 0f);
        cameraTransform.localEulerAngles = new Vector3(lookRotation.x, lookRotation.y, 0f);

        //Set Location (following player)
        Vector3 followPosition = (headTransform.position - cameraTransform.position) * followStrength * Time.deltaTime;
        followPosition += cameraTransform.position;
        cameraTransform.position = followPosition;
    }
}
