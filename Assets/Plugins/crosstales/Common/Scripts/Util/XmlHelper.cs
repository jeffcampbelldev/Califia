﻿using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>Helper-class for XML.</summary>
   public static class XmlHelper
   {
#if !UNITY_WEBGL || UNITY_EDITOR

      /// <summary>Serialize an object to an XML-file.</summary>
      /// <param name="obj">Object to serialize.</param>
      /// <param name="filename">File name of the XML.</param>
      public static void SerializeToFile<T>(T obj, string filename)
      {
         if (null == obj)
            throw new System.ArgumentNullException(nameof(obj));

         if (filename == null)
            throw new System.ArgumentNullException(nameof(filename));

         try
         {
            System.IO.File.WriteAllText(filename, SerializeToString(obj));
         }
         catch (System.Exception ex)
         {
            Debug.LogError($"Could not serialize the object to a file: {ex}");
         }
      }

      /// <summary>Deserialize a XML-file to an object.</summary>
      /// <param name="filename">XML-file of the object</param>
      /// <param name="skipBOM">Skip BOM (optional, default: false)</param>
      /// <returns>Object</returns>
      public static T DeserializeFromFile<T>(string filename, bool skipBOM = false)
      {
         if (filename == null)
            throw new System.ArgumentNullException(nameof(filename));

         try
         {
            if (System.IO.File.Exists(filename))
               return DeserializeFromString<T>(System.IO.File.ReadAllText(filename), skipBOM);

            Debug.LogError($"File doesn't exist: {filename}");
         }
         catch (System.Exception ex)
         {
            Debug.LogError($"Could not deserialize the object from a file: {ex}");
         }

         return default;
      }

#endif

      /// <summary>Serialize an object to an XML-string.</summary>
      /// <param name="obj">Object to serialize.</param>
      /// <returns>Object as XML-stringValid path</returns>
      public static string SerializeToString<T>(T obj)
      {
         if (null == obj)
            throw new System.ArgumentNullException(nameof(obj));

         try
         {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();

            System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(obj.GetType());
            System.Xml.XmlTextWriter xmlTextWriter = new System.Xml.XmlTextWriter(ms, System.Text.Encoding.UTF8);
            xs.Serialize(xmlTextWriter, obj);

            ms = (System.IO.MemoryStream)xmlTextWriter.BaseStream;
            return System.Text.Encoding.UTF8.GetString(ms.ToArray());
         }
         catch (System.Exception ex)
         {
            Debug.LogError($"Could not serialize the object to a string: {ex}");
         }

         return string.Empty;
      }

      /// <summary>Deserialize a XML-string to an object.</summary>
      /// <param name="xmlAsString">XML of the object</param>
      /// <param name="skipBOM">Skip BOM (optional, default: true)</param>
      /// <returns>Object</returns>
      public static T DeserializeFromString<T>(string xmlAsString, bool skipBOM = true)
      {
         if (string.IsNullOrEmpty(xmlAsString))
            throw new System.ArgumentNullException(nameof(xmlAsString));

         try
         {
            System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(T));

            using (System.IO.StringReader sr = new System.IO.StringReader(xmlAsString.Trim()))
            {
               if (skipBOM)
                  sr.Read(); //skip BOM

               return (T)xs.Deserialize(sr);
            }
         }
         catch (System.Exception ex)
         {
            Debug.LogError($"Could not deserialize the object from a string: {ex}");
         }

         return default;
      }

      /// <summary>Deserialize a Unity XML resource (TextAsset) to an object.</summary>
      /// <param name="resourceName">Name of the resource</param>
      /// <param name="skipBOM">Skip BOM (optional, default: true)</param>
      /// <returns>Object</returns>
      public static T DeserializeFromResource<T>(string resourceName, bool skipBOM = true)
      {
         if (string.IsNullOrEmpty(resourceName))
            throw new System.ArgumentNullException(nameof(resourceName));

         // Load the resource
         TextAsset xml = Resources.Load(resourceName) as TextAsset;

         return xml != null ? DeserializeFromString<T>(xml.text, skipBOM) : default;
      }
   }
}
// © 2014-2020 crosstales LLC (https://www.crosstales.com)