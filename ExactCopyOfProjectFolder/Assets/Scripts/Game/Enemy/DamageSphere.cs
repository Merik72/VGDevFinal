using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gamekit3D
{
    public class DamageSphere : MonoBehaviour
    {
        public float size = 12.5f;
        public float disappearRate = 0.3f;
        public bool disappearing = false;
        private void OnEnable()
        {
            transform.localScale = Vector3.one * size;
            disappearing = true;
        }
        private void FixedUpdate()
        {
            if (disappearing)
            {
                transform.localScale -= Vector3.one * disappearRate;
            }
            if (transform.localScale.z <= 0)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
