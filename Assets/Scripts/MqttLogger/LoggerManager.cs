using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using System.IO;
using System;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace MqttLogger
{
    public class LoggerManager
    {
        public static string localAppData = System.Environment.ExpandEnvironmentVariables("%localappdata%").Replace("\\","/");
        public static Dictionary<int, string> topicsIndexes;
        public static Dictionary<int, int> topicsTypes;
        public static List<Log> logs = new List<Log>();
        public static bool initialized;

        public static void LoadLogHistory(){
            if(initialized)
                return;      
            string filePath = localAppData+"/Cal3D/mqttlogs.txt";
            try{
                string [] logsText = File.ReadAllLines(filePath);
                foreach (string logText in logsText){
                    Log newLog = JsonUtility.FromJson<Log>(logText);
                    logs.Add(newLog);
                }
                initialized = true;
            }
            catch(Exception exception){
				Debug.Log(exception.Message);
                //Not handled exception, prevent to stop the main thread
            }

            if(topicsIndexes == null)
                LoadTopicsDictionary();
        }

        public static void LoadTopicsDictionary(){
            string filePath = localAppData+"/Cal3D/EDGECALIFIA3D_DETAILED.INI";

            topicsIndexes = IniHelper.GetStrings(filePath, "sub");
            topicsTypes = IniHelper.GetInts(filePath, "types");
        }

        public static void SaveTopic(object sender, string topic, byte[] data, LogType type){
            MqttClient senderData = (MqttClient)sender;
            int topicType = 0;
            string rawData = "";
            
            var keyIndex = topicsIndexes.Where(x => x.Value == topic).First();
            if(keyIndex.Value != null){
                if(topicsTypes.ContainsKey(keyIndex.Key))
                    topicType = topicsTypes[keyIndex.Key];
            }

            if(topicType == 0){
                rawData = BitConverter.ToSingle(data, 0).ToString();
            }
            else if(topicType == 1){
                rawData = Encoding.UTF8.GetString(data);
            }
            else if(topicType == 2){
                if(data.Length!=1600){
                    return;
                }
                float[] floatArr = new float[data.Length / 4];
                int j=0;
                for (int i = 0; i < data.Length; i += 4)
                {
                    float convertedFloat = System.BitConverter.ToSingle(data, i);
                    floatArr[j] = convertedFloat;
                    j++;
                }
                rawData = floatArr.ToString();
            } 

            string senderPrefix = senderData.ClientId.Replace("_EdgeCalifia3D", "");

            Log newLog = new Log(System.DateTime.Now, senderPrefix, topic, rawData, type);
            logs.Add(newLog);

		    StoreNewLog(newLog); 
        }

        public static void SaveTopic(string sender, string topic, string data, LogType type){
            
            Log newLog = new Log(System.DateTime.Now, sender, topic, data, type);
            logs.Add(newLog);

            StoreNewLog(newLog);
        }

        public static void StoreNewLog(Log log){
            string filePath = localAppData+"/Cal3D/mqttlogs.txt";
            try{
                using (StreamWriter w = File.AppendText(filePath)){
                    w.WriteLine(JsonUtility.ToJson(log));
                }
            }
            catch(Exception exception){
				Debug.Log(exception.Message);
                //Not handled exception
            }
        }

        public static void ClearLogs(){
            logs = new List<Log>();
            string filePath = localAppData+"/Cal3D/mqttlogs.txt";
            using(StreamWriter writer = new StreamWriter(filePath)){
                    writer.Write(string.Empty); 
            }
        }

        public static List<Log> GetLogs(LogFilters filter, string keyword){
            switch (filter){
                case LogFilters.none:
                    return logs;
                case LogFilters.topic:
                    return GetLogsFilteredByTopic(keyword);
                case LogFilters.payload:
                    return GetLogsFilteredByPayload(keyword);
                default:
                    return logs;
            }
        }

        public static List<Log> GetLogsFilteredByTopic(string keyword){
            keyword = keyword.ToLower();
            var filterResult = logs.Where(x => x.topic.ToLower().Contains(keyword)).ToList();

            return filterResult;
        }

        public static List<Log> GetLogsFilteredByPayload(string keyword){
            keyword = keyword.ToLower();
            var filterResult = logs.Where(x => x.message.ToLower().Replace('"'.ToString(),"").Contains(keyword)).ToList();

            return filterResult;
        }
    }
}
