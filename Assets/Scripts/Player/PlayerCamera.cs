using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    public Camera camera;
    public Transform target;
    public Transform rotationPivot;
    public Transform cameraDepthTarget;
    public LayerMask occlusionMask;
    [Range(0f, 1f)] 
    public float sensitivityX;
    [Range(0f, 1f)]
    public float sensitivityY;
    public float rotaitonEasingSpeed;
    public float maxAngle;
    public float minAngle;
    public float positionTrackSpeed;
    public bool debug;

    public Vector3 horizontalPlaneOrientation { get; private set; } 

    private PlayerCameraInput inputActions;
    //ia = input action
    private InputAction iaLook;

    private Vector3 lookGoal;
    private Vector3 positionGoal;

    private void Awake()
    {
        inputActions = new PlayerCameraInput();

        lookGoal = transform.forward;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        iaLook = inputActions.Player.Look;
        iaLook.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }


    private void FixedUpdate()
    {
        UpdateRotation();
        UpdatePosition();
        UpdateDepth();
    }

    protected void UpdateRotation ()
    {
        Vector2 rotationValue = iaLook.ReadValue<Vector2>();
        rotationValue.x *= sensitivityX;
        rotationValue.y *= sensitivityY;

        Vector3 perpendicularAxisX = Vector3.Cross(lookGoal.normalized, Vector3.up);

        lookGoal = Quaternion.AngleAxis(rotationValue.y, perpendicularAxisX) * lookGoal.normalized;
        lookGoal = Quaternion.AngleAxis(rotationValue.x, Vector3.up) * lookGoal.normalized;

        //Has to apply perpendicular update after the applied rotation
        perpendicularAxisX = Vector3.Cross(lookGoal.normalized, Vector3.up);
        Vector3 horizontalRotationDefault = Vector3.Cross(-perpendicularAxisX, Vector3.up);
        horizontalPlaneOrientation = horizontalRotationDefault;

        float angle = Get180Angle(lookGoal.normalized, horizontalRotationDefault, perpendicularAxisX);

        if (angle > maxAngle)
        {
            lookGoal = (Quaternion.AngleAxis(-maxAngle, perpendicularAxisX) * horizontalRotationDefault).normalized;
        }
        if (angle < minAngle)
        {
            lookGoal = (Quaternion.AngleAxis(-minAngle, perpendicularAxisX) * horizontalRotationDefault).normalized;
        }

        if (debug)
        {
            DebugDraw.Draw(
                    () =>
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(transform.position, transform.position + perpendicularAxisX * 10f);
                        Gizmos.color = Color.blue;
                        Gizmos.DrawLine(transform.position, transform.position + lookGoal * 10f);
                        Gizmos.color = Color.black;
                        Gizmos.DrawLine(transform.position, transform.position + horizontalRotationDefault * 10f);
                    }); ;
        }

        Vector3 currentLook = Vector3.Slerp(rotationPivot.forward, lookGoal, rotaitonEasingSpeed * Time.fixedDeltaTime);

        rotationPivot.transform.rotation = Quaternion.LookRotation(currentLook);
    }

    protected void UpdatePosition ()
    {
        positionGoal = target.position;

        Vector3 currentPosition = Vector3.Lerp(transform.position, positionGoal, positionTrackSpeed * Time.fixedDeltaTime);
        transform.position = currentPosition;
    }

    protected void UpdateDepth ()
    {
        Vector3 delta = cameraDepthTarget.position - target.position;
        RaycastHit hit;
        float depth = 0f;

        if (Physics.Raycast(target.position, delta, out hit, delta.magnitude, occlusionMask.value))
        {
            depth = delta.magnitude - hit.distance;
            depth += 1f;
        }

        Vector3 goalPosition = Vector3.zero;
        goalPosition.z = depth;

        camera.transform.localPosition = goalPosition;
    }

    public Vector3 RotateTowardsCamera(Vector2 value)
    {
        Vector3 axisX = Vector3.Cross(camera.transform.forward, Vector3.up);
        Vector3 axisZ = Vector3.Cross(axisX, Vector3.up);

        Vector3 outValue = axisZ * -value.y;
        outValue += axisX * -value.x;
        outValue.Normalize();

        return outValue;
    }
    public float Get360Angle (Vector3 vector, Vector3 target, Vector3 axis)
    {
        float angle = Vector3.SignedAngle(vector, target, axis); //Returns the angle between
        if (angle < 0)
        {
            angle = 360 - angle * -1;
        }
        return angle;
    }
    public float Get180Angle(Vector3 vector, Vector3 target, Vector3 axis)
    {
        float angle = Vector3.SignedAngle(vector, target, axis); //Returns the angle between
        return angle;
    }
}
