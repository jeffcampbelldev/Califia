using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>Base-class for all singletons.</summary>
   public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
   {
      [Tooltip("Don't destroy gameobject during scene switches (default: true)."), SerializeField] private bool dontDestroy = true;

      // Check to see if we're about to be destroyed.
      private static readonly object lockObj = new object();

      protected static T instance;
      private static bool isQuitting;

      /// <summary>Returns the singleton instance of this class.</summary>
      /// <returns>Singleton instance of this class.</returns>
      public static T Instance
      {
         get
         {
            lock (lockObj)
            {
               if (instance != null)
                  return instance;

               // Search for existing instance.
               instance = (T)FindObjectOfType(typeof(T));

               // Create new instance if one doesn't already exist.
               if (!isQuitting && instance == null)
                  instance = new GameObject($"{typeof(T).Name} (Singleton)").AddComponent<T>();

               return instance;
            }
         }

         protected set
         {
            lock (lockObj)
               instance = value;
         }
      }

      /// <summary>Don't destroy gameobject during scene switches.</summary>
      public bool DontDestroy
      {
         get => dontDestroy;
         set => dontDestroy = value;
      }

      /// <summary>Resets this object.</summary>
      //[RuntimeInitializeOnLoadMethod]
      public static void DeleteInstance()
      {
         //Debug.LogWarning($"DELETE: {Instance.name}");
         Instance = null;
      }


      protected virtual void Awake()
      {
         Util.BaseHelper.ApplicationIsPlaying = Application.isPlaying; //needed to enforce the right mode
         isQuitting = false;

         if (instance == null)
         {
            Instance = GetComponent<T>();

            if (!Util.BaseHelper.isEditorMode && dontDestroy)
               DontDestroyOnLoad(transform.root.gameObject);

            //Debug.LogWarning($"Using new instance: {Instance.name}", this);
         }

         /*
         else
         {
            if (!Util.BaseHelper.isEditorMode && dontDestroy && instance != this)
            {
               //string s_Name = typeof(T) + " (Singleton)";
               //Debug.LogWarning("Only one active instance of '" + s_Name + "' allowed in all scenes!" + System.Environment.NewLine + "This object will now be destroyed.", this);
               Destroy(gameObject, 0.1f);
            }

            //Debug.LogWarning($"Using old instance: {Instance.name}", this);
         }
         */
      }

      protected virtual void OnDestroy()
      {
         if (instance == this)
            DeleteInstance();
      }


      protected virtual void OnApplicationQuit()
      {
         Util.BaseHelper.ApplicationIsPlaying = false;
         isQuitting = true;
      }
   }
}