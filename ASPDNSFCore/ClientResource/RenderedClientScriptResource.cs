// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Linq;

namespace AspDotNetStorefront.ClientResource
{
	/// <summary>
	/// A <see cref="ClientScriptResource"/> that holds the final rendered output.
	/// </summary>
	public class RenderedClientScriptResource : ClientScriptResource
	{
		public readonly string Content;

		public RenderedClientScriptResource(string content)
			: base(Enumerable.Empty<string>())
		{
			Content = content;
		}
	}
}
