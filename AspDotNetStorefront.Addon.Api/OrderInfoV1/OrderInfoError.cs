// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
namespace AspDotNetStorefront.Addon.Api.OrderInfoV1
{
	public class OrderInfoError : Error
	{
		public OrderInfoError(string message)
			: base(message)
		{ }

		public OrderInfoError(string message, Error innerError)
			: base(message, innerError)
		{ }

		public class InvalidOrderNumber : OrderInfoError
		{
			public InvalidOrderNumber()
				: base("Order numbers must be greater than zero")
			{ }
		}

		public class OrderNotFound : OrderInfoError
		{
			public int OrderNumber { get; }

			public OrderNotFound(int orderNumber)
				: base($"No matching order was found for order number {orderNumber}")
			{
				OrderNumber = orderNumber;
			}
		}
	}
}
