﻿using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Linq;

namespace Crosstales.RTVoice.Provider
{
   /// <summary>MaryTTS voice provider.</summary>
   public class VoiceProviderMary : BaseVoiceProvider<VoiceProviderMary>
   {
      #region Variables

      //private static VoiceProviderMary instance;

      private string uri;
      private System.Collections.Generic.Dictionary<string, string> headers = new System.Collections.Generic.Dictionary<string, string>();

      private string lastUrl;
      private int lastPort;
      private string lastUser;
      private string lastPassword;

      #endregion


      #region Properties
/*
      /// <summary>Returns the singleton instance of this class.</summary>
      /// <returns>Singleton instance of this class.</returns>
      public static VoiceProviderMary Instance => instance ?? (instance = new VoiceProviderMary());
*/
      public override string AudioFileExtension => ".wav";

      public override AudioType AudioFileType => AudioType.WAV;

      public override string DefaultVoiceName => "cmu-rms-hsmm";

      public override bool isWorkingInEditor => false;

      public override bool isWorkingInPlaymode => true;

      public override int MaxTextLength => 256000;

      public override bool isSpeakNativeSupported => false;

      public override bool isSpeakSupported => true;

      public override bool isPlatformSupported => true;

      public override bool isSSMLSupported => true;

      public override bool isOnlineService => true;

      public override bool hasCoRoutines => true;

      public override bool isIL2CPPSupported => true;

      public override bool hasVoicesInEditor => true;

      #endregion


/*
      /// <summary>
      /// Constructor for VoiceProviderMary. Needed to pass IP and Port of the MaryTTS server to the Provider.
      /// </summary>
      /// <param name="url">IP-Address of the MaryTTS-server</param>
      /// <param name="port">Port to connect to on the MaryTTS-server</param>
      /// <param name="user">User for HTTP-auth</param>
      /// <param name="password">Password for HTTP-auth</param>
      public VoiceProviderMary(string url, int port = 59125, string user = "", string password = "")
      {
         if (!string.IsNullOrEmpty(url))
            uri = Util.Helper.CleanUrl(url, false, false) + ":" + port;

         if (!string.IsNullOrEmpty(user))
         {
            headers["Authorization"] =
               "Basic " + System.Convert.ToBase64String(
                  System.Text.Encoding.ASCII.GetBytes(user + ":" + password));
         }

         Load();
      }
*/


      #region Implemented methods

      public override void Load(bool forceReload = false)
      {
         bool _forceReload = forceReload;

         if (Speaker.Instance.MaryTTSUrl != lastUrl ||
             Speaker.Instance.MaryTTSPort != lastPort ||
             Speaker.Instance.MaryTTSUser != lastUser ||
             Speaker.Instance.MaryTTSPassword != lastPassword)
         {
            lastUrl = Speaker.Instance.MaryTTSUrl;
            lastPort = Speaker.Instance.MaryTTSPort;
            lastUser = Speaker.Instance.MaryTTSUser;
            lastPassword = Speaker.Instance.MaryTTSPassword;

            _forceReload = true;
         }

         if (cachedVoices?.Count == 0 || _forceReload)
         {
            if (!string.IsNullOrEmpty(Speaker.Instance.MaryTTSUrl))
               uri = Util.Helper.CleanUrl(Speaker.Instance.MaryTTSUrl, false, false) + ":" + Speaker.Instance.MaryTTSPort;

            if (!string.IsNullOrEmpty(Speaker.Instance.MaryTTSUser))
               headers["Authorization"] = "Basic " + System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(Speaker.Instance.MaryTTSUser + ":" + Speaker.Instance.MaryTTSPassword));

            if (Util.Helper.isEditorMode)
            {
#if UNITY_EDITOR
               getVoicesInEditor();
#endif
            }
            else
            {
               Speaker.Instance.StartCoroutine(getVoices());
            }
         }
         else
         {
            onVoicesReady();
         }
      }

      public override IEnumerator SpeakNative(Model.Wrapper wrapper)
      {
         yield return speak(wrapper, true);
      }

      public override IEnumerator Speak(Model.Wrapper wrapper)
      {
         yield return speak(wrapper, false);
      }

