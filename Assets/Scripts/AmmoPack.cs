using UnityEngine;

public class AmmoPack : MonoBehaviour, IItem
{
    public int ammo = 30;

    public void Use(GameObject target)
    {
        var playerShooter = target.GetComponent<PlayerShooter>();
        if (null != playerShooter && null != playerShooter.gun)
        {
            playerShooter.gun.ammoRemain += ammo;
        }

        Destroy(gameObject);
    }
}