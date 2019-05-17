using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

public class localMqtt : MonoBehaviour
{

    private MqttClient mqttClient;

    // a list of IDs that will be kept to have multiple tags visualised.
    private List<string> ids;
    // a list of tags that are be visualised
    private List<GameObject> tags;
    // create a queue where the positions will be added, that is emptied during update
    private List<PositionData> positionQueue;

    [Tooltip("Visualisation prefab for the tags.")]
    public GameObject tag;


    private void Start()
    {
        // initializing the 
        positionQueue = new List<PositionData>();
        // initializing the ids list
        ids = new List<string>();
        // initializing the tags list
        tags = new List<GameObject>();
        
        // create a MQTT client which will connect to the local MQTT broker.
        mqttClient = new MqttClient(IPAddress.Parse("127.0.0.1"), 1883, false, null);

        // connect to a callback, this function will be triggered every time a message is received.
        mqttClient.MqttMsgPublishReceived += onMqttMessageReceived;

        // create a random ID for the client to connect to the broker with.
        var clientId = Guid.NewGuid().ToString();
        mqttClient.Connect(clientId);

        // subscribe to the "tags" topic. This is an MQTT topic that will provide data with the lowest latency.
        mqttClient.Subscribe(new[] {"tags"}, new[] {MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE});
    }

    private void onMqttMessageReceived(object sender, MqttMsgPublishEventArgs e)
    {
        // we take the message from the MQTT event and parse it as UTF8.
        var message = Encoding.UTF8.GetString(e.Message);

        // the data comes in as an array.
        var messageData = JArray.Parse(message);
		
        // we turn the parsed message into a proper array.
        var messageObj = JArray.Parse(messageData.ToString());
        
        // iterate over each tag packet
        foreach (var tagData in messageObj)
        {
            // this catches unsuccessful positioning packets
            if (!(bool) tagData["success"]) continue;
			
            // if the packet is successful, we add it to the positionQueue
            var positionData = new PositionData((string) tagData["tagId"], (int) tagData["data"]["coordinates"]["x"],
                (int) tagData["data"]["coordinates"]["y"], (int) tagData["data"]["coordinates"]["z"]);

            positionQueue.Add(positionData);
        }
    }

    private void Update()
    {
        // The list positionQueue is used as a FIFO queue, is filled by the MQTT callback 
        // and emptied here.
        while (positionQueue.Count > 0)
        {
            var positionData = positionQueue[0];
            Debug.Log("Found with ID " + positionData.ID);
            // Modified because of Unity axis system
            AddPosition(positionData.ID, positionData.x, positionData.z, positionData.y);

            // This would use the original Unity system
            // AddPosition(positionData.ID, positionData.x, positionData.y, positionData.z);
            
            // the position is now removed from the queue
            positionQueue.Remove(positionData);
        }
    }

    private void AddPosition(string id, int x, int y, int z)
    {
        for (var i = 0; i < ids.Count; i++)
            if (ids[i].Equals(id))
            {
                tags[i].transform.position = new Vector3(x / 1000.0f,
                    y / 1000.0f,
                    z / 1000.0f);
                return;
            }

        Debug.Log("Adding new object!");

        // not in current ID list
        ids.Add(id);
        var new_player = Instantiate(tag);
        new_player.transform.position = new Vector3(x / 1000.0f,
            y / 1000.0f,
            z / 1000.0f);
        new_player.name = string.Format("Tag {0}", id);
        tags.Add(new_player);
    }
}