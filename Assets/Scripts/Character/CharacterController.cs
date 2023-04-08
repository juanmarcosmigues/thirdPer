using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : MonoBehaviour
{
    public Rigidbody rigidBody;
    public CapsuleCollider characterBody;

    [Space]
    [Header("Character Settings")]
    public CharacterControllerSettings settings;
    public LayerMask groundMask;
    public float weight;

    [Space]
    [Header("Turn Forces")]
    public Force turnMovement;
    [Header("Forces")]
    public Force forceMovement;

    [Space]
    [Header("Debug")]
    public bool debug = false;

    public bool Grounded { get; private set; }

    //Storage
    private PhysicsTools.RaycastHitPlus[] cgCastHits;
    private GroundContactPoint[] cgContacts;
    private GroundContactPoint[] cgActiveContacts;
    private float cgSlopeValue;
    private float cgGroundInfluence;

    private float colliderRadius => characterBody.radius;
    private Vector3 colliderSphereBottomCenter => colliderBottom + Vector3.up * colliderRadius;
    private Vector3 colliderPosition => characterBody.bounds.center;
    private Vector3 colliderBottom => colliderPosition + Vector3.down *
            characterBody.bounds.extents.y;

    //velocity
    private Vector3 forceGravity;

    [System.Serializable]       
    public class Force
    {
        public float acceleration;
        public float deceleration;
        public float maxVelocity;
        public float turnAceleration;
        public float turnDeceleration;
        public bool sphericalInterpolation;

        private float lastTimeApplied;
        private float timeApplying;
        private float currentTurnFactor;
        private float currentVelocity;
        private Vector3 currentDirection;

        private Vector3 goalDirection;
        private float goalVelocity;

        public bool Applying => Time.time - lastTimeApplied <= Time.deltaTime * 1.5f;
        public Vector3 CurrentForce => currentDirection.normalized * currentVelocity;

        public void Apply(Vector3 direction, float velocityMultiplier = 1f)
        {
            lastTimeApplied = Time.time;
            goalDirection = direction;
            goalVelocity = maxVelocity * velocityMultiplier;
        }

        public void FixedUpdate()
        {
            if (Applying)
            {
                timeApplying += Time.fixedDeltaTime;

                currentTurnFactor = Mathf.Clamp01
                    (currentTurnFactor + turnAceleration * Time.fixedDeltaTime);

                currentVelocity = Mathf.Lerp
                    (currentVelocity, goalVelocity, acceleration * Time.fixedDeltaTime);
                currentDirection = 
                    sphericalInterpolation ?
                    Vector3.Slerp
                    (currentDirection, goalDirection, turnAceleration * Time.fixedDeltaTime) 
                    :
                    Vector3.Lerp
                    (currentDirection, goalDirection, turnAceleration * Time.fixedDeltaTime);
            }
            else
            {
                timeApplying = 0f;

                currentTurnFactor = Mathf.Clamp01
                    (currentTurnFactor - turnDeceleration * Time.fixedDeltaTime);

                currentVelocity = Mathf.Lerp
                    (currentVelocity, 0f, deceleration * Time.fixedDeltaTime);
                currentDirection = sphericalInterpolation ?
                    Vector3.Slerp
                    (currentDirection, goalDirection, (turnAceleration * Time.fixedDeltaTime) * currentTurnFactor)
                    :
                    Vector3.Lerp
                    (currentDirection, goalDirection, (turnAceleration * Time.fixedDeltaTime) * currentTurnFactor);
            }
        }
    }

    [System.Serializable]
    public class GroundContactPoint
    {
        protected CharacterController character;
        protected int customID;
        public GroundContactPoint(CharacterController c, int id)
        {
            character = c;
            customID = id;
        }

        public bool contact;
        public Vector3 point;
        public Vector3 normal;

        public int id => customID;
        public Vector3 worldPosition { get; private set; }
        public Vector3 offset { get; private set; }
        public float horizontalOffset { get; private set; }
        public float verticalOffset { get; private set; }
        public float slope { get; private set; }
        public float influence { get; private set; }
        
        public void Set(Vector3 point, Vector3 normal)
        {
            this.point = point;
            this.normal = normal;

            worldPosition = point + normal * character.colliderRadius;
            slope = Vector3.Angle(Vector3.up, normal);

            contact = slope <= character.settings.maxDetectSlope;

            offset = character.colliderSphereBottomCenter - worldPosition;
            verticalOffset = offset.y;
            horizontalOffset = Vector3.SqrMagnitude(offset - verticalOffset * Vector3.up);

            //those hardcoded values are totally
            //arbitrary to find a nice point to get a nice influence value
            influence = 1f - Mathf.Clamp01(slope / character.settings.maxGroundSlope) * 0.5f;
            influence *= 1f - Mathf.Clamp01(horizontalOffset / (character.colliderRadius * 0.4f));
        }
        public void Clear ()
        {
            contact = false;
            point = Vector3.zero;
            normal = Vector3.zero;
            worldPosition = Vector3.zero;
            offset = Vector3.zero;
            slope = 0f;
            verticalOffset = 0f;
            horizontalOffset = 0f;
            influence = 0f;
        }
        public void CopyFrom (GroundContactPoint source)
        {
            contact = source.contact;
            point = source.point;
            normal = source.normal;
            worldPosition = source.worldPosition;
            offset = source.offset;
            slope = source.slope;
            verticalOffset = source.verticalOffset;
            horizontalOffset = source.horizontalOffset;
            influence = source.influence;
        }
    }
    
    private void Awake()
    {
        InitiateCG();
    }

    protected void InitiateCG ()
    {
        int sphereRayAmount = (settings.cgSphereRayDensityResolution * settings.cgSphereRayResolution) + 1; //+1 is the center ray of the sphere
        cgCastHits = new PhysicsTools.RaycastHitPlus[sphereRayAmount];
        cgContacts = new GroundContactPoint[cgCastHits.Length];
        cgActiveContacts = new GroundContactPoint[settings.cgMaxActivePoints];

        for (int i = 0; i < cgContacts.Length; i++)
        {
            cgContacts[i] = new GroundContactPoint(this, i);
        }
    }

    private void FixedUpdate()
    {
        StepGrounded();
        StepMovement();

        Vector3 currentMovementForce = forceMovement.CurrentForce;
        forceGravity += Vector3.down * weight * Time.fixedDeltaTime;

        if (Grounded)
        {
            StickToGround();
            forceGravity = Vector3.zero;
            //currentMovementForce = Vector3.ProjectOnPlane(currentMovementForce, GroundNormal);
        }


        rigidBody.rotation = Quaternion.LookRotation(turnMovement.CurrentForce.normalized);
        rigidBody.velocity = forceGravity + currentMovementForce;
    }

    protected void StepGrounded ()
    {
        CheckGroundedSphereCast();
        float groundAmount = GetGroundedAmount();
        Grounded = groundAmount > settings.groundAmountThreshold;
    }
    protected void StepMovement()
    {
        forceMovement.FixedUpdate();
        turnMovement.FixedUpdate();
    }
    protected float GetGroundedAmount ()
    {
        //if (!cgActiveContact.contact) return 0f;

        //cgSlopeValue =  slopeValueCurve.Evaluate(cgActiveContact.slope/maxSlope);
        //cgContactPointValue = contactPointsCurve.Evaluate((float)cgActiveContactsAmount/cgContactPool);
        //cgContactPointValue *= 2f;

        //cgGroundedValue = (cgSlopeValue + cgContactPointValue) / 3f;
        return 1f;
    }
    protected void CheckGroundedSphereCast()
    {
        for (int i = 0; i < cgCastHits.Length; i++)
        {
            cgCastHits[i].Clear();
            cgContacts[i].Clear();
            cgActiveContacts[Mathf.Clamp(i, 0, cgActiveContacts.Length-1)] = null;
        }

        int hits = 
            PhysicsTools.SphereCastDownwards
            (colliderSphereBottomCenter, colliderRadius,
            settings.cgDistance, groundMask.value,
            settings.cgSphereRayDensityResolution, settings.cgSphereRayResolution, 
            cgCastHits, debug);

        for (int c = 0; c < hits; c++)
        {
            //set new contact point
            cgContacts[c].Set(cgCastHits[c].hit.point, cgCastHits[c].hit.normal); 

            for (int ac = 0; ac < cgActiveContacts.Length; ac++) //already evaluates it on the pool of active contact
            {
                if (cgActiveContacts[ac] != null && cgActiveContacts[ac].contact) //if there was already a contact point evaluates
                {
                    if (cgActiveContacts[ac].influence < cgContacts[c].influence) //is a better contact then pick this
                    {
                        if (ac + 1 < cgActiveContacts.Length) //move up already existing points,  
                        {  
                            if (ac + 2 < cgActiveContacts.Length) // if there was two places move second and third up
                            {
                                cgActiveContacts[ac + 2] = cgActiveContacts[ac + 1];
                                cgActiveContacts[ac + 1] = cgActiveContacts[ac];
                            }
                            else //if it was only one place move it up,
                            {
                                cgActiveContacts[ac + 1] = cgActiveContacts[ac];
                            }
                        }

                        //assign new contact point in the active contacts
                        cgActiveContacts[ac] = cgContacts[c]; 
                        break;
                    }
                }
                else //if there was not contact point just put it there
                {
                    cgActiveContacts[ac] = cgContacts[c];
                    break;
                }
            }

            if (debug)
            {
                int index = c;
                DebugDraw.Draw(() =>
                {
                    Gizmos.color = Color.Lerp(Color.cyan, Color.red, cgContacts[index].influence);
                    Gizmos.DrawSphere(cgContacts[index].point, 0.02f);
                    Gizmos.DrawLine(cgContacts[index].point, cgContacts[index].worldPosition);
                //Gizmos.color = Color.white;
                //Gizmos.DrawLine(cgContacts[index].worldPosition, cgContacts[index].worldPosition + offsetOnHorizontalPlane);
                });
            }
        }

        if (debug)
        {
            DebugDraw.Draw(() =>
            {
                Gizmos.color = Color.white;
                for (int i = 0; i < cgActiveContacts.Length; i++)
                {
                    if (cgActiveContacts[i] != null && cgActiveContacts[i].contact)
                        Gizmos.DrawWireCube(cgActiveContacts[i].point, 0.05f * Vector3.one);
                }

            });
        }
    }
    protected void StickToGround ()
    {
        //Vector3 heightDelta = cgActiveContact.worldPosition - colliderSphereBottomCenter;
        //heightDelta.y += 0;
        //heightDelta.x = 0f;
        //heightDelta.z = 0f;

        //DebugDraw.Draw(() =>
        //{
        //    Gizmos.color = Color.red;
        //    Gizmos.DrawSphere(GroundContactPont, 0.1f);
        //});

        //rigidBody.position += heightDelta;
    }

    public void InputMove (Vector3 direction, float velocity, float factor)
    {
        Vector3 moveOutput = direction;
        moveOutput = moveOutput.normalized * velocity;

        forceMovement.Apply(moveOutput, factor);
    }
    public void InputTurn (Vector3 lookDirection)
    {
        turnMovement.Apply(lookDirection);
    }
}
