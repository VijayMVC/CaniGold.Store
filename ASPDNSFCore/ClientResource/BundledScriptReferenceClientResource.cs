// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Diagnostics;

namespace AspDotNetStorefront.ClientResource
{
	/// <summary>
	/// A <see cref="ClientScriptResource"/> that defines a bundled script.
	/// </summary>
	[DebuggerDisplay("Bundled: {Path}")]
	public class BundledScriptReferenceClientResource : ClientScriptResource
	{
		public readonly string Path;
		public readonly string Bundle;

		public BundledScriptReferenceClientResource(string path, string bundle, IEnumerable<string> requirements = null)
			: base(requirements)
		{
			Bundle = bundle;
			Path = path;
		}
	}
}
