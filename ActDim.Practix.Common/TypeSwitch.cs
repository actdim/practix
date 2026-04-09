#region License

/*
 * Copyright © 2002-2011 Paul Borodaev.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

using System;

namespace ActDim.Practix
{
	public static class TypeSwitch
	{
		public class CaseInfo
		{
			public bool IsDefault { get; set; }
			public Type Target { get; set; }
			public Action<object> Action { get; set; }
		}

		public static void Do(object source, params CaseInfo[] cases)
		{
			var type = source.GetType();
			foreach (var entry in cases)
			{
				//type.IsAssignableFrom(entry.Target)
				if (entry.IsDefault || type == entry.Target || type.IsSubclassOf(entry.Target))
				{
					entry.Action(source);
					break;
				}
			}
		}

		public static CaseInfo Case<T>(Action action)
		{
			return new CaseInfo
			{
				Action = x => action(),
				Target = typeof(T)
			};
		}

		public static CaseInfo Case<T>(Action<T> action)
		{
			return new CaseInfo
			{
				Action = (x) => action((T)x),
				Target = typeof(T)
			};
		}

		public static CaseInfo Default(Action action)
		{
			return new CaseInfo
			{
				Action = x => action(),
				IsDefault = true
			};
		}
	}
}
