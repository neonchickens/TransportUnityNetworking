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
        if (!no.isLocalPlayer)
        {
            return;
        }

        if (Input.GetAxis("Vertical") != 0)
        {
            if (Input.GetAxis("Vertical") > 0)
            {
                pc.WalkForward();
            }
            else
            {
                //Walk backwards?
            }
        }

        if (Input.GetAxis("Horizontal") != 0)
        {
            if (Input.GetAxis("Horizontal") > 0)
            {
                pc.TurnLeft();
            } else
            {
                pc.TurnRight();
            }
        }
    }
}
