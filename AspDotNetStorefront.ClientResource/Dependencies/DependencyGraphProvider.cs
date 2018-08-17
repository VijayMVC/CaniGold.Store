// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;

namespace AspDotNetStorefront.ClientResource.Dependencies
{
	public class DependencyGraphProvider
	{
		/// <summary>
		/// Convienience method that creates a dependency graph from a Dictionary rather than a collection of <see cref="GraphNode{TValue, TDirection}"/>s.
		/// </summary>
		/// <typeparam name="TValue">The type of the values stored in the dependency graph.</typeparam>
		/// <param name="dependencyMap">A dictionary where the keys are the graph node values and the values are the graph node dependencies.</param>
		/// <param name="valueEqualityComparer">An equality comparer for the values. If null, the default equality comparer will be used.</param>
		/// <returns>A <see cref="DependencyGraph{TValue}"/> that represents the relationships between all nodes in the provided graph.</returns>
		public DependencyGraph<TValue> CreateDependencyGraph<TValue>(IDictionary<TValue, IEnumerable<TValue>> dependencyMap, IEqualityComparer<TValue> valueEqualityComparer = null)
		{
			valueEqualityComparer = valueEqualityComparer ?? EqualityComparer<TValue>.Default;

			return CreateDependencyGraph(
				dependencyMap
					.Select(kvp => new GraphNode<TValue, DependencyDirection>(
						value: kvp.Key,
						valueEqualityComparer: valueEqualityComparer,
						edges: kvp
							.Value
							.Select(d => new GraphEdge<TValue, DependencyDirection>(
								direction: DependencyDirection.DependsOn,
								target: new GraphNode<TValue, DependencyDirection>(
									value: d,
									valueEqualityComparer: valueEqualityComparer))))));
		}

		/// <summary>
		/// Takes a number of stated dependencies and creates a minimal number of graphs to represent the relationships.
		/// </summary>
		/// <typeparam name="TValue">The type of the values stored in the dependency graph.</typeparam>
		/// <param name="nodes">Any number of dependency trees.</param>
		/// <param name="valueEqualityComparer">An equality comparer for the values. If null, the default equality comparer will be used.</param>
		/// <returns>A <see cref="DependencyGraph{TValue}"/> that represents the relationships between all nodes in the provided graph.</returns>
		public DependencyGraph<TValue> CreateDependencyGraph<TValue>(IEnumerable<GraphNode<TValue, DependencyDirection>> nodes, IEqualityComparer<TValue> valueEqualityComparer = null)
		{
			valueEqualityComparer = valueEqualityComparer ?? EqualityComparer<TValue>.Default;

			// Get a list of all the distinct values
			var values = nodes
				.SelectMany(node => node.TraverseDepthFirst(DependencyDirection.DependsOn, valueEqualityComparer))
				.Select(node => node.Value)
				.Distinct(valueEqualityComparer);

			// Now walk the graph for each value and build separate dictionaries of dependencies and dependents
			var valueIndex = new Dictionary<TValue, GraphNode<TValue, DependencyDirection>>();
			foreach(var value in values)
			{
				var sourceNode = valueIndex.ContainsKey(value)
					? valueIndex[value]
					: valueIndex[value] = new GraphNode<TValue, DependencyDirection>(value);

				// Find all values that are dependent on the one we're checking
				var dependentValues = nodes
					.Where(node => node
						.TraverseDepthFirst(DependencyDirection.DependsOn)
						.Select(dependent => dependent.Value)
						.Contains(value, valueEqualityComparer))
					.Select(node => node.Value)
					.Except(new[] { value })
					.Distinct();

				// Create nodes for the dependents and establish the edge relationships
				foreach(var dependentValue in dependentValues)
				{
					var targetNode = valueIndex.ContainsKey(dependentValue)
						? valueIndex[dependentValue]
						: valueIndex[dependentValue] = new GraphNode<TValue, DependencyDirection>(dependentValue);

					targetNode.Edges.Add(new GraphEdge<TValue, DependencyDirection>(DependencyDirection.DependsOn, sourceNode));
					sourceNode.Edges.Add(new GraphEdge<TValue, DependencyDirection>(DependencyDirection.RequiredBy, targetNode));
				}
			}

			return new DependencyGraph<TValue>(valueIndex);
		}

		/// <summary>
		/// Sorts a dependency graph in an order that ensures all dependents come after their dependencies.
		/// </summary>
		/// <typeparam name="TValue">The type of the values stored in the dependency graph.</typeparam>
		/// <param name="dependencyGraph">The dependency graph to sort.</param>
		/// <param name="valueEqualityComparer">An equality comparer for the values. If null, the default equality comparer will be used.</param>
		/// <returns>An enumerable of values with the dependencies above all dependents.</returns>
		public IEnumerable<TValue> TopologicalSort<TValue>(DependencyGraph<TValue> dependencyGraph, IEqualityComparer<TValue> valueEqualityComparer = null)
		{
			var graphNodeEqualityComparer = new GraphNodeEqualityComparer<TValue, DependencyDirection>(valueEqualityComparer ?? EqualityComparer<TValue>.Default);

			// This collection will track the nodes that have already been processed.
			var visitedNodes = new List<GraphNode<TValue, DependencyDirection>>();

			// Walk through each node and ensure it's visited once. Because we're tracking the visited nodes externally (side effects),
			// we have to eagerly evaluate the expression with .ToArray().
			return dependencyGraph
				.NodeIndex
				.Values
				.Where(node => !visitedNodes.Contains(node, graphNodeEqualityComparer))
				.SelectMany(node => Visit(
					graphNodeEqualityComparer,
					Enumerable.Empty<GraphNode<TValue, DependencyDirection>>(),
					visitedNodes,
					node))
				.Reverse()
				.ToArray();
		}

		IEnumerable<TValue> Visit<TValue>(IEqualityComparer<GraphNode<TValue, DependencyDirection>> graphNodeEqualityComparer, IEnumerable<GraphNode<TValue, DependencyDirection>> inProgressNodes, ICollection<GraphNode<TValue, DependencyDirection>> processedNodes, GraphNode<TValue, DependencyDirection> node)
		{
			// If we're checking a node and we come back to the same node, there's a loop in the graph.
			if(inProgressNodes.Contains(node, graphNodeEqualityComparer))
				throw new Exception("Cycle detected");

			// If this node has already been processed, skip it.
			if(processedNodes.Contains(node, graphNodeEqualityComparer))
				yield break;

			// Find all of the dependencies of this node and visit them.
			var dependencyResults = node
				.Edges
				.InDirection(DependencyDirection.RequiredBy)
				.SelectMany(edge => Visit(
					graphNodeEqualityComparer,
					inProgressNodes.Concat(new[] { node }),
					processedNodes,
					edge.Target));

			foreach(var result in dependencyResults)
				yield return result;

			// Update the list of processed nodes and return this node's value
			processedNodes.Add(node);
			yield return node.Value;
		}
	}
}
