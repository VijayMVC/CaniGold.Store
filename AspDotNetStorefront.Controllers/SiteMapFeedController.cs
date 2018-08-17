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
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Xml;
using AspDotNetStorefront.Routing;
using AspDotNetStorefrontCore;
using AspDotNetStorefront.Controllers.Classes;

namespace AspDotNetStorefront.Controllers
{
	public class SiteMapFeedController : Controller
	{
		readonly SiteMapSettingsProvider SiteMapSettingsProvider;
		readonly SiteMapEntityHelper SiteMapEntityHelper;
		readonly NestedSiteMapEntityHelper NestedSiteMapEntityHelper;

		public SiteMapFeedController(SiteMapSettingsProvider siteMapSettingsProvider)
		{
			SiteMapSettingsProvider = siteMapSettingsProvider;
			SiteMapEntityHelper = new SiteMapEntityHelper(Url);
			NestedSiteMapEntityHelper = new NestedSiteMapEntityHelper(Url, SiteMapEntityHelper);
		}

		public ActionResult Index()
		{
			Response.ContentType = "text/xml";
			Response.ContentEncoding = new UTF8Encoding();
			Response.Write("<?xml version='1.0' encoding='UTF-8'?>\n");

			Response.Write("<sitemapindex xmlns='http://www.sitemaps.org/schemas/sitemap/0.9'>\n");
			Response.Write("<sitemap>");
			var topicUrl = Url.Action(ActionNames.Topics, ControllerNames.SiteMapFeed);
			var storeLocation = new Uri(AppLogic.GetStoreHTTPLocation(true));
			var topicMapLocation = new Uri(storeLocation, topicUrl);
			Response.Write(string.Format("<loc>{0}</loc>", topicMapLocation));
			Response.Write("</sitemap>\n");

			var siteMap = new StandardSiteMap(Url, SiteMapEntityHelper, NestedSiteMapEntityHelper);

			var settings = SiteMapSettingsProvider.LoadSiteMapSettings();
			Response.Write(siteMap.GetEntitySiteMap("category", settings));
			Response.Write(siteMap.GetEntitySiteMap("section", settings));
			Response.Write(siteMap.GetEntitySiteMap("manufacturer", settings));
			Response.Write(siteMap.GetEntitySiteMap("genre", settings));
			Response.Write(siteMap.GetEntitySiteMap("vector", settings));

			Response.Write("</sitemapindex>");
			return new EmptyResult();
		}

		public ActionResult Entity()
		{
			Response.ContentType = "text/xml";
			Response.ContentEncoding = new UTF8Encoding();
			Response.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");

			var entityName = CommonLogic.QueryStringCanBeDangerousContent("EntityName");
			AppLogic.CheckForScriptTag(entityName);
			int entityID = CommonLogic.QueryStringUSInt("EntityID");

			var enityHelper = AppLogic.LookupHelper(entityName, 0);

			Response.Write("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n");

			Response.Write("<url>");

			var settings = SiteMapSettingsProvider.LoadSiteMapSettings();

			var entityUrl = Url.BuildEntityLink(entityName, entityID, string.Empty);
			var storeLocation = new Uri(AppLogic.GetStoreHTTPLocation(true));
			var fullEntityLocation = new Uri(storeLocation, entityUrl);
			Response.Write(string.Format("<loc>{0}</loc>", XmlCommon.XmlEncode(fullEntityLocation.ToString())));
			Response.Write("<changefreq>" + settings.EntityChangeFrequency + "</changefreq> ");
			Response.Write("<priority>" + settings.EntityPriority + "</priority> ");
			Response.Write("</url>\n");

			var siteMap = new StandardSiteMap(Url, SiteMapEntityHelper, NestedSiteMapEntityHelper);
			Response.Write(siteMap.GetEntityProductURLNodes(entityName, entityID, settings));

			Response.Write("</urlset>");
			return new EmptyResult();
		}

