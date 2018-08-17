// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using AspDotNetStorefront.Routing;

namespace AspDotNetStorefrontCore
{
	/// <summary>
	/// Summary description for ProductImageGallery.
	/// </summary>
	public class ProductImageGallery
	{
		readonly int ProductId;
		readonly int VariantId;
		readonly int SkinId;
		readonly string LocaleSetting;
		readonly string ProductSku;
		int ImageCountIndex; // will be 0 if empty
		string VariantColorsCsv;
		string[] VariantColors;
		static readonly string[] ImageNumbers = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" };
		string[,] ImageUrlsIcon;
		string[,] ImageUrlsMedium;
		string[,] ImageUrlsLarge;
		bool HasLargeImage;
		readonly SkinProvider SkinProvider;

		public ProductImageGallery()
		{
			SkinProvider = new SkinProvider();
		}

		public ProductImageGallery(int ProductID, int SkinID, string LocaleSetting, string SKU)
			: this()
		{
			ProductId = ProductID;
			VariantId = AppLogic.GetDefaultProductVariant(ProductId);
			SkinId = SkinID;
			this.LocaleSetting = LocaleSetting;
			ImageCountIndex = 0;
			VariantColorsCsv = string.Empty;
			GalleryHtml = string.Empty;
			GalleryScript = string.Empty;
			HasLargeImage = false;
			ProductSku = SKU;

			BuildVariantColors();

			var useProductIconPics = AppLogic.AppConfigBool("MultiImage.UseProductIconPics");
			var watermarksEnabled = AppLogic.AppConfigBool("Watermark.Enabled");

			if(useProductIconPics)
				BuildUrlsForIconImages(watermarksEnabled);

			// "watermarksEnabled" is intentionally not used for some reason.
			BuildUrlsForMediumImages();
			BuildUrlsForLargeImages(watermarksEnabled);

			// IsEmpty = no icon or medium images were found.
			// Must be checked after attempting to build Icon and Medium
			if(IsEmpty())
				return;

			var productIdSuffix = "_" + ProductId.ToString();
			var urlHelper = DependencyResolver.Current.GetService<UrlHelper>();

			BuildImageGalleryScript(productIdSuffix, watermarksEnabled, urlHelper);

			// This is the logic as described in AppConfig descriptions.
			var useNumberedListForMultiNav = !AppLogic.AppConfigBool("UseImagesForMultiNav") && !useProductIconPics;
			var galleryHasMultipleImages = ImageCountIndex > 1;

			if(!galleryHasMultipleImages)
				return;

			if(useNumberedListForMultiNav)
				BuildGalleryNavAsNumberedLinks(productIdSuffix);
			else
				BuildGalleryNavWithImages(productIdSuffix, useProductIconPics);

		}

		public string GalleryScript { get; set; }

		public string GalleryHtml { get; set; }

		public bool HasSomeLarge => HasLargeImage;

		public bool IsEmpty() => ImageCountIndex == 0;

