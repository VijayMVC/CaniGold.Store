// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;

namespace AspDotNetStorefront.Addon.Api.OrderInfoV1
{
	class OrderInfo : IOrderInfo
	{
		readonly Func<IOrderReader> OrderReaderFactory;

		public OrderInfo(Func<IOrderReader> orderReaderFactory)
		{
			OrderReaderFactory = orderReaderFactory;
		}

		public IOrderReader CreateOrderReader()
			=> OrderReaderFactory();
	}
}
