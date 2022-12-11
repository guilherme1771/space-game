using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityVolume : MonoBehaviour
{
    private CelestialBody _attachedCelestialBody;

    private void Awake()
    {
        _attachedCelestialBody = GetComponentInParent<CelestialBody>();
    }

    public CelestialBody GetAttachedCelestialBody()
    {
        return _attachedCelestialBody ? _attachedCelestialBody : null;
    }
}
