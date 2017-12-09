﻿using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Advobot.UILauncher.Classes
{
	public class Holder<T> : INotifyPropertyChanged
	{
		private T _HeldObject;
		public T HeldObject
		{
			get => _HeldObject;
			internal set
			{
				this._HeldObject = value;
				NotifyPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] string name = "")
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}
