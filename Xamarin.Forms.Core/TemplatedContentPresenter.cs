using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xamarin.Forms
{
	public class TemplatedContentPresenter : Layout
	{
		public TemplatedContentPresenter() : base()
		{		
		}

		#region object Content dependency property
		public static BindableProperty ContentProperty = BindableProperty.Create(
			"Content",
			typeof(object),
			typeof(TemplatedContentPresenter),
			propertyChanged: OnContentChanged);
		public object Content
		{
			get
			{
				return (object)GetValue(ContentProperty);
			}
			set
			{
				SetValue(ContentProperty, value);
			}
		}
		private static void OnContentChanged(BindableObject obj, object oldValue, object newValue)
		{
			var cp = obj as TemplatedContentPresenter;
			cp?.ApplyDataTemplate(newValue, cp.ContentTemplate);
		}
		#endregion

		#region DataTemplate ContentTemplate dependency property
		public static BindableProperty ContentTemplateProperty = BindableProperty.Create(
			"ContentTemplate",
			typeof(DataTemplate),
			typeof(TemplatedContentPresenter),
			propertyChanged: OnContentTemplateChanged);
		public DataTemplate ContentTemplate
		{
			get
			{
				return (DataTemplate)GetValue(ContentTemplateProperty);
			}
			set
			{
				SetValue(ContentTemplateProperty, value);
			}
		}
		private static void OnContentTemplateChanged(BindableObject obj, object oldValue, object newValue)
		{
			var cp = obj as TemplatedContentPresenter;
			cp?.ApplyDataTemplate(cp.Content, newValue as DataTemplate);
		}
		#endregion

		#region DataTemplateSelector ContentTemplateSelector dependency property
		public static BindableProperty ContentTemplateSelectorProperty = BindableProperty.Create(
			"ContentTemplateSelector",
			typeof(DataTemplateSelector),
			typeof(ContentPresenter),
			null);
		public DataTemplateSelector ContentTemplateSelector
		{
			get
			{
				return (DataTemplateSelector)GetValue(ContentTemplateSelectorProperty);
			}
			set
			{
				SetValue(ContentTemplateSelectorProperty, value);
			}
		}
		#endregion

		internal Element InternalChild
		{
			get
			{
				return this.Children?.FirstOrDefault();
			}
			set
			{
				this.InternalChildren.Clear();
				if (value != null)
					this.InternalChildren.Add(value);
			}
		}

		private void ApplyDataTemplate(object content, DataTemplate contentTemplate)
		{
			if (content is View v)
			{
				this.InternalChild = v;
				this.InvalidateMeasure();
				this.InvalidateLayout();
				return;
			}

			if ( contentTemplate == null)
				contentTemplate = this.ContentTemplateSelector?.SelectTemplate(content, this);

			if (content == null)
			{
				this.InternalChild = null;
			}
			else if (contentTemplate == null)
			{
				Label label = new Label
				{
					Text = (content as string) ?? content?.ToString(),
				};
				this.InternalChild = label;
			}
			else
			{
				View view = contentTemplate.CreateContent() as View;
				this.InternalChild = view;
				this.InternalChild.BindingContext = content;
			}

			this.InvalidateMeasure();
			this.InvalidateLayout();
		}

		protected override void LayoutChildren(double x, double y, double width, double height)
		{
			if (this.InternalChild is View view)
				LayoutChildIntoBoundingRegion(view, new Rectangle(x, y, width, height));
		}

		protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
		{
			if (this.InternalChild is View view)
			{
				var sz = view.Measure(widthConstraint, heightConstraint);
				return sz;
			}
			return base.OnMeasure(widthConstraint, heightConstraint);
		}
	}
}
