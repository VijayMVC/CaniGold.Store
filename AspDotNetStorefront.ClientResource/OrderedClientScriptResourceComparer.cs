// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Collections.Generic;

namespace AspDotNetStorefront.ClientResource
{
	/// <summary>
	/// Compares <see cref=" OrderedClientScriptResource"/>s to maintain their declared ordering.
	/// </summary>
	public class OrderedClientScriptResourceComparer : Comparer<OrderedClientScriptResource>
	{
		readonly IEqualityComparer<ClientScriptResource> ResourceEqualityComparer;

		public OrderedClientScriptResourceComparer(IEqualityComparer<ClientScriptResource> resourceEqualityComparer)
		{
			ResourceEqualityComparer = resourceEqualityComparer;
		}

		public override int Compare(OrderedClientScriptResource x, OrderedClientScriptResource y)
		{
			if(ReferenceEquals(x, y))
				return 0;

			if(ReferenceEquals(x, null))
				return -1;

			if(ReferenceEquals(y, null))
				return 1;

			if(ResourceEqualityComparer.Equals(x.Resource, y.Resource))
				return 0;

			return y.Order - x.Order;
		}
	}
}
