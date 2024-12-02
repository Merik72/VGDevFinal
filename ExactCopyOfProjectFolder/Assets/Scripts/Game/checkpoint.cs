using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gamekit3D
{
    public class checkpoint : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            {
                if (other.transform.gameObject.GetComponent<PlayerControl>() != null)
                {
                    print("did it");
                    other.transform.gameObject.GetComponent<PlayerControl>().spawnCoords = transform.position;
                    transform.Find("Area Light").gameObject.SetActive(true);
                }
            }
        }

    }
}