		public ActionResult Topics()
		{
			Response.ContentType = "text/xml";
			Response.ContentEncoding = new UTF8Encoding();
			Response.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");

			var skinID = 1; // not sure what to do about this...google can't invoke different skins easily
			var storeLocation = new Uri(AppLogic.GetStoreHTTPLocation(true));

			var filter = AppLogic.GlobalConfigBool("AllowTopicFiltering");
			var dupes = new List<string>();

			Response.Write("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

			var settings = SiteMapSettingsProvider.LoadSiteMapSettings();

			if(settings.ShowTopics)
			{
				// DB Topics:
				using(SqlConnection conn = DB.dbConn())
				{
					conn.Open();
					if(filter)
					{
						using(IDataReader rs = DB.GetRS(string.Format("select Name from Topic with (NOLOCK) where {0} Deleted=0 and (SkinID IS NULL or SkinID=0 or SkinID={1}) and (StoreID=0 OR StoreID=" + AppLogic.StoreID() + ") Order By StoreID DESC, DisplayOrder, Name ASC", CommonLogic.IIF(AppLogic.IsAdminSite, "", "ShowInSiteMap=1 and "), skinID.ToString()), conn))
						{
							while(rs.Read())
							{
								//Only display the first instance of the topic name, store-specific version first
								if(!dupes.Contains(DB.RSFieldByLocale(rs, "Name", Localization.GetDefaultLocale())))
								{
									Response.Write("<url>");
									var topicUrl = Url.BuildTopicLink(DB.RSFieldByLocale(rs, "Name", Localization.GetDefaultLocale()));
									var fullTopicUrl = new Uri(storeLocation, topicUrl);
									Response.Write(string.Format("<loc>{0}</loc>", XmlCommon.XmlEncode(fullTopicUrl.ToString())));
									Response.Write("<changefreq>" + settings.TopicChangeFrequency + " </changefreq> ");
									Response.Write("<priority>" + settings.TopicPriority + "</priority> ");
									Response.Write("</url>");
									dupes.Add(DB.RSFieldByLocale(rs, "Name", Localization.GetDefaultLocale()));
								}
							}
						}
					}
					else
					{
						using(IDataReader rs = DB.GetRS(string.Format("select Name from Topic with (NOLOCK) where {0} Deleted=0 and (SkinID IS NULL or SkinID=0 or SkinID={1}) and (StoreID=0) Order By DisplayOrder, Name ASC", CommonLogic.IIF(AppLogic.IsAdminSite, "", "ShowInSiteMap=1 and "), skinID.ToString()), conn))
						{
							while(rs.Read())
							{
								Response.Write("<url>");
								var topicUrl = Url.BuildTopicLink(DB.RSFieldByLocale(rs, "Name", Localization.GetDefaultLocale()));
								var fullTopicUrl = new Uri(storeLocation, topicUrl);
								Response.Write(string.Format("<loc>{0}</loc>", XmlCommon.XmlEncode(fullTopicUrl.ToString())));
								Response.Write("<changefreq>" + settings.TopicChangeFrequency + "</changefreq> ");
								Response.Write("<priority>" + settings.TopicPriority + "</priority> ");
								Response.Write("</url>");
								dupes.Add(DB.RSFieldByLocale(rs, "Name", Localization.GetDefaultLocale()));
							}
						}
					}
				}
			}

			Response.Write("</urlset>");
			return new EmptyResult();
		}
	}

	class SiteMapEntityHelper
	{
		readonly UrlHelper Url;

		public SiteMapEntityHelper(UrlHelper url)
		{
			Url = url;
		}

		public XmlNode SiteMapNode(string Text, string URL, XmlDocument context)
		{
			var eleNode = context.CreateElement("node");
			var atrText = context.CreateAttribute("Text");
			atrText.Value = Text;
			eleNode.Attributes.Append(atrText);
			var atrURL = context.CreateAttribute("NavigateUrl");
			atrURL.Value = URL;
			eleNode.Attributes.Append(atrURL);
			return eleNode;
		}
	}

	class SiteMapEntity
	{
		const string RetrieveSQL =
			@"SELECT [{0}ID] AS ID, [Name], [Name] as [SEName] FROM {0}
			WHERE [{0}ID] IN (SELECT [EntityID] FROM EntityStore WHERE StoreID = @StoreID AND EntityType='{0}') OR @StoreID IS NULL AND ShowInSiteMap = 1";

		public int EntityID
		{ get; set; }

		public virtual string EntityType
		{ get; set; }

		public string Name
		{ get; set; }

		public string SEName
		{ get; set; }

		protected readonly UrlHelper Url;
		protected readonly SiteMapEntityHelper SiteMapEntityHelper;

		public SiteMapEntity(UrlHelper url, SiteMapEntityHelper siteMapEntityHelper)
		{
			Url = url;
			SiteMapEntityHelper = siteMapEntityHelper;
		}

		public virtual XmlNode ToSiteMapNode(XmlDocument context, SiteMapSettings settings)
		{
			return SiteMapEntityHelper.SiteMapNode(Name, Url.BuildEntityLink(EntityType, EntityID, SEName), context);
		}
	}

