using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour
{
    public PlayerController controller;
    public PlayerCamera playerCamera;

    //iv = input value
    public Vector2 ivMove { get; private set; }
    public float valueMovementAxis => ivMove.magnitude;

    private PlayerInputActions inputActions;
    //ia = input action
    private InputAction iaMove;
    private InputAction iaAction;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        iaMove = inputActions.Player.Move;
        iaMove.Enable();

        iaAction = inputActions.Player.Action;
        iaAction.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        ivMove = iaMove.ReadValue<Vector2>();
        Vector3 direction = playerCamera.RotateTowardsCamera(ivMove);

        if (valueMovementAxis > 0.0f)
        {
            controller.Move(direction, valueMovementAxis);
        }
    }
}
