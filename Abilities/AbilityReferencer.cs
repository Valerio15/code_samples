using System.Collections.Generic;
using UnityEngine;

public class AbilityReferencer : MonoBehaviour
{
    public static AbilityReferencer Instance { get; private set; }
    public List<IAbility> abilities;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        abilities = new List<IAbility>(GetComponents<IAbility>());
    }
}
