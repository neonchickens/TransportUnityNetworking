using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //PlayerController controls the physical actions taken by the player
    //The player (or server) sets the boolean action they want to happen
    //The PC will then carry out that action 

    private Rigidbody rb;
    private Animator anim;

    public float speed;
    public float turn;

    //Track these vars across the server
    [NetworkVar]
    public bool forward, left, right, back;

    [NetworkVar]
    public int ps;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    void FixedUpdate()
    {
        rb.angularVelocity = new Vector3();
        //Takes actions set by Player/Server
        if (forward || back)
        {
            rb.AddForce(transform.forward * speed * (ps == 3 ? .75f : 1) * (ps == 2 ? 1.25f : 1) * (forward ? 1 : -1));
        }
        if (left || right)
        {
            //rb.AddTorque(turn * new Vector3(0, (left ? -1 : 1), 0));
            rb.MoveRotation(Quaternion.Euler(rb.rotation.eulerAngles + turn * new Vector3(0, (left ? -1 : 1), 0)));
        }

        if (ps != anim.GetInteger("PlayerState"))
        {
            anim.SetInteger("PlayerState", ps);
        }
    }

    public enum PlayerState { IDLE, WALK, RUN, SNEAK, DANCE}
}
