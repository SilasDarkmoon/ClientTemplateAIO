using System.Collections.Generic;
using UnityEngine;

namespace UnityEngineEx.UI
{
    [AddComponentMenu("UI/GameObject Pool", 35)]
    class GameObjectPool : MonoBehaviour
    {
        private Dictionary<string, Queue<GameObject>> m_GameObjectPoolDict = new Dictionary<string, Queue<GameObject>>();
        public GameObject GetFromPool(string tag)
        {
            if (m_GameObjectPoolDict.ContainsKey(tag))
            {
                var queue = m_GameObjectPoolDict[tag];
                if (queue.Count == 0) return null;
                var go = queue.Dequeue();
                return go;
            }
            else
            {
                return null;
            }
        }
        public void ReturnToPool(GameObject go, string tag)
        {
            if (!m_GameObjectPoolDict.ContainsKey(tag)) m_GameObjectPoolDict[tag] = new Queue<GameObject>();
            //if (go.activeSelf) go.SetActiveWithValidation(false);
            m_GameObjectPoolDict[tag].Enqueue(go);
        }
        public void Clear()
        {
            int count = transform.childCount;
            foreach (var queue in m_GameObjectPoolDict)
            {
                foreach (var item in queue.Value)
                {
                    Object.Destroy(item);
                }
            }
            m_GameObjectPoolDict.Clear();
        }
    }
}