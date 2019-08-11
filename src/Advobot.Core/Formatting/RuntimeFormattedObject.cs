﻿namespace Advobot.Formatting
{
	/// <summary>
	/// Gets around the limitation of not being able to use a variable as a format in <see cref="string.Format(string, object)"/>.
	/// </summary>
	public readonly struct RuntimeFormattedObject
	{
		/// <summary>
		/// The value to format.
		/// </summary>
		public object Value { get; }
		/// <summary>
		/// The format to use for the value.
		/// </summary>
		public string Format { get; }

		private RuntimeFormattedObject(object value, string? format)
		{
			Value = value;
			Format = format ?? "G";
		}

		/// <summary>
		/// Returns <see cref="Value"/> as a string.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
			=> Value.ToString();

		/// <summary>
		/// Creates an instance of <see cref="RuntimeFormattedObject"/> with no format.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static RuntimeFormattedObject None(object value)
			=> new RuntimeFormattedObject(value, null);
		/// <summary>
		/// Creates an instance of <see cref="RuntimeFormattedObject"/> with the specified format.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="format"></param>
		/// <returns></returns>
		public static RuntimeFormattedObject Create(object value, string format)
			=> new RuntimeFormattedObject(value, format);
	}
}