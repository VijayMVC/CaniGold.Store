// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Collections.Generic;

namespace AspDotNetStorefront.ClientResource.Dependencies
{
	public class DependencyGraph<TValue>
	{
		public readonly IReadOnlyDictionary<TValue, GraphNode<TValue, DependencyDirection>> NodeIndex;

		public DependencyGraph(Dictionary<TValue, GraphNode<TValue, DependencyDirection>> nodeIndex)
		{
			NodeIndex = nodeIndex;
		}
	}
}