		void BuildGalleryNavAsNumberedLinks(string productIdSuffix)
		{
			var galleryHtml = new StringBuilder(4096);

			// Build list of links
			galleryHtml.Append("<ul class=\"pagination image-paging\">");
			for(int i = 1; i <= ImageCountIndex; i++)
			{
				if(i == 1)
					galleryHtml.Append("<li class=\"page-link active\">");
				else
					galleryHtml.Append("<li class=\"page-link\">");

				galleryHtml.Append(string.Format("<a href=\"javascript:void(0);\" onclick='setcolorpicidx{0}({1});setActive(this);' class=\"page-number\">{1}", productIdSuffix, i));

				if(i == 1)
					galleryHtml.Append("<span class=\"screen-reader-only\"> Selected</span>");

				galleryHtml.Append("</a></li>");
			}
			galleryHtml.Append("</ul>");

			GalleryHtml = galleryHtml.ToString();
		}
		void BuildGalleryNavWithImages(string productIdSuffix, bool useProductIconPics)
		{
			var useRolloverForMicroImages = AppLogic.AppConfigBool("UseRolloverForMultiNav");
			var jsImageEvent = useRolloverForMicroImages
				? "onMouseOver"
				: "onclick";

			var imageSize = useProductIconPics
				? nameof(ProductImageSize.icon)
				: nameof(ProductImageSize.micro);

			var productIdentifier = AppLogic.AppConfigBool("UseSKUForProductImageName")
				? ProductSku
				: ProductId.ToString();

			var galleryHtml = new StringBuilder(4096);

			galleryHtml.Append("<div class=\"product-gallery-items\">");
			for(int i = 1; i <= ImageCountIndex; i++)
			{
				var imageUrl = GetImageUrl(
					size: imageSize,
					identifier: productIdentifier,
					index: i);

				galleryHtml.Append("<div class=\"product-gallery-item\">");
				if(imageUrl.Length > 0)
				{
					galleryHtml.Append($"	<button class=\"gallery-item-inner button-transparent\" {jsImageEvent}='setcolorpicidx{productIdSuffix}({i});' >");
					galleryHtml.Append($"		<img class='product-gallery-image' alt='Show Picture {i}' src='{imageUrl}' border='0' />");
					galleryHtml.Append("	</button>");
				}
				galleryHtml.Append("</div>");
			}
			galleryHtml.Append("</div>");

			GalleryHtml = galleryHtml.ToString();
		}

