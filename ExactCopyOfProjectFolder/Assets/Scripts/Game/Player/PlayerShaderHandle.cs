using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;

namespace Gamekit3D
{
    namespace Message
    {
        [RequireComponent(typeof(Damageable))]
        public class PlayerShaderHandle : MonoBehaviour
        {
            public Damageable damageable;
            public Material playerMat;
            [SerializeField]
            protected Color m_invincibleColor;
            protected Color m_normalColor;

            private void OnEnable()
            {
                damageable = gameObject.GetComponent<Damageable>();
                damageable.OnReceiveDamage.AddListener(OnInvuln);
                damageable.OnBecomeVulnerable.AddListener(OnVuln);
                damageable.OnResetDamage.AddListener(OnVuln);
                //playerMat = gameObject.GetComponent<Material>();
                // playerMat = GetComponent<Renderer>().material;
                m_normalColor = playerMat.color;
                m_normalColor.a = 1f;
                m_invincibleColor = playerMat.color;
                m_invincibleColor.a = 0.5f;
            }
            void OnInvuln()
            {
                playerMat.color = m_invincibleColor;
                // playerMat.SetColor(name, m_invincibleColor);
                // print("Invuln");
            }
            void OnVuln()
            {
                playerMat.color = m_normalColor;
                // playerMat.SetColor(name, m_normalColor);
                //print("Vuln");
            }
        }
    }
}
