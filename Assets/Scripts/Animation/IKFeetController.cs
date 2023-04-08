using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKFeetController : MonoBehaviour
{
    public LayerMask ikMask;
    [Range(0f, 1f)]
    public float footDistanceToGround;
    [Range(0f, 2f)]
    public float rayDistance;
    public bool active;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (!active) return;

        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1f);
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1f);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1f);

        RaycastHit hit;

        Ray ray = new Ray(animator.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up, Vector3.down);
        if (Physics.Raycast(ray, out hit, rayDistance, ikMask.value))
        {
            Vector3 footPosition = hit.point;
            footPosition.y += footDistanceToGround;
            animator.SetIKPosition(AvatarIKGoal.LeftFoot, footPosition);
        }

        ray = new Ray(animator.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up, Vector3.down);
        if (Physics.Raycast(ray, out hit, rayDistance, ikMask.value))
        {
            Vector3 footPosition = hit.point;
            footPosition.y += footDistanceToGround;
            animator.SetIKPosition(AvatarIKGoal.RightFoot, footPosition);          
        }
    }
}
