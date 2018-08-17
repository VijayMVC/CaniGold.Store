// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Addon.Api.ShippingV1
{
	class OrderShipmentManager : IOrderShipmentManager
	{
		public IResult MarkOrderAsShipped(
			int orderNumber,
			string shippedVia,
			string trackingNumber,
			DateTime shippedOn,
			bool suppressEmail = false)
		{
			Order.MarkOrderAsShipped(orderNumber, shippedVia, trackingNumber, shippedOn, isRecurring: false, disableEmail: suppressEmail);

			return Result.Ok();
		}
	}
}
