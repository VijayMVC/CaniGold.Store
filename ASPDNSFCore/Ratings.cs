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
using System.Threading;
using System.Web;
using System.Web.Mvc;
using AspDotNetStorefront.ClientResource;
using AspDotNetStorefront.Routing;

namespace AspDotNetStorefrontCore
{
	/// <summary>
	/// Summary description for Ratings.
	/// </summary>
	public class Ratings
	{
		static readonly SkinProvider SkinProvider;

		static Ratings()
		{
			SkinProvider = new SkinProvider();
		}

		static UrlHelper Url
		{ get { return DependencyResolver.Current.GetService<UrlHelper>(); } }

		/// <summary>
		/// Gets the product rating.
		/// </summary>
		/// <param name="CustomerID">The CustomerID.</param>
		/// <param name="ProductID">The ProductID.</param>
		/// <returns>Returns the product rating.</returns>
		static public int GetProductRating(int CustomerID, int ProductID)
		{
			if(CustomerID == 0)
				return 0;

			int uname = 0;
			using(SqlConnection dbconn = new SqlConnection(DB.GetDBConn()))
			{
				dbconn.Open();
				using(IDataReader rs = DB.GetRS("Select rating from Rating   with (NOLOCK)  where CustomerID=" + CustomerID.ToString() + " and ProductID=" + ProductID.ToString(), dbconn))
				{
					if(rs.Read())
					{
						uname = DB.RSFieldInt(rs, "rating");
					}
				}
			}
			return uname;
		}

		/// <summary>
		/// Determine if the string has bad words.
		/// </summary>
		/// <param name="s">The string.</param>
		/// <returns>Returns TRUE if the string has bad words otherwise FALSE.</returns>
		static public bool StringHasBadWords(String s)
		{
			if(string.IsNullOrWhiteSpace(s))
			{
				return false;
			}

			String sql = "aspdnsf_CheckFilthy " + DB.SQuote(s) + "," + DB.SQuote(Thread.CurrentThread.CurrentUICulture.Name);

			bool hasBad = false;

			using(SqlConnection dbconn = new SqlConnection(DB.GetDBConn()))
			{
				dbconn.Open();
				using(IDataReader rs = DB.GetRS(sql, dbconn))
				{
					rs.Read();
					int IsFilthy = DB.RSFieldInt(rs, "IsFilthy");

					if(IsFilthy == 1)
						hasBad = true;

				}

			}

			return hasBad;
		}

