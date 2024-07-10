using UnityEngine;

public class WeaponDataHolder : MonoBehaviour
{
    public WeaponDataSO weaponData;

    // Variables to hold weapon data
    [HideInInspector] public Sprite weaponIcon;
    [HideInInspector] public string weaponName;
    [HideInInspector] public WeaponType weaponType;
    [HideInInspector] public float damage;
    [HideInInspector] public float fireRate;
    [HideInInspector] public float range;
    [HideInInspector] public float parryTime;

    void Start()
    {
        if (weaponData != null)
        {
            AssignData();
        }
    }

    public void AssignData()
    {
        weaponIcon = weaponData.weaponIcon;
        weaponName = weaponData.weaponName;
        weaponType = weaponData.weaponType;
        damage = weaponData.damage;
        fireRate = weaponData.fireRate;
        range = weaponData.range;
        parryTime = weaponData.parryTime;
    }
}
