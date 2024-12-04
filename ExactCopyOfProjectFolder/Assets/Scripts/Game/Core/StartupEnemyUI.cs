using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartupEnemyUI : MonoBehaviour
{
    Canvas m_canvas;
    // Start is called before the first frame update
    void Start()
    {
        m_canvas = GetComponent<Canvas>();
        m_canvas.worldCamera = Camera.main;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.LookAt(Camera.main.transform.position);
    }
}
