// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;

namespace AspDotNetStorefront.Entry.Component
{
	public class WrapperEntryComponent : IEntryComponent<WrapperContext>
	{
		public IHtmlString Build<TModel, TValue>(
			HtmlHelper<TModel> htmlHelper,
			Expression<Func<TModel, TValue>> expression,
			WrapperContext context)
		{
			var tagBuilder = new TagBuilder(context.Tag);

			if(!string.IsNullOrEmpty(context.Class))
				tagBuilder.AddCssClass(context.Class);

			if(context.HtmlAttributes != null)
				foreach(var attribute in context.HtmlAttributes)
					tagBuilder.Attributes[attribute.Key] = attribute.Value.ToString();

			tagBuilder.InnerHtml = context.Contents.ToString();

			return new MvcHtmlString(tagBuilder.ToString(TagRenderMode.Normal));
		}
	}

	public class WrapperContext
	{
		public readonly string Tag;
		public readonly string Class;
		public readonly IDictionary<string, object> HtmlAttributes;
		public readonly IHtmlString Contents;

		public WrapperContext(string tag, string @class, IHtmlString contents, IDictionary<string, object> htmlAttributes = null)
		{
			Tag = tag;
			Class = @class;
			HtmlAttributes = htmlAttributes;
			Contents = contents;
		}
	}
}
