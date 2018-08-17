// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AspDotNetStorefront.ClientResource.Dependencies;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.ClientResource
{
	/// <summary>
	/// Manages a collection of all client resources registered for the current request.
	/// </summary>
	public class ClientScriptRegistry : IClientScriptRegistry
	{
		const string DeferredRenderingEnabledAppConfigName = "ClientResources.Script.DeferredRenderingEnabled";
		const string HttpContextItemsKey = "AspDotNetStorefront.ClientScriptResourceRegistry";
		const string HttpContextIndexKey = "AspDotNetStorefront.ClientScriptResourceRegistry.Index";

		readonly ClientScriptResourceHandler ClientScriptResourceHandler;
		readonly AppConfigProvider AppConfigProvider;
		readonly IEqualityComparer<ClientScriptResource> ResourceEqualityComparer;
		readonly DependencyGraphProvider DependencyGraphProvider;

		public ClientScriptRegistry(ClientScriptResourceHandler clientScriptResourceHandler, AppConfigProvider appConfigProvider, IEqualityComparer<ClientScriptResource> resourceEqualityComparer, DependencyGraphProvider dependencyGraphProvider)
		{
			ClientScriptResourceHandler = clientScriptResourceHandler;
			AppConfigProvider = appConfigProvider;
			ResourceEqualityComparer = resourceEqualityComparer;
			DependencyGraphProvider = dependencyGraphProvider;
		}

		ISet<OrderedClientScriptResource> GetOrderedResourceCollection(HttpContextBase httpContext)
		{
			var resourceCollection = httpContext.Items[HttpContextItemsKey] as ISet<OrderedClientScriptResource>;
			if(resourceCollection == null)
				httpContext.Items[HttpContextItemsKey]
				= resourceCollection
				= new SortedSet<OrderedClientScriptResource>(new OrderedClientScriptResourceComparer(ResourceEqualityComparer));

			return resourceCollection;
		}

		public IEnumerable<string> Register(HttpContextBase httpContext, IEnumerable<ClientScriptResource> resources)
		{
			var normalizedResources = resources.Select(resource => NormalizeResource(httpContext, resource));

			// Non-deferred registrations are rendered immediately and not registered for deferred rendering.
			var deferredRenderingEnabled = AppConfigProvider.GetAppConfigValue<bool>(DeferredRenderingEnabledAppConfigName);
			if(!deferredRenderingEnabled)
				return ClientScriptResourceHandler.Render(normalizedResources, RenderMode.Immediate);

			var resourceCollection = GetOrderedResourceCollection(httpContext);
			foreach(var resource in normalizedResources)
			{
				var index = (int?)httpContext.Items[HttpContextIndexKey] ?? 0;
				if(resourceCollection.Add(new OrderedClientScriptResource(index, resource)))
					httpContext.Items[HttpContextIndexKey] = index + 1;
			}

			return Enumerable.Empty<string>();
		}

		ClientScriptResource NormalizeResource(HttpContextBase httpContext, ClientScriptResource clientScriptResource)
		{
			if(clientScriptResource is InlineScriptClientResource)
			{
				var resource = (InlineScriptClientResource)clientScriptResource;
				return new InlineScriptClientResource(
					resource.Content,
					resource.Name,
					resource.AddScriptTag,
					resource.Dependencies.Select(path => NormalizePath(httpContext, path)));
			}

			if(clientScriptResource is BundledScriptReferenceClientResource)
			{
				var resource = (BundledScriptReferenceClientResource)clientScriptResource;
				return new BundledScriptReferenceClientResource(
					NormalizePath(httpContext, resource.Path),
					NormalizePath(httpContext, resource.Bundle),
					resource.Dependencies.Select(path => NormalizePath(httpContext, path)));
			}

			if(clientScriptResource is ScriptReferenceClientResource)
			{
				var resource = (ScriptReferenceClientResource)clientScriptResource;
				return new ScriptReferenceClientResource(
					NormalizePath(httpContext, resource.Path),
					resource.Async,
					resource.Dependencies.Select(path => NormalizePath(httpContext, path)));

			}

			return clientScriptResource;
		}

		string NormalizePath(HttpContextBase httpContext, string path)
		{
			if(string.IsNullOrWhiteSpace(path))
				return path;

			if(path.StartsWith("../"))
				path = VirtualPathUtility.Combine(
					httpContext.Request.AppRelativeCurrentExecutionFilePath,
					path);

			if(VirtualPathUtility.IsAppRelative(path))
				return VirtualPathUtility.ToAbsolute(path);

			return path;
		}

		public string RenderDeferredResources(HttpContextBase httpContext)
		{
			// Build an unordered dictionary of known resources
			var knownResources = ClientScriptResourceHandler
				.GetKeys(
					GetOrderedResourceCollection(httpContext)
						.Select(orderedResource => orderedResource.Resource)
						.Distinct(ResourceEqualityComparer))
				.ToDictionary(
					keyedResource => keyedResource.Key,
					keyedResource => keyedResource.Resource,
					StringComparer.OrdinalIgnoreCase);

			// Build a map of resources and their dependencies.
			// Use a known dependency if we have it, otherwise treat it
			// as a generic ScriptReferenceClientResource.
			var dependencyMap = GetOrderedResourceCollection(httpContext)
				.ToDictionary(
					orderedResource => orderedResource.Resource,
					orderedResource => orderedResource
						.Resource
						.Dependencies
						.Where(requirement => !string.IsNullOrWhiteSpace(requirement))
						.Select(requirement => knownResources.ContainsKey(requirement)
							? knownResources[requirement]
							: new ScriptReferenceClientResource(requirement)));

			// Build a dependency graph and apply a topological sort to get a "dependency ordered" list.
			var dependencyGraph = DependencyGraphProvider.CreateDependencyGraph(dependencyMap, ResourceEqualityComparer);
			var sortedResources = DependencyGraphProvider.TopologicalSort(dependencyGraph, ResourceEqualityComparer);

			// Render out the resources in the sorted order.
			return string.Join(
				Environment.NewLine,
				ClientScriptResourceHandler.Render(sortedResources, RenderMode.Deferred));
		}
	}
}