		/// <summary>
		/// Builds ImgDHTML
		/// </summary>
		void BuildImageGalleryScript(string productIdSuffix, bool m_WatermarksEnabled, UrlHelper urlHelper)
		{
			var scriptBuilder = new StringBuilder(4096);
			scriptBuilder.AppendLine("<script type=\"text/javascript\">");
			scriptBuilder.AppendLine("var ProductPicIndex" + productIdSuffix + " = 1;");
			scriptBuilder.AppendLine("var ProductColor" + productIdSuffix + " = '';");
			scriptBuilder.AppendLine("var boardpics" + productIdSuffix + " = new Array();");
			scriptBuilder.AppendLine("var boardpicslg" + productIdSuffix + " = new Array();");
			scriptBuilder.AppendLine("var boardpicslgAltText" + productIdSuffix + " = new Array();");
			scriptBuilder.AppendLine("var boardpicslgwidth" + productIdSuffix + " = new Array();");
			scriptBuilder.AppendLine("var boardpicslgheight" + productIdSuffix + " = new Array();");

			var product = new Product(ProductId);
			var altText = HttpContext.Current.Server.UrlEncode(
				string.IsNullOrEmpty(product.SEAltText)
					? string.Format(AppLogic.GetString("popup.alttext"), product.LocaleName)
					: product.SEAltText);

			var popupImageBuilder = DependencyResolver.Current.GetService<IPopupImageBuilder>();

			for(int i = 1; i <= ImageCountIndex; i++)
			{
				foreach(string c in VariantColors)
				{
					string MdUrl = ImageUrl(i, c, nameof(ProductImageSize.medium)).ToLowerInvariant();
					string MdWatermarkedUrl = MdUrl;

					if(m_WatermarksEnabled)
					{
						if(MdUrl.Length > 0)
						{
							string[] split = MdUrl.Split('/');
							string lastPart = split.Last();
							MdUrl = AppLogic.LocateImageURL(lastPart, "PRODUCT", nameof(ProductImageSize.medium), "");
						}
					}

					scriptBuilder.AppendLine("boardpics" + productIdSuffix + "['" + i.ToString() + "," + c + "'] = '" + MdWatermarkedUrl + "';");

					string LgUrl = ImageUrl(i, c, nameof(ProductImageSize.large)).ToLowerInvariant();
					string LgWatermarkedUrl = LgUrl;

					if(m_WatermarksEnabled)
					{
						if(LgUrl.Length > 0)
						{
							string[] split = LgUrl.Split('/');
							string lastPart = split.Last();
							LgUrl = AppLogic.LocateImageURL(lastPart, "PRODUCT", nameof(ProductImageSize.large), "");
						}
					}

					scriptBuilder.AppendLine("boardpicslg" + productIdSuffix + "['" + i.ToString() + "," + c + "'] = '" + LgWatermarkedUrl + "';");

					if(LgUrl.Length > 0)
					{
						var encodedAltText = popupImageBuilder.EncodePopupAltText($"Picture {i} {altText}");
						scriptBuilder.AppendLine("boardpicslgAltText" + productIdSuffix + "['" + i.ToString() + "," + c + "'] = '" + encodedAltText + "';");

						System.Drawing.Size lgsz = CommonLogic.GetImagePixelSize(LgUrl);
						scriptBuilder.AppendLine("boardpicslgwidth" + productIdSuffix + "['" + i.ToString() + "," + c + "'] = '" + lgsz.Width.ToString() + "';");
						scriptBuilder.AppendLine("boardpicslgheight" + productIdSuffix + "['" + i.ToString() + "," + c + "'] = '" + lgsz.Height.ToString() + "';");
					}
				}
			}

			scriptBuilder.AppendLine("function changecolorimg" + productIdSuffix + "() {");
			scriptBuilder.AppendLine("	var scidx = ProductPicIndex" + productIdSuffix + " + ',' + ProductColor" + productIdSuffix + ".toLowerCase();");
			scriptBuilder.AppendLine("	document.ProductPic" + ProductId.ToString() + ".src=boardpics" + productIdSuffix + "[scidx];");
			scriptBuilder.AppendLine("}");

			scriptBuilder.AppendLine("function popuplarge" + productIdSuffix + "() {");
			scriptBuilder.AppendLine("	var scidx = ProductPicIndex" + productIdSuffix + " + ',' + ProductColor" + productIdSuffix + ".toLowerCase();");
			scriptBuilder.AppendLine("	var LargeSrc = encodeURIComponent(boardpicslg" + productIdSuffix + "[scidx]);");
			scriptBuilder.AppendLine("	if(boardpicslg" + productIdSuffix + "[scidx] != '') {");

			var popupUrl = urlHelper.Action(ActionNames.PopUp, ControllerNames.Image);

			scriptBuilder.AppendLine("		window.open('" + popupUrl + "?" + RouteDataKeys.ImagePath + "=' + LargeSrc + '&altText=' + boardpicslgAltText" + productIdSuffix + "[scidx],'LargerImage" + CommonLogic.GetRandomNumber(1, 100000) + "','toolbar=no,location=no,directories=no,status=no,menubar=no,scrollbars=" + CommonLogic.IIF(AppLogic.AppConfigBool("ResizableLargeImagePopup"), "yes", "no") + ",resizable=" + CommonLogic.IIF(AppLogic.AppConfigBool("ResizableLargeImagePopup"), "yes", "no") + ",copyhistory=no,width=' + boardpicslgwidth" + productIdSuffix + "[scidx] + ',height=' + boardpicslgheight" + productIdSuffix + "[scidx] + ',left=0,top=0');");
			scriptBuilder.AppendLine("	} else {");
			scriptBuilder.AppendLine("		alert('There is no large image available for this picture');");
			scriptBuilder.AppendLine("	}");
			scriptBuilder.AppendLine("}");

			scriptBuilder.AppendLine("function setcolorpicidx" + productIdSuffix + "(idx) {");
			scriptBuilder.AppendLine("	ProductPicIndex" + productIdSuffix + " = idx;");
			scriptBuilder.AppendLine("	changecolorimg" + productIdSuffix + "();");
			scriptBuilder.AppendLine("}");

			scriptBuilder.AppendLine("function setActive(element) {");
			scriptBuilder.AppendLine("	adnsf$('li.page-link').removeClass('active');");
			scriptBuilder.AppendLine("  adnsf$('a.page-number').children().remove();");
			scriptBuilder.AppendLine("	adnsf$(element).parent().addClass('active');");
			scriptBuilder.AppendLine("  adnsf$(element).append('<span class=\"screen-reader-only\"> Selected</span>');");
			scriptBuilder.AppendLine("}");

			scriptBuilder.AppendLine("function cleansizecoloroption" + productIdSuffix + "(theVal) {");
			scriptBuilder.AppendLine("	if(theVal.indexOf('[') != -1) {");
			scriptBuilder.AppendLine("		theVal = theVal.substring(0, theVal.indexOf('['))");
			scriptBuilder.AppendLine("	}");
			scriptBuilder.AppendLine("	theVal = theVal.replace(/[\\W]/g,\"\");");
			scriptBuilder.AppendLine("	theVal = theVal.toLowerCase();");
			scriptBuilder.AppendLine("	return theVal;");
			scriptBuilder.AppendLine("}");

			scriptBuilder.AppendLine("function setcolorpic" + productIdSuffix + "(color) {");
			scriptBuilder.AppendLine("	while(color != unescape(color)) {");
			scriptBuilder.AppendLine("		color = unescape(color);");
			scriptBuilder.AppendLine("	}");

			scriptBuilder.AppendLine("	if(color == '-,-' || color == '-') {");
			scriptBuilder.AppendLine("		color = '';");
			scriptBuilder.AppendLine("	}");

			scriptBuilder.AppendLine("	if(color != '' && color.indexOf(',') != -1) {");
			scriptBuilder.AppendLine("		color = color.substring(0,color.indexOf(',')).replace(new RegExp(\"'\", 'gi'), '');"); // remove sku from color select value
			scriptBuilder.AppendLine("	}");

			scriptBuilder.AppendLine("	if(color != '' && color.indexOf('[') != -1) {");
			scriptBuilder.AppendLine("		color = color.substring(0,color.indexOf('[')).replace(new RegExp(\"'\", 'gi'), '');");
			scriptBuilder.AppendLine("		color = color.replace(/[\\s]+$/g,\"\");");
			scriptBuilder.AppendLine("	}");

			scriptBuilder.AppendLine("	ProductColor" + productIdSuffix + " = cleansizecoloroption" + productIdSuffix + "(color);");
			scriptBuilder.AppendLine("	changecolorimg" + productIdSuffix + "();");
			scriptBuilder.AppendLine("	return (true);");
			scriptBuilder.AppendLine("}");

			scriptBuilder.AppendLine("</script>");
			GalleryScript = scriptBuilder.ToString();
		}