	class SiteMapProduct : SiteMapEntity
	{
		public override string EntityType
		{
			get { return "product"; }
			set { }
		}

		public SiteMapProduct(UrlHelper url, SiteMapEntityHelper siteMapEntityHelper)
			: base(url, siteMapEntityHelper)
		{ }
	}

	class NestedSiteMapEntityHelper
	{
		const string RetrieveSQL =
			@"SELECT [{0}ID] AS ID, [Name], [SEName], [Parent{0}ID] AS ParentID FROM {0}
			WHERE ([{0}ID] IN (SELECT [EntityID] FROM EntityStore WHERE StoreID = @StoreID AND EntityType='{0}') OR @StoreID IS NULL) AND Published = 1 AND Deleted = 0 ORDER BY DisplayOrder";

		readonly UrlHelper Url;
		readonly SiteMapEntityHelper SiteMapEntityHelper;

		public NestedSiteMapEntityHelper(UrlHelper url, SiteMapEntityHelper siteMapEntityHelper)
		{
			Url = url;
			SiteMapEntityHelper = siteMapEntityHelper;
		}

		public NestedSiteMapEntity[] GetEntities(string EntityType, SiteMapSettings settings)
		{
			var _list = new Dictionary<int, NestedSiteMapEntity>();

			var getCommand = GetEntitySQL(EntityType, settings);
			Action<System.Data.IDataReader> readEntities = rd =>
			{
				while(rd.Read())
				{
					var entity = new NestedSiteMapEntity(Url, SiteMapEntityHelper)
					{
						EntityID = rd.FieldInt("ID"),
						Name = XmlCommon.GetLocaleEntry(rd.Field("Name"), Customer.Current.LocaleSetting, false),
						SEName = rd.Field("SEName"),
						ParentEntityID = rd.FieldInt("ParentID"),
						EntityType = EntityType,
					};

					entity.GetProducts(settings);
					_list.Add(entity.EntityID, entity);
				}
			};

			DB.UseDataReader(getCommand, readEntities);

			return OrganizeEntities(_list).ToArray();
		}

		List<NestedSiteMapEntity> OrganizeEntities(Dictionary<int, NestedSiteMapEntity> entities)
		{
			foreach(var ent in entities.Values.Where(e => e.ParentEntityID != 0))
			{
				if(!entities.ContainsKey(ent.ParentEntityID) || entities[ent.ParentEntityID] == null)
					continue;

				var _children = new List<NestedSiteMapEntity>(entities[ent.ParentEntityID].Children);
				_children.Add(ent);
				entities[ent.ParentEntityID].Children = _children.ToArray();
			}

			return new List<NestedSiteMapEntity>(entities.Values.Where(e => e.ParentEntityID == 0));
		}

		SqlCommand GetEntitySQL(string entityType, SiteMapSettings settings)
		{
			entityType = entityType.ToLowerInvariant();

			var cmdGetEntities = new SqlCommand(string.Format(RetrieveSQL, entityType));
			cmdGetEntities.Parameters.Add(
				new SqlParameter("@StoreID", DBNull.Value));

			if(settings.EntityFiltering
				&& (entityType == "category"
					|| entityType == "manufacturer"
					|| entityType == "section"
					|| entityType == "vector"
					|| entityType == "genre"
					|| entityType == "distributor"))
			{
				cmdGetEntities.Parameters["@StoreID"].Value = AppLogic.StoreID();
			}

			return cmdGetEntities;
		}
	}

	class NestedSiteMapEntity : SiteMapEntity
	{
		public int ParentEntityID
		{ get; set; }

		public NestedSiteMapEntity[] Children
		{ get; set; }

		public SiteMapProduct[] Products
		{ get; set; }

		public NestedSiteMapEntity(UrlHelper url, SiteMapEntityHelper siteMapEntityHelper)
			: base(url, siteMapEntityHelper)
		{
			Children = new NestedSiteMapEntity[0];
			Products = new SiteMapProduct[0];
		}

		public void GetProducts(SiteMapSettings settings)
		{
			var retCmd = GetProductsCommand();
			var xList = new List<SiteMapProduct>();

			if(settings.ProductFiltering)
				retCmd.Parameters["@StoreID"].Value = AppLogic.StoreID();

			Action<System.Data.IDataReader> readEntities = rd =>
			{
				while(rd.Read())
				{
					var prd = new SiteMapProduct(Url, SiteMapEntityHelper);
					prd.EntityID = rd.FieldInt("ProductID");
					prd.Name = XmlCommon.GetLocaleEntry(rd.Field("Name"), Customer.Current.LocaleSetting, false);
					prd.SEName = rd.Field("SEName");
					xList.Add(prd);
				}
			};

			DB.UseDataReader(retCmd, readEntities);
			Products = xList.ToArray();
		}

