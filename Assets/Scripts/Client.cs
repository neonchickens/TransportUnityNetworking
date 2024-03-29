﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Client : MonoBehaviour
{
    //Communicates and recieves messages from the server

    private Dictionary<int, GameObject> dicPlayers;
    private int connectionId;
    public Dictionary<int, NetworkedObject> dicNetObjects;

    int hostId;
    int myReliableChannelId;

    GameObject goLocalPlayer;
    int localPlayerId = -1;
    bool isConnected = false;

    void Start()
    {
        dicPlayers = new Dictionary<int, GameObject>();
        dicNetObjects = new Dictionary<int, NetworkedObject>();

        //Sets up and attempts to connect to local connection
        GlobalConfig gConfig = new GlobalConfig();
        NetworkTransport.Init(gConfig);
        ConnectionConfig config = new ConnectionConfig();
        myReliableChannelId = config.AddChannel(QosType.Reliable);
        HostTopology topology = new HostTopology(config, 1);
        hostId = NetworkTransport.AddHost(topology);
        byte error;
        connectionId = NetworkTransport.Connect(hostId, "34.205.7.163", 8888, 0, out error);
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
            //Recieve message
            recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);

            if ((byte)NetworkError.Ok != error)
            {
                Debug.Log((NetworkError)error);
            }

            switch (recData)
            {
                //Successfully connected to the server
                case NetworkEventType.ConnectEvent:

                    //Spawn a local game object to play as
                    //goLocalPlayer = Instantiate(FindObjectOfType<Items>().getItem("player"));
                    //goLocalPlayer.GetComponent<NetworkedObject>().Setup("player", true);
                    //FindObjectOfType<CameraFollow>().SetPlayer(goLocalPlayer);
                    SendMessageToServer(NetworkedObject.csvRecord(',', "connect", PlayerPrefs.GetString("username")));

                    break;

                    //Recieves a typical data message
                case NetworkEventType.DataEvent:

                    //Decode the message into a string array for processing
                    string data = System.Text.Encoding.ASCII.GetString(recBuffer, 0, dataSize);
                    Debug.Log(data);
                    string[] cmd = data.Split(',');

                    int plrId = int.Parse(cmd[0]);
                    string command = cmd[1];

                    //Don't let the server tell you what to do
                    if (command.Equals("connect"))
                    {
                        //We're freshly spawned and this will be our id
                        localPlayerId = plrId;
                    }
                    else if (plrId != localPlayerId)
                    {
                        int objNetId = int.Parse(cmd[2]);

                        //Spawn an object
                        if (command.Equals("spawn"))
                        {
                            //If we already know of it, don't spawn it
                            if (!dicNetObjects.ContainsKey(objNetId))
                            {
                                GameObject goPlayer = Instantiate(FindObjectOfType<Items>().getItem(cmd[3]));
                                NetworkedObject no = goPlayer.GetComponent<NetworkedObject>();
                                no.Setup(cmd[3], bool.Parse(cmd[4]));
                                no.SetNetworkId(objNetId, true);
                                dicNetObjects.Add(objNetId, no);
                            }
                        }
                        else if (command.Equals("connect"))
                        {
                            //We're freshly spawned and this will be our id
                            localPlayerId = plrId;
                        }
                        else if (command.Equals("transform"))
                        {
                            //Used for initial transform setting
                            int index = 3;
                            NetworkedObject no = dicNetObjects[objNetId];
                            Vector3 pos = NetworkedObject.ArrToV3(cmd, index);
                            Vector3 rot = NetworkedObject.ArrToV3(cmd, index + 3);

                            no.SetTransform(pos, rot);
                        }
                        else if (command.Equals("utransform"))
                        {
                            //Updates transform when we're out of sync
                            int index = 3;
                            NetworkedObject no = dicNetObjects[objNetId];
                            Vector3 pos = NetworkedObject.ArrToV3(cmd, index);
                            Vector3 rot = NetworkedObject.ArrToV3(cmd, index + 3);

                            if ((no.transform.position - pos).magnitude > .5 || Quaternion.Angle(no.transform.rotation, Quaternion.Euler(rot)) > 2)
                            {
                                Debug.Log("Updating position and rotation");
                                no.SetTransform(pos, rot);
                            }
                        }
                        else if (command.Equals("urigidbody"))
                        {
                            //Updates rigidbody when we're out of sync
                            int index = 3;
                            NetworkedObject no = dicNetObjects[objNetId];
                            Vector3 vel = NetworkedObject.ArrToV3(cmd, index);
                            Vector3 rotVel = NetworkedObject.ArrToV3(cmd, index + 3);

                            if ((no.rb.velocity - vel).magnitude > .5 || Quaternion.Angle(no.rb.rotation, Quaternion.Euler(rotVel)) > 2)
                            {
                                Debug.Log("Updating rigidbody");
                                no.SetRigidbody(vel, rotVel);
                            }
                        }
                        else if (command.Equals("rigidbody"))
                        {
                            //Used for initial rigidbody setting
                            int index = 3;
                            NetworkedObject no = dicNetObjects[objNetId];
                            Vector3 posVel = NetworkedObject.ArrToV3(cmd, index);
                            Vector3 rotVel = NetworkedObject.ArrToV3(cmd, index + 3);

                            no.SetRigidbody(posVel, rotVel);
                        }
                        else if (command.Equals("assign"))
                        {
                            //We have a freshly spawned item and this will be it's id
                            int oldId = objNetId;
                            int newId = int.Parse(cmd[3]);
                            dicNetObjects.Add(newId, dicNetObjects[oldId]);
                            dicNetObjects.Remove(oldId);
                            dicNetObjects[newId].SetNetworkId(newId, false);
                        }
                        else if (command.Equals("change"))
                        {
                            //An object someone else controls send an update
                            dicNetObjects[objNetId].UpdateNetVar(cmd[3] + "," + cmd[4], cmd[5]);
                        }
                        else if (command.Equals("remove"))
                        {
                            Destroy(dicNetObjects[objNetId].gameObject);
                            dicNetObjects.Remove(objNetId);
                        }
                        else if (command.Equals("x"))
                        {
                            //Someone disconnected (kill them)
                            Destroy(dicPlayers[plrId]);
                            dicPlayers.Remove(plrId);
                        }
                    }

                    break;
                case NetworkEventType.DisconnectEvent:

                    //If we disconnect from the server, quit the application
                    Application.Quit();

                    break;

                case NetworkEventType.BroadcastEvent: break;
                case NetworkEventType.Nothing: break;
            }

            //Recieve messages until there are none waiting
        } while (recData != NetworkEventType.Nothing);
    }

    //Allows for local registry of networked objects
    public void RegNetObj(int id, NetworkedObject no)
    {
        if (dicNetObjects == null)
        {
            dicNetObjects = new Dictionary<int, NetworkedObject>();
        }
        if (!dicNetObjects.ContainsValue(no))
        {
            dicNetObjects.Add(id, no);
        }
    }

    //Sends a string message to the server
    public void SendMessageToServer(string message)
    {
        byte[] bytes = NetworkedObject.encode(localPlayerId + "," + message);
        byte error;
        NetworkTransport.Send(hostId, connectionId, myReliableChannelId, bytes, bytes.Length, out error);
        if (error != 0)
        {
            Debug.LogError(bytes.Length + " bytes. " + (NetworkError)error);
        }
    }
}
