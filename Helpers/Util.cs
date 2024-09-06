using System.IO.Pipes;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace SensorsInterface.Helpers;

public static class Util
{
	static Util()
	{
		var dynamicMethod = new DynamicMethod("Memset", MethodAttributes.Public | MethodAttributes.Static,
			CallingConventions.Standard,
			null, new[] { typeof(IntPtr), typeof(byte), typeof(int) }, typeof(Util), true);

		var generator = dynamicMethod.GetILGenerator();
		generator.Emit(OpCodes.Ldarg_0);
		generator.Emit(OpCodes.Ldarg_1);
		generator.Emit(OpCodes.Ldarg_2);
		generator.Emit(OpCodes.Initblk);
		generator.Emit(OpCodes.Ret);

		MemsetDelegate = (Action<IntPtr, byte, int>)dynamicMethod.CreateDelegate(typeof(Action<IntPtr, byte, int>));
	}

	public static void Memset(byte[] array, byte what, int length)
	{
		var gcHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
		MemsetDelegate(gcHandle.AddrOfPinnedObject(), what, length);
		gcHandle.Free();
	}

	public static void ForMemset(byte[] array, byte what, int length)
	{
		for (int i = 0; i < length; i++)
		{
			array[i] = what;
		}
	}

	private static Action<IntPtr, byte, int> MemsetDelegate;
	
	public static void WaitForConnectionEx(this NamedPipeServerStream stream)
	{
		Exception e = null;
		AutoResetEvent connectEvent = new AutoResetEvent(false);
		stream.BeginWaitForConnection(ar =>
		{
			try
			{
				stream.EndWaitForConnection(ar);
			}
			catch (Exception er)
			{
				e = er;
			}
			connectEvent.Set();
		}, null);
		Thread.Sleep(2000);
		if (e!=null)
			stream.Close();
		if (e != null)
			throw e; // rethrow exception
	}
	
	public static T FindChild<T>(DependencyObject parent, string childName)
		where T : DependencyObject
	{    
		// Confirm parent and childName are valid. 
		if (parent == null) return null;

		T foundChild = null;

		int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
		for (int i = 0; i < childrenCount; i++)
		{
			var child = VisualTreeHelper.GetChild(parent, i);
			// If the child is not of the request child type child
			T childType = child as T;
			if (childType == null)
			{
				// recursively drill down the tree
				foundChild = FindChild<T>(child, childName);

				// If the child is found, break so we do not overwrite the found child. 
				if (foundChild != null) break;
			}
			else if (!string.IsNullOrEmpty(childName))
			{
				var frameworkElement = child as FrameworkElement;
				// If the child's name is set for search
				if (frameworkElement != null && frameworkElement.Name == childName)
				{
					// if the child's name is of the request name
					foundChild = (T)child;
					break;
				}
			}
			else
			{
				// child element found.
				foundChild = (T)child;
				break;
			}
		}

		return foundChild;
	}

	public static K FindKeyByIndex<K, V>(this Dictionary<K, V> dictionary, int index, K defaultObject = default)
	{
		return FindByIndex(dictionary, index).Key ?? defaultObject;
	}
	public static V FindValueByIndex<K, V>(this Dictionary<K, V> dictionary, int index, V defaultObject = default)
	{
		return FindByIndex(dictionary, index).Value ?? defaultObject;
	}
	
	public static KeyValuePair<K,V> FindByIndex<K, V>(this Dictionary<K, V> dictionary, int index, KeyValuePair<K,V> defaultObject = default)
	{
		int i = 0;
		foreach (var pair in dictionary.Where(pair => i++ == index))
			return pair;
		return defaultObject;
	}
	
	public static int FindIndexByKey<K, V>(this Dictionary<K, V> dictionary, K key)
	{
		int i = 0;
		foreach (var pair in dictionary)
		{
			if (pair.Key.ToString() == key.ToString())
				return i;
			i++;
		}
		return -1;
	}
}