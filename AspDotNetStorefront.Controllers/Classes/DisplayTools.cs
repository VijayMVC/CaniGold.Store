// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;

namespace AspDotNetStorefront.Classes
{
	public class DisplayTools
	{
		public static string GetCardImage(string imagePath, string cardName)
		{
			if(cardName.StartsWith("visaelectron", StringComparison.InvariantCultureIgnoreCase))
			{
				return string.Format("{0}cc_visaelectron.png", imagePath);
			}
			if(cardName.StartsWith("Visa", StringComparison.InvariantCultureIgnoreCase))
			{
				return string.Format("{0}visa.gif", imagePath);
			}
			if(cardName.StartsWith("MasterCard", StringComparison.InvariantCultureIgnoreCase))
			{
				return string.Format("{0}mastercard.gif", imagePath);
			}
			if(cardName.StartsWith("Ame", StringComparison.InvariantCultureIgnoreCase))
			{
				return string.Format("{0}amex.gif", imagePath);
			}
			if(cardName.StartsWith("Diners", StringComparison.InvariantCultureIgnoreCase))
			{
				return string.Format("{0}cc_diners.gif", imagePath);
			}
			if(cardName.StartsWith("Discover", StringComparison.InvariantCultureIgnoreCase))
			{
				return string.Format("{0}discover.gif", imagePath);
			}
			if(cardName.StartsWith("EnRoute", StringComparison.InvariantCultureIgnoreCase))
			{
				return string.Format("{0}enroute.gif", imagePath);
			}
			if(cardName.StartsWith("JCB", StringComparison.InvariantCultureIgnoreCase))
			{
				return string.Format("{0}jcb.gif", imagePath);
			}
			if(cardName.StartsWith("maestro", StringComparison.InvariantCultureIgnoreCase))
			{
				return string.Format("{0}cc_maestro.jpg", imagePath);
			}

			return string.Format("{0}genericCard.gif", imagePath);
		}

	}
}
