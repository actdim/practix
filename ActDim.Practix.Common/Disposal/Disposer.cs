using System;
using System.Collections.Generic;

namespace ActDim.Practix.Disposal
{
	/// <summary>
	/// Class that does the pattern of checking for null, disposing the object and setting its variable to null.
	/// </summary>
	public static class Disposer
	{
		/// <summary>
		/// Disposes the object if needed and sets its variable to null.
		/// </summary>
		public static void Dispose<T>(ref T disposable)
		where
			T: class, IDisposable
		{
			var content = disposable;
			if (content != null)
			{
				disposable = null;
				content.Dispose();
			}
		}

		/// <summary>
		/// Disposes all the items in a given collection.
		/// </summary>
		public static void DisposeCollection<T>(ref T collectionOfDisposables)
		where
			T: class, IEnumerable<IDisposable>
		{
			var collection = collectionOfDisposables;
			if (collection == null)
			{
				return;
			}

			collectionOfDisposables = null;
			foreach (var item in collection)
			{
				item.Dispose();
			}
		}
	}
}
