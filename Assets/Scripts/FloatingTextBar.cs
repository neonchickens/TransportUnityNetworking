using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingTextBar : MonoBehaviour
{
    private GameObject camMain;

    void Start()
    {
        camMain = FindObjectOfType<CameraFollow>().gameObject;
    }

    void Update()
    {
        //Make easy to read for camera
        transform.LookAt(camMain.transform);
    }
}
