using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class MovementForce : VirtualForce
{
    public float heavyTurnAngleThreshold;
    [Range(0f, 1f)]
    public float heavyTurnVelocityThreshold;
    public float heavyTurnDuration;

    /// <summary>
    /// Direction over time is used to get a angle variation from the input in a bigger time window,
    /// since to detect a turn of 180 degrees on a single frame is not good bc the user input usually
    /// has a few more frames to turn 180.
    /// </summary>
    protected Vector3 directionOverTime;
    /// <summary>
    /// Same for velocity over time, get us a more time window to detect velocity changes not just frame perfect.
    /// </summary>
    protected float velocityOverTime;
    /// <summary>
    /// A second calculation step over first current Direction
    /// </summary>
    protected Vector3 secondCurrentDirection;
    protected float heavyTurnCurrentValue;
    protected float responsiveness => 1f - heavyTurnCurrentValue / heavyTurnDuration;

    public override void Apply(Vector3 direction, float velocityMultiplier = 1)
    {
        //Got the first frame of applying
        if (!Applying)
        {
            directionOverTime = direction;
        }

        base.Apply(direction, velocityMultiplier);
    }
    protected override void StepCalculations()
    {
        directionOverTime = Vector3.Slerp(directionOverTime, currentDirection, 10f * Time.fixedDeltaTime);
        velocityOverTime = Mathf.Lerp(velocityOverTime, currentForce.magnitude, 10f * Time.fixedDeltaTime);

        float currentAngle =
            Vector3.Angle
            (
                directionOverTime,
                goalDirection
            );

        if (currentAngle > heavyTurnAngleThreshold 
            && velocityOverTime > GoalVelocity * heavyTurnVelocityThreshold)
        {
            heavyTurnCurrentValue = heavyTurnDuration;
        }

        heavyTurnCurrentValue -= Time.fixedDeltaTime;

        secondCurrentDirection = Vector3.Lerp(secondCurrentDirection,
            currentDirection, responsiveness);

        currentForce = secondCurrentDirection * currentVelocity;

        if (debug)
        {
            DebugDraw.Draw(() =>
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(Vector3.zero, secondCurrentDirection.normalized * 5f);
            });
        }
    }
}