		void BuildUrlsForLargeImages(bool m_WatermarksEnabled)
		{
			ImageUrlsLarge = new string[ImageNumbers.Length, VariantColors.Length];
			for(int j = ImageNumbers.GetLowerBound(0); j <= ImageNumbers.GetUpperBound(0); j++)
			{
				int ImgIdx = Localization.ParseUSInt(ImageNumbers[j]);
				for(int i = VariantColors.GetLowerBound(0); i <= VariantColors.GetUpperBound(0); i++)
				{
					string Url = string.Empty;
					if(ProductSku == string.Empty)
					{
						Url = AppLogic.LookupProductImageByNumberAndColor(ProductId, SkinId, LocaleSetting, ImgIdx, AppLogic.RemoveAttributePriceModifier(VariantColors[i]), nameof(ProductImageSize.large));
					}
					else
					{
						Url = AppLogic.LookupProductImageByNumberAndColor(ProductId, SkinId, ProductSku, LocaleSetting, ImgIdx, AppLogic.RemoveAttributePriceModifier(VariantColors[i]), nameof(ProductImageSize.large));
					}

					if(m_WatermarksEnabled && Url.Length != 0 && Url.IndexOf("nopicture") == -1)
					{
						if(Url.StartsWith("/"))
						{
							ImageUrlsLarge[j, i] = Url.Substring(HttpContext.Current.Request.ApplicationPath.Length);
						}
						else
						{
							ImageUrlsLarge[j, i] = Url.Substring(HttpContext.Current.Request.ApplicationPath.Length - 1);
						}

						if(ImageUrlsLarge[j, i].StartsWith("/"))
						{
							ImageUrlsLarge[j, i] = ImageUrlsLarge[j, i].TrimStart('/');
						}

						HasLargeImage = true;
					}
					else if(Url.Length == 0 || Url.IndexOf("nopicture") != -1)
					{
						ImageUrlsLarge[j, i] = string.Empty;
					}
					else
					{
						HasLargeImage = true;
						ImageUrlsLarge[j, i] = Url;
					}
				}
			}
		}

