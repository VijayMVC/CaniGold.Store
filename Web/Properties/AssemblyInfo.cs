// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Reflection;
using Microsoft.Owin;

[assembly: AssemblyTitle("Web")]
[assembly: AssemblyDescription("AspDotNetStorefront Web Site")]

[assembly: OwinStartup(typeof(AspDotNetStorefront.Application.MvcApplication), "Owin_Start")]
