using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    public float rotationTransitionAngleThreshold;
    public float rotationTransitionTurnFactor;
    public enum RotationState{Free, Locked, Transitioning}
    public RotationState rotationState;
    
    [SerializeField] private CelestialBody initialSurfaceBody;
    [SerializeField] private Transform groundCheckTransform;
    [SerializeField] private float groundCheckMaxDistance;
    [SerializeField] private float groundCheckSphereRadius;
    //[SerializeField] private Vector3 groundCheckCubeSize;
    [SerializeField] private LayerMask walkableLayer;
    [SerializeField] private LayerMask collidableLayer;
    [SerializeField] private PhysicMaterial airborneMaterial;
    [SerializeField] private PhysicMaterial walkingMaterial;
    [SerializeField] private PhysicMaterial standingMaterial;
    [SerializeField] private float cameraMovementSpeed;
    [SerializeField] private float walkingSpeed;
    [SerializeField] private float runningSpeed;
    [SerializeField] private float jumpForce;
    
    private Rigidbody _rigidbody;
    private Collider _collider;
    private CelestialBody _surfaceBody;
    private CelestialBody _gravityVolumeBody;
    private Camera _camera;
    private Vector3 _surfaceVelocity;
    private float _yawInput;
    private float _pitchInput;
    private RaycastHit _groundCheckRaycastHit;
    private Vector2 _movementInput;
    private float _movementDeadzone = 0.1f;
    private float _movementSpeed;
    private bool _isMoving;
    private bool _wasMoving;
    private bool _isGrounded;
    private bool _wasGrounded;
    private float _pitch;
    private bool _jumpFlag;
    private bool _jumpOnNextFixedUpdate;
    private Vector3 _contactPoint;
    private Vector3 _surfaceNormal;

    private void Awake()
    {
        _camera = Camera.main;
        _camera.transform.localEulerAngles = new Vector3(0, 0, 0);
        
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        _collider.material = airborneMaterial;

        _movementSpeed = walkingSpeed;
        
        _wasGrounded = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        _jumpFlag = false;
    }

    private void Update()
    {
        _movementInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        _yawInput = Input.GetAxisRaw("Mouse X") * cameraMovementSpeed * Time.deltaTime;
        _pitchInput = -Input.GetAxisRaw("Mouse Y") * cameraMovementSpeed * Time.deltaTime;

        if(_gravityVolumeBody)
            _pitch = Mathf.Clamp(_pitch + _pitchInput, -80f, 80f);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _isGrounded = false;
            _surfaceBody = null;
            _jumpOnNextFixedUpdate = true;
        }

        _isMoving = _movementInput.sqrMagnitude > _movementDeadzone * _movementDeadzone;
        _surfaceVelocity = _isMoving ? _surfaceVelocity : Vector3.zero;
    }

    private void FixedUpdate()
    {
        CheckGrounded();
        
        _rigidbody.MoveRotation(Quaternion.Euler(GetUpDirection() * GetYawInput()) * _rigidbody.rotation);

        if (_gravityVolumeBody)
        {
            _gravityVolumeBody.CalculatePlayerMovement();
            if (rotationState == RotationState.Transitioning)
            {
                if (Vector3.Angle(_gravityVolumeBody.GetPlayerUpDirection(), GetUpDirection()) <= rotationTransitionAngleThreshold)
                {
                    rotationState = RotationState.Locked;
                }
            }

            if (_isGrounded)
            {
                UpdateMovement();
                if (_jumpOnNextFixedUpdate)
                {
                    _rigidbody.AddForce(GetUpDirection() * jumpForce, ForceMode.Impulse);
                    _rigidbody.constraints = RigidbodyConstraints.None;
                    _rigidbody.angularVelocity = _gravityVolumeBody.GetRigidbody().angularVelocity;
                    _isGrounded = false;
                    _surfaceBody = null;

                    _jumpFlag = true;
                    _jumpOnNextFixedUpdate = false;
                }
            }
        }

        _movementSpeed = Input.GetKey(KeyCode.LeftShift) ? runningSpeed : walkingSpeed;

        if (_gravityVolumeBody)
        {
            _camera.transform.localRotation = Quaternion.Euler(new Vector3(_pitch, 0f, 0f));
        }
        else
        {
            _rigidbody.MoveRotation(Quaternion.Euler(GetRightDirection() * _pitchInput) * _rigidbody.rotation);
        }

        _wasGrounded = _isGrounded;
        _wasMoving = _isMoving;
    }

    private void UpdateMovement()
    {
        if (_isMoving)
        {
            if (!_wasMoving)
            {
                OnStartMoving();
            }
            
            OnStayMoving();
        }
        else
        {
            if (_wasMoving)
            {
                OnStopMoving();
            }
        }
    }

    private void OnStartMoving()
    {
        _collider.material = walkingMaterial;
    }

    private void OnStayMoving()
    {
        _surfaceVelocity = Vector3.ClampMagnitude(new Vector3(_movementInput.x, 0f, _movementInput.y), 1f) * _movementSpeed;
    }

    private void OnStopMoving()
    {
        _collider.material = standingMaterial;
        _surfaceVelocity = Vector3.zero;
    }

    private void CheckGrounded()
    {
        Vector3 relativeVelocity = Vector3.zero;

        //bool flag = Physics.BoxCast(groundCheckTransform.position, groundCheckCubeSize, -GetUpDirection(), out _groundCheckRaycastHit, _rigidbody.rotation, groundCheckMaxDistance, walkableLayer);
        bool flag = Physics.SphereCast(groundCheckTransform.position, groundCheckSphereRadius, -GetUpDirection(),
            out _groundCheckRaycastHit, groundCheckMaxDistance, walkableLayer);

        if (_gravityVolumeBody)
        {
            relativeVelocity = _rigidbody.velocity - _gravityVolumeBody.GetRigidbody().velocity -
                               Vector3.Cross(_gravityVolumeBody.GetRigidbody().angularVelocity,
                                   _rigidbody.position - _gravityVolumeBody.GetRigidbody().position) -
                               _gravityVolumeBody.GetModifiedSurfaceVelocity();

            /*if (relativeVelocity.y < 0)
            {
                if (_rigidbody.SweepTest(-GetUpDirection(), out var hit, relativeVelocity.y * Time.fixedDeltaTime))
                {
                    _rigidbody.MovePosition(-GetUpDirection() * hit.distance);
                }
            }*/
        }

        if (flag &&  relativeVelocity.y < jumpForce * .5f && !_jumpFlag)
        {
            _isGrounded = true;
            _contactPoint = _groundCheckRaycastHit.point;
            _surfaceNormal = _groundCheckRaycastHit.normal;

            if (!_wasGrounded)
            {
                OnGetGrounded();
            }

            OnStayGrounded();
        }
        else
        {
            _isGrounded = false;
            _jumpFlag = false;

            if (_wasGrounded)
            {
                OnGetUngrounded();
            }
        }
    }

    public RaycastHit GetGroundedRaycastHit()
    {
        return _groundCheckRaycastHit;
    }

    public Vector3 GetContactPoint()
    {
        return _contactPoint;
    }

    private void OnGetGrounded()
    {
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        _surfaceBody = _gravityVolumeBody;
        _collider.material = standingMaterial;

        Debug.Log("grounded");
    }

    private void OnStayGrounded()
    {
        
    }

    private void OnGetUngrounded()
    {
        _surfaceBody = null;
        _collider.material = airborneMaterial;
        
        Debug.Log("ungrounded");
    }

    public CelestialBody GetSurfaceBody()
    {
        return _surfaceBody;
    }
    
    public CelestialBody GetGravityVolumeBody()
    {
        return _gravityVolumeBody;
    }

    public Vector3 GetSurfaceVelocity()
    {
        return _surfaceVelocity;
    }

    public Vector3 GetSurfaceNormal()
    {
        return _surfaceNormal;
    }
    
    public Rigidbody GetRigidbody()
    {
        return _rigidbody ? _rigidbody : null;
    }

    public float GetYawInput()
    {
        return _yawInput;
    }

    public Vector3 GetUpDirection()
    {
        return _rigidbody.rotation * Vector3.up;
    }
    
    public Vector3 GetForwardDirection()
    {
        return _rigidbody.rotation * Vector3.forward;
    }
    
    public Vector3 GetRightDirection()
    {
        return _rigidbody.rotation * Vector3.right;
    }

    public bool GetGrounded()
    {
        return _isGrounded;
    }

    public LayerMask GetWalkableLayerMask()
    {
        return walkableLayer;
    }

    public LayerMask GetCollidableLayerMask()
    {
        return collidableLayer;
    }

    private void SetGravityVolume(CelestialBody body)
    {
        if(body != null){
            _gravityVolumeBody = body;
            rotationState = RotationState.Transitioning;
        }else
        {
            _jumpFlag = false;
            _gravityVolumeBody = null;
            _surfaceBody = null;
            _isGrounded = false;
            _rigidbody.rotation = Quaternion.FromToRotation(transform.forward, _camera.transform.forward) * _rigidbody.rotation;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            _camera.transform.localEulerAngles = Vector3.zero;
            _pitch = 0;
            rotationState = RotationState.Free;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        GravityVolume gravityVolume = other.GetComponent<GravityVolume>();

        if (gravityVolume)
        {
            if (gravityVolume.GetAttachedCelestialBody() != _gravityVolumeBody)
            {
                SetGravityVolume(gravityVolume.GetAttachedCelestialBody());
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        GravityVolume gravityVolume = other.GetComponent<GravityVolume>();

        if (gravityVolume)
        {
            if (gravityVolume.GetAttachedCelestialBody() == _gravityVolumeBody)
            {
                SetGravityVolume(null);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        
        Gizmos.DrawSphere(transform.position - transform.up * groundCheckMaxDistance, groundCheckSphereRadius);
    }
}