		/// <summary>
		/// Displays the product rating
		/// </summary>
		/// <param name="customer">Customer object</param>
		/// <param name="productId">Product ID of the product rating to display</param>
		/// <param name="categoryId">Category ID of the product rating to display</param>
		/// <param name="sectionId">Section ID of the product rating to display</param>
		/// <param name="manufacturerId">Manufacturer ID of the product rating to display</param>
		/// <param name="skinId">skin id of the page</param>
		/// <param name="encloseInTab">set to true if not to be displayed in a tabUI</param>
		/// <returns>returns string html to be rendered</returns>
		static public string Display(Customer customer, int productId, int categoryId, int sectionId, int manufacturerId, int skinId, bool encloseInTab)
		{
			var productName = AppLogic.GetProductName(productId, customer.LocaleSetting);
			var outputString = new StringBuilder(50000);

			if(!AppLogic.IsAdminSite)
			{
				outputString.Append($"<input type=\"hidden\" name=\"ProductID\" value=\"{productId}\">");
				outputString.Append($"<input type=\"hidden\" name=\"CategoryID\" value=\"{categoryId}\">");
				outputString.Append($"<input type=\"hidden\" name=\"SectionID\" value=\"{sectionId}\">");
				outputString.Append($"<input type=\"hidden\" name=\"ManufacturerID\" value=\"{manufacturerId}\">");

				if(!encloseInTab)
					outputString.Append("<input type=\"hidden\" name=\"productTabs\" value=\"2\">");
			}

			if(encloseInTab)
				outputString.Append($"<h2 class=\"group-header rating-header\">{AppLogic.GetString("Header.ProductRatings")}</h2>");

			// RATINGS BODY:
			var productQuery = "aspdnsf_ProductStats @productId, @storeId";
			var productParams = new SqlParameter[]
			{
				new SqlParameter("@productId", productId),
				new SqlParameter("@storeId", AppLogic.StoreID())
			};

			var ratingsCount = 0;
			var ratingsAverage = 0M;

			using(var connection = new SqlConnection(DB.GetDBConn()))
			{
				connection.Open();
				using(var reader = DB.GetRS(productQuery, connection, productParams))
				{
					reader.Read();
					ratingsCount = DB.RSFieldInt(reader, "NumRatings");
					ratingsAverage = DB.RSFieldDecimal(reader, "AvgRating");
				}
			}

			var ratingPercentages = new int[6]; // indexes 0-5, but we only use indexes 1-5

			using(var connection = new SqlConnection(DB.GetDBConn()))
			{
				var ratingQuery = @"SELECT ProductID, Rating, COUNT(Rating) AS N
									FROM Rating WITH (NOLOCK)
									WHERE Productid = @productId
										AND StoreID = @storeId
									GROUP BY Productid, Rating
									ORDER BY Rating";
				var ratingParams = new SqlParameter[]
				{
					new SqlParameter("@productId", productId),
					new SqlParameter("@storeId", AppLogic.StoreID())
				};

				connection.Open();
				using(var reader = DB.GetRS(ratingQuery, connection, ratingParams))
				{
					while(reader.Read())
					{
						var numRatings = DB.RSFieldInt(reader, "N");
						var ratingPercentage = ((decimal)numRatings) / ratingsCount;
						var ratingForDisplay = (int)(ratingPercentage * 100.0M);

						ratingPercentages[DB.RSFieldInt(reader, "Rating")] = ratingForDisplay;
					}
				}
			}

			var orderIndex = 0;
			if("OrderBy".Equals(CommonLogic.FormCanBeDangerousContent("__EVENTTARGET"), StringComparison.InvariantCultureIgnoreCase))
				orderIndex = CommonLogic.FormNativeInt("OrderBy");

			if(orderIndex == 0)
				orderIndex = 3;

			var pageSize = AppLogic.AppConfigUSInt("RatingsPageSize");
			var pageNumber = CommonLogic.QueryStringUSInt("PageNum");

			if(pageNumber == 0)
				pageNumber = 1;

			if(pageSize == 0)
				pageSize = 10;

			if(CommonLogic.QueryStringCanBeDangerousContent("show") == "all")
			{
				pageSize = 1000000;
				pageNumber = 1;
			}

			using(var connection = new SqlConnection(DB.GetDBConn()))
			{
				connection.Open();
				using(var command = new SqlCommand())
				{
					command.Connection = connection;
					command.CommandType = CommandType.StoredProcedure;
					command.CommandText = "aspdnsf_GetProductComments";
					command.Parameters.Add(new SqlParameter("@ProductID", SqlDbType.Int));
					command.Parameters.Add(new SqlParameter("@votingcustomer", SqlDbType.Int));
					command.Parameters.Add(new SqlParameter("@pagesize", SqlDbType.Int));
					command.Parameters.Add(new SqlParameter("@pagenum", SqlDbType.Int));
					command.Parameters.Add(new SqlParameter("@sort", SqlDbType.TinyInt));
					command.Parameters.Add(new SqlParameter("@storeID", SqlDbType.Int));

					command.Parameters["@ProductID"].Value = productId;
					command.Parameters["@votingcustomer"].Value = customer.CustomerID;
					command.Parameters["@pagesize"].Value = pageSize;
					command.Parameters["@pagenum"].Value = pageNumber;
					command.Parameters["@sort"].Value = orderIndex;
					command.Parameters["@storeID"].Value = AppLogic.StoreID();

					var reader = command.ExecuteReader();
					reader.Read();

					var rowsCount = Convert.ToInt32(reader["totalcomments"]);
					var pagesCount = Convert.ToInt32(reader["pages"]);
					reader.NextResult();

					if(pageNumber > pagesCount && pageNumber > 1 && rowsCount == 0)
					{
						reader.Close();

						var redirectUrl = Url.BuildProductLink(
							id: productId,
							additionalRouteValues: new Dictionary<string, object>
							{
								{ "pagenum", pageNumber - 1 }
							});

						HttpContext.Current.Response.Redirect(redirectUrl);
					}

					var startRow = (pageSize * (pageNumber - 1)) + 1;
					var stopRow = (startRow + pageSize - 1) > rowsCount
						? rowsCount
						: startRow + pageSize - 1;

					if(ratingsCount > 0)
					{
						outputString.Append($"<span itemprop=\"aggregateRating\" itemscope itemtype=\"https://schema.org/AggregateRating\">{Environment.NewLine}");
						outputString.Append($"<meta itemprop=\"ratingValue\" content=\"{ratingsAverage}\"/>{Environment.NewLine}");
						outputString.Append($"<meta itemprop=\"reviewCount\" content=\"{ratingsCount}\"/>{Environment.NewLine}");
						outputString.Append($"<meta itemprop=\"bestRating\" content=\"5\"/>{Environment.NewLine}");
						outputString.Append($"<meta itemprop=\"worstRating\" content=\"1\"/>{Environment.NewLine}");
						outputString.Append($"</span>{Environment.NewLine}");
					}

					outputString.Append("<div class=\"page-row total-rating-row\">");
					outputString.Append("   <div class=\"rating-stars-wrap\">");
					outputString.Append(BuildStarImages(ratingsAverage, skinId) + "<span class=\"ratings-average-wrap\"> (" + string.Format("{0:f}", ratingsAverage) + ")");
					outputString.Append($"<span class=\"screen-reader-only\">{AppLogic.GetString("ratings.outof5")}</span></span>");
					outputString.Append("   </div>");
					outputString.Append("   <div class=\"rating-count-wrap\">");
					outputString.Append($"       <span>{AppLogic.GetString("ratings.cs.23")}</span> {ratingsCount}");
					outputString.Append("   </div>");
					outputString.Append("</div>");

					var rateScript = $"javascript:RateIt({productId});";
					var productRating = GetProductRating(customer.CustomerID, productId);

					outputString.Append("<div class=\"page-row rating-link-row\">");

					if(productRating != 0)
					{
						outputString.Append("<div class=\"rating-link-wrap\">");
						outputString.Append("   <span>" + AppLogic.GetString("ratings.cs.24", skinId, Thread.CurrentThread.CurrentUICulture.Name) + " " + productRating.ToString() + "</span>");
						outputString.Append("</div>");

						if(!AppLogic.IsAdminSite)
						{
							outputString.Append("<div class=\"rating-link-wrap\">");
							outputString.Append($"   <a class=\"btn btn-default change-rating-button\" href=\"{rateScript}\">{AppLogic.GetString("ratings.cs.25")}</a> ");
							outputString.Append($"	<span>{AppLogic.GetString("ratings.cs.26")}</span>");
							outputString.Append("</div>");
						}
					}
					else
					{
						if((AppLogic.AppConfigBool("RatingsCanBeDoneByAnons") || customer.IsRegistered) && !AppLogic.IsAdminSite)
						{
							outputString.Append("<div class=\"rating-link-wrap\">");
							outputString.Append($"   <a class=\"btn btn-default add-rating-button\" href=\"{rateScript}\">{AppLogic.GetString("ratings.cs.28")}</a> ");
							outputString.Append($"	<span>{AppLogic.GetString("ratings.cs.27")}</span>");
							outputString.Append("</div>");
						}
						else
						{
							outputString.Append("<div class=\"rating-link-wrap\">");
							outputString.Append($"   <span>{AppLogic.GetString("ratings.cs.29")}</span>");
							outputString.Append("</div>");
						}
					}
					outputString.Append("</div>");

					if(rowsCount > 0)
					{
						int idSuffix = 0;
						while(reader.Read())
						{
							var firstName = string.IsNullOrEmpty(reader["FirstName"].ToString())
								? AppLogic.GetString("ratings.cs.14")
								: reader["FirstName"].ToString();

							outputString.Append($"<div class=\"page-row rating-comment-row\" itemprop=\"review\" itemscope itemtype=\"https://schema.org/Review\">{Environment.NewLine}");
							outputString.Append($"<meta itemprop=\"datePublished\" content=\"{Convert.ToDateTime(reader["CreatedOn"]).ToString("yyyy-MM-dd")}\"/>{Environment.NewLine}");
							outputString.Append($"<meta itemprop=\"itemReviewed\" content=\"{productName}\"/>{Environment.NewLine}");
							outputString.Append("	<div class=\"rating-author-wrap\">\n");

							outputString.Append($"		<span class=\"rating-row-number\">{reader["rownum"]}. </span><span class=\"rating-row-author\" itemprop=\"author\">"
								+ HttpContext.Current.Server.HtmlEncode(firstName)
								+ "</span> <span class=\"rating-row-said\">"
								+ AppLogic.GetString("ratings.cs.15")
								+ " "
								+ Localization.ToThreadCultureShortDateString(Convert.ToDateTime(reader["CreatedOn"]))
								+ ", "
								+ AppLogic.GetString("ratings.cs.16")
								+ " </span>");

							var userRating = (int)(reader["Rating"]);

							outputString.Append("	</div>");
							outputString.Append($"<div class=\"rating-comment-stars\" itemprop=\"reviewRating\" itemscope itemtype=\"https://schema.org/Rating\">{Environment.NewLine}");
							outputString.Append($"<meta itemprop=\"bestRating\" content=\"5\"/>{Environment.NewLine}");
							outputString.Append($"<meta itemprop=\"worstRating\" content=\"1\"/>{Environment.NewLine}");
							outputString.Append($"<meta itemprop=\"ratingValue\" content=\"{userRating}\"/>{Environment.NewLine}");
							outputString.Append($"<span class=\"screen-reader-only\">{userRating} {AppLogic.GetString("ratings.outof5")}</span></span>");
							outputString.Append(BuildStarImages(Convert.ToDecimal(reader["Rating"]), skinId));
							outputString.Append("	</div>");
							outputString.Append("	<div class=\"rating-comments\" itemprop=\"reviewBody\">\n");
							outputString.Append(HttpContext.Current.Server.HtmlEncode(reader["Comments"].ToString()));
							outputString.Append("	</div>\n");
							outputString.Append("</div>\n");
							outputString.Append("<div class=\"form rating-comment-helpfulness-wrap\">");
							outputString.Append("	<div class=\"form-group\">");
							outputString.Append("		<fieldset>");

							if(customer.CustomerID != Convert.ToInt32(reader["CustomerID"]))
							{
								outputString.Append($"<legend id=\"ratings-legend\" class=\"rating-comment-helpfulness-legend\">{AppLogic.GetString("ratings.cs.42")}</legend>");

								idSuffix++;
								if(!AppLogic.IsAdminSite)
								{
									outputString.Append($"<input id=\"helpfulyes_{idSuffix}\" type=\"radio\" name=\"helpful_{productId}_{reader["CustomerID"]}\" onClick=\"return RateComment('{productId}'," +
										$"'{customer.CustomerID}','Yes','{reader["CustomerID"]}');" +
										$"\" {CommonLogic.IIF(Convert.ToInt16(reader["CommentHelpFul"]) == 1, " checked ", string.Empty)}\">\n");

									outputString.Append($"<label for=\"helpfulyes_{idSuffix}\">{AppLogic.GetString("ratings.cs.43")}</label> \n");

									outputString.Append($"<input id=\"helpfulno_{idSuffix}\" type=\"radio\" name=\"helpful_{productId}_{reader["CustomerID"]}\" onClick=\"return RateComment('{productId}'," +
										$"'{customer.CustomerID}','No','{reader["CustomerID"]}');" +
										$"\" {CommonLogic.IIF(Convert.ToInt16(reader["CommentHelpFul"]) == 0, " checked ", string.Empty)}\">\n");

									outputString.Append($"<label for=\"helpfulno_{idSuffix}\">{AppLogic.GetString("ratings.cs.44")}</label> \n");
								}
								else
								{
									outputString.Append($"<input id=\"helpfulyes_{idSuffix}\" type=\"radio\" name=\"helpful_{productId}_{reader["CustomerID"]}\" " +
										$"{CommonLogic.IIF(Convert.ToInt16(reader["CommentHelpFul"]) == 1, " checked ", string.Empty)}>\n");

									outputString.Append($"<label for=\"helpfulyes_{idSuffix}\">{AppLogic.GetString("ratings.cs.43")}</label>\n");

									outputString.Append($"<input id=\"helpfulno_{idSuffix}\" type=\"radio\" name=\"helpful_{productId}_{reader["CustomerID"]}\" " +
										$"{ CommonLogic.IIF(Convert.ToInt16(reader["CommentHelpFul"]) == 0, " checked ", string.Empty)}>\n");

									outputString.Append($"<label for=\"helpfulno_{idSuffix}\" >{AppLogic.GetString("ratings.cs.44")}</label>\n");
								}
							}

							outputString.Append("		</fieldset>\n");
							outputString.Append("	</div>\n");
							outputString.Append("	<div class=\"form-text rating-helpfulness-text\">");

							outputString.Append($"			({reader["FoundHelpful"].ToString()} {AppLogic.GetString("ratings.cs.17")} " +
								$"{CommonLogic.IIF(customer.CustomerID != Convert.ToInt32(reader["CustomerID"]), AppLogic.GetString("ratings.cs.18"), AppLogic.GetString("ratings.cs.19"))} " +
								$"{AppLogic.GetString("ratings.cs.20")}, {reader["FoundNotHelpful"].ToString()} {AppLogic.GetString("ratings.cs.21")})");

							outputString.Append("	</div>\n");
							outputString.Append("</div>\n");
						}
					}
					reader.Close();

					if(rowsCount > 0)
					{
						outputString.Append("<div class=\"page-row comments-count-wrap\">");
						outputString.AppendFormat(AppLogic.GetString("ratings.cs.37"), startRow, stopRow, rowsCount);

						if(pagesCount > 1)
						{
							outputString.Append(" (");
							if(pageNumber > 1)
							{
								var url = Url.BuildProductLink(
									id: CommonLogic.QueryStringUSInt("ProductID"),
									additionalRouteValues: new Dictionary<string, object>
									{
										{ "OrderBy", orderIndex },
										{ "pagenum", pageNumber - 1 },
									});

								outputString.AppendFormat(
									"<a href=\"{0}\">{1} {2}</a>",
									url,
									AppLogic.GetString("ratings.cs.10"),
									pageSize);
							}

							if(pageNumber > 1 && pageNumber < pagesCount)
								outputString.Append(" | ");

							if(pageNumber < pagesCount)
							{
								var url = Url.BuildProductLink(
									id: CommonLogic.QueryStringUSInt("ProductID"),
									additionalRouteValues: new Dictionary<string, object>
									{
										{ "OrderBy", orderIndex },
										{ "pagenum", pageNumber + 1 },
									});

								outputString.AppendFormat(
									"<a href=\"{0}\">{1} {2}</a>",
									url,
									AppLogic.GetString("ratings.cs.11"),
									pageSize);
							}

							outputString.Append(")");
						}

						outputString.Append("</div>\n");
						outputString.Append("<div class=\"page-row comments-pager-wrap\">");
						if(pagesCount > 1)
						{
							var url = Url.BuildProductLink(
								id: CommonLogic.QueryStringUSInt("ProductID"),
								additionalRouteValues: new Dictionary<string, object>
								{
									{ "show", "all" },
									{ "pagenum", pageNumber + 1 },
								});

							outputString.AppendFormat(
								"<a href=\"{0}\">{1}</a> {2}",
								url,
								AppLogic.GetString("ratings.cs.28"),
								AppLogic.GetString("ratings.cs.38"));
						}
						outputString.Append("</div>\n");
					}

					// END RATINGS BODY:

					if(!AppLogic.IsAdminSite)
					{
						var rateCommentUrl = Url.Action(
							actionName: ActionNames.RateComment,
							controllerName: ControllerNames.Rating);

						var rateProductUrl = Url.Action(
							actionName: ActionNames.Index,
							controllerName: ControllerNames.Rating);

						outputString.AppendLine($"<div id=\"RateCommentDiv\" name=\"RateCommentDiv\" style=\"position:absolute; left:0px; top:0px; visibility:{AppLogic.AppConfig("RatingsCommentFrameVisibility")}; z-index:2000; \">");
						outputString.Append($"<iframe name=\"RateCommentFrm\" id=\"RateCommentFrm\" width=\"400\" height=\"100\" hspace=\"0\" vspace=\"0\" marginheight=\"0\" marginwidth=\"0\" frameborder=\"0\" noresize scrolling=\"yes\" src=\"{Url.Content("~/empty.htm")}\"></iframe>");
						outputString.AppendLine("</div>");

						var scriptBuilder = new StringBuilder();
						scriptBuilder.AppendLine("<script type=\"text/javascript\">");
						scriptBuilder.AppendLine("function RateComment(ProductID, MyCustomerID, MyVote, RatersCustomerID) {");
						scriptBuilder.AppendLine($"	RateCommentFrm.location = '{rateCommentUrl}?Productid=' + ProductID + '&VotingCustomerID=' + MyCustomerID + '&Vote=' + MyVote + '&RatingCustomerID=' + RatersCustomerID");
						scriptBuilder.AppendLine("}");
						scriptBuilder.AppendLine("function RateIt(ProductID) {");
						scriptBuilder.AppendLine($"	window.open('{rateProductUrl}?Productid=' + ProductID + '&refresh=no&returnurl={HttpContext.Current.Server.UrlEncode(CommonLogic.PageInvocation())}','ASPDNSF_ML{CommonLogic.GetRandomNumber(1, 100000)}','height=550,width=400,top=10,left=20,status=no,toolbar=no,menubar=no,scrollbars=yes,location=no')");
						scriptBuilder.AppendLine("}");
						scriptBuilder.AppendLine("</script>");

						var httpContext = DependencyResolver.Current.GetService<HttpContextBase>();
						var clientScriptRegistry = DependencyResolver.Current.GetService<IClientScriptRegistry>();
						outputString.Append(clientScriptRegistry.RegisterInlineScript(httpContext, scriptBuilder.ToString()));
					}
				}
			}
			return outputString.ToString();
		}

