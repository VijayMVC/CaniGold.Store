// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Diagnostics;

namespace AspDotNetStorefront.ClientResource.Dependencies
{
	/// <summary>
	/// Represents a connection between two nodes.
	/// </summary>
	/// <typeparam name="TValue">The type of the value in the connected nodes.</typeparam>
	/// <typeparam name="TDirection">The type used to classify this edge.</typeparam>
	[DebuggerDisplay("{Direction} {Target.Value}")]
	public class GraphEdge<TValue, TDirection>
	{
		public readonly TDirection Direction;
		public readonly GraphNode<TValue, TDirection> Target;

		public GraphEdge(TDirection direction, GraphNode<TValue, TDirection> target)
		{
			Direction = direction;
			Target = target;
		}
	}
}
