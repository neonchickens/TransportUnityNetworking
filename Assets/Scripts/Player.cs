﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    //Player is the local player controls being recieved

    private PlayerController pc;
    private NetworkedObject no;

    void Start()
    {
        pc = GetComponent<PlayerController>();
        no = GetComponent<NetworkedObject>();
    }

    void FixedUpdate()
    {
        if (!no.GetLocal())
        {
            return;
        }
        
        //We are manipulating variables controlling the playercontroller
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

        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.W))
        {
            pc.ps = (int)(PlayerController.PlayerState.RUN);
        }
        else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.W))
        {
            pc.ps = (int)(PlayerController.PlayerState.SNEAK);
        }
        else if (Input.GetKey(KeyCode.W))
        {
            pc.ps = (int)(PlayerController.PlayerState.WALK);
        }
        else if(Input.GetKey(KeyCode.G))
        {
            pc.ps = (int)(PlayerController.PlayerState.DANCE);
        } else
        {
            pc.ps = (int)(PlayerController.PlayerState.IDLE);
        }
    }
}
