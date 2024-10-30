// Attatch me to your player
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerControl : MonoBehaviour
{
    [Header("Tunable Parameters")]
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
    
    [Header("State Information")]
    // Jumping
    public bool isGrounded;
    public bool previouslyGrounded;
    public bool jump;
    public bool readyToJump;
    public bool firstJumpFinished;
    public bool readyToDoubleJump;
    // public BoxCollider foot;

    // Moving
    public float verticalSpeed;
    public float desiredForwardSpeed;
    public float forwardSpeed;
    public Vector3 movement;
    public Vector2 movementInput;
    public Quaternion m_TargetRotation;
    public float m_AngleDiff;

    // Fighting
    public bool inAttack;

    // Other
    protected Collider[] touchingColliders = new Collider[8];
    const float k_MinEnemyDotCoeff = 0.2f; // how close an enemy can be ?
    public CharacterController charCtrl;
    private void Awake()
    {
        charCtrl = gameObject.GetComponent<CharacterController>();
        //foot = gameObject.GetComponent<BoxCollider>();
        //rbPlayer = gameObject.GetComponent<Rigidbody>();
    }

    void Update()
    {
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
        CalculateForwardMovement();
        CalculateVerticalMovement();
        SetTargetRotation();
        movement = forwardSpeed * transform.forward * Time.deltaTime;
        charCtrl.Move(new Vector3(movement.x, verticalSpeed, movement.z));
        if(movementInput != Vector2.zero)
            UpdateOrientation();
        isGrounded = charCtrl.isGrounded;
        previouslyGrounded = isGrounded;

        /* from gamekit3D 
        CacheAnimatorState();

        UpdateInputBlocking();

        EquipMeleeWeapon(IsWeaponEquiped());

        m_Animator.SetFloat(m_HashStateTime, Mathf.Repeat(m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f));
        m_Animator.ResetTrigger(m_HashMeleeAttack);

        if (m_Input.Attack && canAttack)
            m_Animator.SetTrigger(m_HashMeleeAttack);

        CalculateForwardMovement();
        CalculateVerticalMovement();

        SetTargetRotation();

        if (IsOrientationUpdated() && IsMoveInput)
            UpdateOrientation();

        PlayAudio();

        TimeoutToIdle();

        m_PreviouslyGrounded = m_IsGrounded;

        */
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
        forwardSpeed = Mathf.MoveTowards(forwardSpeed, desiredForwardSpeed, acceleration * Time.deltaTime);
    }
    void CalculateVerticalMovement()
    {
        // If jump is not currently held and Ellen is on the ground then she is ready to jump.
        if (!jump && isGrounded)
        {
            readyToJump = true;
            readyToDoubleJump = true;
            firstJumpFinished = false;
        }
        if (!jump && !isGrounded)
        {
            firstJumpFinished = true;
        }
        if (isGrounded)
        {
            // When grounded we apply a slight negative vertical speed to make Ellen "stick" to the ground.
            verticalSpeed = -gravity * k_StickingGravityProportion;

            // If jump is held, Ellen is ready to jump and not currently in the middle of a melee combo...
            if (jump && readyToJump && !inAttack)
            {
                // ... then override the previously set vertical speed and make sure she cannot jump again.
                verticalSpeed = jumpPower;
                isGrounded = false;
                readyToJump = false;
            }
        }
        else
        {
            // If Ellen is airborne, the jump button is not held and Ellen is currently moving upwards...
            if (!jump && verticalSpeed > 0.0f)
            {
                // ... decrease Ellen's vertical speed.
                // This is what causes holding jump to jump higher that tapping jump.
                verticalSpeed -= k_JumpAbortSpeed * Time.deltaTime;
            }
            

            // If a jump is approximately peaking, make it absolute.
            if (Mathf.Approximately(verticalSpeed, 0f))
            {
                verticalSpeed = 0f;
            }

            // If Ellen is airborne, apply gravity.
            verticalSpeed -= gravity * Time.deltaTime;
            if(jump && readyToDoubleJump && firstJumpFinished)
            {
                verticalSpeed = jumpPower;
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

        // Insert Greg's camera control/////////////////////////////////////////////////////////////////////////////////////////////////////////////
        Vector3 forward = /*Quaternion.Euler(0f, cameraSettings.Current.m_XAxis.Value, 0f) * */ Vector3.forward;
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


    const float k_AirborneTurnSpeedProportion = 5.4f;
    const float k_InverseOneEighty = 1f / 180f;
    // Called each physics step after SetTargetRotation if there is move input and Ellen is in the correct animator state according to IsOrientationUpdated.
    void UpdateOrientation()
    {
        // m_Animator.SetFloat(m_HashAngleDeltaRad, m_AngleDiff * Mathf.Deg2Rad);

        Vector3 localInput = new Vector3(movementInput.x, 0f, movementInput.y);
        float groundedTurnSpeed = Mathf.Lerp(maxTurnSpeed, minTurnSpeed, forwardSpeed / desiredForwardSpeed);
        float actualTurnSpeed = isGrounded ? groundedTurnSpeed : Vector3.Angle(transform.forward, localInput) * k_InverseOneEighty * k_AirborneTurnSpeedProportion * groundedTurnSpeed;
        m_TargetRotation = Quaternion.RotateTowards(transform.rotation, m_TargetRotation, actualTurnSpeed * Time.deltaTime);

        transform.rotation = m_TargetRotation;
    }
}
