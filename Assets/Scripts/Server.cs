using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Server : MonoBehaviour
{
    private Dictionary<int, GameObject> dicPlayers;
    private int intObjIdGen = 1;
    private Dictionary<int, NetworkedObject> dicNetObjects;

    private int hostId;
    private int myReliableChannelId;

    void Start()
    {
        Screen.fullScreen = !Screen.fullScreen;
        dicPlayers = new Dictionary<int, GameObject>();

        //initializing the Transport Layer
        GlobalConfig gConfig = new GlobalConfig();
        NetworkTransport.Init(gConfig);

        ConnectionConfig config = new ConnectionConfig();
        myReliableChannelId = config.AddChannel(QosType.Reliable);

        HostTopology topology = new HostTopology(config, 10);
        hostId = NetworkTransport.AddHost(topology, 8888);

        dicNetObjects = new Dictionary<int, NetworkedObject>();
        Debug.Log("Socket Open");
    }

    // Update is called once per frame
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
            recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
            switch (recData)
            {
                case NetworkEventType.Nothing: break;
                case NetworkEventType.ConnectEvent:
                    Debug.Log("New connection:" + connectionId);

                    ////Initialize new player in local
                    //GameObject go = Instantiate(FindObjectOfType<Items>().getItem("player"));
                    //intObjIdGen++;

                    ////Give player their id
                    //SendMessageToUser(NetworkedObject.csvRecord(',', connectionId.ToString(), "spawn", intObjIdGen.ToString(), "player"), connectionId);
                    SendMessageToUser(NetworkedObject.csvRecord(',', connectionId.ToString(), "connect"), connectionId);

                    //Tell new player about all existing players
                    Vector3 pos, rot, posVel, rotVel;
                    foreach (int id in dicPlayers.Keys)
                    {
                        //Send your spawn, transform, and rigidbody in 3 messages
                        //TODO: Maybe if we trim the info (don't need float values) we can shorten these messages
                        dicPlayers[id].GetComponent<PlayerController>().Copy(out pos, out rot, out posVel, out rotVel);
                        SendMessageToUser(NetworkedObject.csvRecord(',', id.ToString(), "spawn", intObjIdGen.ToString(), "player"), connectionId);
                        SendMessageToUser(NetworkedObject.csvRecord(',', id.ToString(), "transform", pos.x.ToString("F1"), pos.y.ToString("F1"), pos.z.ToString("F1"),
                            rot.x.ToString("F1"), rot.y.ToString("F1"), rot.z.ToString("F1")), connectionId);
                        SendMessageToUser(NetworkedObject.csvRecord(',', id.ToString(), "rigidbody", posVel.x.ToString("F1"), posVel.y.ToString("F1"), posVel.z.ToString("F1"),
                            rotVel.x.ToString("F1"), rotVel.y.ToString("F1"), rotVel.z.ToString("F1")), connectionId);
                    }

                    ////Tell other players about new player
                    //go.GetComponent<PlayerController>().Copy(out pos, out rot, out posVel, out rotVel);
                    //string newPlayerMessageSpawn = NetworkedObject.csvRecord(',', connectionId.ToString(), "spawn", intObjIdGen.ToString(), "player");
                    //string newPlayerMessageTransform = NetworkedObject.csvRecord(',', connectionId.ToString(), "transform", pos.x.ToString("F1"), pos.y.ToString("F1"), pos.z.ToString("F1"),
                    //        rot.x.ToString("F1"), rot.y.ToString("F1"), rot.z.ToString("F1"));
                    //string newPlayerMessageRigidbody = NetworkedObject.csvRecord(',', connectionId.ToString(), "rigidbody", posVel.x.ToString("F1"), posVel.y.ToString("F1"), posVel.z.ToString("F1"),
                    //        rotVel.x.ToString("F1"), rotVel.y.ToString("F1"), rotVel.z.ToString("F1"));
                    //foreach (int id in dicPlayers.Keys)
                    //{
                    //    SendMessageToUser(newPlayerMessageSpawn, id);
                    //    SendMessageToUser(newPlayerMessageTransform, id);
                    //    SendMessageToUser(newPlayerMessageRigidbody, id);
                    //}

                    //NetworkedObject no = go.GetComponent<NetworkedObject>();
                    //no.SetPrefab("player");
                    //no.SetNetworkId(intObjIdGen);

                    ////Do this last to avoid treating new player as old player
                    //dicPlayers.Add(connectionId, go);
                    //dicNetObjects.Add(intObjIdGen, no);

                    break;

                case NetworkEventType.DataEvent:

                    string data = NetworkedObject.decode(recBuffer, dataSize);
                    string[] cmd = data.Split(',');

                    if (cmd[1].Equals("uutransform"))
                    {
                        int index = 2;
                        GameObject goPlayer = dicNetObjects[int.Parse(cmd[2])].gameObject;
                        Vector3 upos = NetworkedObject.ArrToV3(cmd, index);
                        Vector3 urot = NetworkedObject.ArrToV3(cmd, index + 3);
                        Debug.Log("Difference position: " + (goPlayer.transform.position - upos).magnitude);
                        if ((goPlayer.transform.position - upos).magnitude > 2)
                        {
                            Debug.Log("Updating position");
                            goPlayer.GetComponent<Rigidbody>().MovePosition(upos);
                        }
                        if (Quaternion.Angle(goPlayer.transform.rotation, Quaternion.Euler(urot)) > 5)
                        {
                            Debug.Log("Updating angle");
                            goPlayer.GetComponent<Rigidbody>().MoveRotation(Quaternion.Euler(urot));
                        }
                    }
                    else if (cmd[1].Equals("spawn"))
                    {
                        GameObject goPlayer = Instantiate(FindObjectOfType<Items>().getItem(cmd[3]));
                        NetworkedObject no1 = goPlayer.GetComponent<NetworkedObject>();
                        no1.SetPrefab(cmd[3]);
                        no1.SetNetworkId(int.Parse(cmd[2]));
                        dicNetObjects.Add(int.Parse(cmd[2]), no1);
                        //if (dicPlayers.Count == 0)
                        //{
                        //    localPlayerId = playerId;
                        //    dicPlayers.Add(localPlayerId, goLocalPlayer);
                        //}
                        //else
                        //{
                        //    dicPlayers.Add(playerId, goPlayer);
                        //}

                    }
                    else if (cmd[1].Equals("register"))
                    {
                        SendMessageToUser(NetworkedObject.csvRecord(',', "0", "assign", cmd[2], (intObjIdGen++).ToString()), connectionId);
                    }
                    else if (cmd[1].Equals("change"))
                    {
                        dicNetObjects[int.Parse(cmd[2])].UpdateNetVar(cmd[3] + "," + cmd[4], cmd[5]);
                    }
                    else
                    {
                        PlayerController pcPlayer = dicNetObjects[int.Parse(cmd[2])].gameObject.GetComponent<PlayerController>();
                        switch (cmd[1].ToCharArray()[0])
                        {
                            case 'j':
                                pcPlayer.Jump();
                                break;
                        }
                    }
                    SendMessageToAll(data);

                    break;

                case NetworkEventType.DisconnectEvent:
                    Debug.Log("Lost connection:" + connectionId);
                    SendMessageToAll(connectionId + ",x");

                    Destroy(dicPlayers[connectionId]);
                    dicPlayers.Remove(connectionId);

                    break;
                case NetworkEventType.BroadcastEvent:



                    break;
            }

            recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
        } while (recData != NetworkEventType.Nothing);
    }

    public void SendMessageToAll(string message)
    {
        byte[] bytes = NetworkedObject.encode(message);
        foreach (int id in dicPlayers.Keys)
        {
            SendEncodedMessage(bytes, id);
        }
    }

    public void SendMessageToUser(string message, int connectionId)
    {
        SendEncodedMessage(NetworkedObject.encode(message), connectionId);
    }

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
