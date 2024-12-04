using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gamekit3D.Message;
using UnityEngine.AI;

namespace Gamekit3D
{
    //[DefaultExecutionOrder(10)]
    [RequireComponent(typeof(NavMeshAgent))]
    public class Sword_PossessedArmorController : MonoBehaviour, IMessageReceiver
    {
        public GameObject sword;
        public bool alive;
        public Vector3 targetDirection;
        public float distanceToPlayer;
        public Vector3 nextStep;
        public Damageable m_Damageable;


        private Animator m_Animator;
        public Vector3 target;
        public Vector3 homePosition;
        public Vector2 homeRotation;
        private PlayerControl m_playerControl;
        public float maxTurnSpeed = 120;
        public float minTurnSpeed = 12;
        public float dontCareDistance = 80f;

        public static readonly int hashPlayerDistance = Animator.StringToHash("PlayerDistance");
        public static readonly int hashPlayerJumping = Animator.StringToHash("PlayerJumping");

        private float desiredForwardSpeed;
        private float maxForwardSpeed = 10;
        private float m_ForwardSpeed;
        private NavMeshAgent m_NavMeshAgent;
        private bool attacking;
        // Start is called before the first frame update
        void OnEnable()
        {
            alive = true;
            m_Damageable = GetComponentInChildren<Damageable>();
            m_Damageable.onDamageMessageReceivers.Add(this);


            m_playerControl = PlayerControl.instance;
            m_Animator = GetComponent<Animator>();
            m_NavMeshAgent = gameObject.GetComponent<NavMeshAgent>();
            homePosition = transform.position;
            homeRotation = new Vector2(transform.forward.x, transform.forward.z);
            alive = true;
        }
        public void Attacking()
        {
            attacking = true;
        }
        public void NotAttacking()
        {
            attacking = false;
        }
        // Update is called once per frame
        void Update()
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
            if(target == m_playerControl.transform.position)
            {
                m_Animator.SetFloat(hashPlayerDistance, distanceToPlayer);
                m_Animator.SetBool(hashPlayerJumping, !m_playerControl.isGrounded);
            }
            if (!attacking)
            {
                if (!alive) print("how did we get here");
                Rotate();
                Walk();
            }
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
            m_ForwardSpeed = Mathf.Max(0, maxForwardSpeed - (Vector3.Angle(transform.forward, targetDirection) / 90));
            // m_ForwardSpeed = Mathf.MoveTowards(m_ForwardSpeed, desiredForwardSpeed, acceleration * Time.deltaTime);
            Vector3 desiredPosition = Vector3.Lerp(transform.position, nextStep, m_ForwardSpeed * Time.deltaTime);
            transform.position = desiredPosition;
        }

        void Damaging()
        {
            sword.GetComponent<BoxCollider>().isTrigger = true;
        }
        void NotDamaging()
        {
            sword.GetComponent<BoxCollider>().isTrigger = false;
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
            Transform ragdoll = transform.Find("ragdoll");
            ragdoll.gameObject.SetActive(true);
            if (attacking)
                ragdoll.Find("Base").gameObject.SetActive(true);
            for (int i = 0; i < ragdoll.childCount; i++)
            {
                Transform child = ragdoll.GetChild(i);
                Transform match = RecursiveFindChild(transform, child.name);
                child.position = match.position;
                child.rotation = match.rotation;
            }
            transform.Find("Root").gameObject.SetActive(false);
            transform.Find("Sword").gameObject.SetActive(false);
            print("OWHY");
            alive = false;
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

            // m_Animator.SetTrigger(hashDamaged);
        }
        Transform RecursiveFindChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child;
                }
                else
                {
                    Transform found = RecursiveFindChild(child, childName);
                    if (found != null)
                    {
                        print(childName + " found");
                        return found;
                    }
                }
            }
            return null;
        }
    }

}
