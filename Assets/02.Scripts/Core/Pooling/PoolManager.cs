using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using MiniExtractionShooter.Core;

public class PoolManager : Singleton<PoolManager>
{
    private Dictionary<string, object> pools = new Dictionary<string, object>();

    public void CreatePool<T>(T prefab, int initCount, Transform parent = null) where T : Component
    {
        if (prefab == null) return;

        string key = prefab.name;
        if (pools.ContainsKey(key)) return;
        ObjectPool<T> pool = new ObjectPool<T>(prefab, initCount, parent);
        pools.Add(key, pool);
    }


    public T GetFromPool<T>(T prefab) where T : Component
    {
        if (prefab == null) return null;

        if (!pools.TryGetValue(prefab.name, out var box))
        {
            return null;
        }
        var pool = box as ObjectPool<T>;

        if (pool != null)
        {
            return pool.Dequeue();
        }
        return null;
    }

    public void ReturnPool<T>(T instance, bool isActive = false) where T : Component
    {
        if (instance == null) return;

        if (!pools.TryGetValue(instance.gameObject.name, out var box))
        {
            Destroy(instance.gameObject);
            return;
        }

        var pool = box as ObjectPool<T>;

        if (pool != null)
        {
            pool.Enqueue(instance, isActive);
        }
    }

    public void ClearAllPoolsKey()
    {
        pools.Clear();
    }

    public void ReturnAfterDelay<T>(T instance, float delay) where T : Component
    {
        if (instance == null) return;
        StartCoroutine(ReturnAfterDelayCoroutine(instance, delay));
    }

    private System.Collections.IEnumerator ReturnAfterDelayCoroutine<T>(T instance, float delay) where T : Component
    {
        yield return new WaitForSeconds(delay);
        ReturnPool(instance, false);
    }
}
