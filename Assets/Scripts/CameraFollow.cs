using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private GameObject goPlayer;

    public void SetPlayer(GameObject plr)
    {
        goPlayer = plr;
    }

    void Update()
    {
        if (goPlayer != null)
        {
            //Camera sits far behind and a little above the player
            transform.position = (goPlayer.transform.position - (8 * goPlayer.transform.forward) + (2 * goPlayer.transform.up));
            transform.LookAt(goPlayer.transform);
        }
    }
}