		static public string BuildStarImages(decimal rating, int skinId)
		{
			var starTypes = new[]
			{
				rating < 0.25M
					? StarType.Empty
					: rating >= 0.25M && rating < 0.75M
						? StarType.Half
						: StarType.Full,
				rating < 1.25M
					? StarType.Empty
					: rating >= 1.25M && rating < 1.75M
						? StarType.Half
						: StarType.Full,
				rating < 2.25M
					? StarType.Empty
					: rating >= 2.25M && rating < 2.75M
						? StarType.Half
						: StarType.Full,
				rating < 3.25M
					? StarType.Empty
					: rating >= 3.25M && rating < 3.75M
						? StarType.Half
						: StarType.Full,
				rating < 4.25M
					? StarType.Empty
					: rating >= 4.25M && rating < 4.75M
						? StarType.Half
						: StarType.Full,
			};

			var skinName = SkinProvider.GetSkinNameById(skinId);

			return string.Join("", starTypes.Select((star, idx) =>
				$"<img class='ratings-star-{idx}-{star}' src='{AppLogic.LocateImageURL(string.Format("~/Skins/{0}/images/{1}", skinName, StarImage(star)))}' alt='' />"));
		}

		static string StarImage(StarType starType)
		{
			switch(starType)
			{
				default:
				case StarType.Empty:
					return "stare.gif";
				case StarType.Half:
					return "starh.gif";
				case StarType.Full:
					return "starf.gif";
			}
		}

		enum StarType
		{
			Empty,
			Half,
			Full
		}
	}
}
