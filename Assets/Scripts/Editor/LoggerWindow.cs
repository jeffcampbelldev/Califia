using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MqttLogger
{
    public class LoggerWindow : EditorWindow
    {
        Vector2 scrollPosition = Vector2.zero; //scroll initial position
        int index; //dropdown index
        string [] filters = new string[] {"all", "topic", "payload"}; //dropdown options
        string keyword; //keyword input
        LogFilters filter = LogFilters.none; //selected filter
        string selectedKeyword = "";  //selected keyword
        int layoutCountLogs;
        List<Log> logs = new List<Log>();

        [MenuItem("Tools/Logger")]
        public static void ShowWindow(){
            EditorWindow.GetWindow<LoggerWindow>("Logger");
            LoggerManager.LoadLogHistory(); 
        }

        void  OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI(){
            if(Event.current.type == EventType.Layout){
                logs = LoggerManager.GetLogs(filter, selectedKeyword);
                layoutCountLogs = logs.Count;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter by: ", GUILayout.Width(70));
            index = EditorGUILayout.Popup(index, filters, GUILayout.Width(100));
            keyword = GUILayout.TextField(keyword, GUILayout.Width(300));
            if(GUILayout.Button("Apply")){
                selectedKeyword = keyword;
                filter = (LogFilters)index;
            }
            if(GUILayout.Button("Reset")){
                keyword = selectedKeyword = "";
                filter = LogFilters.none;
                index = 0;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Date", EditorStyles.boldLabel, GUILayout.Width(150));
            GUILayout.Label("Prefix", EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.Label("Topic", EditorStyles.boldLabel, GUILayout.Width(250));
            GUILayout.Label("Message", EditorStyles.boldLabel);
            GUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            if(Event.current.type == EventType.Repaint || Event.current.type == EventType.Layout){
                for(int i=0; i<layoutCountLogs; i++)
                {
                    GUILayout.BeginHorizontal();
                    if(logs[i].type == LogType.income)
                        GUI.contentColor = Color.green;
                    else if(logs[i].type == LogType.outgoing)
                        GUI.contentColor = Color.yellow;
                
                    GUILayout.Box(logs[i].date, GUILayout.Width(150));
                    GUILayout.Box(logs[i].prefix, GUILayout.Width(100));
                    GUILayout.Box(logs[i].topic, GUILayout.Width(250));
                    GUILayout.Box(logs[i].message);
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            
            GUI.contentColor = Color.white;
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("Clear")){
                LoggerManager.ClearLogs();
                Debug.Log("Mqtt log history cleared");
            }
        }
    }    
}
