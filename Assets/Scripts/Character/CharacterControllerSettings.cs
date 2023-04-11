using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSettings", menuName = "Character Controller/Character Settings")]
public class CharacterControllerSettings : ScriptableObject
{
    [Header("Check ground ray")]
    public float cgDistance = 0.2f;
    public int cgSphereRayResolution = 8;
    public int cgSphereRayDensityResolution = 3;
    public int cgMaxActivePoints = 3;

    [Header("Check ground filters")]
    [Range(0f, 1f)]
    public float groundAmountThreshold;
    public float maxDetectSlope;
    public float maxGroundSlope;
    public AnimationCurve groundInfluenceCurve;

    [Header("On grounded variables")]
    public float groundOffset = 0.05f;

    [Header("Movement variables")]
    public AnimationCurve inertiaSwitchDirectionCurve;
    public AnimationCurve inertiaVelocityInfluenceCurve;
}
