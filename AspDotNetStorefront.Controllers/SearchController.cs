// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Data.SqlClient;
using System.Web.Mvc;
using AspDotNetStorefront.Filters;
using AspDotNetStorefront.Models;
using AspDotNetStorefront.Routing;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Controllers
{
	[PageTypeFilter(PageTypes.Search)]
	public class SearchController : Controller
	{
		public ActionResult Index(string searchTerm = null)
		{

			var customer = HttpContext.GetCustomer();

			var pageContent = string.Empty;
			var minimumSearchTermLength = AppLogic.AppConfigNativeInt("MinSearchStringLength");
			var searchTermLength = string.IsNullOrWhiteSpace(searchTerm) ? 0 : searchTerm.Length;

			if(minimumSearchTermLength <= searchTermLength)
				pageContent = GetSearchResultsHtml(searchTerm, AppLogic.AppConfig("XmlPackage.SearchPage"), customer);
			else
				pageContent = string.Format(AppLogic.GetString("search.aspx.2", customer.LocaleSetting), minimumSearchTermLength);

			var searchViewModel = new SearchViewModel
			{
				SearchTerm = searchTerm,
				PageContent = pageContent,
				PageTitle = AppLogic.GetString("search.aspx.1", customer.LocaleSetting)
			};

			return View(searchViewModel);
		}

		public ActionResult AdvancedSearch(string searchTerm = null)
		{
			var customer = HttpContext.GetCustomer();

			var searchXmlPackageName = string.IsNullOrEmpty(AppLogic.AppConfig("XmlPackage.SearchAdvPage"))
				? "page.searchadv.xml.config"
				: AppLogic.AppConfig("XmlPackage.SearchAdvPage");

			var pageContent = GetSearchResultsHtml(searchTerm, searchXmlPackageName, customer);

			var searchViewModel = new SearchViewModel
			{
				SearchTerm = searchTerm,
				PageContent = pageContent,
				PageTitle = AppLogic.GetString("searchadv.aspx.1", customer.LocaleSetting)
			};

			return View(ActionNames.Index, searchViewModel);
		}

		string GetSearchResultsHtml(string searchTerm, string xmlpackageName, Customer customer)
		{
			if(!string.IsNullOrWhiteSpace(searchTerm)
				&& AppLogic.AppConfigBool("Search_LogSearches"))
				DB.ExecuteSQL("insert into SearchLog(SearchTerm,CustomerID,LocaleSetting) values(@SearchTerm,@CustomerID,@Locale)", new SqlParameter[] {
					new SqlParameter("@SearchTerm",  CommonLogic.Ellipses(searchTerm, 97, true)),
					new SqlParameter("@CustomerID", customer.CustomerID),
					new SqlParameter("@Locale", customer.LocaleSetting)});

			var searchResultHtml = AppLogic.RunXmlPackage(
				XmlPackageName: xmlpackageName,
				UseParser: null,
				ThisCustomer: customer,
				SkinID: customer.SkinID,
				RunTimeQuery: string.Empty,
				RunTimeParams: string.Format("SearchTerm={0}", searchTerm),
				ReplaceTokens: true,
				WriteExceptionMessage: false);

			return searchResultHtml;
		}
	}
}
