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
    [Range(0.1f, 1f)]
    public float groundFriction;

    [Space]
    [Header("Forces")]
    public MovementForce inputForceMovement;

    [Space]
    [Header("Debug")]
    public bool debug = false;

    public event System.Action OnFixedUpdate;
    public bool Grounded { get; private set; }

    //Storage
    private PhysicsTools.RaycastHitPlus[] cgCastHits;
    private GroundContactPoint[] cgContacts;
    private GroundContactPoint[] cgActiveContacts;
    private Vector3 currentGroundNormal;
    private float currentGroundInfluence;

    private float colliderRadius => characterBody.radius;
    private Vector3 colliderSphereBottomCenter => colliderBottom + Vector3.up * colliderRadius;
    private Vector3 colliderPosition => characterBody.bounds.center;
    private Vector3 colliderBottom => colliderPosition + Vector3.down *
            characterBody.bounds.extents.y;

    //velocity
    private Vector3 currentGravityForce;
    /// <summary>
    /// Virtual direction is a spherical interpolated vector that is only used to calculate
    /// the magnitude of the currentMovementForce by the angle between desired goal and this, 
    /// not the direction.
    /// </summary>
    private Vector3 currentVirtualMovementSpherical;
    private Vector3 currentVirtualMovementLinear;
    private Vector3 currentMovementForce;
    private Vector3 currentRotation;

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
        public Vector3 GoalDirection => goalDirection;
        public float GoalVelocity => goalVelocity;  

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

        OnFixedUpdate += inputForceMovement.Step;
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
        OnFixedUpdate?.Invoke();

        UpdateGrounded();
        
        //UPDATE GRAVITY FORCE

        Vector3 gravityStep = Vector3.down * weight * Time.fixedDeltaTime;
        currentGravityForce += gravityStep;
        currentGravityForce -= gravityStep * 
            settings.groundInfluenceCurve.Evaluate(currentGroundInfluence); 
        
        if (Grounded)
        {
            StickToGround();
            currentGravityForce = Vector3.zero;            
        }

        //UPDATE MOVEMENT FORCE

        //float currentVelocityNormalized = 
        //    currentMovementForce.magnitude / forceMovement.maxVelocity;
        //float inputMovementOffsetAngle = 
        //    Vector3.Angle(currentVirtualMovementSpherical, forceMovement.GoalDirection);
        //float inertia =
        //    Mathf.Lerp(
        //    1f,
        //    1f - settings.inertiaSwitchDirectionCurve.
        //    Evaluate(inputMovementOffsetAngle / 180f),
        //    settings.inertiaVelocityInfluenceCurve.
        //    Evaluate(currentVelocityNormalized)
        //    ); 
        //float currentFriction = 
        //    groundFriction * 20f * inertia;

        //currentVirtualMovementSpherical = Vector3.Slerp(currentVirtualMovementSpherical,
        //    forceMovement.CurrentForce, currentFriction * Time.fixedDeltaTime);
        //currentVirtualMovementLinear = Vector3.Lerp(currentVirtualMovementLinear,
        //    forceMovement.CurrentForce, currentFriction * Time.fixedDeltaTime);

        //float currentMovementMagnitude = 
        //    Vector3.Project
        //    (
        //    currentVirtualMovementSpherical.normalized,
        //    forceMovement.GoalDirection
        //    ).magnitude;

        currentMovementForce = inputForceMovement.CurrentForce;

        //if (debug)
        //{
        //    DebugDraw.Draw(() => 
        //    {
        //        Gizmos.color = Color.red;
        //        Gizmos.DrawLine(colliderPosition, colliderPosition + currentMovementForce * 0.5f);
        //        Gizmos.DrawSphere(colliderPosition + currentMovementForce * 0.5f, 0.1f);
        //        Gizmos.color = Color.yellow;
        //        Gizmos.DrawLine(colliderPosition, colliderPosition + forceMovement.GoalDirection * 5f);
        //        Gizmos.color = Color.green;
        //        Gizmos.DrawLine(colliderPosition, colliderPosition + currentVirtualMovementSpherical.normalized * 5f);
        //    });
        //}

        if (Grounded)
        {
            currentMovementForce =
                    Vector3.ProjectOnPlane(currentMovementForce, currentGroundNormal).normalized
                    * currentMovementForce.magnitude;
        }

        //UPDATE ROTATION

        if (currentVirtualMovementSpherical.sqrMagnitude > 1f)
            currentRotation = currentVirtualMovementSpherical;
        currentRotation.y = 0f;
        currentRotation.Normalize();

        //APPLY ALL

        rigidBody.rotation = Quaternion.LookRotation(currentRotation);
        rigidBody.velocity = currentGravityForce + currentMovementForce;
    }

    protected void UpdateGrounded ()
    {
        CheckGroundedSphereCast();
        UpdateGroundInfluence();
        Grounded = currentGroundInfluence > settings.groundAmountThreshold;
    }
    protected void UpdateGroundInfluence ()
    {
        float groundInfluence = 0f;
        Vector3 averageGroundNormal = Vector3.zero;

        for (int i = 0; i < cgActiveContacts.Length; i++)
        {
            groundInfluence += cgActiveContacts[i] != null ? cgActiveContacts[i].influence : 0f;
            averageGroundNormal += cgActiveContacts[i] != null ? cgActiveContacts[i].normal : Vector3.zero;
        }

        groundInfluence /= settings.cgMaxActivePoints;
        averageGroundNormal /= settings.cgMaxActivePoints;
        averageGroundNormal.Normalize();

        currentGroundInfluence = groundInfluence;
        currentGroundNormal = averageGroundNormal;
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
        Vector3 bestContactPoint = cgActiveContacts[0].worldPosition;
        Vector3 heightDelta = bestContactPoint - colliderSphereBottomCenter;
        heightDelta.y += settings.groundOffset;
        heightDelta.x = 0f;
        heightDelta.z = 0f;

        rigidBody.position += heightDelta;
    }

    #region Input

    public void InputMove (Vector3 direction, float velocity, float factor)
    {
        Vector3 moveOutput = direction;
        moveOutput = moveOutput.normalized * velocity;

        inputForceMovement.Apply(moveOutput, factor);
    }
    public void InputTurn (Vector3 lookDirection)
    {
        //turnMovement.Apply(lookDirection);
    }
    public void InputJump () { }
    public void InputExternalForce() { }

    #endregion
}
