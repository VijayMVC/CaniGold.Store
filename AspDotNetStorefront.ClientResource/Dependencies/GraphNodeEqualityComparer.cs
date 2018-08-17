// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Collections.Generic;

namespace AspDotNetStorefront.ClientResource.Dependencies
{
	/// <summary>
	/// Checks the equality of the value of a <see cref="GraphNode{TValue, TDirection}{TValue, TDirection}"/>
	/// </summary>
	public class GraphNodeEqualityComparer<TValue, TDirection> : EqualityComparer<GraphNode<TValue, TDirection>>
	{
		readonly IEqualityComparer<TValue> ValueEqualityComparer;

		public GraphNodeEqualityComparer(IEqualityComparer<TValue> valueEqualityComparer = null)
		{
			ValueEqualityComparer = valueEqualityComparer ?? EqualityComparer<TValue>.Default;
		}

		public override bool Equals(GraphNode<TValue, TDirection> x, GraphNode<TValue, TDirection> y)
		{
			if(ReferenceEquals(x, y))
				return true;

			if(ReferenceEquals(x, null) || ReferenceEquals(y, null))
				return false;

			return ValueEqualityComparer.Equals(x.Value, y.Value);
		}

		public override int GetHashCode(GraphNode<TValue, TDirection> obj)
		{
			if(ReferenceEquals(obj, null))
				return 0;

			return ValueEqualityComparer.GetHashCode(obj.Value);
		}
	}
}
