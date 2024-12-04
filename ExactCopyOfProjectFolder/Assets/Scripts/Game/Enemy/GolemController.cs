using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gamekit3D.Message;
using UnityEngine.AI;

namespace Gamekit3D
{
    //[DefaultExecutionOrder(10)]
    [RequireComponent(typeof(NavMeshAgent))]
    public class GolemController : MonoBehaviour, IMessageReceiver
    {
        [SerializeField]
        protected Quaternion targetRotation;
        public float dontCareDistance = 100f;
        public float aimShotDistance = 50f;
        public float meleeDistance = 15f;
        public float slamDistance = 10f;
        public float distanceToPlayer;
        public bool attacking;
        public float aimShotAngle = 30f;

        public float maxTurnSpeed = 1000f;
        public float minTurnSpeed = 500f;
        public float desiredForwardSpeed;
        public float forwardSpeed;
        public float maxForwardSpeed = 50f;
        public float acceleration = 10f;
        public float m_ForwardSpeed;

        public static readonly int hashDoCare = Animator.StringToHash("DoCare");
        public static readonly int hashSlam = Animator.StringToHash("Slam");
        public static readonly int hashSwing = Animator.StringToHash("Swing");
        public static readonly int hashShoot = Animator.StringToHash("Shoot");
        public static readonly int hashDamaged = Animator.StringToHash("Damaged");
        public static readonly int hashDead = Animator.StringToHash("Dead");
        

        protected PlayerControl m_playerControl;
        [SerializeField]
        protected NavMeshAgent m_NavMeshAgent;
        protected bool m_FollowNavmeshAgent;
        public Vector3 target;
        public Vector3 targetDirection;
        public Vector3 nextStep;
        protected Animator m_Animator;
        protected Rigidbody m_Rigidbody;
        public Vector3 toTarget;

        public GameObject Lfist;
        public GameObject Rfist;
        public GameObject LDamageFist;
        public GameObject RDamageFist;
        public float shotSpeed = 100f;
        public GameObject damageSphere;
        public Damageable m_Damageable;
        public bool alive;

        // For when the player is far away
        public Vector3 homePosition;
        public Vector3 homeRotation;
        private void Awake()
        {
            alive = true;
            m_Damageable = GetComponentInChildren<Damageable>();
            m_Damageable.onDamageMessageReceivers.Add(this);

            m_Damageable.isInvulnerable = true;
        }
        private void OnEnable()
        {
            homePosition = transform.position;
            homeRotation = transform.forward;
            m_playerControl = PlayerControl.instance;
            distanceToPlayer = Vector3.Distance(transform.position, m_playerControl.transform.position);
            m_NavMeshAgent = GetComponent<NavMeshAgent>();
            m_NavMeshAgent.updatePosition = true;
            maxForwardSpeed = m_NavMeshAgent.speed;
            acceleration = m_NavMeshAgent.acceleration;

            m_Animator = GetComponent<Animator>();
            m_Rigidbody = GetComponentInChildren<Rigidbody>();
            if (m_Rigidbody == null)
                m_Rigidbody = gameObject.AddComponent<Rigidbody>();

            m_Rigidbody.isKinematic = true;
            m_FollowNavmeshAgent = true;
            m_Rigidbody.useGravity = false;
            m_NavMeshAgent.updatePosition = false;
        }
        private void Start()
        {
            Lfist = transform.Find("Body").Find("ArmL").Find("ArmL-Body").Find("Fist").gameObject;
            Rfist = transform.Find("Body").Find("ArmR").Find("ArmR-Body").Find("Fist").gameObject;
            LDamageFist = transform.Find("Body").Find("ArmL").Find("ArmL-Body").Find("Fist").Find("DamageFist").gameObject;
            RDamageFist = transform.Find("Body").Find("ArmR").Find("ArmR-Body").Find("Fist").Find("DamageFist").gameObject;
            damageSphere = transform.Find("Body").Find("DamageSphere").gameObject;

        }
        /*
        private void LateUpdate()
        {
            target = m_playerControl.transform.position;
            m_NavMeshAgent.SetDestination(target);
            nextStep = m_NavMeshAgent.nextPosition;
            targetDirection = transform.position - nextStep; // I hope this works
            Vector3 moveDirection = targetDirection;
            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }
            desiredForwardSpeed = moveDirection.magnitude * maxForwardSpeed;
            // print(target);
        }
        */
        private void FixedUpdate()
        {
            if (!alive) return;
            Vector2 player = new Vector2(m_playerControl.transform.position.x, m_playerControl.transform.position.z);
            Vector2 self = new Vector2(transform.position.x, transform.position.z);
            float angleToTarget = Vector3.Angle(-transform.forward, (m_playerControl.transform.position - transform.position));
            distanceToPlayer = Vector3.Distance(player, self);

            // Set target
            if (distanceToPlayer < dontCareDistance)
                target = m_playerControl.transform.position;
            else
            {
                m_Animator.SetBool(hashDoCare, false);
                m_Animator.ResetTrigger(hashShoot);
                m_Animator.SetBool(hashSlam, false);
                m_Animator.SetBool(hashSwing, false);

                target = homePosition;
            }
            m_NavMeshAgent.SetDestination(target);
            nextStep = m_NavMeshAgent.nextPosition;
            targetDirection = transform.position - nextStep; // I hope this works
            Vector3 moveDirection = targetDirection;
            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }
            desiredForwardSpeed = moveDirection.magnitude * maxForwardSpeed;

            
            // I THINK THAT THIS IS A REALLY BAD WAY TO DO IT
            // Either :
                // Lean more into the logic in the Animator screen, using very simple commands to set these true and false
                // Use a lot of conditionals in the code, and use simpler logic in the Animator screen
            if (distanceToPlayer < dontCareDistance)
            {
                m_Animator.SetBool(hashDoCare, true);
                if (distanceToPlayer < meleeDistance)
                {
                    //Prefer slamming to swinging
                    if (distanceToPlayer < slamDistance)
                    {
                        m_Animator.SetBool(hashSlam, true);
                    }
                    else if (angleToTarget < 180f)
                    {
                        m_Animator.SetBool(hashSwing, true);
                    }
                    //m_Animator.SetTrigger(hashSlam);
                }
                else
                {
                    m_Animator.SetBool(hashSlam, false);
                    m_Animator.SetBool(hashSwing, false);
                }
                if (distanceToPlayer >= aimShotDistance && angleToTarget < aimShotAngle)
                {
                    
                    m_Animator.SetTrigger(hashShoot);
                }
            }
            else
            {
                m_Animator.SetBool(hashDoCare, false);
            }
            if (!attacking)
            {
                if(Vector3.Distance(target, transform.position) > m_NavMeshAgent.stoppingDistance)
                {
                    Rotate();
                    Walk();
                }
            }
        }
        public void SetForward(Vector3 forward)
        {
            Quaternion targetRotation = Quaternion.LookRotation(forward);

            //if (interpolateTurning)
            //{
            //    targetRotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
            //        m_NavMeshAgent.angularSpeed * Time.deltaTime);
            //}

            transform.rotation = targetRotation;
        }
        
