using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private PlayerController pc;
    private NetworkedObject no;

    // Start is called before the first frame update
    void Start()
    {
        pc = GetComponent<PlayerController>();
        no = GetComponent<NetworkedObject>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!no.GetLocal())
        {
            return;
        }

        if (Input.GetAxis("Vertical") != 0)
        {
            if (Input.GetAxis("Vertical") > 0)
            {
                pc.forward = true;
                pc.back = false;
            }
            else
            {
                //Walk backwards?
                pc.forward = false;
                pc.back = true;
            }
        } else
        {
            pc.forward = false;
            pc.back = false;
        }

        if (Input.GetAxis("Horizontal") != 0)
        {
            if (Input.GetAxis("Horizontal") > 0)
            {
                pc.left = false;
                pc.right = true;
            } else
            {
                pc.left = true;
                pc.right = false;
            }
        } else
        {
            pc.left = false;
            pc.right = false;
        }
    }
}
