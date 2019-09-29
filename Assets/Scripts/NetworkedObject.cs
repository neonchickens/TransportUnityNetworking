using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkedObject : MonoBehaviour
{
    public bool isLocalPlayer = false;
    public bool updateTransform = true;
    public bool updateRigidbody = true;

    private Rigidbody rb;

    private Client client;

    void Start()
    {
        client = FindObjectOfType<Client>();

        rb = GetComponent<Rigidbody>();

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            if (updateTransform)
            {
                Vector3 pos = gameObject.transform.position;
                Vector3 rot = gameObject.transform.rotation.eulerAngles;
                client.SendMessageToServer(csvRecord(',', "utransform", pos.x.ToString("F0"), pos.y.ToString("F0"), pos.z.ToString("F0"),
                    rot.x.ToString("F0"), rot.y.ToString("F0"), rot.z.ToString("F0")));
            }
        }
    }

    public static Vector3 ArrToV3(string[] arr, int start)
    {
        Vector3 v3 = new Vector3();
        v3.x = int.Parse(arr[start++]);
        v3.y = int.Parse(arr[start++]);
        v3.z = int.Parse(arr[start++]);
        return v3;
    }

    public static byte[] encode(string str)
    {
        return System.Text.Encoding.UTF8.GetBytes(str);
    }
    public static string decode(byte[] str, int size)
    {
        string data = System.Text.Encoding.UTF8.GetString(str, 0, size);
        Debug.Log("Decoding [" + data + "]");
        return data;
    }
    public static string csvRecord(char sep, params string[] vals)
    {
        if (vals.Length == 0)
        {
            return null;
        }
        else if (vals.Length == 1)
        {
            return vals[0];
        }
        else
        {
            string str = vals[0];
            for (int i = 1; i < vals.Length; i++)
            {
                str += ',' + vals[i];
            }
            return str;
        }

    }
}
