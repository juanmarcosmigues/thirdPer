using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class VirtualForce
{
    public float velocity;
    public float movementAcceleration;
    public float turnAcceleration;
    public bool debug;

    protected float lastTimeApplied;
    protected float timeApplying;

    protected Vector3 goalDirection;
    protected float goalVelocity;

    protected float currentVelocity;
    protected Vector3 currentDirection;

    protected Vector3 currentForce;

    public bool Applying => Time.time - lastTimeApplied <= Time.deltaTime * 1.5f;
    public Vector3 GoalDirection => goalDirection;
    public float GoalVelocity => goalVelocity;
    public Vector3 GoalForce => goalDirection * goalVelocity;
    public Vector3 CurrentForce => currentForce;
    public float CurrentVelocityNormalized => currentVelocity / velocity;

    public virtual void Apply(Vector3 direction, float velocityMultiplier = 1f)
    {
        lastTimeApplied = Time.time;
        goalDirection = direction;
        goalVelocity = velocity * velocityMultiplier;
    }

    public virtual void Step ()
    {
        StepInput();

        StepCalculations();

        if (debug)
        {
            DebugDraw.Draw(() =>
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(Vector3.zero, currentForce);
                Gizmos.DrawSphere(currentForce, 0.1f);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(Vector3.zero, GoalDirection * 5f);
            });
        }
    }

    protected virtual void StepInput ()
    {
        if (Applying)
        {
            timeApplying += Time.fixedDeltaTime;

            currentVelocity =
                Mathf.Clamp(
                currentVelocity
                + movementAcceleration * Time.fixedDeltaTime,
                0f, velocity);

            currentDirection =
                Vector3.Lerp(
                    currentDirection,
                    goalDirection,
                    turnAcceleration * Time.fixedDeltaTime);
        }
        else
        {
            timeApplying = 0f;

            currentVelocity =
                Mathf.Clamp(
                currentVelocity
                - movementAcceleration * Time.fixedDeltaTime,
                0f, velocity);
        }
    }
    protected virtual void StepCalculations ()
    {
        currentForce = currentDirection * currentVelocity;
    }
}
