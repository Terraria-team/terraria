using System;
using UnityEngine;

public class SpriteAnimator : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    [SerializeField] private float fps = 12f;

    private Sprite[] _frames;
    private float _startTime;
    private float _timer;
    private int _currentFrame;
    
    public event Action OnFinished;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (_spriteRenderer == null)
            Debug.LogError("Sprite renderer is null on SpriteAnimator!");
    }

    public void Play(Sprite[] sprites)
    {
        _frames = sprites;
        _timer = 0f;
        _currentFrame = 0;
        _spriteRenderer.sprite = _frames[0];
    }

    void Update()
    {
        if (_frames == null)
            return;

        _timer += Time.deltaTime;

        while (_timer >= 1f / fps)
        {
            _timer -= 1f / fps;

            var lastFrame = _currentFrame;
            
            _currentFrame = (_currentFrame + 1) % _frames.Length;
            
            if (lastFrame == _frames.Length - 1 && _currentFrame == 0)
                OnFinished?.Invoke();
            
            _spriteRenderer.sprite = _frames[_currentFrame];
        }
    }
}