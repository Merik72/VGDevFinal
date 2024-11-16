using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class followPlayer : MonoBehaviour
{
    // uhh how do i make a singleton again?
    // public static followPlayer S;

    private const float YMin = -50.0f;
    private const float YMax = 50.0f;

    public Transform lookAt;

    public float distance = 10.0f;
    private float currentX = 0.0f;
    private float currentY = 0.0f;
    public float sensivityX = 120.0f;
    public float sensivityY = 100.0f;

    // Update is called once per frame
    void LateUpdate()
    {
        currentX += Input.GetAxis("Mouse X") * sensivityX * Time.deltaTime;
        currentY -= Input.GetAxis("Mouse Y") * sensivityY * Time.deltaTime;

        currentY = Mathf.Clamp(currentY, YMin, YMax);

        Vector3 Direction = new Vector3(0, 5, -distance);
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        transform.position = lookAt.position + rotation * Direction;

        transform.LookAt(lookAt.position);
    }
}