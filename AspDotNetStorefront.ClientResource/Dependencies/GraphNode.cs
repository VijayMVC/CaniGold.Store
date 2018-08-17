// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AspDotNetStorefront.ClientResource.Dependencies
{
	/// <summary>
	/// Represents a node in a graph.
	/// </summary>
	/// <typeparam name="TValue">The type of the value in this node.</typeparam>
	/// <typeparam name="TDirection">The type used to classify edges in this node.</typeparam>
	[DebuggerDisplay("{Value} ({Edges.Count})")]
	public class GraphNode<TValue, TDirection>
	{
		public readonly TValue Value;
		public readonly ISet<GraphEdge<TValue, TDirection>> Edges;

		public GraphNode(TValue value, IEnumerable<GraphEdge<TValue, TDirection>> edges = null, IEqualityComparer<TValue> valueEqualityComparer = null, IEqualityComparer<TDirection> directionEqualityComparer = null)
		{
			Value = value;
			Edges = new HashSet<GraphEdge<TValue, TDirection>>(
				collection: edges ?? Enumerable.Empty<GraphEdge<TValue, TDirection>>(),
				comparer: new GraphEdgeEqualityComparer<TValue, TDirection>(new GraphNodeEqualityComparer<TValue, TDirection>(valueEqualityComparer), directionEqualityComparer));
		}
	}
}