      public override IEnumerator Generate(Model.Wrapper wrapper)
      {
#if !UNITY_WEBGL
         if (wrapper == null)
         {
            Debug.LogWarning("'wrapper' is null!");
         }
         else
         {
            if (string.IsNullOrEmpty(wrapper.Text))
            {
               Debug.LogWarning("'wrapper.Text' is null or empty: " + wrapper);
            }
            else
            {
               if (!Util.Helper.isInternetAvailable)
               {
                  const string errorMessage = "Internet is not available - can't use MaryTTS right now!";
                  Debug.LogError(errorMessage);
                  onErrorInfo(wrapper, errorMessage);
               }
               else
               {
                  if (uri != null)
                  {
                     yield return null; //return to the main process (uid)

                     string voiceCulture = getVoiceCulture(wrapper);
                     string voiceName = getVoiceName(wrapper);

                     silence = false;

                     onSpeakAudioGenerationStart(wrapper);

                     System.Text.StringBuilder sbXML = new System.Text.StringBuilder();
                     string request;

                     switch (Speaker.Instance.MaryTTSType)
                     {
                        case Model.Enum.MaryTTSType.RAWMARYXML:
                           //RAWMARYXML
                           sbXML.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
                           sbXML.Append(
                              "<maryxml version=\"0.5\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://mary.dfki.de/2002/MaryXML\" xml:lang=\"");
                           sbXML.Append(voiceCulture);
                           sbXML.Append("\">");

                           sbXML.Append(prepareProsody(wrapper));

                           sbXML.Append("</maryxml>");

                           request = uri + "/process?INPUT_TEXT=" +
                                     System.Uri.EscapeDataString(sbXML.ToString()) +
                                     "&INPUT_TYPE=RAWMARYXML&OUTPUT_TYPE=AUDIO&AUDIO=WAVE_FILE&LOCALE=" +
                                     voiceCulture + "&VOICE=" + voiceName;
                           break;
                        case Model.Enum.MaryTTSType.EMOTIONML:
                           //EMOTIONML
                           sbXML.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
                           sbXML.Append("<emotionml version=\"1.0\" ");
                           sbXML.Append("xmlns=\"http://www.w3.org/2009/10/emotionml\"");
                           //sbXML.Append (" category-set=\"http://www.w3.org/TR/emotion-voc/xml#everyday-categories\"");
                           sbXML.Append(" category-set=\"http://www.w3.org/TR/emotion-voc/xml#big6\"");
                           sbXML.Append(">");

                           sbXML.Append(getValidXML(wrapper.Text));
                           sbXML.Append("</emotionml>");

                           request = uri + "/process?INPUT_TEXT=" +
                                     System.Uri.EscapeDataString(sbXML.ToString()) +
                                     "&INPUT_TYPE=EMOTIONML&OUTPUT_TYPE=AUDIO&AUDIO=WAVE_FILE&LOCALE=" +
                                     voiceCulture + "&VOICE=" + voiceName;
                           break;
                        case Model.Enum.MaryTTSType.SSML:
                           //SSML
                           sbXML.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
                           sbXML.Append(
                              "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"");
                           sbXML.Append(voiceCulture);
                           sbXML.Append("\"");
                           //sbXML.Append (" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
                           //sbXML.Append (" xsi:schemaLocation=\"http://www.w3.org/2001/10/synthesis http://www.w3.org/TR/speech-synthesis/synthesis.xsd\"");
                           sbXML.Append(">");

                           sbXML.Append(prepareProsody(wrapper));

                           sbXML.Append("</speak>");

                           request = uri + "/process?INPUT_TEXT=" +
                                     System.Uri.EscapeDataString(sbXML.ToString()) +
                                     "&INPUT_TYPE=SSML&OUTPUT_TYPE=AUDIO&AUDIO=WAVE_FILE&LOCALE=" +
                                     voiceCulture + "&VOICE=" + voiceName;
                           break;
                        default:
                           //TEXT
                           request = uri + "/process?INPUT_TEXT=" + System.Uri.EscapeDataString(wrapper.Text) +
                                     "&INPUT_TYPE=TEXT&OUTPUT_TYPE=AUDIO&AUDIO=WAVE_FILE&LOCALE=" +
                                     voiceCulture + "&VOICE=" + voiceName;
                           break;
                     }

                     if (Util.Constants.DEV_DEBUG)
                        Debug.Log(sbXML);

                     if (Mathf.Abs(wrapper.Volume - 1f) > Common.Util.BaseConstants.FLOAT_TOLERANCE)
                     {
                        request += "&effect_Volume_selected=on&effect_Volume_parameters=amount:" +
                                   wrapper.Volume;
                     }

                     using (UnityWebRequest www = UnityWebRequest.Get(request.Trim()))
                     {
                        if (headers != null)
                        {
                           foreach (System.Collections.Generic.KeyValuePair<string, string> kvp in headers)
                           {
                              www.SetRequestHeader(kvp.Key, kvp.Value);
                           }
                        }

                        www.downloadHandler = new DownloadHandlerBuffer();
                        yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                        if (www.result != UnityWebRequest.Result.ProtocolError && www.result != UnityWebRequest.Result.ConnectionError)
#else
                        if (!www.isHttpError && !www.isNetworkError)
#endif
                        {
                           processAudioFile(wrapper, wrapper.OutputFile, false, www.downloadHandler.data);
                        }
                        else
                        {
                           string errorMessage =
                              "Could not generate the speech: " + wrapper + System.Environment.NewLine +
                              "WWW error: " + www.error;
                           Debug.LogError(errorMessage);
                           onErrorInfo(wrapper, errorMessage);
                        }
                     }
                  }
               }
            }
         }
#else
         Debug.LogError("'Generate' is not supported under WebGL!");
         yield return null;
#endif
      }

      #endregion


      #region Private methods

      private IEnumerator speak(Model.Wrapper wrapper, bool isNative)
      {
         if (wrapper == null)
         {
            Debug.LogWarning("'wrapper' is null!");
         }
         else
         {
            if (string.IsNullOrEmpty(wrapper.Text))
            {
               Debug.LogWarning("'wrapper.Text' is null or empty: " + wrapper);
            }
            else
            {
               if (wrapper.Source == null)
               {
                  Debug.LogWarning("'wrapper.Source' is null: " + wrapper);
               }
               else
               {
                  if (!Common.Util.BaseHelper.isInternetAvailable)
                  {
                     const string errorMessage = "Internet is not available - can't use MaryTTS right now!";
                     Debug.LogError(errorMessage);
                     onErrorInfo(wrapper, errorMessage);
                  }
                  else
                  {
                     if (uri != null)
                     {
                        yield return null; //return to the main process (uid)

                        string voiceCulture = getVoiceCulture(wrapper);
                        string voiceName = getVoiceName(wrapper);

                        silence = false;

                        if (!isNative)
                           onSpeakAudioGenerationStart(wrapper);

                        System.Text.StringBuilder sbXML = new System.Text.StringBuilder();
                        string request;

                        switch (Speaker.Instance.MaryTTSType)
                        {
                           case Model.Enum.MaryTTSType.RAWMARYXML:
                              //RAWMARYXML
                              sbXML.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
                              sbXML.Append("<maryxml version=\"0.5\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://mary.dfki.de/2002/MaryXML\" xml:lang=\"");
                              sbXML.Append(voiceCulture);
                              sbXML.Append("\">");

                              sbXML.Append(prepareProsody(wrapper));

                              sbXML.Append("</maryxml>");

                              request = uri + "/process?INPUT_TEXT=" +
                                        System.Uri.EscapeDataString(sbXML.ToString()) +
                                        "&INPUT_TYPE=RAWMARYXML&OUTPUT_TYPE=AUDIO&AUDIO=WAVE_FILE&LOCALE=" +
                                        voiceCulture + "&VOICE=" + voiceName;
                              break;
                           case Model.Enum.MaryTTSType.EMOTIONML:
                              //EMOTIONML
                              sbXML.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
                              sbXML.Append("<emotionml version=\"1.0\" ");
                              sbXML.Append("xmlns=\"http://www.w3.org/2009/10/emotionml\"");
                              sbXML.Append(" category-set=\"http://www.w3.org/TR/emotion-voc/xml#big6\""); //TODO needed?
                              sbXML.Append(">");

                              sbXML.Append(getValidXML(wrapper.Text));
                              sbXML.Append("</emotionml>");

                              request = uri + "/process?INPUT_TEXT=" +
                                        System.Uri.EscapeDataString(sbXML.ToString()) +
                                        "&INPUT_TYPE=EMOTIONML&OUTPUT_TYPE=AUDIO&AUDIO=WAVE_FILE&LOCALE=" +
                                        voiceCulture + "&VOICE=" + voiceName;
                              break;
                           case Model.Enum.MaryTTSType.SSML:
                              //SSML
                              sbXML.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
                              sbXML.Append("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"");
                              sbXML.Append(voiceCulture);
                              sbXML.Append("\"");
                              sbXML.Append(">");

                              sbXML.Append(prepareProsody(wrapper));

                              sbXML.Append("</speak>");

                              request = uri + "/process?INPUT_TEXT=" +
                                        System.Uri.EscapeDataString(sbXML.ToString()) +
                                        "&INPUT_TYPE=SSML&OUTPUT_TYPE=AUDIO&AUDIO=WAVE_FILE&LOCALE=" +
                                        voiceCulture + "&VOICE=" + voiceName;
                              break;
                           default:
                              //TEXT
                              request = uri + "/process?INPUT_TEXT=" +
                                        System.Uri.EscapeDataString(wrapper.Text) +
                                        "&INPUT_TYPE=TEXT&OUTPUT_TYPE=AUDIO&AUDIO=WAVE_FILE&LOCALE=" +
                                        voiceCulture + "&VOICE=" + voiceName;
                              break;
                        }

                        if (Util.Constants.DEV_DEBUG)
                           Debug.Log(sbXML);

                        if (Mathf.Abs(wrapper.Volume - 1f) > Common.Util.BaseConstants.FLOAT_TOLERANCE)
                           request += "&effect_Volume_selected=on&effect_Volume_parameters=amount:" +
                                      wrapper.Volume;

                        yield return playAudioFile(wrapper, request, wrapper.OutputFile, AudioFileType,
                           isNative, false, headers);
                     }
                  }
               }
            }
         }
      }

      private IEnumerator getVoices()
      {
         System.Collections.Generic.List<string[]> serverVoicesResponse =
            new System.Collections.Generic.List<string[]>();

         if (!Util.Helper.isInternetAvailable)
         {
            const string errorMessage = "Internet is not available - can't use MaryTTS right now!";
            Debug.LogError(errorMessage);
            onErrorInfo(null, errorMessage);
         }
         else
         {
            if (uri != null)
            {
               using (UnityWebRequest www = UnityWebRequest.Get(uri + "/voices"))
               {
                  if (headers != null)
                  {
                     foreach (System.Collections.Generic.KeyValuePair<string, string> kvp in headers)
                     {
                        www.SetRequestHeader(kvp.Key, kvp.Value);
                     }
                  }

                  www.downloadHandler = new DownloadHandlerBuffer();
                  yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                  if (www.result != UnityWebRequest.Result.ProtocolError && www.result != UnityWebRequest.Result.ConnectionError)
#else
                  if (!www.isHttpError && !www.isNetworkError)
#endif
                  {
                     string[] rawVoices = www.downloadHandler.text.Split('\n');
                     foreach (string rawVoice in rawVoices)
                     {
                        try
                        {
                           if (!string.IsNullOrEmpty(rawVoice))
                           {
                              string[] newVoice =
                              {
                                 rawVoice.Split(' ')[0],
                                 rawVoice.Split(' ')[1],
                                 rawVoice.Split(' ')[2]
                              };
                              serverVoicesResponse.Add(newVoice);
                           }
                        }
                        catch (System.Exception ex)
                        {
                           Debug.LogWarning("Problem preparing voice: " + rawVoice + " - " + ex);
                        }
                     }

                     System.Collections.Generic.List<Model.Voice> voices =
                        new System.Collections.Generic.List<Model.Voice>(40);
                     voices.AddRange(serverVoicesResponse.Select(voice => new Model.Voice(voice[0],
                        "MaryTTS voice: " + voice[0], Util.Helper.StringToGender(voice[2]), "unknown",
                        voice[1])));

                     cachedVoices = voices.OrderBy(s => s.Name).ToList();

                     if (Common.Util.BaseConstants.DEV_DEBUG)
                        Debug.Log("Voices read: " + cachedVoices.CTDump());
                  }
                  else
                  {
                     string errorMessage = "Could not get the voices: " + www.error;

                     Debug.LogError(errorMessage);
                     onErrorInfo(null, errorMessage);
                  }
               }
            }

            onVoicesReady();
         }
      }

      private static string prepareProsody(Model.Wrapper wrapper)
      {
         System.Text.StringBuilder sbXML = new System.Text.StringBuilder();

         if (Mathf.Abs(wrapper.Rate - 1f) > Common.Util.BaseConstants.FLOAT_TOLERANCE ||
             Mathf.Abs(wrapper.Pitch - 1f) > Common.Util.BaseConstants.FLOAT_TOLERANCE ||
             Mathf.Abs(wrapper.Volume - 1f) > Common.Util.BaseConstants.FLOAT_TOLERANCE)
         {
            sbXML.Append("<prosody");

            if (Mathf.Abs(wrapper.Rate - 1f) > Common.Util.BaseConstants.FLOAT_TOLERANCE)
            {
               float _rate = wrapper.Rate > 1 ? (wrapper.Rate - 1f) * 0.5f : wrapper.Rate - 1f;

               sbXML.Append(" rate=\"");
               sbXML.Append(_rate >= 0f
                  ? _rate.ToString("+#0%", Util.Helper.BaseCulture)
                  : _rate.ToString("#0%", Util.Helper.BaseCulture));

               sbXML.Append("\"");
            }

            if (Mathf.Abs(wrapper.Pitch - 1f) > Common.Util.BaseConstants.FLOAT_TOLERANCE)
            {
               float _pitch = wrapper.Pitch - 1f;

               sbXML.Append(" pitch=\"");
               sbXML.Append(_pitch >= 0f
                  ? _pitch.ToString("+#0%", Util.Helper.BaseCulture)
                  : _pitch.ToString("#0%", Util.Helper.BaseCulture));

               sbXML.Append("\"");
            }

            if (Mathf.Abs(wrapper.Volume - 1f) > Common.Util.BaseConstants.FLOAT_TOLERANCE)
            {
               sbXML.Append(" volume=\"");
               sbXML.Append((100 * wrapper.Volume).ToString("#0", Util.Helper.BaseCulture));

               sbXML.Append("\"");
            }

            sbXML.Append(">");

            sbXML.Append(wrapper.Text);

            sbXML.Append("</prosody>");
         }
         else
         {
            sbXML.Append(wrapper.Text);
         }

         //Debug.Log(sbXML.ToString());
         return getValidXML(sbXML.ToString());
      }

      private static string getVoiceCulture(Model.Wrapper wrapper)
      {
         if (string.IsNullOrEmpty(wrapper.Voice?.Culture))
         {
            if (Util.Config.DEBUG)
               Debug.LogWarning(
                  "'wrapper.Voice' or 'wrapper.Voice.Culture' is null! Using the 'default' English voice.");

            //always use English as fallback
            return "en-US";
         }

         return wrapper.Voice?.Culture;
      }

      #endregion


      #region Editor-only methods

#if UNITY_EDITOR

      public override void GenerateInEditor(Model.Wrapper wrapper)
      {
         Debug.LogError("'GenerateInEditor' is not supported for MaryTTS!");
      }

      public override void SpeakNativeInEditor(Model.Wrapper wrapper)
      {
         Debug.LogError("'SpeakNativeInEditor' is not supported for MaryTTS!");
      }

      private void getVoicesInEditor()
      {
         System.Collections.Generic.List<string[]> serverVoicesResponse =
            new System.Collections.Generic.List<string[]>();
         if (!Common.Util.BaseHelper.isInternetAvailable)
         {
            const string errorMessage = "Internet is not available - can't use MaryTTS right now!";
            Debug.LogError(errorMessage);
         }
         else
         {
            if (uri != null)
            {
               try
               {
                  System.Net.ServicePointManager.ServerCertificateValidationCallback =
                     Util.Helper.RemoteCertificateValidationCallback;

                  using (System.Net.WebClient client = new Common.Util.CTWebClient())
                  {
                     if (headers != null)
                     {
                        foreach (System.Collections.Generic.KeyValuePair<string, string> kvp in headers)
                        {
                           client.Headers.Add(kvp.Key, kvp.Value);
                        }
                     }

                     using (System.IO.Stream stream = client.OpenRead(uri + "/voices"))
                     {
                        if (stream != null)
                        {
                           using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
                           {
                              string content = reader.ReadToEnd();

                              if (Util.Config.DEBUG)
                                 Debug.Log(content);

                              string[] rawVoices = content.Split('\n');
                              foreach (string rawVoice in rawVoices)
                              {
                                 try
                                 {
                                    if (!string.IsNullOrEmpty(rawVoice))
                                    {
                                       string[] newVoice =
                                       {
                                          rawVoice.Split(' ')[0],
                                          rawVoice.Split(' ')[1],
                                          rawVoice.Split(' ')[2]
                                       };
                                       serverVoicesResponse.Add(newVoice);
                                    }
                                 }
                                 catch (System.Exception ex)
                                 {
                                    Debug.LogWarning("Problem preparing voice: " + rawVoice + " - " + ex);
                                 }
                              }

                              System.Collections.Generic.List<Model.Voice> voices =
                                 new System.Collections.Generic.List<Model.Voice>(40);
                              voices.AddRange(serverVoicesResponse.Select(voice =>
                                 new Model.Voice(voice[0], "MaryTTS voice: " + voice[0],
                                    Util.Helper.StringToGender(voice[2]), "unknown", voice[1])));

                              cachedVoices = voices.OrderBy(s => s.Name).ToList();

                              if (Common.Util.BaseConstants.DEV_DEBUG)
                                 Debug.Log("Voices read: " + cachedVoices.CTDump());
                           }
                        }
                        else
                        {
                           Debug.LogError("Stream to voices URI was null: " + uri + "/voices");
                        }
                     }
                  }
               }
               catch (System.Exception ex)
               {
                  Debug.LogError(ex);
               }
            }

            onVoicesReady();
         }
      }
#endif

      #endregion
   }
}
// © 2016-2020 crosstales LLC (https://www.crosstales.com)