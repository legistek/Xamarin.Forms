using System;
using System.Collections.Generic;
using System.Text;

namespace Xamarin.Forms
{
	public class RelativeSourceBinding
	{
		static RelativeSourceBinding _self;
		static RelativeSourceBinding _templatedParent;
		Type _ancestorType = null;
		int _ancestorLevel = 1;

		public RelativeSourceBinding()
		{
		}

		public RelativeSourceBinding(RelativeSourceBindingMode mode)
		{
			this.Mode = mode;
		}

		public RelativeSourceBindingMode Mode
		{
			get;
			set;
		}

		public Type AncestorType
		{
			get => _ancestorType;
			set
			{
				_ancestorType = value;
				if (_ancestorType != null)
					this.Mode = RelativeSourceBindingMode.FindAncestor;
			}
		}

		public int AncestorLevel
		{
			get => _ancestorLevel;
			set
			{
				_ancestorLevel = value;
				if (_ancestorLevel > 0)
					this.Mode = RelativeSourceBindingMode.FindAncestor;
			}
		}

		public static RelativeSourceBinding Self
		{
			get
			{
				return _self ?? (_self = new RelativeSourceBinding(RelativeSourceBindingMode.Self));
			}
		}

		public static RelativeSourceBinding TemplatedParent
		{
			get
			{
				return _templatedParent ?? (_templatedParent = new RelativeSourceBinding(RelativeSourceBindingMode.TemplatedParent));
			}
		}
	}
}
