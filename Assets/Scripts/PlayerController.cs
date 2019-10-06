﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //PlayerController controls the physical actions taken by the player
    //The player (or server) sets the boolean action they want to happen
    //The PC will then carry out that action 

    private Rigidbody rb;

    public float speed;
    public float turn;

    //Track these vars across the server
    [NetworkVar]
    public bool forward, left, right, back;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        //Takes actions set by Player/Server
        if (forward || back)
        {
            rb.AddForce(transform.forward * speed * (forward ? 1 : -1));
        }
        if (left || right)
        {
            rb.AddTorque(turn * new Vector3(0, (left ? -1 : 1), 0));
        }
    }

    //Should the object lag behind, these methods will be used to reset it to the current position
    public void SetTransform(Vector3 pos, Vector3 rot)
    {
        transform.SetPositionAndRotation(pos, Quaternion.Euler(rot));
    }
    public void SetRigidbody(Vector3 posVel, Vector3 rotVel)
    {
        rb.velocity = posVel;
        rb.rotation = Quaternion.Euler(rotVel);
    }

}
