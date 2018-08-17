// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefrontControls.Listing
{
	public partial class BooleanFilter : FilterControl
	{
		public string Label
		{ get; set; }

		public string FieldName
		{ get; set; }

		public string NullDisplayName
		{ get; set; }

		public bool? DefaultValue
		{ get; set; }

		protected override void OnInit(EventArgs e)
		{
			if(!IsPostBack)
			{
				Value.Items.Add(new ListItem(NullDisplayName ?? AppLogic.GetStringForDefaultLocale("admin.common.Unspecified"), String.Empty));
				Value.Items.Add(new ListItem("Yes", Boolean.TrueString));
				Value.Items.Add(new ListItem("No", Boolean.FalseString));
			}

			ResetValueToDefault();

			base.OnInit(e);
		}

		protected override void OnPreRender(EventArgs e)
		{
			ValueLabel.Visible = !String.IsNullOrEmpty(Label);
			ValueLabel.Text = Label;

			var filterNames = GenerateFilterNames();
			Value.Attributes.Add("data-filter-name", filterNames.First());
			Value.Attributes.Add("data-filter-apply-empty-value", "true");
		}

		protected override void SetValues(IEnumerable<KeyValuePair<string, string>> values)
		{
			if(values.First().Value == null)
				ResetValueToDefault();
			else
				Value.SelectFirstByValue(values.First().Value, StringComparer.OrdinalIgnoreCase);
		}

		protected override FilterClause GetFilterClause(IEnumerable<string> parameterNames)
		{
			var selectedValueParameterName = parameterNames.First();

			return new FilterClause(
				String.IsNullOrWhiteSpace(FieldName)
					? null
					: String.Format("(@{0} is null or {1} = @{0})", selectedValueParameterName, FieldName),
				new[] { new ControlParameter(selectedValueParameterName, System.Data.DbType.Boolean, Value.UniqueID, "SelectedValue") });
		}

		void ResetValueToDefault()
		{
			Value.SelectFirstByValue(
				DefaultValue.HasValue
					? DefaultValue.ToString()
					: null,
				StringComparer.OrdinalIgnoreCase);
		}
	}
}
