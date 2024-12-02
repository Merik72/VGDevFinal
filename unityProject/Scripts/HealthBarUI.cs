using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gamekit3D
{
    public class HealthBar : MonoBehaviour
    {
        public Slider healthSlider;
        public Damageable playerDamageable;

        void Start()
        {
            if (healthSlider == null)
            {
                Debug.LogError("Health Slider not assigned in the inspector.");
                return;
            }

            if (playerDamageable == null)
            {
                Debug.LogError("Player's Damageable component not assigned in the inspector.");
                return;
            }
            UpdateHealthBar();
        }

        void Update()
        {
            UpdateHealthBar();
        }

        void UpdateHealthBar()
        {
            if (playerDamageable != null && healthSlider != null)
            {
                float healthPercentage = (float)playerDamageable.currentHitPoints / (float)playerDamageable.maxHitPoints;
                healthSlider.value = healthPercentage;
            }
        }
    }
}