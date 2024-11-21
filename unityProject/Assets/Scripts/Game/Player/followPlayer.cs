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

    public Vector3 target;
    public float offsetY = 5f;
    public float camHeight = 7f;
    public float distance = 10.0f;
    private float currentX = 0.0f;
    private float currentY = 0.0f;
    public float sensivityX = 120.0f;
    public float sensivityY = 100.0f;

    // Update is called once per frame
    void LateUpdate()
    {
        target = lookAt.position;
        target.y += offsetY;

        currentX += Input.GetAxis("Mouse X") * sensivityX * Time.deltaTime;
        currentY -= Input.GetAxis("Mouse Y") * sensivityY * Time.deltaTime;

        currentY = Mathf.Clamp(currentY, YMin, YMax);

        Vector3 Direction = new Vector3(0, camHeight, -distance);
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        transform.position = target + rotation * Direction;

        transform.LookAt(target);
    }
}