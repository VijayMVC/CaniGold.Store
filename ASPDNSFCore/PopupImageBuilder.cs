// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Text;
using System.Web;
using System.Web.Mvc;
using AspDotNetStorefront.Routing;

namespace AspDotNetStorefrontCore
{
	public interface IPopupImageBuilder
	{
		string BuildPopupImageScript(UrlHelper urlHelper, Size imageSize, string altText);
		string BuildInlinePopupImageScript(UrlHelper urlHelper, string imageUrl, Size imageSize, string altText);
		string EncodePopupImageUrl(string imageUrl);
		string EncodePopupAltText(string altText);
	}

	public class PopupImageBuilder : IPopupImageBuilder
	{
		public string BuildPopupImageScript(UrlHelper urlHelper, Size imageSize, string altText)
		{
			var popupImageUrl = urlHelper.Action(ActionNames.PopUp, ControllerNames.Image);
			var encodedAltText = EncodePopupAltText(altText);
			var randomNumber = new Random().Next(100000) + 1;
			var resizeable = AppLogic.AppConfigBool("ResizableLargeImagePopup")
				? "yes"
				: "no";

			var popupImageScript = $@"
				<script type='text/javascript'>
					function popupimg()
					{{
						var imagePath = document.getElementById('popupImageURL').value;

						window.open(
							'{popupImageUrl}?{RouteDataKeys.ImagePath}=' + imagePath + '&{RouteDataKeys.AltText}={encodedAltText}',
							'LargerImage{randomNumber}',
							'toolbar=no,location=no,directories=no,status=no,menubar=no,scrollbars={resizeable},resizable={resizeable},copyhistory=no,width={imageSize.Width},height={imageSize.Height},left=0,top=0');

						return true;
					}}
				</script>";

			return popupImageScript;
		}

		public string BuildInlinePopupImageScript(UrlHelper urlHelper, string imageUrl, Size imageSize, string altText)
		{
			var popupImageUrl = urlHelper.Action(ActionNames.PopUp, ControllerNames.Image);
			var encodedImageUrl = EncodePopupImageUrl(imageUrl);
			var encodedAltText = EncodePopupAltText(altText);
			var randomNumber = new Random().Next(100000) + 1;
			var resizeable = AppLogic.AppConfigBool("ResizableLargeImagePopup")
				? "yes"
				: "no";

			var popupImageScript = $@"window.open('{popupImageUrl}?{RouteDataKeys.ImagePath}={encodedImageUrl}&{RouteDataKeys.AltText}={encodedAltText}', 'LargerImage{randomNumber}', 'toolbar=no,location=no,directories=no,status=no,menubar=no,scrollbars={resizeable},resizable={resizeable},copyhistory=no,width={imageSize.Width},height={imageSize.Height},left=0,top=0');";

			return popupImageScript;
		}

		public string EncodePopupImageUrl(string imageUrl)
			=> Uri.EscapeDataString(imageUrl ?? string.Empty);

		public string EncodePopupAltText(string altText)
			=> HttpServerUtility.UrlTokenEncode(Encoding.UTF8.GetBytes(altText ?? string.Empty));
	}
}
