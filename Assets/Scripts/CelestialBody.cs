using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CelestialBody : MonoBehaviour
{
    public static List<CelestialBody> Bodies;
    public static float GravityConstant = 0.1f;
    
    private Rigidbody _rigidbody;

    public float mass;
    private Vector3 _angularVelocity;
    [SerializeField] private Vector3 initialAngularVelocity;

    [SerializeField] private PlayerController playerController;

    private bool _playerOnSurfaceFlag = false;

    private Vector3 _modifiedPlayerSurfaceVelocity;

    private Vector3 _playerDirection;


    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();

        _angularVelocity = initialAngularVelocity;
    }

    private void FixedUpdate()
    {
        _rigidbody.MoveRotation(Quaternion.Euler(_rigidbody.rotation.eulerAngles + _angularVelocity * Time.fixedDeltaTime));
        
        _playerOnSurfaceFlag = playerController.GetGrounded();
        
        CalculatePlayerGravity();

        if (playerController.GetGravityVolumeBody() == this)
        {
            Quaternion newPlayerRotation = playerController.GetRigidbody().rotation;
            Vector3 playerDirection = (playerController.GetRigidbody().position - _rigidbody.position + playerController.GetRigidbody().rotation * playerController.GetSurfaceVelocity() * Time.fixedDeltaTime).normalized;
            _playerDirection = playerDirection;
            if (playerController.GetSurfaceBody() == this)
            {
                Vector3 relativePosition = playerController.GetRigidbody().position - _rigidbody.position;
                newPlayerRotation = Quaternion.Euler(_angularVelocity * Time.fixedDeltaTime) * newPlayerRotation;

                RaycastHit hit;

                Vector3 playerSurfaceVelocity;

                playerSurfaceVelocity =
                    Quaternion.LookRotation(playerController.GetForwardDirection(),
                        playerController.GetSurfaceNormal()) * playerController.GetSurfaceVelocity();

                _modifiedPlayerSurfaceVelocity = playerSurfaceVelocity;

                Vector3 newPlayerPosition =
                    RotatePointAround(
                        playerController.GetRigidbody().position, _rigidbody.position,
                        _angularVelocity);

                playerController.GetRigidbody().velocity = (newPlayerPosition - playerController.GetRigidbody().position) / Time.fixedDeltaTime + playerSurfaceVelocity;
            }

            switch (playerController.rotationState)
            {
                case PlayerController.RotationState.Free:
                    
                    break;
                
                case PlayerController.RotationState.Locked:
                    newPlayerRotation = Quaternion.FromToRotation(newPlayerRotation * Vector3.up, playerDirection) * newPlayerRotation;
                    break;
                
                case PlayerController.RotationState.Transitioning:
                    newPlayerRotation = Quaternion.Slerp(newPlayerRotation,
                        Quaternion.FromToRotation(newPlayerRotation * Vector3.up, playerDirection) * newPlayerRotation,
                        Time.fixedDeltaTime * playerController.rotationTransitionTurnFactor);
                    break;
            }
            playerController.GetRigidbody().MoveRotation(newPlayerRotation);
        }
    }

    private void CalculatePlayerGravity()
    {
        Rigidbody playerRigidbody = playerController.GetRigidbody();
        Vector3 positionDifference = _rigidbody.position - playerRigidbody.position;
        float sqrDistance = positionDifference.magnitude;

        if (sqrDistance == 0)
            return;

        Vector3 direction = positionDifference.normalized;
        float magnitude = mass / sqrDistance;
        if(!playerRigidbody.SweepTest(direction, out var hitInfo, magnitude * Time.fixedDeltaTime))
            playerRigidbody.AddForce(direction * magnitude, ForceMode.Acceleration);
    }

    public Vector3 RotatePointAround(Vector3 point, Vector3 center, Vector3 angularVelocity)
    {
        return Quaternion.Euler(angularVelocity * Time.fixedDeltaTime) * (point - center) + center;
    }

    public Vector3 GetPlayerUpDirection()
    {
        return _playerDirection;
    }

    public Rigidbody GetRigidbody()
    {
        return _rigidbody ? _rigidbody : null;
    }

    private void OnEnable()
    {
        Bodies ??= new List<CelestialBody>();
        Bodies.Add(this);
    }

    private void OnDisable()
    {
        Bodies.Remove(this);
    }

    public Vector3 GetModifiedSurfaceVelocity()
    {
        return _modifiedPlayerSurfaceVelocity;
    }
}
