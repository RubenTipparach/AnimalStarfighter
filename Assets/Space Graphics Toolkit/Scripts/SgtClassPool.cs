using UnityEngine;
using System.Collections.Generic;

// This class can be used to pool normal C# classes
public static class SgtClassPool<T>
	where T : class
{
	private static List<T> pool = new List<T>();
	
	public static int Count
	{
		get
		{
			return pool.Count;
		}
	}
	
	static SgtClassPool()
	{
		if (typeof(T).IsSubclassOf(typeof(Object)))
		{
			Debug.LogError("Attempting to use " + typeof(T).Name + " with SgtClassPool. Use SgtObjectPool instead.");
		}
	}
	
	public static T Add(T entry)
	{
		return Add(entry, null);
	}
	
	public static T Add(T element, System.Action<T> onAdd)
	{
		if (element != null)
		{
			if (onAdd != null)
			{
				onAdd(element);
			}
			
			pool.Add(element);
		}
		
		return null;
	}
	
	public static T Pop()
	{
		if (pool.Count > 0)
		{
			var index   = pool.Count - 1;
			var element = pool[index];
			
			pool.RemoveAt(index);
			
			return element;
		}
		
		return null;
	}
}