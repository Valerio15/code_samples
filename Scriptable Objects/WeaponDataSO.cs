using UnityEngine;

public enum WeaponType {
    one_hand,
    two_hand,
    fists,
    spear,
    magic_crystal
}
public enum WeaponClass {
    blunt,
    cut,
    pierce,
    magic
}

[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapon/Weapon Data", order = 0)]
public class WeaponDataSO : ScriptableObject
{

    [Header("WEAPON TYPE")]
    public Sprite weaponIcon;

    [Header("WEAPON TYPE")]
    public WeaponType weaponType;

    [Header("WEAPON TYPE")]
    public WeaponClass weaponClass;

    [Header("WEAPON NAME")]
    public string weaponName;

    [Header("DAMAGE")]
    public float damage;

    [Header("FIRE RATE")]
    public float fireRate;

    [Header("RANGE")]
    public float range;

    [Header("PARRY TIME")]
    public float parryTime;
}
