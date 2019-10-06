using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;

public class NetworkedObject : MonoBehaviour
{
    //A NetworkedObject keeps track of our corresponding object. It records all NetworkVar
    //tagged variables and translates them, as well as sending updates of itself to the server.

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
        GetAllNetVars();

        rb = GetComponent<Rigidbody>();
        client = FindObjectOfType<Client>();
    }

    //Setup metadata. Start server tracking if local
    public void Setup(string prefab, bool local)
    {
        this.prefab = prefab;
        isLocalPlayer = local;
        if (local)
        {
            //Generate temporary negative client-side id
            id = -idGenLocal++;
            if (client == null)
            {
                client = FindObjectOfType<Client>();
            }
            client.RegNetObj(id, this);

            //Ask server with a new global id
            client.SendMessageToServer(csvRecord(',', "register", id.ToString()));
        }
    }

    public bool GetLocal()
    {
        return isLocalPlayer;
    }

    //Server will use this to set a global id
    public void SetNetworkId(int networkId)
    {
        id = networkId;
        if (isLocalPlayer)
        {
            client.SendMessageToServer(csvRecord(',', "spawn", id.ToString(), prefab));
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            if (updateTransform)
            {
                //Vector3 pos = gameObject.transform.position;
                //Vector3 rot = gameObject.transform.rotation.eulerAngles;
                //client.SendMessageToServer(csvRecord(',', "utransform", id.ToString(), pos.x.ToString("F1"), pos.y.ToString("F1"), pos.z.ToString("F1"),
                //    rot.x.ToString("F1"), rot.y.ToString("F1"), rot.z.ToString("F1")));
            }

            //Check for any updates to NetworkVars
            CheckAllNetVars();
        }
    }

    //Gathers all NetworkVars attached to the current gameObject
    //Needs to be called again to be updated
    private void GetAllNetVars()
    {
        if (dicNetVars == null)
        {
            dicNetVars = new Dictionary<string, string>();
        }


        MonoBehaviour[] mbs = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour mb in mbs)
        {
            FieldInfo[] pis = mb.GetType().GetFields();
            foreach (FieldInfo pi in pis)
            {
                object[] os = pi.GetCustomAttributes(true);
                foreach (object o in os)
                {
                    NetworkVar nv = o as NetworkVar;
                    if (nv != null)
                    {
                        //Key is comprised of component type + var name
                        string key = pi.ReflectedType + "," + pi.Name;
                        if (!dicNetVars.ContainsKey(key))
                        {
                            //The value is the string object
                            dicNetVars.Add(key, pi.GetValue(mb).ToString());

                            //TODO hold pointers instead?
                        }
                    }
                }
            }
        }
    }

    //Checks for update on any vars found in GetAllNetVars()
    //Sends an update to the server should an update be found
    private void CheckAllNetVars()
    {
        if (dicNetVars == null)
        {
            dicNetVars = new Dictionary<string, string>();
        }

        MonoBehaviour[] mbs = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour mb in mbs)
        {
            FieldInfo[] pis = mb.GetType().GetFields();
            foreach (FieldInfo pi in pis)
            {
                object[] os = pi.GetCustomAttributes(true);
                foreach (object o in os)
                {
                    NetworkVar nv = o as NetworkVar;
                    if (nv != null)
                    {
                        string key = pi.ReflectedType + "," + pi.Name;
                        if (!dicNetVars[key].ToString().Equals(pi.GetValue(mb).ToString()))
                        {
                            //The value has changed, send an update message and our current transform/rigidbody
                            dicNetVars[key] = pi.GetValue(mb).ToString();
                            client.SendMessageToServer(csvRecord(',', "change", id.ToString(), key, dicNetVars[key]));
                            Debug.Log("Var Change: " + pi.GetValue(mb).ToString());

                            Vector3 pos = gameObject.transform.position;
                            Vector3 rot = gameObject.transform.rotation.eulerAngles;
                            client.SendMessageToServer(csvRecord(',', "utransform", id.ToString(), pos.x.ToString("F1"), pos.y.ToString("F1"), pos.z.ToString("F1"),
                                rot.x.ToString("F1"), rot.y.ToString("F1"), rot.z.ToString("F1")));

                            //TODO add rigidbody update
                        }
                    }
                }
            }
        }
    }

    //When a client send an update from CheckAllNetVars(), it sends a message
    // which will be decoded and called into this on the server/other cliends side
    public void UpdateNetVar(string key, string value)
    {
        MonoBehaviour[] mbs = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour mb in mbs)
        {
            FieldInfo[] pis = mb.GetType().GetFields();
            foreach (FieldInfo pi in pis)
            {
                object[] os = pi.GetCustomAttributes(true);
                foreach (object o in os)
                {
                    NetworkVar nv = o as NetworkVar;
                    if (nv != null)
                    {
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

    //Helps translate objects to new players
    public void Copy(out Vector3 pos, out Vector3 rot, out Vector3 posVel, out Vector3 rotVel)
    {
        pos = transform.position;
        rot = transform.rotation.eulerAngles;
        posVel = rb.velocity;
        rotVel = rb.rotation.eulerAngles;
    }

    //Helps translate subsets of the array to a vector3
    public static Vector3 ArrToV3(string[] arr, int start)
    {
        Vector3 v3 = new Vector3();
        v3.x = float.Parse(arr[start++]);
        v3.y = float.Parse(arr[start++]);
        v3.z = float.Parse(arr[start++]);
        return v3;
    }

    //Helps encode strings to byte arrays for transmittion
    public static byte[] encode(string str)
    {
        return System.Text.Encoding.UTF8.GetBytes(str);
    }

    //Helps decode recieved transmissions to strings
    public static string decode(byte[] str, int size)
    {
        string data = System.Text.Encoding.UTF8.GetString(str, 0, size);
        Debug.Log("Decoding [" + data + "]");
        return data;
    }

    //Encodes an array or many string, it will put them into a csv string
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
