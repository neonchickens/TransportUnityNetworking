using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;
    private NetworkedObject no;
    private Client c;

    public float speed;
    public float jump;
    public float turn;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        no = GetComponent<NetworkedObject>();
        c = FindObjectOfType<Client>();
    }

    public void SetTransform(Vector3 pos, Vector3 rot)
    {
        transform.SetPositionAndRotation(pos, Quaternion.Euler(rot));
    }
    public void SetRigidbody(Vector3 posVel, Vector3 rotVel)
    {
        rb.velocity = posVel;
        rb.rotation = Quaternion.Euler(rotVel);
    }

    public void Copy(out Vector3 pos, out Vector3 rot, out Vector3 posVel, out Vector3 rotVel)
    {
        pos = transform.position;
        rot = transform.rotation.eulerAngles;
        posVel = rb.velocity;
        rotVel = rb.rotation.eulerAngles;
    }

    public void Jump()
    {
        rb.velocity += jump * new Vector3(0, 1, 0);
        if (no.isLocalPlayer)
        {
            c.SendMessageToServer("j");
        }
    }

    public void WalkForward()
    {
        rb.AddForce(transform.forward * speed);
        if (no.isLocalPlayer)
        {
            c.SendMessageToServer("w");
        }
    }

    public void TurnLeft()
    {
        rb.AddTorque(turn * new Vector3(0, 1, 0));
        if (no.isLocalPlayer)
        {
            c.SendMessageToServer("l");
        }
    }
    public void TurnRight()
    {
        rb.AddTorque(turn * new Vector3(0, -1, 0));
        if (no.isLocalPlayer)
        {
            c.SendMessageToServer("r");
        }
    }

}