        void Rotate()
        {
            Quaternion targetRotation;
            if (Mathf.Approximately(Vector3.Dot(targetDirection, Vector3.forward), -1.0f))
            {
                // print("180 time");
                targetRotation = Quaternion.LookRotation(-Vector3.forward);
            }
            else
            {
                // print("normal nav");
                // Otherwise the rotation should be the offset of the input from the camera's forward.
                Quaternion cameraToInputOffset = Quaternion.FromToRotation(Vector3.forward, targetDirection);
                targetRotation = Quaternion.LookRotation(cameraToInputOffset * Vector3.forward);
            }
            float turnSpeed = Mathf.Lerp(maxTurnSpeed, minTurnSpeed, m_ForwardSpeed / desiredForwardSpeed);
            targetRotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

            transform.rotation = targetRotation;
        }
        void Walk()
        {
            m_ForwardSpeed = Mathf.Max(0, maxForwardSpeed - (Vector3.Angle(transform.forward, targetDirection)/90));
            // m_ForwardSpeed = Mathf.MoveTowards(m_ForwardSpeed, desiredForwardSpeed, acceleration * Time.deltaTime);
            Vector3 desiredPosition = Vector3.Lerp(transform.position, nextStep, m_ForwardSpeed*Time.deltaTime);
            transform.position = desiredPosition;
        }
        void MakeAttack()
        {
            damageSphere.SetActive(true);
        }
        /// <summary>
        /// Called by animation events.
        /// </summary>
        void ShootRocks()
        {
            GameObject LFistDupe = Instantiate(Lfist, Lfist.transform.position, Lfist.transform.rotation);
            GameObject RFistDupe = Instantiate(Rfist, Rfist.transform.position, Rfist.transform.rotation);
            LFistDupe.transform.localScale *= transform.localScale.x;
            RFistDupe.transform.localScale *= transform.localScale.x;
            Rigidbody rb;
            LFistDupe.AddComponent<FlyingFist>();
            rb = LFistDupe.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(((target-LFistDupe.transform.position).normalized ) * shotSpeed);
            
            RFistDupe.AddComponent<FlyingFist>();
            rb = RFistDupe.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(((target-RFistDupe.transform.position).normalized ) * shotSpeed);
            print("ShootRock!");
        }
        void Attacking()
        {
            attacking = true;
        }
        void notAttacking()
        {
            attacking = false;
        }
        void Damaging()
        {
            LDamageFist.GetComponent<BoxCollider>().isTrigger = true;
            RDamageFist.GetComponent<BoxCollider>().isTrigger = true;
        }
        void notDamaging()
        {
            LDamageFist.GetComponent<BoxCollider>().isTrigger = false;
            RDamageFist.GetComponent<BoxCollider>().isTrigger = false;
        }
        public void OnReceiveMessage(MessageType type, object sender, object msg)
        {
            print("I was hit but what");
            print(type);
            print(sender);
            print(msg);
            switch (type)
            {
                case Message.MessageType.DEAD:
                    Death((Damageable.DamageMessage)msg);
                    break;
                case Message.MessageType.DAMAGED:
                    ApplyDamage((Damageable.DamageMessage)msg);
                    break;
                default:
                    break;
            }
        }

        private void Death(Damageable.DamageMessage damageMessage)
        {
            // die and ragdoll
            transform.Find("legs").gameObject.SetActive(false);
            transform.Find("Body").gameObject.SetActive(false);
            transform.Find("Head").gameObject.SetActive(false);
            transform.Find("golem_ragdoll").gameObject.SetActive(true);
            alive = false;
            print("OWHY");
            m_Animator.enabled = false;
        }
        private void ApplyDamage(Damageable.DamageMessage damageMessage)
        {
            // play damaged animation
            /*
            float verticalDot = Vector3.Dot(Vector3.up, msg.direction);
            float horizontalDot = Vector3.Dot(transform.right, msg.direction);

            Vector3 pushForce = transform.position - msg.damageSource;

            pushForce.y = 0;

            transform.forward = -pushForce.normalized;
            controller.AddForce(pushForce.normalized * 5.5f, false);

            controller.animator.SetFloat(hashVerticalDot, verticalDot);
            controller.animator.SetFloat(hashHorizontalDot, horizontalDot);

            controller.animator.SetTrigger(hashHit);
            */

            m_Animator.SetTrigger(hashDamaged);
        }
    }

}