		void BuildUrlsForMediumImages()
		{
			ImageUrlsMedium = new string[ImageNumbers.Length, VariantColors.Length];
			for(int j = ImageNumbers.GetLowerBound(0); j <= ImageNumbers.GetUpperBound(0); j++)
			{
				int ImgIdx = Localization.ParseUSInt(ImageNumbers[j]);
				for(int i = VariantColors.GetLowerBound(0); i <= VariantColors.GetUpperBound(0); i++)
				{
					if(ProductSku == string.Empty)
					{
						ImageUrlsMedium[j, i] = AppLogic.LookupProductImageByNumberAndColor(ProductId, SkinId, LocaleSetting, ImgIdx, AppLogic.RemoveAttributePriceModifier(VariantColors[i]), nameof(ProductImageSize.medium));
					}
					else
					{
						ImageUrlsMedium[j, i] = AppLogic.LookupProductImageByNumberAndColor(ProductId, SkinId, ProductSku, LocaleSetting, ImgIdx, AppLogic.RemoveAttributePriceModifier(VariantColors[i]), nameof(ProductImageSize.medium));
					}
				}
			}
			for(int j = ImageNumbers.GetLowerBound(0); j <= ImageNumbers.GetUpperBound(0); j++)
			{
				int ImgIdx = Localization.ParseUSInt(ImageNumbers[j]);
				if(ImageUrlsMedium[j, 0].IndexOf("nopicture") == -1)
				{
					ImageCountIndex = ImgIdx;
				}
			}
		}

		void BuildUrlsForIconImages(bool m_WatermarksEnabled)
		{
			ImageUrlsIcon = new string[ImageNumbers.Length, VariantColors.Length];
			for(int x = ImageNumbers.GetLowerBound(0); x <= ImageNumbers.GetUpperBound(0); x++)
			{
				int ImgIdx = Localization.ParseUSInt(ImageNumbers[x]);
				for(int i = VariantColors.GetLowerBound(0); i <= VariantColors.GetUpperBound(0); i++)
				{
					string Url = string.Empty;
					if(ProductSku == string.Empty)
					{
						Url = AppLogic.LookupProductImageByNumberAndColor(ProductId, SkinId, LocaleSetting, ImgIdx, AppLogic.RemoveAttributePriceModifier(VariantColors[i]), nameof(ProductImageSize.icon));
					}
					else
					{
						Url = AppLogic.LookupProductImageByNumberAndColor(ProductId, SkinId, ProductSku, LocaleSetting, ImgIdx, AppLogic.RemoveAttributePriceModifier(VariantColors[i]), nameof(ProductImageSize.icon));
					}
					if(m_WatermarksEnabled && Url.Length != 0 && Url.IndexOf("nopicture") == -1)
					{
						if(Url.StartsWith("/"))
						{
							ImageUrlsIcon[x, i] = Url.Substring(HttpContext.Current.Request.ApplicationPath.Length);
						}
						else
						{
							ImageUrlsIcon[x, i] = Url.Substring(HttpContext.Current.Request.ApplicationPath.Length - 1);
						}

						if(ImageUrlsIcon[x, i].StartsWith("/"))
						{
							ImageUrlsIcon[x, i] = ImageUrlsIcon[x, i].TrimStart('/');
						}
					}
					else
					{
						ImageUrlsIcon[x, i] = Url;
					}
				}
			}
			for(int x = ImageNumbers.GetLowerBound(0); x <= ImageNumbers.GetUpperBound(0); x++)
			{
				int ImgIdx = Localization.ParseUSInt(ImageNumbers[x]);
				if(ImageUrlsIcon[x, 0].IndexOf("nopicture") == -1)
				{
					ImageCountIndex = ImgIdx;
				}
			}
		}

