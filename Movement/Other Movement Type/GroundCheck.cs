using System.Collections;
using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    PlayerMovement playerMovement;
    
    [Tooltip("Select the ground layer")]
    public LayerMask groundLayerMask;
    public Vector3 groundCheckSize;
    private bool isCollisionDisabled;

    private void Awake()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    public bool CheckIfGrounded()
    {
        if (isCollisionDisabled) return false;
        Collider[] hitColliders = Physics.OverlapBox(
            transform.position, groundCheckSize, Quaternion.identity, groundLayerMask);
        return hitColliders.Length > 0;
    }

    public void DisableCollisionCheck(float duration)
    {
        StopCoroutine(ReEnableCollision(duration));
        isCollisionDisabled = true;
        StartCoroutine(ReEnableCollision(duration));
    }

    private IEnumerator ReEnableCollision(float duration)
    {
        yield return new WaitForSeconds(duration);
        isCollisionDisabled = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = CheckIfGrounded() ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, groundCheckSize);
    }
}
