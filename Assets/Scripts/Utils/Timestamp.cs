using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Timestamp
{
    public float timeStamp;
    public float goalTime;
    public float time
    {
        get { return Time.time - timeStamp; }
    }
    public float remainingTime
    {
        get { return goalTime - time; }
    }
    public float remainingTimeNormalized
    {
        get { return remainingTime / goalTime; }
    }
    public void Set(float goalTime = 0.0f)
    {
        this.goalTime = goalTime;
        timeStamp = Time.time;
    }
}