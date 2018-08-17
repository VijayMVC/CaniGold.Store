// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;

namespace AspDotNetStorefrontCore.Tokens.DynamicHandlers
{
	public class Literal : ITokenHandler
	{
		public string Name { get; }
		public string Value { get; }

		public Literal(string name, string value)
		{
			Name = name;
			Value = value;
		}

		public string RenderToken(TokenHandlerContext context)
		{
			if(!StringComparer.OrdinalIgnoreCase.Equals(Name, context.Token))
				return null;

			return Value;
		}
	}
}
