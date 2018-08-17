// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Data.SqlClient;
using System.Linq;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Addon.Api.InventoryV1
{
	class InventoryManager : IInventoryManager
	{
		readonly ILocalizedStringConverter LocalizedStringConverter;

		public InventoryManager(ILocalizedStringConverter localizedStringConverter)
		{
			LocalizedStringConverter = localizedStringConverter;
		}

		public IResult SetInventory(int quantity, int variantId)
			=> SetInventory(quantity, variantId, null, null, null);

		public IResult SetInventory(int quantity, int variantId, string locale, string size = null, string color = null)
		{
			using(var connection = new SqlConnection(DB.GetDBConn()))
			{
				connection.Open();

				var inventoryDeterminationResult = DetermineInventoryDetails(connection, variantId, locale, size, color);
				if(!inventoryDeterminationResult.IsSuccess)
					return inventoryDeterminationResult;

				if(inventoryDeterminationResult.Value.InventoryRecord == InventoryRecord.Inventory)
					return SetInventoryRecordQuantity(connection, quantity, inventoryDeterminationResult.Value);
				else
					return SetVariantRecordQuantity(connection, quantity, inventoryDeterminationResult.Value);
			}
		}

		IResult<InventoryDetermination> DetermineInventoryDetails(SqlConnection connection, int variantId, string locale, string size, string color)
		{
			using(var command = connection.CreateCommand())
			{
				command.CommandText = @"
					select
						ProductVariant.Sizes,
						ProductVariant.Colors,
						Product.TrackInventoryBySizeAndColor
					from ProductVariant
						inner join Product on ProductVariant.ProductID = Product.ProductID
					where
						ProductVariant.VariantID = @variantId";

				command.Parameters.AddWithValue("@variantId", variantId);

				using(var reader = command.ExecuteReader())
				{
					if(!reader.Read())
						return Result.Error<InventoryDetermination>(new InventoryError.VariantNotFound(variantId));

					var sizes = LocalizedStringConverter
						.ParseMlData(reader.Read<string>("Sizes"))
						[locale]
						?.ParseAsDelimitedList()
						.Select(AppLogic.CleanSizeColorOption)
						.ToArray()
						?? new string[0];

					var colors = LocalizedStringConverter
						.ParseMlData(reader.Read<string>("Colors"))
						[locale]
						?.ParseAsDelimitedList()
						.Select(AppLogic.CleanSizeColorOption)
						.ToArray()
						?? new string[0];

					var trackInventoryBySizeAndColor = reader.ReadNullable<byte>("TrackInventoryBySizeAndColor") == 1;

					// Ensure any provided size and/or color is defined on the variant
					if(!string.IsNullOrEmpty(size) && !sizes.Contains(size, StringComparer.OrdinalIgnoreCase))
						return Result.Error<InventoryDetermination>(new InventoryError.SizeNotDefinedOnVariant(variantId, size));

					if(!string.IsNullOrEmpty(color) && !colors.Contains(color, StringComparer.OrdinalIgnoreCase))
						return Result.Error<InventoryDetermination>(new InventoryError.ColorNotDefinedOnVariant(variantId, color));

					// Get the size/color as defined on the variant
					var normalizedSize = sizes
						.Where(s => StringComparer.OrdinalIgnoreCase.Equals(s, size))
						.FirstOrDefault();

					var normalizedColor = colors
						.Where(c => StringComparer.OrdinalIgnoreCase.Equals(c, color))
						.FirstOrDefault();

					// If the product is set to track inventory by size and color and at least one size or color is defined,
					// then inventory is tracked in the Inventory table. Inventory is tracked on the variant in all other cases.
					InventoryRecord inventoryRecord;
					if(trackInventoryBySizeAndColor && (sizes.Any() || colors.Any()))
						inventoryRecord = InventoryRecord.Inventory;
					else
						inventoryRecord = InventoryRecord.Variant;

					return Result.Ok(new InventoryDetermination(
						variantId,
						normalizedSize,
						normalizedColor,
						inventoryRecord));
				}
			}
		}

		IResult SetInventoryRecordQuantity(SqlConnection connection, int quantity, InventoryDetermination inventoryDetermination)
		{
			// Determine the inventory record to update
			int? inventoryId;
			using(var command = connection.CreateCommand())
			{
				command.CommandText = @"
					select InventoryID
					from Inventory
					where
						VariantID = @variantId
						and coalesce(Size, '') = coalesce(@size, '')
						and coalesce(Color, '') = coalesce(@color, '')";

				command.Parameters.AddWithValue("@variantId", inventoryDetermination.VariantId);
				command.Parameters.AddWithValue("@size", inventoryDetermination.Size);
				command.Parameters.AddWithValue("@color", inventoryDetermination.Color);

				inventoryId = command.ExecuteScalar() as int?;
			}

			// If no inventory record exists, create one
			if(inventoryId == null)
			{
				using(var command = connection.CreateCommand())
				{
					command.CommandText = @"
						insert into Inventory(
							[VariantID], [Color], [Size], [Quan])
						values(
							@variantId, @color, @size @quantity)";

					command.Parameters.AddWithValue("@variantId", inventoryDetermination.VariantId);
					command.Parameters.AddWithValue("@size", inventoryDetermination.Size);
					command.Parameters.AddWithValue("@color", inventoryDetermination.Color);
					command.Parameters.AddWithValue("@quantity", quantity);

					command.ExecuteNonQuery();

					return Result.Ok();
				}
			}

			// Update the existing inventory record
			using(var command = connection.CreateCommand())
			{
				command.CommandText = @"
					update Inventory
					set Quan = @quantity
					where InventoryID = @inventoryId";

				command.Parameters.AddWithValue("@quantity", quantity);
				command.Parameters.AddWithValue("@inventoryId", inventoryId.Value);

				// If no records were updated, then the inventory record got deleted after we identified it
				if(command.ExecuteNonQuery() == 0)
					return Result.Error<InventoryDetermination>(new InventoryError.InventoryRecordNotFound(inventoryDetermination.VariantId, inventoryDetermination.Size, inventoryDetermination.Color));

				return Result.Ok();
			}
		}

		IResult SetVariantRecordQuantity(SqlConnection connection, int quantity, InventoryDetermination inventoryDetermination)
		{
			// Update the existing variant record
			using(var command = connection.CreateCommand())
			{
				command.CommandText = @"
					update ProductVariant
					set Inventory = @quantity
					where VariantID = @variantId";

				command.Parameters.AddWithValue("@quantity", quantity);
				command.Parameters.AddWithValue("@variantId", inventoryDetermination.VariantId);

				// If no records were updated, then the variant record got deleted after we identified it
				if(command.ExecuteNonQuery() == 0)
					return Result.Error<InventoryDetermination>(new InventoryError.VariantNotFound(inventoryDetermination.VariantId));

				return Result.Ok();
			}
		}

		enum InventoryRecord
		{
			Variant = 1,
			Inventory = 2,
		}

		class InventoryDetermination
		{
			public int VariantId { get; }
			public string Size { get; }
			public string Color { get; }
			public InventoryRecord InventoryRecord { get; }

			public InventoryDetermination(
				int variantId,
				string size,
				string color,
				InventoryRecord inventoryRecord)
			{
				VariantId = variantId;
				Size = size;
				Color = color;
				InventoryRecord = inventoryRecord;
			}
		}
	}
}
