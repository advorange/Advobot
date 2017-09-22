﻿using Advobot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Advobot
{
	public static class ExtendedActions
	{
		/// <summary>
		/// Locks the list then adds the object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="obj"></param>
		public static void ThreadSafeAdd<T>(this List<T> list, T obj)
		{
			lock (list)
			{
				list.Add(obj);
			}
		}
		/// <summary>
		/// Locks the input list then concats the second list to it.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="objs"></param>
		/// <returns></returns>
		public static IEnumerable<T> ThreadSafeConcat<T>(this List<T> list, IEnumerable<T> objs)
		{
			lock (list)
			{
				return list.Concat(objs);
			}
		}
		/// <summary>
		/// Locks the input list then adds the second list to it.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="objs"></param>
		public static void ThreadSafeAddRange<T>(this List<T> list, IEnumerable<T> objs)
		{
			lock (list)
			{
				list.AddRange(objs);
			}
		}
		/// <summary>
		/// Locks the list then removes the object from the list.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static bool ThreadSafeRemove<T>(this List<T> list, T obj)
		{
			lock (list)
			{
				return list.Remove(obj);
			}
		}
		/// <summary>
		/// Locks the list then removes all objects which match the predicate.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="match"></param>
		/// <returns></returns>
		public static int ThreadSafeRemoveAll<T>(this List<T> list, Predicate<T> match)
		{
			lock (list)
			{
				return list.RemoveAll(match);
			}
		}

		/// <summary>
		/// Utilizes <see cref="StringComparison.OrdinalIgnoreCase"/> to check if two strings are the same.
		/// </summary>
		/// <param name="str1"></param>
		/// <param name="str2"></param>
		/// <returns></returns>
		public static bool CaseInsEquals(this string str1, string str2)
		{
			//null == null
			if (str1 == null)
			{
				return str2 == null;
			}
			//x != null
			else if (str2 == null)
			{
				return false;
			}
			//x ?= x
			else
			{
				return str1.Equals(str2, StringComparison.OrdinalIgnoreCase);
			}
		}
		/// <summary>
		/// Utilizes <see cref="StringComparison.OrdinalIgnoreCase"/> to check if a string contains a search string.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="search"></param>
		/// <returns></returns>
		public static bool CaseInsContains(this string source, string search)
		{
			if (source == null || search == null)
			{
				return false;
			}
			else
			{
				return source.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
			}
		}
		/// <summary>
		/// Utilizes <see cref="StringComparison.OrdinalIgnoreCase"/> to return the index of a search string.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="search"></param>
		/// <param name="position"></param>
		/// <returns></returns>
		public static bool CaseInsIndexOf(this string source, string search, out int position)
		{
			position = -1;
			if (source == null || search == null)
			{
				return false;
			}
			else
			{
				return (position = source.IndexOf(search, StringComparison.OrdinalIgnoreCase)) >= 0;
			}
		}
		/// <summary>
		/// Utilizes <see cref="StringComparison.OrdinalIgnoreCase"/> to check if a string ends with a search string.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="search"></param>
		/// <returns></returns>
		public static bool CaseInsStartsWith(this string source, string search)
		{
			if (source == null || search == null)
			{
				return false;
			}
			else
			{
				return source.StartsWith(search, StringComparison.OrdinalIgnoreCase);
			}
		}
		/// <summary>
		/// Utilizes <see cref="StringComparison.OrdinalIgnoreCase"/> to check if a string ends with a search string.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="search"></param>
		/// <returns></returns>
		public static bool CaseInsEndsWith(this string source, string search)
		{
			if (source == null || search == null)
			{
				return false;
			}
			else
			{
				return source.EndsWith(search, StringComparison.OrdinalIgnoreCase);
			}
		}
		/// <summary>
		/// Returns the string with the oldValue replaced with the newValue case insensitively.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="oldValue"></param>
		/// <param name="newValue"></param>
		/// <returns></returns>
		public static string CaseInsReplace(this string source, string oldValue, string newValue)
		{
			var sb = new StringBuilder();
			var previousIndex = 0;
			var index = source.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);
			while (index != -1)
			{
				sb.Append(source.Substring(previousIndex, index - previousIndex));
				sb.Append(newValue);
				index += oldValue.Length;

				previousIndex = index;
				index = source.IndexOf(oldValue, index, StringComparison.OrdinalIgnoreCase);
			}
			return sb.Append(source.Substring(previousIndex)).ToString();
		}
		/// <summary>
		/// Utilizes <see cref="CaseInsEquals(string, string)"/> to check if every string is the same.
		/// </summary>
		/// <param name="enumerable"></param>
		/// <returns></returns>
		public static bool CaseInsEverythingSame(this IEnumerable<string> enumerable)
		{
			var array = enumerable.ToArray();
			for (int i = 1; i < array.Length; ++i)
			{
				if (!array[i - 1].CaseInsEquals(array[i]))
				{
					return false;
				}
			}
			return true;
		}
		/// <summary>
		/// Utilizes <see cref="StringComparer.OrdinalIgnoreCase"/> to see if the search string is in the enumerable.
		/// </summary>
		/// <param name="enumerable"></param>
		/// <param name="search"></param>
		/// <returns></returns>
		public static bool CaseInsContains(this IEnumerable<string> enumerable, string search)
		{
			if (enumerable.Any())
			{
				return enumerable.Contains(search, StringComparer.OrdinalIgnoreCase);
			}
			return false;
		}
		/// <summary>
		/// Verifies all characters in the string have a value of a less than the upperlimit.
		/// </summary>
		/// <param name="str"></param>
		/// <param name="upperLimit"></param>
		/// <returns></returns>
		public static bool AllCharactersAreWithinUpperLimit(this string str, int upperLimit)
		{
			foreach (var c in str ?? String.Empty)
			{
				if (c > upperLimit)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Returns the enum's name as a string.
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		public static string EnumName(this Enum e)
		{
			return Enum.GetName(e.GetType(), e);
		}
		/// <summary>
		/// Splits the input at spaces unless the space is inside a quote.
		/// </summary>
		/// <param name="inputString"></param>
		/// <param name="inputChar"></param>
		/// <returns></returns>
		public static string[] SplitExceptInQuotes(this string inputString)
		{
			if (inputString == null)
			{
				return null;
			}

			return inputString.Split('"').Select((element, index) =>
			{
				if (index % 2 == 0)
				{
					return element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				}
				else
				{
					return new[] { element };
				}
			}).SelectMany(x => x).Where(x => !String.IsNullOrWhiteSpace(x)).ToArray();
		}

		/// <summary>
		/// Returns the count of characters equal to \r or \n.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static int CountLineBreaks(this string str)
		{
			return str?.Count(x => x == '\r' || x == '\n') ?? 0;
		}
		/// <summary>
		/// Counts how many times something that implements <see cref="ITime"/> has occurred within a given timeframe.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="timeList"></param>
		/// <param name="timeFrame"></param>
		/// <returns></returns>
		public static int CountItemsInTimeFrame<T>(this List<T> timeList, int timeFrame = 0) where T : IHasTime
		{
			lock (timeList)
			{
				//No timeFrame given means that it's a spam prevention that doesn't check against time, like longmessage or mentions
				var listLength = timeList.Count;
				if (timeFrame <= 0 || listLength < 2)
				{
					return listLength;
				}

				//If there is a timeFrame then that means to gather the highest amount of messages that are in the time frame
				var count = 0;
				for (int i = 0; i < listLength; ++i)
				{
					for (int j = i + 1; j < listLength; ++j)
					{
						if ((int)timeList[j].GetTime().Subtract(timeList[i].GetTime()).TotalSeconds < timeFrame)
						{
							continue;
						}
						//Optimization by checking if the time difference between two numbers is too high to bother starting at j - 1
						else if ((int)timeList[j].GetTime().Subtract(timeList[j - 1].GetTime()).TotalSeconds > timeFrame)
						{
							i = j;
						}
						break;
					}
				}

				//Remove all that are older than the given timeframe (with an added 1 second margin)
				var nowTime = DateTime.UtcNow;
				for (int i = listLength - 1; i >= 0; --i)
				{
					if ((int)nowTime.Subtract(timeList[i].GetTime()).TotalSeconds < timeFrame + 1)
					{
						continue;
					}

					timeList.RemoveRange(0, i + 1);
					break;
				}

				return count;
			}
		}

		/// <summary>
		/// Takes a variable number of integers and cuts the list the smallest one (including the list's length).
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="x"></param>
		/// <returns></returns>
		public static List<T> GetUpToAndIncludingMinNum<T>(this List<T> list, params int[] x)
		{
			return list.GetRange(0, Math.Max(0, Math.Min(list.Count, x.Min())));
		}
		/// <summary>
		/// Removes <see cref="ITime"/> objects where their time is below <see cref="DateTime.UtcNow"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="inputList"></param>
		/// <returns></returns>
		public static List<T> GetOutTimedObjects<T>(this List<T> inputList) where T : IHasTime
		{
			if (inputList == null)
			{
				return null;
			}

			var eligibleToBeGotten = inputList.Where(x => x.GetTime() < DateTime.UtcNow).ToList();
			foreach (var obj in eligibleToBeGotten)
			{
				inputList.ThreadSafeRemove(obj);
			}
			return eligibleToBeGotten;
		}
		/// <summary>
		/// Removes <see cref="ITime"/> key value pairs where their time is below <see cref="DateTime.UtcNow"/>.
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="inputDict"></param>
		/// <returns></returns>
		public static Dictionary<TKey, TValue> GetOutTimedObjects<TKey, TValue>(this Dictionary<TKey, TValue> inputDict) where TValue : IHasTime
		{
			if (inputDict == null)
			{
				return null;
			}

			var elligibleToBeGotten = inputDict.Where(x => x.Value.GetTime() < DateTime.UtcNow).ToList();
			foreach (var value in elligibleToBeGotten)
			{
				inputDict.Remove(value.Key);
			}
			return elligibleToBeGotten.ToDictionary(x => x.Key, x => x.Value);
		}

		/// <summary>
		/// Returns the service from the provider with the supplied type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="provider"></param>
		/// <returns></returns>
		public static T GetService<T>(this IServiceProvider provider)
		{
			return (T)provider.GetService(typeof(T));
		}
		/// <summary>
		/// Returns the attribute from the class type with the supplied type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="classType"></param>
		/// <returns></returns>
		public static T GetCustomAttribute<T>(this Type classType) where T : Attribute
		{
			return (T)classType.GetCustomAttribute(typeof(T));
		}
	}
}
