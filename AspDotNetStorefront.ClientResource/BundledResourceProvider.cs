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
using System.Web.Mvc;
using System.Web.Optimization;

namespace AspDotNetStorefront.ClientResource
{
	public class BundledResourceProvider : IBundledResourceProvider
	{
		public string RenderStyleBundle(string bundlePath, IEnumerable<string> filePaths, bool inline = false)
		{
			return RenderBundle(
				bundlePath,
				filePaths,
				bundle => new StyleBundle(bundle),
				bundle => inline
					? RenderBundleInline(bundle)
					: Styles.Render(bundle)).ToString();
		}

		public string RenderScriptBundle(string bundlePath, IEnumerable<string> filePaths)
		{
			return RenderBundle(
				bundlePath,
				filePaths,
				bundle => new ScriptBundle(bundle),
				bundle => Scripts.Render(bundle)).ToString();
		}

		string RenderBundle(string bundlePath, IEnumerable<string> filePaths, Func<string, Bundle> bundleFactory, Func<string, IHtmlString> renderDelegate)
		{
			// Make sure file paths are app relative (~/).
			var relativeFilePaths = filePaths
				.Select(VirtualPathUtility.ToAppRelative)
				.ToArray();

			// Make sure the bundle path is app relative
			bundlePath = VirtualPathUtility.ToAppRelative(bundlePath);

			var matchingBundles = BundleTable
				.Bundles
				.GetRegisteredBundles()
				.Where(bundle => bundle.Path == bundlePath);

			if(!matchingBundles.Any())
			{
				var bundle = bundleFactory(bundlePath);
				bundle.Orderer = new AsProvidedOrderer();
				bundle.Include(relativeFilePaths);
				BundleTable.Bundles.Add(bundle);
			}
			else
			{
				var bundle = matchingBundles.First();

				var bundleContext = new BundleContext(
					context: new HttpContextWrapper(HttpContext.Current),
					collection: BundleTable.Bundles,
					bundleVirtualPath: bundlePath);

				var newBundleFiles = relativeFilePaths
					.Except(bundle
							.EnumerateFiles(bundleContext)
							.Select(file => file.IncludedVirtualPath),
						StringComparer.OrdinalIgnoreCase)
					.ToArray();

				if(newBundleFiles.Any())
					bundle.Include(newBundleFiles);
			}

			var bundleStartDelimiter = BundleTable.EnableOptimizations
				? string.Empty
				: string.Format(@"<!-- Begin Bundle ""{0}"" -->", bundlePath);

			var bundleEndDelimiter = BundleTable.EnableOptimizations
				? string.Empty
				: string.Format(@"<!-- End Bundle ""{0}"" -->", bundlePath);

			return string.Concat(
				bundleStartDelimiter,
				Environment.NewLine,
				renderDelegate(bundlePath),
				bundleEndDelimiter);
		}

		IHtmlString RenderBundleInline(string bundlePath)
		{
			var bundle = BundleTable.Bundles.GetBundleFor(bundlePath);
			var bundleResponse = bundle.GenerateBundleResponse(
				context: new BundleContext(
					context: new HttpContextWrapper(HttpContext.Current),
					collection: BundleTable.Bundles,
					bundleVirtualPath: bundlePath));

			return MvcHtmlString.Create(string.Format("<style type=\"text/css\">{0}</style>", bundleResponse.Content));
		}

		class AsProvidedOrderer : IBundleOrderer
		{
			public IEnumerable<BundleFile> OrderFiles(BundleContext context, IEnumerable<BundleFile> files)
			{
				return files;
			}
		}
	}
}
