using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class randomRotation : MonoBehaviour
{
    void Awake()
    {
        float scale = Random.Range(1.7f, 2.5f);
        float x = Random.Range(0f, 180f);
        float y = Random.Range(0f, 180f);
        float z = Random.Range(0f, 180f);
        float w = Random.Range(0f, 180f);
        transform.rotation = new Quaternion(x, y, z, w);
        transform.localScale = new Vector3(scale, scale, scale);
        print(x + y + z + w + scale);
    }
}
