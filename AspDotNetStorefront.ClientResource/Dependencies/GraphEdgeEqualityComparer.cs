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
	/// Checks the equality of the direction and target node of a <see cref="GraphEdge{TValue, TDirection}"/>
	/// </summary>
	public class GraphEdgeEqualityComparer<TValue, TDirection> : EqualityComparer<GraphEdge<TValue, TDirection>>
	{
		readonly IEqualityComparer<GraphNode<TValue, TDirection>> GraphNodeEqualityComparer;
		readonly IEqualityComparer<TDirection> DirectionEqualityComparer;

		public GraphEdgeEqualityComparer(IEqualityComparer<GraphNode<TValue, TDirection>> graphNodeEqualityComparer = null, IEqualityComparer<TDirection> directionEqualityComparer = null)
		{
			GraphNodeEqualityComparer = graphNodeEqualityComparer ?? new GraphNodeEqualityComparer<TValue, TDirection>();
			DirectionEqualityComparer = directionEqualityComparer ?? EqualityComparer<TDirection>.Default;
		}

		public override bool Equals(GraphEdge<TValue, TDirection> x, GraphEdge<TValue, TDirection> y)
		{
			if(ReferenceEquals(x, y))
				return true;

			if(ReferenceEquals(x, null) || ReferenceEquals(y, null))
				return false;

			return DirectionEqualityComparer.Equals(x.Direction, y.Direction)
				&& GraphNodeEqualityComparer.Equals(x.Target, y.Target);
		}

		public override int GetHashCode(GraphEdge<TValue, TDirection> obj)
		{
			if(ReferenceEquals(obj, null))
				return 0;

			// Combine the high and low words of the direction and target hash codes.
			return DirectionEqualityComparer.GetHashCode(obj.Direction) << 16 | GraphNodeEqualityComparer.GetHashCode(obj.Target) >> 16;
		}
	}
}
