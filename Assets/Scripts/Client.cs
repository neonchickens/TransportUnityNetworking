﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Client : MonoBehaviour
{
    private int connectionId;
    private Dictionary<int, GameObject> dicPlayers;

    int hostId;
    int myReliableChannelId;

    GameObject goLocalPlayer;
    int localPlayerId = -1;

    // Start is called before the first frame update
    void Start()
    {
        dicPlayers = new Dictionary<int, GameObject>();

        // An example of initializing the Transport Layer with custom settings
        GlobalConfig gConfig = new GlobalConfig();
        NetworkTransport.Init(gConfig);
        ConnectionConfig config = new ConnectionConfig();
        myReliableChannelId = config.AddChannel(QosType.Reliable);
        HostTopology topology = new HostTopology(config, 1);
        hostId = NetworkTransport.AddHost(topology);
        byte error;
        connectionId = NetworkTransport.Connect(hostId, "10.0.0.252", 8888, 0, out error);
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
        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
        do
        {

            switch (recData)
            {
                case NetworkEventType.Nothing: break;
                case NetworkEventType.ConnectEvent:

                    goLocalPlayer = Instantiate(FindObjectOfType<Items>().getItem("player"));
                    goLocalPlayer.GetComponent<NetworkedObject>().isLocalPlayer = true;
                    FindObjectOfType<CameraFollow>().SetPlayer(goLocalPlayer);

                    break;
                case NetworkEventType.DataEvent:

                    string data = System.Text.Encoding.ASCII.GetString(recBuffer, 0, dataSize);
                    Debug.Log(data);
                    string[] cmd = data.Split(',');
                    if (int.TryParse(cmd[0], out int playerId))
                    {
                        if (playerId != localPlayerId)
                        {
                            if (cmd[1].Equals("spawn"))
                            {
                                if (dicPlayers.Count == 0)
                                {
                                    localPlayerId = playerId;
                                    dicPlayers.Add(localPlayerId, goLocalPlayer);
                                }
                                else
                                {
                                    GameObject goPlayer = Instantiate(FindObjectOfType<Items>().getItem("player"));
                                    dicPlayers.Add(playerId, goPlayer);
                                }

                            }

                            PlayerController pcPlayer = dicPlayers[playerId].GetComponent<PlayerController>();

                            if (cmd[1].Equals("transform"))
                            {
                                //Used for initial transform setting
                                int index = 2;
                                GameObject goPlayer = dicPlayers[playerId];
                                Vector3 pos = NetworkedObject.ArrToV3(cmd, index);
                                Vector3 rot = NetworkedObject.ArrToV3(cmd, index + 3);

                                goPlayer.GetComponent<PlayerController>().SetTransform(pos, rot);
                            }
                            else if (cmd[1].Equals("utransform"))
                            {
                                int index = 2;
                                GameObject goPlayer = dicPlayers[playerId];
                                Vector3 pos = NetworkedObject.ArrToV3(cmd, index);
                                Vector3 rot = NetworkedObject.ArrToV3(cmd, index + 3);

                                Debug.Log("Difference position: " + (goPlayer.transform.position - pos).magnitude);
                                if ((goPlayer.transform.position - pos).magnitude > 2)
                                {
                                    Debug.Log("Updating position");
                                    goPlayer.GetComponent<Rigidbody>().MovePosition(pos);
                                }
                                if (Quaternion.Angle(goPlayer.transform.rotation, Quaternion.Euler(rot)) > 5)
                                {
                                    Debug.Log("Updating angle");
                                    goPlayer.GetComponent<Rigidbody>().MoveRotation(Quaternion.Euler(rot));
                                }
                            }
                            else if (cmd[1].Equals("rigidbody"))
                            {
                                int index = 2;
                                GameObject goPlayer = dicPlayers[playerId];
                                Vector3 posVel = NetworkedObject.ArrToV3(cmd, index);
                                Vector3 rotVel = NetworkedObject.ArrToV3(cmd, index + 3);

                                goPlayer.GetComponent<PlayerController>().SetRigidbody(posVel, rotVel);
                            }
                            else if (cmd[1].Equals("w"))
                            {
                                pcPlayer.WalkForward();
                            }
                            else if (cmd[1].Equals("l"))
                            {
                                pcPlayer.TurnLeft();
                            }
                            else if (cmd[1].Equals("r"))
                            {
                                pcPlayer.TurnRight();
                            }
                            else if (cmd[1].Equals("x"))
                            {
                                Destroy(dicPlayers[playerId]);
                                dicPlayers.Remove(playerId);
                            }
                        }
                    } 

                    break;
                case NetworkEventType.DisconnectEvent:

                    Application.Quit();

                    break;
                case NetworkEventType.BroadcastEvent: break;
            }
            recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
        } while (recData != NetworkEventType.Nothing);
    }

    public void SendMessageToServer(string message)
    {
        byte[] bytes = NetworkedObject.encode(localPlayerId + "," + message);
        byte error;
        NetworkTransport.Send(hostId, connectionId, myReliableChannelId, bytes, bytes.Length, out error);
        if (error != 0)
        {
            Debug.LogError(bytes.Length + " bytes.\n" + (NetworkError)error);
        }
    }
}
