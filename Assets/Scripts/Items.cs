using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Items : MonoBehaviour
{
    //Items keeps all spawnable prefabs for reference by client/server/player

    public Item[] lstItems;
    //Temporary workaround to load items into a hashtable
    private Dictionary<string, GameObject> dicItems;

    private Items items;

    public void Start()
    {
       if (items == null)
        {
            dicItems = new Dictionary<string, GameObject>();
            foreach (Item i in lstItems)
            {
                dicItems.Add(i.name, i.go);
            }
            items = this;
        }
        return;
    }

    public GameObject getItem(string str)
    {
        return dicItems[str];
    }

    [Serializable]
    public struct Item
    {
        public string name;
        public GameObject go;
    }
}
