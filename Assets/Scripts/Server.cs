using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Server : MonoBehaviour
{
    //The server communicates all data between clients

    private Dictionary<int, GameObject> dicPlayers;
    private int intObjIdGen = 1;
    private Dictionary<int, NetworkedObject> dicNetObjects;

    private int hostId;
    private int myReliableChannelId;

    void Start()
    {
        //Seems to like to auto launch full screen, don't do that...
        Screen.fullScreen = !Screen.fullScreen;

        dicPlayers = new Dictionary<int, GameObject>();
        dicNetObjects = new Dictionary<int, NetworkedObject>();

        //initializing the Transport Layer
        GlobalConfig gConfig = new GlobalConfig();
        NetworkTransport.Init(gConfig);
        ConnectionConfig config = new ConnectionConfig();
        myReliableChannelId = config.AddChannel(QosType.Reliable);
        HostTopology topology = new HostTopology(config, 10);
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
                    SendMessageToUser(NetworkedObject.csvRecord(',', connectionId.ToString(), "connect"), connectionId);

                    //Tell player about all existing networked objects
                    Vector3 pos, rot, posVel, rotVel;
                    foreach (int id in dicNetObjects.Keys)
                    {
                        //Send spawn, transform, and rigidbody in 3 messages
                        dicNetObjects[id].Copy(out pos, out rot, out posVel, out rotVel);
                        SendMessageToUser(NetworkedObject.csvRecord(',', "0", "spawn", id.ToString(), "player"), connectionId);
                        SendMessageToUser(NetworkedObject.csvRecord(',', "0", "transform", id.ToString(), pos.x.ToString("F1"), pos.y.ToString("F1"), pos.z.ToString("F1"),
                            rot.x.ToString("F1"), rot.y.ToString("F1"), rot.z.ToString("F1")), connectionId);
                        SendMessageToUser(NetworkedObject.csvRecord(',', "0", "rigidbody", id.ToString(), posVel.x.ToString("F1"), posVel.y.ToString("F1"), posVel.z.ToString("F1"),
                            rotVel.x.ToString("F1"), rotVel.y.ToString("F1"), rotVel.z.ToString("F1")), connectionId);
                    }

                    break;

                case NetworkEventType.DataEvent:

                    //The data should be a csv string
                    string[] cmd = NetworkedObject.decode(recBuffer, dataSize).Split(',');

                    string command = cmd[1];
                    int objNetId = int.Parse(cmd[2]);

                    if (command.Equals("utransform"))
                    {
                        int index = 2;
                        GameObject goPlayer = dicNetObjects[objNetId].gameObject;
                        Vector3 upos = NetworkedObject.ArrToV3(cmd, index);
                        Vector3 urot = NetworkedObject.ArrToV3(cmd, index + 3);
                        Debug.Log("Difference position: " + (goPlayer.transform.position - upos).magnitude);
                        if ((goPlayer.transform.position - upos).magnitude > .5)
                        {
                            Debug.Log("Updating position");
                            goPlayer.GetComponent<Rigidbody>().MovePosition(upos);
                        }
                        if (Quaternion.Angle(goPlayer.transform.rotation, Quaternion.Euler(urot)) > 2)
                        {
                            Debug.Log("Updating angle");
                            goPlayer.GetComponent<Rigidbody>().MoveRotation(Quaternion.Euler(urot));
                        }
                    }
                    else if (command.Equals("spawn"))
                    {
                        GameObject goPlayer = Instantiate(FindObjectOfType<Items>().getItem(cmd[3]));
                        NetworkedObject no1 = goPlayer.GetComponent<NetworkedObject>();
                        no1.Setup(cmd[3], false);
                        no1.SetNetworkId(objNetId);
                        dicNetObjects.Add(objNetId, no1);
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

                    //TODO setup command no longer works
                    Debug.Log("Lost connection:" + connectionId);
                    SendMessageToAll(connectionId + ",x");

                    Destroy(dicPlayers[connectionId]);
                    dicPlayers.Remove(connectionId);

                    break;

                case NetworkEventType.BroadcastEvent: break;
                case NetworkEventType.Nothing: break;
            }

            //Recieve messages until there are none waiting
        } while (recData != NetworkEventType.Nothing);
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

}
