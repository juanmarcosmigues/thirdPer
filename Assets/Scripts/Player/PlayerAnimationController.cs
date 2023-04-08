using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    public Animator animator;
    public PlayerController controller;
    public PlayerInputController inputController;

    private void Update()
    {
        animator.SetBool("Running", inputController.valueMovementAxis > 0.0f);
        animator.SetBool("Grounded", controller.characterController.Grounded);
    }

}
