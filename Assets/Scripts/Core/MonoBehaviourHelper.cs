using UnityEngine;

namespace TopDownGame.Core
{
    internal sealed class MonoBehaviourHelper : MonoBehaviour
    {
        private static MonoBehaviourHelper instance;

        public static MonoBehaviourHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject helperObject = new GameObject("MonoBehaviourHelper");
                    DontDestroyOnLoad(helperObject);
                    instance = helperObject.AddComponent<MonoBehaviourHelper>();
                }

                return instance;
            }
        }
    }
}
