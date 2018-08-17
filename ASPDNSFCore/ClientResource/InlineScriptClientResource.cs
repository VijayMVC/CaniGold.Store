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
	/// A <see cref="ClientScriptResource"/> that defines a referenced script.
	/// </summary>
	[DebuggerDisplay("Inline: {Name}")]
	public class InlineScriptClientResource : ClientScriptResource
	{
		public readonly string Content;
		public readonly string Name;
		public readonly bool AddScriptTag;

		public InlineScriptClientResource(string content, string name = null, bool addScriptTag = false, IEnumerable<string> requirements = null)
			: base(requirements)
		{
			Content = content;
			Name = name;
			AddScriptTag = addScriptTag;
		}
	}
}
