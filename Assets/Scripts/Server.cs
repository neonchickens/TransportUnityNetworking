using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Server : MonoBehaviour
{
    //The server communicates all data between clients

    private Dictionary<int, string> dicPlayers;
    private int intObjIdGen = 1;
    private Dictionary<int, NetworkedObject> dicNetObjects;
    private Dictionary<int, List<int>> dicOwner;

    private int hostId;
    private int myReliableChannelId;

    void Start()
    {
        //Seems to like to auto launch full screen, don't do that...
        Screen.fullScreen = !Screen.fullScreen;

        dicPlayers = new Dictionary<int, string>();
        dicNetObjects = new Dictionary<int, NetworkedObject>();
        dicOwner = new Dictionary<int, List<int>>();

        //initializing the Transport Layer
        GlobalConfig gConfig = new GlobalConfig();
        NetworkTransport.Init(gConfig);
        ConnectionConfig config = new ConnectionConfig();
        myReliableChannelId = config.AddChannel(QosType.Reliable);
        HostTopology topology = new HostTopology(config, 100);
        hostId = NetworkTransport.AddHost(topology, 8888);

        Debug.Log("Ready for connections!");
    }

    void Update()
    {
        int recHostId;
        int connectionId;
        int channelId;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error;
        NetworkEventType recData;
        do
        {
            //Recieve message
            recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);

            switch (recData)
            {
                case NetworkEventType.ConnectEvent:
                    //New player has connected

                    //Give user their id
                    Debug.Log("New connection:" + connectionId);
                    dicPlayers.Add(connectionId, null);
                    dicOwner.Add(connectionId, new List<int>());
                    SendMessageToUser(NetworkedObject.csvRecord(',', connectionId.ToString(), "connect"), connectionId);

                    //Tell player about all existing networked objects
                    Vector3 pos, rot, posVel, rotVel;
                    foreach (int id in dicNetObjects.Keys)
                    {
                        //Send spawn, transform, and rigidbody in 3 messages
                        dicNetObjects[id].Copy(out pos, out rot, out posVel, out rotVel);
                        SendMessageToUser(NetworkedObject.csvRecord(',', "0", "spawn", id.ToString(), "player", false.ToString()), connectionId);
                        SendMessageToUser(NetworkedObject.csvRecord(',', "0", "transform", id.ToString(), pos.x.ToString("F4"), pos.y.ToString("F4"), pos.z.ToString("F4"),
                            rot.x.ToString("F4"), rot.y.ToString("F4"), rot.z.ToString("F4")), connectionId);
                        SendMessageToUser(NetworkedObject.csvRecord(',', "0", "rigidbody", id.ToString(), posVel.x.ToString("F4"), posVel.y.ToString("F4"), posVel.z.ToString("F4"),
                            rotVel.x.ToString("F4"), rotVel.y.ToString("F4"), rotVel.z.ToString("F4")), connectionId);
                    }

                    break;

                case NetworkEventType.DataEvent:

                    //The data should be a csv string
                    string[] cmd = NetworkedObject.decode(recBuffer, dataSize).Split(',');

                    string command = cmd[1];

                    if (command.Equals("connect"))
                    {
                        StartCoroutine(LookupPlayerLoc(connectionId, cmd[2]));
                        dicPlayers[connectionId] = cmd[2];

                        //Server only message
                        return;
                    }

                    int objNetId = int.Parse(cmd[2]);

                    if (command.Equals("utransform"))
                    {
                        //Updates transform when we're out of sync
                        int index = 3;
                        NetworkedObject no = dicNetObjects[objNetId];
                        GameObject goPlayer = dicNetObjects[objNetId].gameObject;
                        Vector3 upos = NetworkedObject.ArrToV3(cmd, index);
                        Vector3 urot = NetworkedObject.ArrToV3(cmd, index + 3);

                        if ((goPlayer.transform.position - upos).magnitude > .5 || Quaternion.Angle(goPlayer.transform.rotation, Quaternion.Euler(urot)) > 2)
                        {
                            Debug.Log("Updating position and rotation");
                            no.SetTransform(upos, urot);
                        }
                    }
                    else if (command.Equals("urigidbody"))
                    {
                        //Updates rigidbody when we're out of sync
                        int index = 3;
                        NetworkedObject no = dicNetObjects[objNetId];
                        Vector3 uvel = NetworkedObject.ArrToV3(cmd, index);
                        Vector3 urotVel = NetworkedObject.ArrToV3(cmd, index + 3);

                        if ((no.rb.velocity - uvel).magnitude > .5 || Quaternion.Angle(no.rb.rotation, Quaternion.Euler(urotVel)) > 2)
                        {
                            Debug.Log("Updating rigidbody");
                            no.SetRigidbody(uvel, urotVel);
                        }
                    }
                    else if (command.Equals("spawn"))
                    {
                        GameObject goPlayer = Instantiate(FindObjectOfType<Items>().getItem(cmd[3]));
                        NetworkedObject no1 = goPlayer.GetComponent<NetworkedObject>();
                        no1.Setup(cmd[3], false);
                        no1.SetNetworkId(objNetId, false);
                        dicNetObjects.Add(objNetId, no1);
                        dicOwner[connectionId].Add(objNetId);
                    }
                    else if (command.Equals("register"))
                    {
                        SendMessageToUser(NetworkedObject.csvRecord(',', "0", "assign", cmd[2], (intObjIdGen++).ToString()), connectionId);

                        //Server only message
                        return;
                    }
                    else if (command.Equals("change"))
                    {
                        dicNetObjects[objNetId].UpdateNetVar(cmd[3] + "," + cmd[4], cmd[5]);
                    }
                    else
                    {
                        //TODO Log error
                    }

                    //New spawns wont have correct conn id in cmd[0]
                    cmd[0] = connectionId.ToString();

                    //Rebroadcast message to other clients
                    SendMessageToAll(NetworkedObject.csvRecord(',', cmd));

                    break;

                case NetworkEventType.DisconnectEvent:

                    string username = dicPlayers[connectionId];
                    dicPlayers.Remove(connectionId);

                    //TODO setup command no longer works
                    Debug.Log("Lost connection:" + connectionId);
                    List<int> lstOrphanObjectIds = dicOwner[connectionId];
                    for (int i = lstOrphanObjectIds.Count - 1; i >= 0; i--)
                    {
                        Debug.Log("Destroying object from disconnected player");
                        if (dicNetObjects[lstOrphanObjectIds[i]].GetPrefab().Equals("player"))
                        {
                            StartCoroutine(RecordPlayerLoc(dicNetObjects[lstOrphanObjectIds[i]].transform.position, username));
                        }
                        Destroy(dicNetObjects[lstOrphanObjectIds[i]].gameObject);
                        dicNetObjects.Remove(lstOrphanObjectIds[i]);
                        SendMessageToAll(NetworkedObject.csvRecord(',', "0", "remove", lstOrphanObjectIds[i].ToString()));
                    }
                    dicOwner.Remove(connectionId);
                    SendMessageToAll(connectionId + ",x");


                    break;

                case NetworkEventType.BroadcastEvent: break;
                case NetworkEventType.Nothing: break;
            }

            //Recieve messages until there are none waiting
        } while (recData != NetworkEventType.Nothing);
    }

    IEnumerator LookupPlayerLoc(int connid, string username)
    {
        //Connect to questions database
        string domain = "http://34.205.7.163/";
        string attempts_url = domain + "mymmo_get_loc.php";

        // Create a form object for sending data to the server
        WWWForm form = new WWWForm();
        form.AddField("username", username);

        var download = UnityWebRequest.Post(attempts_url, form);

        // Wait until the download is done
        yield return download.SendWebRequest();

        if (download.isNetworkError || download.isHttpError)
        {
            Debug.Log("Error downloading: " + download.error);
        }
        else
        {
            Debug.Log(download.downloadHandler.text);
            Vector3 v3 = new Vector3();
            if (download.downloadHandler.text.Length > 0)
            {
                v3 = JsonUtility.FromJson<V3>(download.downloadHandler.text).GetV3();
            }

            int newObjId = intObjIdGen++;
            SendMessageToUser(NetworkedObject.csvRecord(',', "0", "spawn", newObjId.ToString(), "player", true.ToString()), connid);
            SendMessageToAll(NetworkedObject.csvRecord(',', connid.ToString(), "spawn", newObjId.ToString(), "player", false.ToString()));
            SendMessageToAll(NetworkedObject.csvRecord(',', "0", "transform", newObjId.ToString(), v3.x.ToString("F4"), v3.y.ToString("F4"), v3.z.ToString("F4"), "0", "0", "0"));
            GameObject goPlayer = Instantiate(FindObjectOfType<Items>().getItem("player"), v3, Quaternion.identity);
            NetworkedObject no1 = goPlayer.GetComponent<NetworkedObject>();
            no1.Setup("player", false);
            no1.SetNetworkId(newObjId, false);
            dicNetObjects.Add(newObjId, no1);
            dicOwner[connid].Add(newObjId);
        }
    }

    IEnumerator RecordPlayerLoc(Vector3 v3, string username)
    {
        //Connect to questions database
        string domain = "http://34.205.7.163/";
        string attempts_url = domain + "mymmo_set_loc.php";

        // Create a form object for sending data to the server
        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("x", v3.x.ToString());
        form.AddField("y", v3.y.ToString());
        form.AddField("z", v3.z.ToString());

        var download = UnityWebRequest.Post(attempts_url, form);

        // Wait until the download is done
        yield return download.SendWebRequest();

        if (download.isNetworkError || download.isHttpError)
        {
            Debug.Log("Error downloading: " + download.error);
        }
        else
        {
            //Success
        }
    }

    //Sends all players in dicPlayers a command csv
    public void SendMessageToAll(string message)
    {
        byte[] bytes = NetworkedObject.encode(message);
        foreach (int id in dicPlayers.Keys)
        {
            SendEncodedMessage(bytes, id);
        }
    }

    //Sends a player a command csv
    public void SendMessageToUser(string message, int connectionId)
    {
        SendEncodedMessage(NetworkedObject.encode(message), connectionId);
    }

    //Sends a player an encoded command csv
    public void SendEncodedMessage(byte[] message, int connectionId)
    {
        byte error;
        NetworkTransport.Send(hostId, connectionId, myReliableChannelId, message, message.Length, out error);
        if (error != 0)
        {
            Debug.LogError(message.Length + " bytes.\n" + (NetworkError)error);
        }
    }

    [Serializable]
    public class V3
    {
        public float x = 0;
        public float y = 0;
        public float z = 0;

        public Vector3 GetV3()
        {
            return new Vector3(x, y, z);
        }
    }
}
