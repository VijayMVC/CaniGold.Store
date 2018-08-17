// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
namespace AspDotNetStorefront.Addon.Api
{
	public class Error
	{
		public string Message { get; }
		public Error InnerError { get; }

		protected Error(string message, Error innerError = null)
		{
			Message = message;
			InnerError = innerError;
		}
	}
}
