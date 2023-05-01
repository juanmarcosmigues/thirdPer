using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public CharacterController characterController;

    private void Update()
    {

    }
    public void Jump ()
    {
        Debug.Log("JUMP");
        characterController.Jump();
    }
    public void Move (Vector3 direction, float velocity)
    {
        characterController.InputMove(direction, velocity, 1f);
        characterController.InputTurn(direction);
    }
}
