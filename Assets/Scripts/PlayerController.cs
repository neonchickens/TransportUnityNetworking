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

    [NetworkVar]
    public bool forward, left, right, back;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        no = GetComponent<NetworkedObject>();
        c = FindObjectOfType<Client>();
    }

    void FixedUpdate()
    {
        if (forward || back)
        {
            rb.AddForce(transform.forward * speed * (forward ? 1 : -1));
        }
        if (left || right)
        {
            rb.AddTorque(turn * new Vector3(0, (left ? -1 : 1), 0));
        }
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

    public void Jump()
    {
        rb.velocity += jump * new Vector3(0, 1, 0);
        if (no.GetLocal())
        {
            c.SendMessageToServer("j");
        }
    }

}