		SqlCommand GetProductsCommand()
		{
			var xCmd = new SqlCommand(string.Format(
				@"SELECT prod.ProductID, [Name], [SEName] 
				FROM Product AS prod 
				INNER JOIN Product{0} AS pm ON pm.ProductID = prod.ProductID
				WHERE pm.{0}ID = @MapID AND (
				Prod.ProductID IN (SELECT ProductID FROM ProductStore WHERE StoreID = @StoreID) OR @StoreID IS NULL) 
				AND Published = 1 and deleted = 0
				ORDER BY DisplayOrder",
				EntityType));

			xCmd.Parameters.Add(new SqlParameter("@MapID", EntityID));
			xCmd.Parameters.Add(new SqlParameter("@StoreID", DBNull.Value));
			return xCmd;
		}

		public override XmlNode ToSiteMapNode(XmlDocument context, SiteMapSettings settings)
		{
			var node = SiteMapEntityHelper.SiteMapNode(Name, Url.BuildEntityLink(EntityType, EntityID, SEName), context);

			if(!settings.ShowProducts)
				return node;

			foreach(SiteMapProduct prod in Products)
				node.AppendChild(prod.ToSiteMapNode(context, settings));

			foreach(NestedSiteMapEntity ent in Children)
				node.AppendChild(ent.ToSiteMapNode(context, settings));

			return node;
		}
	}

	class StandardSiteMap
	{
		readonly UrlHelper UrlHelper;
		readonly SiteMapEntityHelper SiteMapEntityHelper;
		readonly NestedSiteMapEntityHelper NestedSiteMapEntityHelper;

		public StandardSiteMap(UrlHelper urlHelper, SiteMapEntityHelper siteMapEntityHelper, NestedSiteMapEntityHelper nestedSiteMapEntityHelper)
		{
			UrlHelper = urlHelper;
			SiteMapEntityHelper = siteMapEntityHelper;
			NestedSiteMapEntityHelper = nestedSiteMapEntityHelper;
		}

		public string GetEntitySiteMap(string entityType, SiteMapSettings settings)
		{
			var entityMap = NestedSiteMapEntityHelper.GetEntities(entityType, settings);
			return SiteMapNodes(entityMap);
		}

		public string GetEntityProductURLNodes(string entityType, int EntityID, SiteMapSettings settings)
		{
			var entity = new NestedSiteMapEntity(UrlHelper, SiteMapEntityHelper);
			entity.EntityType = entityType;
			entity.EntityID = EntityID;
			entity.GetProducts(settings);

			var sb = new StringBuilder();
			foreach(var siteMapProduct in entity.Products)
				sb.Append(ProductXML(siteMapProduct, settings));

			return sb.ToString();
		}

		string SiteMapNodes(NestedSiteMapEntity[] mapEntities)
		{
			var builder = new StringBuilder();

			foreach(var siteMapEntity in mapEntities)
			{
				builder.Append(SiteMapEntityXML(siteMapEntity.EntityID, siteMapEntity.EntityType));
				builder.Append(SiteMapNodes(siteMapEntity.Children));
			}

			return builder.ToString();
		}

		string SiteMapEntityXML(int entityId, string entityType)
		{
			var entityUrl = UrlHelper.Action(ActionNames.Entity, ControllerNames.SiteMapFeed);
			var storeLocation = new Uri(AppLogic.GetStoreHTTPLocation(true));
			var entityMapLocation = new Uri(storeLocation, entityUrl);
			return string.Format("<sitemap><loc>{0}?entityname={1}&amp;entityid={2}</loc></sitemap>\n", entityMapLocation, entityType, entityId);
		}

		string ProductXML(SiteMapProduct product, SiteMapSettings settings)
		{
			var link = UrlHelper
				.BuildProductLink(product.EntityID, product.SEName);

			return string.Format(
				"<url><loc>{0}{1}</loc><changefreq>{2}</changefreq><priority>{3}</priority></url>\n",
				AppLogic.GetStoreHTTPLocation(
					useSsl: true,
					noVirtualNoSlash: true),
				link,
				settings.ObjectChangeFrequency,
				settings.ObjectPriority);
		}
	}

}
