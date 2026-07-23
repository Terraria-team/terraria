using Mirror;
using Shared.Components;
using UnityEngine;

public class ProjectileComponent : MonoBehaviour
{
    private static ActionsPrefabsSettings PrefabsSettings => ActionsPrefabsSettings.Instance;
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (NetworkServer.active)
        {
            if (collision.collider.TryGetComponent<HealthComponent>(out var component))
            {
                component.ApplyDamageServerRpc(15);
            }
            
            var explosion = Object.Instantiate(
                PrefabsSettings.swingPrefab,
                transform.position,
                Quaternion.identity
            );
            NetworkServer.Spawn(explosion);
            
            explosion.transform.localScale = Vector3.one * 5;
        }

        Destroy(gameObject);
    }
}