		void BuildVariantColors()
		{
			VariantColors = new string[1] { "" };
			if(VariantColorsCsv == string.Empty)
			{
				using(var dbconn = new SqlConnection(DB.GetDBConn()))
				{
					dbconn.Open();
					using(var rs = DB.GetRS("select Colors from productvariant   with (NOLOCK)  where VariantID=" + VariantId.ToString(), dbconn))
					{
						if(rs.Read())
						{
							VariantColorsCsv = DB.RSFieldByLocale(rs, "Colors", Localization.GetDefaultLocale()); // remember to add "empty" color to front, for no color selected
							if(VariantColorsCsv.Length != 0)
							{
								VariantColors = ("," + VariantColorsCsv).Split(',');
							}
						}
					}
				}
			}
			else
			{
				VariantColors = ("," + VariantColorsCsv).Split(',');
			}
			if(VariantColorsCsv.Length != 0)
			{
				for(int i = VariantColors.GetLowerBound(0); i <= VariantColors.GetUpperBound(0); i++)
				{
					string s2 = AppLogic.RemoveAttributePriceModifier(VariantColors[i]);
					VariantColors[i] = CommonLogic.MakeSafeFilesystemName(s2);
				}
			}
		}

		string GetImageUrl(string size, string identifier, int index)
		{
			var locale = HttpContext.Current.GetCustomer().LocaleSetting;

			var imageUrl = AppLogic.LocateImageURL(string.Format("{0}_{1}.gif", identifier, index), "product", size, locale);
			if(!CommonLogic.FileExists(imageUrl))
				imageUrl = AppLogic.LocateImageURL(string.Format("{0}_{1}_.gif", identifier, index), "product", size, locale);
			if(!CommonLogic.FileExists(imageUrl))
				imageUrl = AppLogic.LocateImageURL(string.Format("{0}_{1}.jpg", identifier, index), "product", size, locale);
			if(!CommonLogic.FileExists(imageUrl))
				imageUrl = AppLogic.LocateImageURL(string.Format("{0}_{1}_.jpg", identifier, index), "product", size, locale);
			if(!CommonLogic.FileExists(imageUrl))
				imageUrl = AppLogic.LocateImageURL(string.Format("{0}_{1}.png", identifier, index), "product", size, locale);
			if(!CommonLogic.FileExists(imageUrl))
				imageUrl = AppLogic.LocateImageURL(string.Format("{0}_{1}_.png", identifier, index), "product", size, locale);
			if(!CommonLogic.FileExists(imageUrl))
			{
				if(StringComparer.OrdinalIgnoreCase.Equals(size, nameof(ProductImageSize.large)) || StringComparer.OrdinalIgnoreCase.Equals(size, nameof(ProductImageSize.medium)))
					imageUrl = AppLogic.LocateImageURL(string.Format("skins/{0}/images/nopicture.gif", SkinProvider.GetSkinNameById(SkinId)));
				else
					imageUrl = AppLogic.LocateImageURL(string.Format("skins/{0}/images/nopicture{1}.gif", SkinProvider.GetSkinNameById(SkinId), size));
			}
			return imageUrl;
		}

		public int MaxImageIndex
		{
			get
			{
				return ImageCountIndex;
			}
		}

		int GetColorIndex(string Color)
		{
			int i = 0;
			foreach(string s in VariantColors)
			{
				if(s == Color)
				{
					return i;
				}
				i++;
			}
			return 0;
		}

		public string ImageUrl(int index, string color, string imageSize)
		{
			imageSize = imageSize.ToLower(CultureInfo.InstalledUICulture);
			try
			{
				switch(imageSize)
				{
					case nameof(ProductImageSize.icon):
						return string.Empty;
					case nameof(ProductImageSize.medium):
						return ImageUrlsMedium[index - 1, GetColorIndex(color)].Replace("//", "/");
					case nameof(ProductImageSize.large):
						return ImageUrlsLarge[index - 1, GetColorIndex(color)].Replace("//", "/");
				}

				return string.Empty;
			}
			catch
			{
				return string.Empty;
			}
		}

	}

	public enum ProductImageSize
	{
		micro,
		icon,
		medium,
		large
	}
}
