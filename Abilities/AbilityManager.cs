using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    public IAbility currentAbility;
    public static AbilityManager Instance {get; private set;}

    private PlayerInputActions playerControls;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        playerControls = new PlayerInputActions();
        playerControls.Player.Ability.performed += _ => TriggerAbility();
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }


    public void EquipAbility(IAbility ability)
    {
        currentAbility = ability;
    }

    private void TriggerAbility()
    {
        if (currentAbility != null)
            currentAbility.UseAbility();
    }
}

