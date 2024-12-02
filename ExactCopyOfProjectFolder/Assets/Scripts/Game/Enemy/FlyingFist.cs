using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gamekit3D
{
    public class FlyingFist : MonoBehaviour
    {
        Vector3 startingPosition;
        public int disappearHeight = 20;
        public float timerLength = 5f;
        private bool disappearing = false;
        private void OnEnable()
        {
            startingPosition = transform.position;
        }
        private void FixedUpdate()
        {
            if (timerLength > 0f)
            {
                if(disappearing)
                    timerLength -= Time.deltaTime;
            }
            else
            {
                transform.localScale -= Vector3.one * Time.deltaTime;
            }
            if (transform.localScale.z <= 0 || (transform.position.y - startingPosition.y > disappearHeight || transform.position.y - startingPosition.y < -disappearHeight))
            {
                Destroy(gameObject);
            } 
        }
        void OnCollisionEnter(Collision other)
        {
            if(other.transform.gameObject.GetComponent<PlayerControl>() == null)
            {
                disappearing = true;
                gameObject.GetComponent<Rigidbody>().isKinematic = true;
                gameObject.GetComponent<Rigidbody>().useGravity = true;
                gameObject.GetComponent<Rigidbody>().isKinematic = false;
                gameObject.GetComponent<BoxCollider>().isTrigger = false;
                // Removed because it makes it wayy too easy to kill the golem
                // gameObject.layer = LayerMask.GetMask("Default");
                if(transform.childCount > 0)
                {
                    Destroy(transform.GetChild(0).gameObject);
                }
            }
        }
    }
}
