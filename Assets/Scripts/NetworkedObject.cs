using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;

public class NetworkedObject : MonoBehaviour
{
    private int id;
    private string prefab;
    private Dictionary<string, string> dicNetVars;

    private bool isLocalPlayer = false;
    public bool updateTransform = true;
    public bool updateRigidbody = true;

    private static int idGenLocal = 1;

    private Rigidbody rb;

    private Client client;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        GetAllNetVars();

        client = FindObjectOfType<Client>();
        if (client != null && isLocalPlayer)
        {
            SetLocal(true);
        }
    }

    public void SetLocal(bool local)
    {
        isLocalPlayer = local;
        if (local)
        {
            id = -idGenLocal++;
            if (client == null)
            {
                client = FindObjectOfType<Client>();
            }
            client.RegNetObj(id, this);
            client.SendMessageToServer(csvRecord(',', "register", id.ToString()));
        }
    }
    public bool GetLocal()
    {
        return isLocalPlayer;
    }

    public void SetNetworkId(int networkId)
    {
        id = networkId;
        if (isLocalPlayer)
        {
            client.SendMessageToServer(csvRecord(',', "spawn", id.ToString(), prefab));
        }
    }
    public void SetPrefab(string prefab)
    {
        this.prefab = prefab;
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
                client.SendMessageToServer(csvRecord(',', "utransform", id.ToString(), pos.x.ToString("F1"), pos.y.ToString("F1"), pos.z.ToString("F1"),
                    rot.x.ToString("F1"), rot.y.ToString("F1"), rot.z.ToString("F1")));
            }
            CheckAllNetVars();
        }
    }

    private void GetAllNetVars()
    {
        if (dicNetVars == null)
        {
            dicNetVars = new Dictionary<string, string>();
        }


        int count1 = 0, count2 = 0, count3 = 0, count4 = 0;
        MonoBehaviour[] mbs = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour mb in mbs)
        {
            count1++;
            FieldInfo[] pis = mb.GetType().GetFields();
            foreach (FieldInfo pi in pis)
            {
                count2++;
                object[] os = pi.GetCustomAttributes(true);
                foreach (object o in os)
                {
                    count3++;
                    NetworkVar nv = o as NetworkVar;
                    if (nv != null)
                    {
                        count4++;
                        //Debug.Log("Network Var, " + pi.Name + ", " + pi.ReflectedType + ", " + pi.GetValue(mb));
                        string key = pi.ReflectedType + "," + pi.Name;
                        if (!dicNetVars.ContainsKey(key))
                        {
                            dicNetVars.Add(key, pi.GetValue(mb).ToString());
                        }
                    }

                }

            }
        }
        //Debug.Log("Net " + count1 + "," + count2 + "," + count3 + "," + count4);
    }

    private void CheckAllNetVars()
    {
        if (dicNetVars == null)
        {
            dicNetVars = new Dictionary<string, string>();
        }

        int count1 = 0, count2 = 0, count3 = 0, count4 = 0;
        MonoBehaviour[] mbs = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour mb in mbs)
        {
            count1++;
            FieldInfo[] pis = mb.GetType().GetFields();
            foreach (FieldInfo pi in pis)
            {
                count2++;
                object[] os = pi.GetCustomAttributes(true);
                foreach (object o in os)
                {
                    count3++;
                    NetworkVar nv = o as NetworkVar;
                    if (nv != null)
                    {
                        count4++;
                        //Debug.Log("Network Var, " + pi.Name + ", " + pi.ReflectedType + ", " + pi.GetValue(mb));
                        string key = pi.ReflectedType + "," + pi.Name;
                        if (!dicNetVars[key].ToString().Equals(pi.GetValue(mb).ToString()))
                        {
                            dicNetVars[key] = pi.GetValue(mb).ToString();
                            client.SendMessageToServer(csvRecord(',', "change", id.ToString(), key, dicNetVars[key]));
                            Debug.Log(pi.GetValue(mb).ToString());
                        }
                    }

                }

            }
        }
        //Debug.Log("Net " + count1 + "," + count2 + "," + count3 + "," + count4);
    }

    public void UpdateNetVar(string key, string value)
    {
        int count1 = 0, count2 = 0, count3 = 0, count4 = 0;
        MonoBehaviour[] mbs = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour mb in mbs)
        {
            count1++;
            FieldInfo[] pis = mb.GetType().GetFields();
            foreach (FieldInfo pi in pis)
            {
                count2++;
                object[] os = pi.GetCustomAttributes(true);
                foreach (object o in os)
                {
                    count3++;
                    NetworkVar nv = o as NetworkVar;
                    if (nv != null)
                    {
                        count4++;
                        //Debug.Log("Network Var, " + pi.Name + ", " + pi.ReflectedType + ", " + pi.GetValue(mb));
                        if (key.Equals(pi.ReflectedType + "," + pi.Name))
                        {
                            Type t = pi.FieldType;
                            pi.SetValue(mb, Convert.ChangeType(value, t));
                        }
                    }

                }

            }
        }
    }

    public static Vector3 ArrToV3(string[] arr, int start)
    {
        Vector3 v3 = new Vector3();
        v3.x = float.Parse(arr[start++]);
        v3.y = float.Parse(arr[start++]);
        v3.z = float.Parse(arr[start++]);
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
