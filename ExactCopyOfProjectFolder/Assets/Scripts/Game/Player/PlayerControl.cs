// Attatch me to your player
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gamekit3D.Message;
using UnityEngine.Events;

namespace Gamekit3D
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Damageable))]
    public class PlayerControl : MonoBehaviour, IMessageReceiver
    {
        // Singleton! Yay!
        protected static PlayerControl s_Instance;
        public static PlayerControl instance { get { return s_Instance; } }

        [Header("Tunable Parameters")]
        public Vector3 spawnCoords;
        public float acceleration = 30f;
        public float maxForwardSpeed = 10f;
        public float k_GroundAcceleration = 20f;
        public float k_GroundDeceleration = 25f;
        private Rigidbody rbPlayer;
        public float jumpPower = 0.06f;
        public float k_JumpAbortSpeed = 0.05f;
        public float gravity = 0.125f;
        public float k_StickingGravityProportion = 0.3f;
        public float maxTurnSpeed = 1200f;
        public float minTurnSpeed = 400f;
        public Material m_playerMat;
        public Input inp_attack;
        public Input inp_skill;
        public Input inp_ult;
        public float instantTurnTimeoutMaxTimer = 1f;
        public float instantTurnTimeoutCurrent = 0;

        [Header("State Information")]
        // Jumping
        public bool isGrounded;
        public bool previouslyGrounded;
        public bool jump;
        public bool readyToJump;
        public bool firstJumpFinished;
        public bool readyToDoubleJump;
        public bool readyToInstantTurn;
        // public BoxCollider foot;

        // Moving
        public float m_VerticalSpeed;
        public float desiredForwardSpeed;
        public float m_ForwardSpeed;
        public Vector3 movement;
        public Vector2 movementInput;
        public Quaternion m_TargetRotation;
        public Quaternion m_PreviousRotation;
        public float m_AngleDiff;

        // Fighting
        public bool inAttack;
        public Damageable m_Damageable;
        public bool m_Respawning;
        public LayerMask enemyLayer;

        public float aoeRadius = 5f;       // AoE radius
        public float aoeDamage = 50f;      // AoE damage

        private float ultimateCooldown = 30.0f;
        private float nextUltimateTime = 0.0f;
        private float aoeCooldown = 15.0f;
        private float nextAoETime = 0.0f;


        // Other
        protected Collider[] touchingColliders = new Collider[8];
        const float k_MinEnemyDotCoeff = 0.2f; // how close an enemy can be ?
        public CharacterController charCtrl;

        private void Awake()
        {
            spawnCoords = transform.position;
            charCtrl = gameObject.GetComponent<CharacterController>();
            s_Instance = this;
            //foot = gameObject.GetComponent<BoxCollider>();
            //rbPlayer = gameObject.GetComponent<Rigidbody>();
            enemyLayer = LayerMask.GetMask("Enemy");
        }

        void OnEnable()
        {
            // SceneLinkedSMB<PlayerController>.Initialise(m_Animator, this);

            m_Damageable = GetComponent<Damageable>();
            m_Damageable.onDamageMessageReceivers.Add(this);

            m_Damageable.isInvulnerable = true;
            m_playerMat = GetComponent<Material>();
            // EquipMeleeWeapon(false);

            // m_Renderers = GetComponentsInChildren<Renderer>();
        }

        void Start()
        {
            instantTurnTimeoutCurrent = 0;
        }

        void Update()
        {
            if (Input.GetButtonDown("Fire1"))
            {
                PerformArcAttack();
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                PerformUltimateBeamAttack();
            }

            if (Input.GetKey(KeyCode.E)) // Check AoE input
            {
                PerformAoEAttack();
            }
        }

        void FixedUpdate()
        {
            if (m_Respawning)
            {
                Respawn();
                return;
            }
            movementInput.Set(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            jump = Input.GetKey("space");

            bool attack = Input.GetMouseButton(1);
            Vector2 camera = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

            // Checking under means you can do it
            /*
             *
            if (myAction < Actionability.attacking)
            {
                if(rbPlayer.velocity.magnitude < maxSpeed)
                {
                    myAction = Actionability.walking;
                    rbPlayer.AddForce(moveDirection*acceleration);
                }
            }
             */

            if (movementInput != Vector2.zero)
            {
                SetTargetRotation();
            }
            else
            {
                instantTurnTimeoutCurrent += Time.deltaTime;
                m_TargetRotation = m_PreviousRotation;
            }
            if(instantTurnTimeoutCurrent >= instantTurnTimeoutMaxTimer)
            {
                readyToInstantTurn = true;
            }
            UpdateOrientation();
            CalculateForwardMovement();
            CalculateVerticalMovement();

            movement = m_ForwardSpeed * transform.forward * Time.deltaTime;

            charCtrl.Move(new Vector3(movement.x, m_VerticalSpeed, movement.z));
            //if(movementInput != Vector2.zero)

            isGrounded = charCtrl.isGrounded;
            previouslyGrounded = isGrounded;
            OOB();

        }
        void PerformAoEAttack()
        {
            if (Time.time < nextAoETime)
            {
                Debug.Log("AOE Attack is on cooldown.");
                return;
            }

            float aoeRadius = 10f; // Radius of the AoE attack
            float aoeDamage = 50f; // Damage dealt by the AoE attack
            float circleDisplayTime = 1.5f; // Duration to display the AoE circle

            // Hardcoded the AOECircle Prefab
            GameObject aoeCirclePrefab = Resources.Load<GameObject>("AoECircle");
            if (aoeCirclePrefab != null)
            {
                // Create a circle visual at the player's position
                GameObject aoeCircle = Instantiate(aoeCirclePrefab, transform.position, Quaternion.identity);

                // Generate the circle points
                LineRenderer lineRenderer = aoeCircle.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    DrawCircle(lineRenderer, aoeRadius);
                }

                Destroy(aoeCircle, circleDisplayTime); // Destroy after display time
            }
            else
            {
                Debug.LogError("AoECircle prefab could not be loaded from Resources!");
            }

            // Find all enemies within the AoE radius
            Collider[] enemiesHit = Physics.OverlapSphere(transform.position, aoeRadius, enemyLayer);

            foreach (Collider enemy in enemiesHit)
            {
                Damageable enemyDamageable = enemy.GetComponentInChildren<Damageable>();
                if (enemyDamageable != null)
                {
                    Debug.Log("AoE Attack Executed!");
                    Damageable.DamageMessage damageMessage = new Damageable.DamageMessage
                    {
                        damageSource = transform.position,
                        damager = this,
                        amount = (int)aoeDamage,
                        direction = (enemy.transform.position - transform.position).normalized,
                        throwing = false
                    };

                    enemyDamageable.ApplyDamage(damageMessage);
                }
            }

            // Set the next AoE available time
            nextAoETime = Time.time + aoeCooldown;
        }

        void DrawCircle(LineRenderer lineRenderer, float radius, int segments = 50)
        {
            lineRenderer.positionCount = segments + 1; // Close the loop
            float angleStep = 360f / segments;

            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.Deg2Rad * i * angleStep;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                lineRenderer.SetPosition(i, new Vector3(x, 0, z));
            }
        }

        void PerformArcAttack()
        {
            float arcAngle = 90f; // Angle of the arc
            float arcDistance = 5f; // Distance of the arc
            float arcDamage = 30f; // Damage dealt by the arc attack
            int segments = 10; // Number of segments to draw the arc

            // Load the new ArcLine prefab
            GameObject arcPrefab = Resources.Load<GameObject>("ArcLine");
            if (arcPrefab != null)
            {
                // Draw the arc by instantiating multiple line segments
                for (int i = 0; i <= segments; i++)
                {
                    float angle = -arcAngle / 2 + (arcAngle / segments) * i;
                    Quaternion rotation = Quaternion.Euler(0, angle, 0);
                    Vector3 direction = rotation * Camera.main.transform.forward; // Use camera's forward direction
                    if (isGrounded)
                    {
                        direction.y = 0;
                        direction.Normalize();
                    }
                    Vector3 position = transform.position + direction.normalized * arcDistance;

                    GameObject arcInstance = Instantiate(arcPrefab, position, Quaternion.identity);

                    // Set the rotation of each segment to face outward
                    arcInstance.transform.rotation = Quaternion.LookRotation(direction);

                    // Set the scale to match the desired distance
                    arcInstance.transform.localScale = new Vector3(arcDistance / segments, 1, arcDistance / segments);

                    // Destroy the arc after a short duration to simulate the attack
                    Destroy(arcInstance, 0.5f);
                }
            }
            else
            {
                Debug.LogError("ArcLine prefab could not be loaded from Resources!");
            }

            // Find all enemies within the arc distance
            Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, arcDistance, enemyLayer);

            foreach (Collider enemy in enemiesInRange)
            {
                Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
                float angleToEnemy = Vector3.Angle(Camera.main.transform.forward, directionToEnemy); // Use camera's forward direction

                // Check if the enemy is within the arc angle
                if (angleToEnemy <= arcAngle / 2)
                {
                    Damageable enemyDamageable = enemy.GetComponentInChildren<Damageable>();
                    if (enemyDamageable != null)
                    {
                        Debug.Log("Arc Attack Executed!");
                        Damageable.DamageMessage damageMessage = new Damageable.DamageMessage
                        {
                            damageSource = transform.position,
                            damager = this,
                            amount = (int)arcDamage,
                            direction = directionToEnemy,
                            throwing = false
                        };

                        enemyDamageable.ApplyDamage(damageMessage);
                    }
                }
            }
        }


        IEnumerator ShrinkAndDestroy(GameObject beamInstance, float duration)
        {
            float elapsedTime = 0f;
            Vector3 initialScale = beamInstance.transform.localScale;
            while (elapsedTime < duration)
            {
                float scaleAmount = Mathf.Lerp(1f, 0f, elapsedTime / duration);
                beamInstance.transform.localScale = new Vector3(initialScale.x * scaleAmount, initialScale.y, initialScale.z * scaleAmount);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            Destroy(beamInstance);
        }

        void PerformUltimateBeamAttack()
        {
            if (Time.time < nextUltimateTime)
            {
                Debug.Log("Ultimate Beam Attack is on cooldown.");
                return;
            }

            float beamLength = 2000f; // Length of the beam
            float beamDamage = 100f; // Damage dealt by the beam
            float beamDuration = 1.5f; // Duration of the beam

            // Create a cylinder to represent the beam
            GameObject beamInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            if (beamInstance != null)
            {
                // Set the initial position to be in front of the player, slightly further away
                Vector3 beamStartPosition = transform.position + Camera.main.transform.forward * (beamLength + 5.0f); // Move it further away
                beamStartPosition.y += 1.0f; // Raise the beam above the ground
                beamInstance.transform.position = beamStartPosition;

                // Set the rotation of the beam to match the camera's forward direction
                beamInstance.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
                beamInstance.transform.Rotate(90, 0, 0); // Rotate to align the cylinder horizontally

                // Set the scale to make the beam look like a big cylinder
                beamInstance.transform.localScale = new Vector3(4, beamLength, 4); // Make the cylinder bigger (Width, Length, Depth)

                // Change the material to make it look more like a beam (optional)
                Renderer beamRenderer = beamInstance.GetComponent<Renderer>();
                if (beamRenderer != null)
                {
                    beamRenderer.material.color = Color.red; // Change color to red or any desired color
                }

                // Assign the beam to a specific layer
                int beamLayer = LayerMask.NameToLayer("PlayerAttack");
                if (beamLayer != -1)
                {
                    beamInstance.layer = beamLayer;
                    // Ignore collision between player and the beam layer
                    // Physics.IgnoreLayerCollision(gameObject.layer, beamLayer);
                }
                else
                {
                    Debug.LogError("Layer 'PlayerAttack' not defined in project settings. Please add it.");
                }
                /*
                // Make sure the player does not collide with the beam
                Collider beamCollider = beamInstance.GetComponent<Collider>();
                if (beamCollider != null)
                {
                    Collider playerCollider = GetComponent<Collider>();
                    if (playerCollider != null)
                    {
                        Physics.IgnoreCollision(beamCollider, playerCollider);
                    }
                }
                 */

                // Start the shrinking and destroying coroutine
                StartCoroutine(ShrinkAndDestroy(beamInstance, beamDuration));

                // Set the next ultimate available time
                nextUltimateTime = Time.time + ultimateCooldown;
            }
            else
            {
                Debug.LogError("Failed to create beam cylinder!");
            }

            // Find all enemies within the beam range
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, 2.5f, Camera.main.transform.forward, beamLength, enemyLayer);

            foreach (RaycastHit hit in hits)
            {
                Damageable enemyDamageable = hit.collider.GetComponentInChildren<Damageable>();
                if (enemyDamageable != null)
                {
                    Debug.Log("Ultimate Beam Attack Executed!");
                    Damageable.DamageMessage damageMessage = new Damageable.DamageMessage
                    {
                        damageSource = transform.position,
                        damager = this,
                        amount = (int)beamDamage,
                        direction = Camera.main.transform.forward,
                        throwing = false
                    };

                    enemyDamageable.ApplyDamage(damageMessage);
                }
            }
        }
        void ResetDamage()
        {
            m_Damageable.ResetDamage();
        }

        void Respawn()
        {
            transform.position = spawnCoords;
            print("respawn");
            m_Respawning = false;
            Invoke("ResetDamage", 0.1f);
        }

        

        void OOB()
        {
            if (transform.position.y > -10)
            {
                return;
            }
            print("Bro, you fell off");
            transform.position = spawnCoords;
            m_Respawning = true;
        }
        void CalculateForwardMovement()
        {
            // Cache the move input and cap it's magnitude at 1.
            Vector2 moveInput = movementInput;
            if (moveInput.sqrMagnitude > 1f)
                moveInput.Normalize();

            // Calculate the speed intended by input.
            desiredForwardSpeed = moveInput.magnitude * maxForwardSpeed;

            // Determine change to speed based on whether there is currently any move input.
            float acceleration = (moveInput != Vector2.zero) ? k_GroundAcceleration : k_GroundDeceleration;

            // Adjust the forward speed towards the desired speed.
            m_ForwardSpeed = Mathf.MoveTowards(m_ForwardSpeed, desiredForwardSpeed, acceleration * Time.deltaTime);
        }
        void CalculateVerticalMovement()
        {
            // If jump is not currently held and Ellen is on the ground then she is ready to jump.
            if (!jump && !isGrounded)
            {
                firstJumpFinished = true;
            }
            if (!jump && isGrounded)
            {
                firstJumpFinished = false;
                readyToJump = true;
                readyToDoubleJump = true;
            }
            if (isGrounded)
            {
                // When grounded we apply a slight negative vertical speed to make Ellen "stick" to the ground.
                m_VerticalSpeed = -gravity * k_StickingGravityProportion;

                // If jump is held, Ellen is ready to jump and not currently in the middle of a melee combo...
                if (jump && readyToJump && !inAttack)
                {
                    // ... then override the previously set vertical speed and make sure she cannot jump again.
                    m_VerticalSpeed = jumpPower;
                    isGrounded = false;
                    readyToJump = false;
                }
            }
            else
            {
                readyToJump = false;
                // If Ellen is airborne, the jump button is not held and Ellen is currently moving upwards...
                if (!jump && m_VerticalSpeed > 0.0f)
                {
                    // ... decrease Ellen's vertical speed.
                    // This is what causes holding jump to jump higher that tapping jump.
                    m_VerticalSpeed -= k_JumpAbortSpeed * Time.deltaTime;
                }


                // If a jump is approximately peaking, make it absolute.
                if (Mathf.Approximately(m_VerticalSpeed, 0f))
                {
                    m_VerticalSpeed = 0f;
                }

                // If Ellen is airborne, apply gravity.
                m_VerticalSpeed -= gravity * Time.deltaTime;
                if (jump && readyToDoubleJump && firstJumpFinished)
                {
                    m_VerticalSpeed = jumpPower;
                    readyToDoubleJump = false;
                }
            }
        }
        // Called each physics step to set the rotation Ellen is aiming to have.
        void SetTargetRotation()
        {
            // Create three variables, move input local to the player, flattened forward direction of the camera and a local target rotation.
            Vector2 moveInput = movementInput;
            Vector3 localMovementDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

            Vector3 forward = Camera.main.transform.forward; // this causes a visual issue when holding forwards and moving the camera quickly
            forward.y = 0f;
            forward.Normalize();

            Quaternion targetRotation;

            // If the local movement direction is the opposite of forward then the target rotation should be towards the camera.
            if (Mathf.Approximately(Vector3.Dot(localMovementDirection, Vector3.forward), -1.0f))
            {
                targetRotation = Quaternion.LookRotation(-forward);
            }
            else
            {
                // Otherwise the rotation should be the offset of the input from the camera's forward.
                Quaternion cameraToInputOffset = Quaternion.FromToRotation(Vector3.forward, localMovementDirection);
                targetRotation = Quaternion.LookRotation(cameraToInputOffset * forward);
            }

            // The desired forward direction of Ellen.
            Vector3 resultingForward = targetRotation * Vector3.forward;

            // If attacking try to orient to close enemies.
            if (inAttack)
            {
                // Find all the enemies in the local area.
                Vector3 centre = transform.position + transform.forward * 2.0f + transform.up;
                Vector3 halfExtents = new Vector3(3.0f, 1.0f, 2.0f);
                int layerMask = 1 << LayerMask.NameToLayer("Enemy");
                int count = Physics.OverlapBoxNonAlloc(centre, halfExtents, touchingColliders, targetRotation, layerMask);

                // Go through all the enemies in the local area...
                float closestDot = 0.0f;
                Vector3 closestForward = Vector3.zero;
                int closest = -1;

                for (int i = 0; i < count; ++i)
                {
                    // ... and for each get a vector from the player to the enemy.
                    Vector3 playerToEnemy = touchingColliders[i].transform.position - transform.position;
                    playerToEnemy.y = 0;
                    playerToEnemy.Normalize();

                    // Find the dot product between the direction the player wants to go and the direction to the enemy.
                    // This will be larger the closer to Ellen's desired direction the direction to the enemy is.
                    float d = Vector3.Dot(resultingForward, playerToEnemy);

                    // Store the closest enemy.
                    if (d > k_MinEnemyDotCoeff && d > closestDot)
                    {
                        closestForward = playerToEnemy;
                        closestDot = d;
                        closest = i;
                    }
                }

                // If there is a close enemy...
                if (closest != -1)
                {
                    // The desired forward is the direction to the closest enemy.
                    resultingForward = closestForward;

                    // We also directly set the rotation, as we want snappy fight and orientation isn't updated in the UpdateOrientation function during an atatck.
                    transform.rotation = Quaternion.LookRotation(resultingForward);
                }
            }

            // Find the difference between the current rotation of the player and the desired rotation of the player in radians.
            float angleCurrent = Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg;
            float targetAngle = Mathf.Atan2(resultingForward.x, resultingForward.z) * Mathf.Rad2Deg;

            m_AngleDiff = Mathf.DeltaAngle(angleCurrent, targetAngle);
            m_TargetRotation = targetRotation;
            m_PreviousRotation = m_TargetRotation;
        }

        /*
        // Called each physics step to help determine whether Ellen can turn under player input.
        bool IsOrientationUpdated()
        {
            bool updateOrientationForLocomotion = !m_IsAnimatorTransitioning && m_CurrentStateInfo.shortNameHash == m_HashLocomotion || m_NextStateInfo.shortNameHash == m_HashLocomotion;
            bool updateOrientationForAirborne = !m_IsAnimatorTransitioning && m_CurrentStateInfo.shortNameHash == m_HashAirborne || m_NextStateInfo.shortNameHash == m_HashAirborne;
            bool updateOrientationForLanding = !m_IsAnimatorTransitioning && m_CurrentStateInfo.shortNameHash == m_HashLanding || m_NextStateInfo.shortNameHash == m_HashLanding;

            return updateOrientationForLocomotion || updateOrientationForAirborne || updateOrientationForLanding || m_InCombo && !m_InAttack;
        }
         */


        const float k_AirborneTurnSpeedProportion = 0.54f;
        const float k_InverseOneEighty = 1f / 180f;
        // Called each physics step after SetTargetRotation if there is move input and Ellen is in the correct animator state according to IsOrientationUpdated.
        void UpdateOrientation()
        {
            // m_Animator.SetFloat(m_HashAngleDeltaRad, m_AngleDiff * Mathf.Deg2Rad);

            // Vector3 localInput = new Vector3(movementInput.x, 0f, movementInput.y);
            float groundedTurnSpeed = Mathf.Lerp(maxTurnSpeed, minTurnSpeed, m_ForwardSpeed / desiredForwardSpeed);
            float actualTurnSpeed = isGrounded ? groundedTurnSpeed : k_AirborneTurnSpeedProportion * groundedTurnSpeed;
            if (readyToInstantTurn)
            {
                actualTurnSpeed = 10000;
                readyToInstantTurn = false;
            }
            m_TargetRotation = Quaternion.RotateTowards(transform.rotation, m_TargetRotation, actualTurnSpeed * Time.deltaTime);
            
            // m_TargetRotation = Quaternion.RotateTowards(transform.rotation, m_TargetRotation, groundedTurnSpeed * Time.deltaTime)
            if(movementInput == Vector2.zero)
            {
                transform.rotation = m_PreviousRotation;
            }
            else
            {

                transform.rotation = m_TargetRotation;
            }
        }
        void Damaged(Damageable.DamageMessage damageMessage)
        {
            // Find the direction of the damage.
            Vector3 forward = damageMessage.damageSource - transform.position;
            forward.y = 0f;

            Vector3 localHurt = transform.InverseTransformDirection(forward);

            print("ouch");

            // Shake the camera.
            // CameraShake.Shake(CameraShake.k_PlayerHitShakeAmount, CameraShake.k_PlayerHitShakeTime);
            /*
            // Play an audio clip of being hurt.
            if (hurtAudioPlayer != null)
            {
                hurtAudioPlayer.PlayRandomClip();
            }
             */
        }
        public void Die(Damageable.DamageMessage damageMessage)
        {
            // m_Animator.SetTrigger(m_HashDeath);
            m_ForwardSpeed = 0f;
            m_VerticalSpeed = 0f;
            m_Respawning = true;
        }

        public void OnReceiveMessage(MessageType type, object sender, object msg)
        {
            switch (type)
            {
                case MessageType.DAMAGED:
                    {
                        Damageable.DamageMessage damageData = (Damageable.DamageMessage)msg;
                        Damaged(damageData);
                    }
                    break;
                case MessageType.DEAD:
                    {
                        Damageable.DamageMessage damageData = (Damageable.DamageMessage)msg;
                        Die(damageData);
                    }
                    break;
            }
        }

        public float GetRemainingAOECooldown()
        {
            return Mathf.Max(0, nextAoETime - Time.time);
        }

        public float GetAOECooldown()
        {
            return aoeCooldown; // Returns the AoE cooldown duration (15 seconds)
        }

        public float GetRemainingUltimateCooldown()
        {
            return Mathf.Max(0, nextUltimateTime - Time.time);
        }

        public float GetUltimateCooldown()
        {
            return ultimateCooldown; // Returns the Ultimate cooldown duration (30 seconds)
        }
    }

}