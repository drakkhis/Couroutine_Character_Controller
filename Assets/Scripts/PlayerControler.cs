using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerControler : MonoBehaviour, PlayerInputActions.IPlayerActions
{
    PlayerInputActions _playerControls;
    private Vector2 _playerInput;
    [SerializeField, Range(0f, 100f)]
    private float _maxSpeed = 10f;
    [SerializeField, Range(0f, 100f)]
    private float _maxAcceleration = 20f, _maxAirAcceleration = 5f;
    private Vector3 _velocity;
    private Rigidbody _body;
    [SerializeField, Range(0f, 10f)]
    private float _jumpHeight = 2f;
    private bool _onGround = true;
    [SerializeField, Range(0, 5)]
    private int _maxAirJumps = 0;
    private int _jumpPhase;
    [SerializeField, Range(0f, 90f)]
    private float _maxGroundAngle = 25f;
    private Coroutine _inst = null;
    private float _minGroundDotProduct;
    private Vector3 _contactNormal;

    void OnValidate()
    {
        _minGroundDotProduct = math.cos(_maxGroundAngle * Mathf.Deg2Rad);
    }
    private void Awake()
    {
        _playerControls = new PlayerInputActions();
        _playerControls.Player.SetCallbacks(this);
        OnValidate();
        _body = GetComponent<Rigidbody>();
        _inst = StartCoroutine(MoveRoutine(_playerInput));
    }
    private void OnEnable()
    {
        _playerControls.Player.Enable();
    }
    private void OnDisable()
    {
        StopCoroutine(_inst);
        _playerControls.Player.Disable();
    }

    void OnCollisionEnter(Collision collision)
    {
        EvaluateCollision(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    void EvaluateCollision(Collision collision) 
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (normal.y >= _minGroundDotProduct)
            {
                _onGround = true;
                _contactNormal = normal;
            }
            if (_onGround)
            {
                _jumpPhase = 0;
            }
            else
            {
                _contactNormal = Vector3.up;
            }
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        _playerInput = context.ReadValue<Vector2>();
        _playerInput = Vector2.ClampMagnitude(_playerInput, 1f);
        StopCoroutine(_inst);
        _inst = StartCoroutine(MoveRoutine(_playerInput));
    }

    private IEnumerator MoveRoutine(Vector2 playerInput)
    {
        bool init = true;
        while (_velocity != Vector3.zero || init == true)
        {
            init = false;
            Vector3 desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * _maxSpeed;
            _velocity = _body.velocity;
            float acceleration = _onGround ? _maxAcceleration : _maxAirAcceleration;
            float maxSpeedChange = acceleration * Time.deltaTime;
            _velocity.x = Mathf.MoveTowards(_velocity.x, desiredVelocity.x, maxSpeedChange);
            _velocity.z = Mathf.MoveTowards(_velocity.z, desiredVelocity.z, maxSpeedChange);
            _body.velocity = _velocity;
            _onGround = false;
            yield return new WaitForEndOfFrame();
        }

    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (_onGround || _jumpPhase < _maxAirJumps)
            {
                _jumpPhase += 1;
                float jumpSpeed = math.sqrt(-2f * Physics.gravity.y * _jumpHeight);
                float alignedSpeed = Vector3.Dot(_velocity, _contactNormal);
                if (alignedSpeed > 0f)
                {
                    jumpSpeed = math.max(jumpSpeed - alignedSpeed, 0f);
                }
                _velocity += _contactNormal * jumpSpeed;
                _body.velocity = _velocity;
                StopCoroutine(_inst);
                _inst = StartCoroutine(MoveRoutine(_playerInput));
            }
        }
    }
}
