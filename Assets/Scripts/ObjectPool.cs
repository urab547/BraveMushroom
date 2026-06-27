using UnityEngine;
using System.Collections.Generic;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool instance;

    private Dictionary<int, Queue<GameObject>> pools = new Dictionary<int, Queue<GameObject>>();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (instance != null)
            return instance.GetFromPool(prefab, position, rotation);
        return Instantiate(prefab, position, rotation);
    }

    public static void Despawn(GameObject obj)
    {
        if (instance != null)
            instance.ReturnToPool(obj);
        else
            Destroy(obj);
    }

    private GameObject GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        int key = prefab.GetInstanceID();

        if (pools.TryGetValue(key, out var queue))
        {
            while (queue.Count > 0)
            {
                GameObject obj = queue.Dequeue();
                if (obj == null) continue;
                obj.transform.SetPositionAndRotation(position, rotation);
                obj.SetActive(true);
                return obj;
            }
        }

        GameObject newObj = Instantiate(prefab, position, rotation);
        PoolTag tag = newObj.AddComponent<PoolTag>();
        tag.prefabId = key;
        return newObj;
    }

    private void ReturnToPool(GameObject obj)
    {
        PoolTag tag = obj.GetComponent<PoolTag>();
        if (tag == null)
        {
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        if (!pools.TryGetValue(tag.prefabId, out var queue))
        {
            queue = new Queue<GameObject>();
            pools[tag.prefabId] = queue;
        }
        queue.Enqueue(obj);
    }
}

public class PoolTag : MonoBehaviour
{
    [HideInInspector]
    public int prefabId;
}
