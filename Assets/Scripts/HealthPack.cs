using UnityEngine;

public class HealthPack : MonoBehaviour, IItem
{
    public float health = 50;

    public void Use(GameObject target)
    {
        var livingEntity = target.GetComponent<LivingEntity>();
        if (null != livingEntity)
        {
            livingEntity.RestoreHealth(health);
        }

        Destroy(gameObject);
    }
}