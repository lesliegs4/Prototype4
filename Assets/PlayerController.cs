using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _acceleration = 35f;
    [SerializeField] private float _deceleration = 55f;
    [SerializeField] private BoxCollider2D _boundaries;
    [SerializeField] private Transform _fruitThrowTransform;

    private Bounds _bounds;

    private float _leftBound;
    private float _rightBound;

    private float _startingLeftBound;
    private float _startingRightBound;

    private float _offset;

    private InputAction _moveAction;
    private float _velocityX;

    private void Awake()
    {
        _bounds = _boundaries.bounds;

        _offset = transform.position.x - _fruitThrowTransform.position.x;

        _leftBound = _bounds.min.x + _offset;
        _rightBound = _bounds.max.x + _offset;

        _startingLeftBound = _leftBound;
        _startingRightBound = _rightBound;
    }

    private void Start()
    {
        if (UserInput.PlayerInput != null && UserInput.PlayerInput.actions != null)
        {
            _moveAction = UserInput.PlayerInput.actions["Move"];
        }
    }

    private void Update()
    {
        HandleGlobalHotkeys();

        float moveX = UserInput.MoveInput.x;
        if (_moveAction != null)
        {
            moveX = _moveAction.ReadValue<Vector2>().x;
        }

        float targetVel = moveX * _moveSpeed;
        float rate = Mathf.Abs(targetVel) > 0.0001f ? _acceleration : _deceleration;
        _velocityX = Mathf.MoveTowards(_velocityX, targetVel, Mathf.Max(0f, rate) * Time.deltaTime);

        Vector3 newPosition = transform.position + new Vector3(_velocityX * Time.deltaTime, 0f, 0f);
        newPosition.x = Mathf.Clamp(newPosition.x, _leftBound, _rightBound);

        transform.position = newPosition;
    }

    private void HandleGlobalHotkeys()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            QuitGame();
            return;
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetGame();
        }
    }

    private void ResetGame()
    {
        Scene current = SceneManager.GetActiveScene();
        if (current.IsValid())
        {
            SceneManager.LoadScene(current.buildIndex);
        }
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ChangeBoundary(float extraWidth)
    {
        _leftBound = _startingLeftBound;
        _rightBound = _startingRightBound;

        _leftBound += ThrowFruitController.instance.Bounds.extents.x + extraWidth;
        _rightBound -= ThrowFruitController.instance.Bounds.extents.x + extraWidth;
    }
}
