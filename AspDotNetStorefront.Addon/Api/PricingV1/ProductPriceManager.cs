// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Data.SqlClient;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Addon.Api.PricingV1
{
	class ProductPriceManager : IProductPriceManager
	{
		public IResult SetPricing(int variantId, decimal price, decimal? salePrice = null)
		{
			using(var connection = new SqlConnection(DB.GetDBConn()))
			using(var command = connection.CreateCommand())
			{
				command.CommandText = @"
					UPDATE ProductVariant
					SET
						Price = @price,
						SalePrice = coalesce(@salePrice, SalePrice)
					WHERE VariantID = @variantId";

				command.Parameters.AddWithValue("@price", price);
				command.Parameters.AddWithValue("@salePrice", (object)salePrice ?? DBNull.Value);
				command.Parameters.AddWithValue("@variantId", variantId);

				connection.Open();

				if(command.ExecuteNonQuery() == 0)
					return Result.Error(new PricingError.VariantNotFound(variantId));

				return Result.Ok();
			}
		}
	}
}
