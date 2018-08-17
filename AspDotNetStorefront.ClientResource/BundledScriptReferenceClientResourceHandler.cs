// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.ClientResource
{
	/// <summary>
	/// An <see cref="IClientScriptResourceHandler"/> for <see cref="BundledScriptReferenceClientResource"/>s.
	/// </summary>
	public class BundledScriptReferenceClientResourceHandler : IClientScriptResourceHandler
	{
		readonly IBundledResourceProvider BundledResourceProvider;

		public BundledScriptReferenceClientResourceHandler(IBundledResourceProvider bundledResourceProvider)
		{
			BundledResourceProvider = bundledResourceProvider;
		}

		public IEnumerable<KeyedClientScriptResource> GetKeys(IEnumerable<ClientScriptResource> resources)
		{
			return resources
				.OfType<BundledScriptReferenceClientResource>()
				.Where(resource => !string.IsNullOrEmpty(resource.Path))
				.Select(resource => new KeyedClientScriptResource(
					key: resource.Path,
					resource: resource));
		}

		public IEnumerable<ClientScriptResource> Render(IEnumerable<ClientScriptResource> resources, RenderMode renderMode)
		{
			// Create an output list of all resources. We'll remove items in-place
			// and return the list.
			var outputResources = new List<ClientScriptResource>();

			// To ensure we iterate the input enumerable only once, we'll evaluate
			// it here. This allows us to scan it later for other resources in the
			// same bundle.
			var inputResources = resources.ToArray();

			foreach(var resource in inputResources)
			{
				if(resource == null)
					continue;

				if(!(resource is BundledScriptReferenceClientResource))
				{
					outputResources.Add(resource);
					continue;
				}

				var bundledResource = (BundledScriptReferenceClientResource)resource;

				if(string.IsNullOrWhiteSpace(bundledResource.Path))
					continue;

				if(string.IsNullOrWhiteSpace(bundledResource.Bundle))
					continue;

				// Scan all other input resources and find any in the same bundle.
				var bundlePaths = inputResources
					.OfType<BundledScriptReferenceClientResource>()
					.Where(bundleItem => !string.IsNullOrWhiteSpace(bundleItem.Path))
					.Where(bundleItem => !string.IsNullOrWhiteSpace(bundleItem.Bundle))
					.Where(bundleItem => StringComparer.OrdinalIgnoreCase.Equals(bundleItem.Bundle, bundledResource.Bundle))
					.Select(bundleItem => bundleItem.Path);

				// In immediate mode we have to create a unique bundle URL in case multiple bundles are registered with the
				// same bundle URL. Deferred mode will render them all together in a single bundle at the same time.
				var effectiveBundle = bundledResource.Bundle;
				if(renderMode == RenderMode.Immediate)
					effectiveBundle += "_" + GetBundleHashForFiles(bundlePaths);

				var bundleAlreadyRendered = outputResources
					.OfType<RenderedBundledClientScriptResource>()
					.Where(rendered => StringComparer.OrdinalIgnoreCase.Equals(rendered.Bundle, effectiveBundle))
					.Any();

				if(bundleAlreadyRendered)
					continue;

				var renderedResource = new RenderedBundledClientScriptResource(
					bundle: effectiveBundle,
					content: BundledResourceProvider.RenderScriptBundle(
						effectiveBundle,
						bundlePaths));

				outputResources.Add(renderedResource);
			}

			return outputResources;
		}

		string GetBundleHashForFiles(IEnumerable<string> filePaths)
		{
			// Create a unique hash for this set of files
			var aggregatedPaths = filePaths.Aggregate((pathString, next) => pathString + next);
			var md5 = MD5.Create();
			var encodedPaths = Encoding.UTF8.GetBytes(aggregatedPaths);
			return md5
				.ComputeHash(encodedPaths)
				.ToString(delimiter: string.Empty)
				.ToLowerInvariant();
		}

		// We need to track the bundle for rendered resources, so we create a
		// subclass for internal use here.
		class RenderedBundledClientScriptResource : RenderedClientScriptResource
		{
			public readonly string Bundle;

			public RenderedBundledClientScriptResource(string bundle, string content)
				: base(content)
			{
				Bundle = bundle;
			}
		}
	}
}
