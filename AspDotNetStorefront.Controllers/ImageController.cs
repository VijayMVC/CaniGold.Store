// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Text;
using System.Web;
using System.Web.Mvc;
using AspDotNetStorefront.Models;
using AspDotNetStorefront.StringResource;

namespace AspDotNetStorefront.Controllers
{
	public class ImageController : Controller
	{
		readonly IStringResourceProvider StringResourceProvider;

		public ImageController(IStringResourceProvider stringResourceProvider)
		{
			StringResourceProvider = stringResourceProvider;
		}

		public ActionResult PopUp(string imagePath, string altText = null)
		{
			if(!imagePath.StartsWith("/"))
				imagePath = string.Format("/{0}", imagePath);

			// Validate the imagePath parameter
			Uri imageUri;
			if(!Uri.TryCreate(imagePath, UriKind.Relative, out imageUri))
				throw new HttpException(404, null);

			var decodedAltText = Encoding.UTF8.GetString(HttpServerUtility.UrlTokenDecode(altText));
			if(string.IsNullOrWhiteSpace(decodedAltText))
				decodedAltText = StringResourceProvider.GetString("popup.aspx.1");

			return View(new PopUpImageViewModel(
				imageUrl: imageUri.ToString(),
				altText: decodedAltText));
		}
	}
}
