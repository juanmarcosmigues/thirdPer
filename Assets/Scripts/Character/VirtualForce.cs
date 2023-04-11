using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class VirtualForce
{
    public float acceleration;
    public float deceleration;
    public float maxVelocity;
    public float inertia;
    public AnimationCurve inertiaCurve;
    public AnimationCurve inertiaInfluenceOnAngle;
    public bool sphericalInterpolation;
    public bool debug;

    private float lastTimeApplied;
    private float timeApplying;

    private Vector3 goalDirection;
    private float goalVelocity;

    protected float currentVelocity;
    private float currentInertia;
    private Vector3 currentSphericalInterpolation;
    private Vector3 currentForce;

    public bool Applying => Time.time - lastTimeApplied <= Time.deltaTime * 1.5f;
    public Vector3 GoalDirection => goalDirection;
    public float GoalVelocity => goalVelocity;
    public Vector3 GoalForce => goalDirection * goalVelocity;
    public Vector3 CurrentForce => currentForce;
    public float CurrentVelocityNormalized => currentVelocity / maxVelocity;

    public void Apply(Vector3 direction, float velocityMultiplier = 1f)
    {
        lastTimeApplied = Time.time;
        goalDirection = direction;
        goalVelocity = maxVelocity * velocityMultiplier;
    }

    public void Step ()
    {
        if (Applying)
        {
            timeApplying += Time.fixedDeltaTime;

            currentVelocity = 
                Mathf.Clamp(
                currentVelocity + acceleration * Time.fixedDeltaTime,
                0f, maxVelocity);            
        }
        else
        {
            timeApplying = 0f;

            currentVelocity =
                Mathf.Clamp(
                currentVelocity - deceleration * Time.fixedDeltaTime,
                0f, maxVelocity);
        }

        currentInertia = inertia * inertiaCurve.Evaluate(CurrentVelocityNormalized);

        currentSphericalInterpolation = Vector3.Slerp(currentSphericalInterpolation,
            GoalForce, (10f / currentInertia) * Time.fixedDeltaTime);

        float angleNormalized =
            Vector3.Angle
            (
                currentSphericalInterpolation.normalized,
                goalDirection
            ) / 180f;
        float dotProduct =
            Vector3.Dot
            (
            currentSphericalInterpolation.normalized,
            goalDirection
            );
        float outputVelocity =
            dotProduct
            * CurrentVelocityNormalized;

        currentForce = outputVelocity * GoalForce;

        if (debug)
        {
            DebugDraw.Draw(() =>
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(Vector3.zero, currentForce);
                Gizmos.DrawSphere(currentForce, 0.1f);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(Vector3.zero, GoalDirection * 5f);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(Vector3.zero, currentSphericalInterpolation.normalized * 5f);
            });
        }
    }
}
