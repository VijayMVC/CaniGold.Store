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
	/// A <see cref="ClientScriptResource"/> that defines a script block.
	/// </summary>
	[DebuggerDisplay("Src: {Path}")]
	public class ScriptReferenceClientResource : ClientScriptResource
	{
		public readonly string Path;
		public readonly bool Async;

		public ScriptReferenceClientResource(string path, bool async = false, IEnumerable<string> requirements = null)
			: base(requirements)
		{
			Path = path;
			Async = async;
		}
	}
}
