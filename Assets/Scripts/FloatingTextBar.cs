using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingTextBar : MonoBehaviour
{
    //FloatingTextBar is used to easily display names over objects
    //This can be used for players or special NPCs that you want to stand out

    private GameObject camMain;

    void Start()
    {
        camMain = FindObjectOfType<CameraFollow>().gameObject;
    }

    void Update()
    {
        //Make easy to read for camera by facing it
        transform.LookAt(camMain.transform);
    }
}
