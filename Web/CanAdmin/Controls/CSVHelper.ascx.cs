// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Web.UI.WebControls;

namespace AspDotNetStorefrontControls
{
	public partial class CSVHelper : System.Web.UI.UserControl
	{
		protected TextBox CSVTextBox { get; set; }
		public string CSVTextBoxID { get; set; }
		public string UniqueJSID { get; set; }
		public string CSVSearchButtonText { get; set; }
		public CSVEntityType _EntityType = CSVEntityType.Product;
		public CSVEntityType EntityType
		{
			get
			{
				return _EntityType;
			}
			set
			{
				_EntityType = value;
			}
		}

		protected void Page_Load(object sender, EventArgs e)
		{
			CSVTextBox = this.Parent.FindControl(CSVTextBoxID) as TextBox;
			if(CSVTextBox == null)
				throw new ArgumentException("CSVHelper must be passed a valid asp:Textbox. \"" + CSVTextBoxID + "\" could not be found.");
			if(string.IsNullOrEmpty(UniqueJSID))
				throw new ArgumentException("CSVHelper must be passed a valid UniqueJSID.");
		}
	}

	public enum CSVEntityType
	{
		Product,
		Category,
		Section,
		Manufacturer,
		CustomerLevel,
		ShippingMethod
	}
}
