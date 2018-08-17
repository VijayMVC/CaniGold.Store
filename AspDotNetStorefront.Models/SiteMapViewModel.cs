// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Collections.Generic;

namespace AspDotNetStorefront.Models
{
	public class SiteMapViewModel
	{
		public SiteMapViewModel()
		{
			Products = new List<SiteMapEntity>();
			Topics = new List<SiteMapEntity>();
			Categories = new List<SiteMapEntity>();
			Sections = new List<SiteMapEntity>();
			Manufacturers = new List<SiteMapEntity>();
			CustomerService = new List<SiteMapEntity>();
		}

		public List<SiteMapEntity> Products { get; set; }
		public List<SiteMapEntity> Topics { get; set; }
		public List<SiteMapEntity> Categories { get; set; }
		public List<SiteMapEntity> Sections { get; set; }
		public List<SiteMapEntity> Manufacturers { get; set; }
		public List<SiteMapEntity> CustomerService { get; set; }

		public bool ShowCategories { get; set; }
		public bool ShowSections { get; set; }
		public bool ShowManufacturers { get; set; }
		public bool ShowTopics { get; set; }
		public bool ShowProducts { get; set; }
		public bool ShowCustomerService { get; set; }
		public bool ProductFiltering { get; set; }
		public bool EntityFiltering { get; set; }
		public bool TopicFiltering { get; set; }
	}

	public class SiteMapEntity
	{
		public SiteMapEntity()
		{
			Children = new List<SiteMapEntity>();
		}

		public string Name { get; set; }
		public string Url { get; set; }
		public List<SiteMapEntity> Children { get; set; }
	}
}
