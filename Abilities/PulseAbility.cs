using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// DEPENDENCIES:
// Variables
// PlayerMovement

public class PulseAbility : MonoBehaviour
{
    // Create a variable where you will save the Variables script for easy access
    private Variables variables;
    private bool isEnemyInRange;

    // Create a variable where you will save the PlayerMovement script for easy access
    private PlayerMovement playerMovement;

    private PlayerInputActions playerControls;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void Awake()
    {
        playerControls = new PlayerInputActions();
    }

    void OnEnable()
    {
        // Connect the variable to the Variables script
        variables = gameObject.GetComponent<Variables>();
        // Connect the variable to the PlayerMovement script
        playerMovement = gameObject.GetComponent<PlayerMovement>();

        playerControls.Enable();
        playerControls.Player.AltFire.performed += _ => AltFire();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void AltFire()
    {
        variables.playerAnimator.Play("fp_shoot_double");
        StartCoroutine(CallPulseAtAnimationProgress());
    }


    private IEnumerator CallPulseAtAnimationProgress()
    {

        // Assuming the name of the animation is "fps_atk_mix" and it's in the "Base Layer"
        AnimatorStateInfo stateInfo = variables.playerAnimator.GetCurrentAnimatorStateInfo(0);
        float animationLength = stateInfo.length;

        // Wait for 70% of the animation's duration
        yield return new WaitForSeconds(animationLength * variables.pulseEffectSpawnTime);
        GameObject pulseEffect = Instantiate(variables.pulsePrefab, variables.spawnLocation.position, variables.spawnLocation.rotation);
        Destroy(pulseEffect, .5f);
        // Call the Pulse method
        Pulse(pulseEffect.transform);
    }

    private void Pulse(Transform pulseEffectTransform)
    {
        Vector3 repulsionDirection = -pulseEffectTransform.forward;

        // Apply the repulsion force to the player
        playerMovement.AddVelocity(repulsionDirection * variables.pulseRepulsionForce);
        // isEnemyInRange = false; //TODO: check if this needed
        // Detect and affect nearby enemies
        Collider[] hitColliders = Physics.OverlapSphere(variables.spawnLocation.position + variables.pulseOffset, variables.pulseRadius, LayerMask.GetMask("Enemy"));
        foreach (var hitCollider in hitColliders)
        {
            if(hitCollider.gameObject.GetComponent<EnemyHealth>() != null)
            {
                // isEnemyInRange = true; //TODO: check if this needed
                StartCoroutine(RepulseEnemy(hitCollider.gameObject));
            }
        }
    }

    private IEnumerator RepulseEnemy(GameObject enemy)
    {        
        float startTime = Time.time;

        Vector3 originalPosition = enemy.transform.position;
        Vector3 repulsionDirection = (enemy.transform.position - transform.position).normalized;
        Vector3 repulsionTarget = originalPosition + repulsionDirection * variables.pulseRepulsionDistance;

        enemy.GetComponent<EnemyHealth>().TakeDamage();

        while (enemy != null && Time.time < startTime + variables.pulseDuration)
        {
            enemy.transform.position = Vector3.Lerp(originalPosition, repulsionTarget, (Time.time - startTime) / variables.pulseDuration);
            yield return null;
        }
    }
}
