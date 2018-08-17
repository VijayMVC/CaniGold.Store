// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
namespace AspDotNetStorefront.Models
{
	public class PopUpImageViewModel
	{
		public readonly string ImageUrl;
		public readonly string AltText;

		public PopUpImageViewModel(string imageUrl, string altText)
		{
			ImageUrl = imageUrl;
			AltText = altText;
		}
	}
}
