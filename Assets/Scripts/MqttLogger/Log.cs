using System;

namespace MqttLogger
{
    [System.Serializable]
    public class Log
    {
        public string date;
        public string prefix;
        public string topic;
        public string message;
        public LogType type;

        public Log(DateTime date, string prefix, string topic, string message, LogType type){
            this.date = date.ToString();
            this.prefix = prefix;
            this.topic = topic;
            this.message = message;
            this.type = type;
        }
    }
}
