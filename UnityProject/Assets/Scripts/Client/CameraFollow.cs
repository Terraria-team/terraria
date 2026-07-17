using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    void LateUpdate()
    {
        if (PlayerController.LocalPlayerTransform != null)
        {
            Transform target = PlayerController.LocalPlayerTransform;
            transform.position = new Vector3(target.position.x, target.position.y, transform.position.z);
        }
    }
}
