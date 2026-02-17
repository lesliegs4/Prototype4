using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitInfo : MonoBehaviour {
    public int FruitIndex = 0;
    public int PointsWhenAnnihilated = 1;
    public float FruitMass = 1f;

    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        _rb.mass = FruitMass;
    }
}
