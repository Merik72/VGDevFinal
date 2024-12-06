using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gamekit3D
{
    public class EnemyHP : MonoBehaviour
    {
        public Slider healthSlider;
        [SerializeField]
        private Damageable m_Damageable;

        private void Awake()
        {
            m_Damageable = RecursiveFindChild(transform.root);
            if (m_Damageable == null) m_Damageable = transform.root.gameObject.GetComponent<Damageable>();
        }
        void Start()
        {
            healthSlider = GetComponent<Slider>();
            if (healthSlider == null)
            {
                Debug.LogError("Health Slider not assigned in the inspector.");
                return;
            }

            if (m_Damageable == null)
            {
                if (m_Damageable == null)
                    Debug.LogError(transform.root.name + " Damageable component not assigned in the inspector.");
                return;
            }
            // else m_Damageable.onDamageMessageReceivers.Add(this);'

            m_Damageable.OnReceiveDamage.AddListener(UpdateHealthBar);
            m_Damageable.OnDeath.AddListener(UpdateHealthBar);

        }

        void UpdateHealthBar()
        {
            print(transform.name + " I tried");
            if (m_Damageable != null && healthSlider != null)
            {
                float healthPercentage = (float)m_Damageable.currentHitPoints / (float)m_Damageable.maxHitPoints;
                healthSlider.value = healthPercentage;
            }
            else
            {
                print("nullied");
            }
        }
        public Damageable RecursiveFindChild(Transform parent)
        {
            foreach (Transform child in parent)
            {
                Damageable localDamageable = child.GetComponent<Damageable>();
                if (child.gameObject.GetComponent<Damageable>() != null)
                {
                    return localDamageable;
                }
                else
                {
                    Damageable found = RecursiveFindChild(child);
                    if (found != null)
                    {
                        //print(childName + " found");
                        return found;
                    }
                }
            }
            return null;
        }
    }
}