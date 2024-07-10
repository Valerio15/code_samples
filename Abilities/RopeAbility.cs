using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// DEPENDENCIES:
// Variables
// PlayerMovement

public class RopeAbility : MonoBehaviour, IAbility
{
    // Create a variable where you will save the Variables script for easy access
    private Variables variables;

    // Create a variable where you will save the PlayerMovement script for easy access
    private PlayerMovement playerMovement;

    private bool isGrappling = false;
    private Vector3 grapplingPoint;
    [SerializeField][Range(0, 1)] private float completionPercentage = 0.7f; // 70% by default, can be changed in the inspector

    void OnEnable()
    {
        // Connect the variable to the Variables script
        variables = gameObject.GetComponentInParent<Variables>();

        // Connect the variable to the PlayerMovement script
        playerMovement = gameObject.GetComponentInParent<PlayerMovement>();

    }

    public void UseAbility()
    {
        // Trigger the rope ability
        Ability("rope");
    }

    // Update is called once per frame
    void Update()
    {
        if (isGrappling)
        {
            // Update the starting point of the line to follow lineLocation
            variables.lineRenderer.SetPosition(0, variables.lineLocation.position);
        }
    }

    public void Ability(string ability)
{
    RaycastHit hit;
    if (Physics.Raycast(variables.spawnLocation.position, variables.spawnLocation.forward, out hit, variables.maxRange))
    {
        variables.playerAnimator.Play($"fp_ab_{ability}");
        grapplingPoint = (hit.point - transform.position).normalized;
        isGrappling = true;

        // Initiate the coroutine to wait for the animation progress
        StartCoroutine(WaitForAnimationAndGrappling(ability, hit.point));
    }
}

    private IEnumerator WaitForAnimationAndGrappling(string ability, Vector3 hitPoint)
    {
        yield return StartCoroutine(WaitForAnimationProgress($"fp_ab_{ability}"));

        // Draw the line from lineLocation to the hit point after animation progress is sufficient
        variables.lineRenderer.enabled = true;
        variables.lineRenderer.SetPosition(0, variables.lineLocation.position);
        variables.lineRenderer.SetPosition(1, hitPoint);

        // Start the coroutine for smooth grappling
        StartCoroutine(SmoothGrappling());

        // Start coroutine to disable the line after a delay
        StartCoroutine(DisableLine());
    }

    private IEnumerator WaitForAnimationProgress(string animationName)
    {
        bool animationStarted = false;
        while (!animationStarted)
        {
            AnimatorStateInfo stateInfo = variables.playerAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName(animationName))
            {
                animationStarted = true;
                float requiredTime = stateInfo.length * completionPercentage;
                float elapsedTime = 0f;
                while (elapsedTime < requiredTime)
                {
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                yield return null;
            }
        }
    }

    private IEnumerator SmoothGrappling()
    {
        float startTime = Time.time;
        Vector3 grapplingImpulseVelocity = grapplingPoint * variables.grapplingImpulse;

        while (Time.time < startTime + variables.grapplingDuration)
        {
            variables._controller.Move(grapplingImpulseVelocity * Time.deltaTime);

            // Directly modify playerVelocity to include the grappling impulse
            playerMovement.SetVelocity(grapplingImpulseVelocity - new Vector3(0, variables.gravity * Time.deltaTime, 0));
            yield return null;
        }

        // After grappling, simply let the player's movement be governed by existing movement logic
        isGrappling = false; // Stop the grappling effect
    }


    private IEnumerator DisableLine()
    {
        yield return new WaitForSeconds(0.5f);
        variables.lineRenderer.enabled = false;
    }

}
