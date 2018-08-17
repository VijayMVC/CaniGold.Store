// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Controllers.Classes
{
	public class SiteMapSettingsProvider
	{
		readonly AppConfigProvider AppConfigProvider;

		public SiteMapSettingsProvider(AppConfigProvider appConfigProvider)
		{
			AppConfigProvider = appConfigProvider;
		}

		public SiteMapSettings LoadSiteMapSettings()
			=> new SiteMapSettings(
				showCategories: AppLogic.IsAdminSite || AppConfigProvider.GetAppConfigValue<bool>("SiteMap.ShowCategories"),
				showSections: AppLogic.IsAdminSite || AppConfigProvider.GetAppConfigValue<bool>("SiteMap.ShowSections"),
				showManufacturers: AppLogic.IsAdminSite || AppConfigProvider.GetAppConfigValue<bool>("SiteMap.ShowManufacturers"),
				showTopics: AppLogic.IsAdminSite || AppConfigProvider.GetAppConfigValue<bool>("SiteMap.ShowTopics"),
				showProducts: AppLogic.IsAdminSite || AppConfigProvider.GetAppConfigValue<bool>("SiteMap.ShowProducts"),
				showCustomerService: AppLogic.IsAdminSite || AppConfigProvider.GetAppConfigValue<bool>("SiteMap.ShowCustomerService"),
				productFiltering: AppConfigProvider.GetAppConfigValue<bool>("AllowProductFiltering"),
				entityFiltering: AppConfigProvider.GetAppConfigValue<bool>("AllowEntityFiltering"),
				topicFiltering: AppConfigProvider.GetAppConfigValue<bool>("AllowTopicFiltering"),
				inventoryThreshold: AppConfigProvider.GetAppConfigValue<int>("HideProductsWithLessThanThisInventoryLevel"),
				entityChangeFrequency: AppConfigProvider.GetAppConfigValue("SiteMapFeed.EntityChangeFreq"),
				entityPriority: AppConfigProvider.GetAppConfigValue("SiteMapFeed.EntityPriority"),
				objectChangeFrequency: AppConfigProvider.GetAppConfigValue("SiteMapFeed.ObjectChangeFreq"),
				objectPriority: AppConfigProvider.GetAppConfigValue("SiteMapFeed.ObjectPriority"),
				topicChangeFrequency: AppConfigProvider.GetAppConfigValue("SiteMapFeed.TopicChangeFreq"),
				topicPriority: AppConfigProvider.GetAppConfigValue("SiteMapFeed.TopicPriority"));
	}

	public class SiteMapSettings
	{
		public bool ShowCategories { get; }
		public bool ShowSections { get; }
		public bool ShowManufacturers { get; }
		public bool ShowTopics { get; }
		public bool ShowProducts { get; }
		public bool ShowCustomerService { get; }
		public bool ProductFiltering { get; }
		public bool EntityFiltering { get; }
		public bool TopicFiltering { get; }
		public int InventoryThreshold { get; }
		public string EntityChangeFrequency { get; }
		public string EntityPriority { get; }
		public string ObjectChangeFrequency { get; }
		public string ObjectPriority { get; }
		public string TopicChangeFrequency { get; }
		public string TopicPriority { get; }

		public SiteMapSettings(
			bool showCategories,
			bool showSections,
			bool showManufacturers,
			bool showTopics,
			bool showProducts,
			bool showCustomerService,
			bool productFiltering,
			bool entityFiltering,
			bool topicFiltering,
			int inventoryThreshold,
			string entityChangeFrequency,
			string entityPriority,
			string objectChangeFrequency,
			string objectPriority,
			string topicChangeFrequency,
			string topicPriority)
		{
			ShowCategories = showCategories;
			ShowSections = showSections;
			ShowManufacturers = showManufacturers;
			ShowTopics = showTopics;
			ShowProducts = showProducts;
			ShowCustomerService = showCustomerService;
			ProductFiltering = productFiltering;
			EntityFiltering = entityFiltering;
			TopicFiltering = topicFiltering;
			InventoryThreshold = inventoryThreshold;
			EntityChangeFrequency = entityChangeFrequency;
			EntityPriority = entityPriority;
			ObjectChangeFrequency = objectChangeFrequency;
			ObjectPriority = objectPriority;
			TopicChangeFrequency = topicChangeFrequency;
			TopicPriority = topicPriority;
		}
	}
}
