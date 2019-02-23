using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OribitableCamera : MonoBehaviour
{
    public float xMouseSensitivity = 30.0f;
    public float yMouseSensitivity = 30.0f;
    public Transform playerView;
    private float rotX, rotY;
    public float OffsetDistance = 2.0f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Input.GetButtonDown("Fire1"))
                Cursor.lockState = CursorLockMode.Locked;
        }

        rotX -= Input.GetAxisRaw("Mouse Y") * xMouseSensitivity * 0.02f;
        rotY += Input.GetAxisRaw("Mouse X") * yMouseSensitivity * 0.02f;

        // Clamp the X rotation
        if (rotX < -90)
            rotX = -90;
        else if (rotX > 90)
            rotX = 90;

        transform.rotation = Quaternion.Euler(0, rotY, 0); // Rotates the collider
        playerView.rotation = Quaternion.Euler(rotX, rotY, 0); // Rotates the camera

        playerView.position = new Vector3(
            transform.position.x,
            transform.position.y,
            transform.position.z) + transform.forward * -OffsetDistance;
    }
}
