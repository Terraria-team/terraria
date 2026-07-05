using UnityEngine;

public class DestroyOnSpriteAnimationEnd : MonoBehaviour
{
    private SpriteAnimator _spriteAnimator;

    void Awake()
    {
        _spriteAnimator = GetComponent<SpriteAnimator>();

        if (_spriteAnimator == null)
        {
            Debug.LogError("DestroyOnSpriteAnimationEnd: No SpriteAnimator component attached");
            return;
        }

        _spriteAnimator.OnFinished += OnFinishedSub;
    }

    void OnFinishedSub()
    {
        Destroy(gameObject);
    }
}
