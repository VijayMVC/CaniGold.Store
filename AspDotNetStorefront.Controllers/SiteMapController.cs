// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using System.Web.Routing;
using System.Xml;
using AspDotNetStorefront.Controllers.Classes;
using AspDotNetStorefront.Models;
using AspDotNetStorefront.Routing;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Controllers
{
	public class SiteMapController : Controller
	{
		readonly IRoutingConfigurationProvider RoutingConfigurationProvider;
		readonly ISearchEngineNameProvider SearchEngineNameProvider;
		readonly SiteMapSettingsProvider SiteMapSettingsProvider;

		public SiteMapController(
			IRoutingConfigurationProvider routingConfigurationProvider,
			ISearchEngineNameProvider searchEngineNameProvider,
			SiteMapSettingsProvider siteMapSettingsProvider)
		{
			RoutingConfigurationProvider = routingConfigurationProvider;
			SearchEngineNameProvider = searchEngineNameProvider;
			SiteMapSettingsProvider = siteMapSettingsProvider;
		}

		public ViewResult Index(int? productTypeFilterId)
		{
			var customer = HttpContext.GetCustomer();
			var settings = SiteMapSettingsProvider.LoadSiteMapSettings();

			var viewModel = new SiteMapViewModel
			{
				ShowCategories = settings.ShowCategories,
				ShowManufacturers = settings.ShowManufacturers,
				ShowSections = settings.ShowSections,
				ShowTopics = settings.ShowTopics,
				ShowProducts = settings.ShowProducts,
				ShowCustomerService = settings.ShowCustomerService
			};

			if(settings.ShowProducts)
			{
				// Using named routes drastically improves performance for sites with many products.
				var routingConfiguration = RoutingConfigurationProvider.GetRoutingConfiguration();

				string productRouteName;
				if(routingConfiguration.LegacyRouteGenerationEnabled)
					productRouteName = RouteNames.Product;
				else if(routingConfiguration.SeNameOnlyRoutesEnabled)
					productRouteName = RouteNames.ModernProductSeNameOnly;
				else
					productRouteName = RouteNames.ModernProduct;

				using(var connection = new SqlConnection(DB.GetDBConn()))
				using(var command = connection.CreateCommand())
				{
					command.CommandType = CommandType.StoredProcedure;
					command.CommandText = "aspdnsf_GetProducts";
					command.Parameters.AddWithValue("@CustomerLevelID", customer.CustomerLevelID);
					command.Parameters.AddWithValue("@affiliateID", customer.AffiliateID);
					command.Parameters.AddWithValue("@ProductTypeID", productTypeFilterId);
					command.Parameters.AddWithValue("@InventoryFilter", settings.InventoryThreshold);
					command.Parameters.AddWithValue("@localeName", customer.LocaleSetting);
					command.Parameters.AddWithValue("@storeID", customer.StoreID);
					command.Parameters.AddWithValue("@filterProduct", settings.ProductFiltering);
					command.Parameters.AddWithValue("@ViewType", 1);
					command.Parameters.AddWithValue("@pagenum", 1);
					command.Parameters.AddWithValue("@pagesize", 2147483647);
					command.Parameters.AddWithValue("@StatsFirst", 0);
					command.Parameters.AddWithValue("@publishedonly", 1);
					command.Parameters.AddWithValue("@ExcludeKits", 0);
					command.Parameters.AddWithValue("@ExcludeSysProds", 1);

					connection.Open();

					using(var reader = command.ExecuteReader())
						while(reader.Read())
						{
							var id = reader.FieldInt("ProductID");

							var localizedName = XmlCommon.GetLocaleEntry(
								reader.Field("name"),
								customer.LocaleSetting,
								fallBack: true);

							var seName = reader.Field("sename");

							viewModel.Products.Add(
								new Models.SiteMapEntity
								{
									Name = localizedName,
									Url = Url.BuildProductLink(id, seName)});
						}
				}
			}

			if(settings.ShowTopics)
			{
				using(var connection = DB.dbConn())
				{
					connection.Open();
					using(var reader = DB.GetRS(
						@"declare @storeSpecificTopics table
						(
							topicid int,
							name varchar(max),
							title varchar(max),
							storeid int,
							displayorder int
						)

						insert into @storeSpecificTopics
							select topicid, name, title, storeid, displayorder from topic as dt with (nolock)
								where showinsitemap = 1
								and deleted = 0
								and published = 1
								and(storeid = @storeId)
								and @filterTopic = 1

						select topicid, name, title, storeid, displayorder from topic as dt with (nolock)
							where showinsitemap = 1
							and deleted = 0
							and published = 1
							and(storeid = 0)
							and name not in (select name from @storeSpecificTopics)
						union select * from @storeSpecificTopics
						order by displayorder, title",
						connection,
						new SqlParameter("@filterTopic", settings.TopicFiltering),
						new SqlParameter("@storeId", customer.StoreID)))
					{
						while(reader.Read())
						{
							var topicId = reader.FieldInt("topicid");

							var name = reader.Field("name");

							var localizedTitle = XmlCommon.GetLocaleEntry(
								reader.Field("title"),
								customer.LocaleSetting,
								fallBack: true);

							viewModel.Topics.Add(
								new Models.SiteMapEntity()
								{
									Name = localizedTitle,
									Url = Url.BuildTopicLink(name)
								});
						}
					}
				}
			}

			if(settings.ShowCustomerService)
			{
				viewModel.CustomerService.Add(
					new Models.SiteMapEntity()
					{
						Name = AppLogic.GetString("menu.YourAccount"),
						Url = Url.Action(ActionNames.Index, ControllerNames.Account)
					});

				viewModel.CustomerService.Add(
					new Models.SiteMapEntity()
					{
						Name = AppLogic.GetString("menu.OrderHistory"),
						Url = string.Concat(
							Url.Action(ActionNames.Index, ControllerNames.Account),
							"#orderhistory")
					});

				viewModel.CustomerService.Add(
					new Models.SiteMapEntity()
					{
						Name = AppLogic.GetString("menu.Contact"),
						Url = Url.Action(ActionNames.Index, ControllerNames.ContactUs)
					});
			}

			if(settings.ShowCategories)
			{
				EntityHelper entityHelper =
					new EntityHelper(
						CacheMinutes: 0,
						eSpecs: EntityDefinitions.LookupSpecs(EntityTypes.Category),
						PublishedOnly: true,
						StoreID: 0);

				viewModel.Categories = AddEntities(
					entityHelper
					.m_TblMgr
					.XmlDoc
					.SelectNodes("/root/Entity"),
					customer,
					EntityTypes.Category);
			}

			if(settings.ShowSections)
			{
				EntityHelper entityHelper =
					new EntityHelper(
						CacheMinutes: 0,
						eSpecs: EntityDefinitions.LookupSpecs(EntityTypes.Section),
						PublishedOnly: true,
						StoreID: 0);

				viewModel.Sections = AddEntities(
					entityHelper
					.m_TblMgr
					.XmlDoc
					.SelectNodes("/root/Entity"),
					customer,
					EntityTypes.Section);
			}

			if(settings.ShowManufacturers)
			{
				EntityHelper entityHelper =
					new EntityHelper(
						CacheMinutes: 0,
						eSpecs: EntityDefinitions.LookupSpecs(EntityTypes.Manufacturer),
						PublishedOnly: true,
						StoreID: 0);

				viewModel.Manufacturers = AddEntities(
					entityHelper
					.m_TblMgr
					.XmlDoc
					.SelectNodes("/root/Entity"),
					customer,
					EntityTypes.Manufacturer);
			}

			return View(ViewNames.SiteMap, viewModel);
		}

		List<Models.SiteMapEntity> AddEntities(XmlNodeList nodes, Customer customer, string entityType)
		{
			var entities = new List<Models.SiteMapEntity>();

			foreach(XmlNode node in nodes)
			{
				var id = Convert.ToInt32(
					node
					.SelectSingleNode("EntityID")
					.InnerText);

				var localizedName = XmlCommon.GetLocaleEntry(
					node.SelectSingleNode("Name").InnerXml,
					customer.LocaleSetting,
					fallBack: true);

				var seName = node.SelectSingleNode("SEName").InnerText;

				var entity = new Models.SiteMapEntity()
				{
					Name = localizedName,
					Url = Url.BuildEntityLink(
						entityType,
						id,
						seName)
				};

				entity.Children = AddEntities(node.SelectNodes("Entity"), customer, entityType);
				entities.Add(entity);
			}

			return entities;
		}
	}
}
