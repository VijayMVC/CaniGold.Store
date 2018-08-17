// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Data.SqlClient;
using System.Web;
using System.Web.Mvc;
using AspDotNetStorefront.Filters;
using AspDotNetStorefront.Models;
using AspDotNetStorefront.Routing;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Controllers
{
	[CopyRouteDataValue("id", "ProductID")]     // Many legacy product XmlPackages expect the product ID to be in the "ProductID" request paramter
	public class ProductController : Controller
	{
		readonly NoticeProvider NoticeProvider;
		readonly ISearchEngineNameProvider SearchEngineNameProvider;

		public ProductController(NoticeProvider noticeProvider, ISearchEngineNameProvider searchEngineNameProvider)
		{
			NoticeProvider = noticeProvider;
			SearchEngineNameProvider = searchEngineNameProvider;
		}

		[PageTypeFilter(PageTypes.Product)]
		public ActionResult Detail(int id, string searchEngineName, int? cartRecordId = null)
		{
			var product = new Product(id);

			// Make sure we should show the product
			if(product.ProductID == 0
				|| !product.Published
				|| !product.IsMappedToStore())
				throw new HttpException(404, null);

			var outOfStockLevel = AppLogic.AppConfigNativeInt("HideProductsWithLessThanThisInventoryLevel");

			if(AppLogic.AppConfigBool("ProductPageOutOfStockRedirect")
				&& outOfStockLevel != -1)
			{
				if(AppLogic.DeterminePurchasableQuantity(product.ProductID,
					AppLogic.GetProductsDefaultVariantID(product.ProductID),
					AppLogic.ProductTracksInventoryBySizeAndColor(product.ProductID),
					"Product") < outOfStockLevel)
					throw new HttpException(404, null);
			}

			// If the search engine name in the URL is incorrect, 301 Redirect to the correct search engine name URL
			if(!string.IsNullOrEmpty(product.SEName)
				&& !StringComparer.OrdinalIgnoreCase.Equals(searchEngineName, product.SEName))
			{
				// The search engine name in the URL is incorrect
				// We should redirect to the correct search engine name, which should be stored in the database 'SEName' field

				// First, let's make sure the SEName field in the database has not been tampered with
				// If the Product.SEName field has been manually modified, we can't redirect properly
				var expectedSENameForURL = SearchEngineNameProvider.GenerateSeName(product.SEName);

				if(expectedSENameForURL == product.SEName)
					// SEName in the database is in the expected format - let's redirect to the expected SEName URL
					return RedirectPermanent(Url.BuildProductLink(id, product.SEName));
				else
					// SEName contains invalid characters, or otherwise is not in the correct format in the database
					// This should only happen if the user is manually modifying their database, or modifying products improperly using Wsi
					// Let's blow up to prevent infinite redirect.  An Admin will need to fix their manually modified SEName field(s)
					throw new HttpException(500, $"Invalid SEName in the database for ProductID {product.ProductID}");
			}

			// Set the xmlpackage
			var xmlpackageName = !string.IsNullOrEmpty(product.XmlPackage)
				? product.XmlPackage
				: AppLogic.ro_DefaultProductXmlPackage;

			// Kits always get the default kit xmlpackage
			if(product.IsAKit)
				xmlpackageName = "page.kitheader.xml.config";

			var customer = HttpContext.GetCustomer();

			// Get the context of where we've come from
			var sourceEntityInfo = GetSourceEntityInfo(product.ProductID, customer.LocaleSetting);

			// Save source entity info to the profile
			Profile["LastViewedEntityName"] = sourceEntityInfo.Type;
			Profile["LastViewedEntityInstanceID"] = sourceEntityInfo.Id.ToString();
			Profile["LastViewedEntityInstanceName"] = sourceEntityInfo.Name;

			if(!product.RequiresRegistration || customer.IsRegistered)
			{
				//Log views to generate dynamic related product results
				if(AppLogic.AppConfigBool("DynamicRelatedProducts.Enabled") && !AppLogic.UserAgentIsKnownBot())
					customer.LogProductView(product.ProductID);

				//Fire an event
				AppLogic.eventHandler("ViewProductPage").CallEvent("&ViewProductPage=true");

				//Update Product looks(views count)
				DB.ExecuteSQL("update product set Looks=Looks+1 where ProductID = @ProductId", new SqlParameter[] { new SqlParameter("ProductId", product.ProductID) });
			}

			// Additional runtime parameters
			var runtimeParameters = string.Format("EntityName={0}&EntityID={1}", sourceEntityInfo.Type, sourceEntityInfo.Id);
			if(Request.Url.Query.Contains("cartrecid"))
				runtimeParameters += string.Format("&cartrecid={0}", CommonLogic.QueryStringUSInt("cartrecid"));

			// Add a showproduct runtime node
			runtimeParameters += "&showproduct=1";

			// Breadcrumb and meta tags
			var pageTitle = Breadcrumb.GetProductBreadcrumb(product.ProductID, product.LocaleName, sourceEntityInfo.Type, sourceEntityInfo.Id, customer);
			var pageContent = string.Empty;

			var metaTitle = XmlCommon.GetLocaleEntry(product.SETitle, customer.LocaleSetting, true);
			if(string.IsNullOrEmpty(metaTitle))
				metaTitle = string.Format("{0} - {1}", AppLogic.AppConfig("StoreName"), product.LocaleName);

			var metaDescription = XmlCommon.GetLocaleEntry(product.SEDescription, customer.LocaleSetting, true);
			if(string.IsNullOrEmpty(metaDescription))
				metaDescription = product.LocaleName;

			var metaKeywords = XmlCommon.GetLocaleEntry(product.SEKeywords, customer.LocaleSetting, true);
			if(string.IsNullOrEmpty(metaKeywords))
				metaKeywords = product.LocaleName;

			var parser = new Parser();

			var xmlpackage = new XmlPackage(
				packageName: xmlpackageName,
				customer: customer,
				additionalRuntimeParms: runtimeParameters,
				htmlHelper: ControllerContext.GetHtmlHelper());

			pageContent = AppLogic.RunXmlPackage(xmlpackage, parser, customer, customer.SkinID, true, true);
			if(!string.IsNullOrEmpty(xmlpackage.SectionTitle))
				pageTitle = xmlpackage.SectionTitle;

			if(!string.IsNullOrEmpty(xmlpackage.SETitle))
				metaTitle = xmlpackage.SETitle;

			if(!string.IsNullOrEmpty(xmlpackage.SEDescription))
				metaDescription = xmlpackage.SEDescription;

			if(xmlpackage.SEKeywords != string.Empty)
				metaKeywords = xmlpackage.SEKeywords;

			// Build the view model
			var productViewModel = new ProductViewModel
			{
				Id = product.ProductID,
				Name = XmlCommon.GetLocaleEntry(product.LocaleName, customer.LocaleSetting, true),
				MetaTitle = metaTitle,
				MetaDescription = metaDescription,
				MetaKeywords = metaKeywords,
				PageTitle = pageTitle,
				PageContent = pageContent,
				LoginRequired = product.RequiresRegistration && !customer.IsRegistered,
				IsAKit = product.IsAKit,
				XmlPackageName = xmlpackageName,
				CartRecordId = cartRecordId
			};

			// Override the layout
			var layoutName = string.Empty;
			if(AppLogic.AppConfigBool("TemplateSwitching.Enabled"))
				layoutName = AppLogic.GetCurrentEntityTemplateName(sourceEntityInfo.Type, sourceEntityInfo.Id);

			// Kits use a separate view
			var viewName = product.IsAKit
				? ViewNames.KitDetail
				: ViewNames.Detail;

			return !string.IsNullOrEmpty(layoutName)
				? View(viewName, layoutName, productViewModel)
				: View(viewName, productViewModel);
		}

		[HttpGet]
		[ImportModelStateFromTempData]
		public ActionResult EmailProduct(int id)
		{
			var product = new Product(id);
			if(product.ProductID == 0 || !product.Published || product.Deleted)
				throw new HttpException(404, null);

			var customer = HttpContext.GetCustomer();

			if(product.RequiresRegistration && !customer.IsRegistered)
			{
				NoticeProvider.PushNotice(AppLogic.GetString("emailproduct.registrationrequired"), NoticeType.Warning);
				return RedirectToAction(ActionNames.SignIn, ControllerNames.Account);
			}

			var productImage = AppLogic.LookupImage(
				ID: product.ProductID,
				EntityOrObjectName: "Product",
				ImgSize: "medium",
				SkinID: customer.SkinID,
				LocaleSetting: customer.LocaleSetting);

			return View(new ProductEmailViewModel(productImage, product.LocaleName)
			{
				ProductId = product.ProductID
			});
		}

		[HttpPost]
		[ExportModelStateToTempData]
		public ActionResult EmailProduct(ProductEmailPostViewModel model)
		{
			var customer = HttpContext.GetCustomer();

			var product = new Product(model.ProductId);
			if(product.ProductID == 0 || !product.Published || product.Deleted)
				throw new HttpException(404, null);
			if(!ModelState.IsValid)
			{
				var productImage = AppLogic.LookupImage(
					ID: model.ProductId,
					EntityOrObjectName: "Product",
					ImgSize: "medium",
					SkinID: customer.SkinID,
					LocaleSetting: customer.LocaleSetting);

				return View(new ProductEmailViewModel(productImage, product.LocaleName)
				{
					ProductId = model.ProductId,
					To = model.To,
					From = model.From,
					Message = model.Message
				});
			}


			if(product.RequiresRegistration && !customer.IsRegistered)
			{
				NoticeProvider.PushNotice(AppLogic.GetString("emailproduct.registrationrequired"), NoticeType.Warning);
				return RedirectToAction(ActionNames.SignIn, ControllerNames.Account);
			}

			var from = AppLogic.AppConfig("ReceiptEMailFrom");
			var body = AppLogic.RunXmlPackage(
				XmlPackageName: "notification.emailproduct.xml.config",
				UseParser: null,
				ThisCustomer: customer,
				SkinID: customer.SkinID,
				RunTimeQuery: string.Empty,
				RunTimeParams: string.Format("message={0}&fromaddress={1}&ProductId={2}",
					Url.Encode(model.Message),
					Url.Encode(model.From),
					product.ProductID),
				ReplaceTokens: false,
				WriteExceptionMessage: false);

			AppLogic.SendMail(
				fromAddress: from,
				fromName: from,
				toAddress: model.To,
				toName: model.To,
				bccAddresses: string.Empty,
				replyToAddress: model.From,
				subject: string.Format("{0} - {1}", AppLogic.AppConfig("StoreName"), product.Name),
				useHtml: true,
				body: body,
				server: AppLogic.MailServer());

			NoticeProvider.PushNotice(AppLogic.GetString("emailproduct.success"), NoticeType.Success);

			return RedirectToAction(ActionNames.EmailProduct, new { id = product.ProductID });
		}

		EntityInfo GetSourceEntityInfo(int productId, string locale)
		{
			var sourceEntityType = Profile["LastViewedEntityName"] != null
				? Profile["LastViewedEntityName"].ToString()
				: string.Empty;

			var sourceEntityName = Profile["LastViewedEntityInstanceName"] != null
				? Profile["LastViewedEntityInstanceName"].ToString()
				: string.Empty;

			var sourceEntityIdString = Profile["LastViewedEntityInstanceID"] != null
				? Profile["LastViewedEntityInstanceID"].ToString()
				: string.Empty;

			int sourceEntityId;
			if(!int.TryParse(sourceEntityIdString, out sourceEntityId))
				sourceEntityId = 0;

			// validate that source entity id is actually valid for this product:
			if(sourceEntityId != 0)
			{
				var sqlParameters = new SqlParameter[] {
					new SqlParameter(
						"AllowEntityFiltering",
						AppLogic.GlobalConfigBool("AllowEntityFiltering")
							? 1
							: 0),
					new SqlParameter("StoreId", AppLogic.StoreID()),
					new SqlParameter("ProductId", productId),
					new SqlParameter("SourceEntityId", sourceEntityId),
					new SqlParameter("SourceEntityType", sourceEntityType)
				};

				var sql = @"select count(*) as N from productentity a with (nolock) 
					inner join (select distinct a.entityid, a.EntityType from productentity a with (nolock) 
						left join EntityStore b with (nolock) on a.EntityID = b.EntityID 
						where (@AllowEntityFiltering = 0 or StoreID = @StoreId)
					) b on a.EntityID = b.EntityID and a.EntityType=b.EntityType 
					where ProductID = @ProductId 
					and a.EntityID = @SourceEntityId 
					and a.EntityType = @SourceEntityType";

				if(DB.GetSqlN(sql, sqlParameters) == 0)
					sourceEntityId = 0;
			}

			if(sourceEntityId == 0)
			{
				//We had no entity context coming in, try to find an entity context for this product
				foreach(var entityType in new string[] { "Category", "Section", "Manufacturer" })
				{
					sourceEntityId = EntityHelper.GetProductsFirstEntity(productId, entityType);
					if(sourceEntityId > 0)
					{
						var entityHelper = AppLogic.LookupHelper(entityType, 0);
						var entityName = entityHelper.GetEntityName(sourceEntityId, locale);
						sourceEntityType = entityType;
						sourceEntityName = entityName;
						break;
					}
				}
			}

			return new EntityInfo(
				id: sourceEntityId,
				type: sourceEntityType,
				name: sourceEntityName);
		}

		class EntityInfo
		{
			public readonly int Id;
			public readonly string Type;
			public readonly string Name;

			public EntityInfo(int id, string type, string name)
			{
				Id = id;
				Type = type;
				Name = name;
			}
		}
	}
}
