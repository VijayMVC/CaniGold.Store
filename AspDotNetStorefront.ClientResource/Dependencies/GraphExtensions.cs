// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Linq;

namespace AspDotNetStorefront.ClientResource.Dependencies
{
	public static class GraphExtensions
	{
		/// <summary>
		/// Returns each node of a dependency graph in depth-first order.
		/// </summary>
		/// <param name="direction">The direction to traverse in. Only edges in the given direction will be traversed.</param>
		/// <returns></returns>
		public static IEnumerable<GraphNode<TValue, TDirection>> TraverseDepthFirst<TValue, TDirection>(
			this GraphNode<TValue, TDirection> head,
			TDirection direction,
			IEqualityComparer<TValue> valueEqualityComparer = null,
			IEqualityComparer<TDirection> directionEqualityComparer = null)
		{
			return VisitDepthFirst(
				directionEqualityComparer: directionEqualityComparer ?? EqualityComparer<TDirection>.Default,
				visited: new HashSet<GraphNode<TValue, TDirection>>(new GraphNodeEqualityComparer<TValue, TDirection>(valueEqualityComparer)),
				direction: direction,
				node: head);
		}

		static IEnumerable<GraphNode<TValue, TDirection>> VisitDepthFirst<TValue, TDirection>(
			IEqualityComparer<TDirection> directionEqualityComparer,
			ISet<GraphNode<TValue, TDirection>> visited,
			TDirection direction,
			GraphNode<TValue, TDirection> node)
		{
			if(visited.Contains(node))
				yield break;

			visited.Add(node);
			yield return node;

			var directedEdges = node
				.Edges
				.Where(edge => directionEqualityComparer.Equals(direction, edge.Direction));

			foreach(var edge in directedEdges)
				foreach(var result in VisitDepthFirst(directionEqualityComparer, visited, direction, edge.Target))
					yield return result;
		}

		public static IEnumerable<GraphEdge<TValue, TDirection>> InDirection<TValue, TDirection>(this IEnumerable<GraphEdge<TValue, TDirection>> edges, TDirection direction, IEqualityComparer<TDirection> equalityComparer = null)
		{
			return edges.Where(edge => (equalityComparer ?? EqualityComparer<TDirection>.Default).Equals(direction, edge.Direction));
		}
	}
}
