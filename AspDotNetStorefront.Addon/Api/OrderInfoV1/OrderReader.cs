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
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Addon.Api.OrderInfoV1
{
	class OrderReader : IOrderReader
	{
		readonly ILocalizedStringConverter LocalizedStringConverter;

		public OrderReader(ILocalizedStringConverter localizedStringConverter)
		{
			LocalizedStringConverter = localizedStringConverter;
		}

		public IResult<Order> GetOrder(int orderNumber)
		{
			if(orderNumber <= 0)
				return Result.Error<Order>(new OrderInfoError.InvalidOrderNumber());

			using(var connection = new SqlConnection(DB.GetDBConn()))
			{
				connection.Open();

				return LoadOrder(connection, orderNumber);
			}
		}

		IResult<Order> LoadOrder(SqlConnection connection, int orderNumber)
		{
			var orderItemsResult = LoadOrderItems(connection, orderNumber);
			if(orderItemsResult is IFailure)
				return Result.Error<Order>((IFailure)orderItemsResult);

			using(var command = connection.CreateCommand())
			{
				command.CommandText = "SELECT * FROM Orders WHERE OrderNumber = @orderNumber";
				command.Parameters.AddWithValue("@orderNumber", orderNumber);

				using(var reader = command.ExecuteReader())
				{
					if(!reader.Read())
						return Result.Error<Order>(new OrderInfoError.OrderNotFound(orderNumber));

					var order = BuildOrder(reader, orderItemsResult.Value);
					return Result.Ok(order);
				}
			}
		}

		Order BuildOrder(SqlDataReader reader, IEnumerable<OrderItem> orderItems)
			=> new Order(
				orderNumber: reader.FieldInt("OrderNumber"),
				storeId: reader.FieldInt("StoreID"),
				parentOrderNumber: reader.FieldInt("ParentOrderNumber"),
				storeVersion: reader.Field("StoreVersion"),
				quoteCheckout: reader.FieldBool("QuoteCheckout"),
				isNew: reader.FieldBool("IsNew"),
				shippedOn: reader.ReadNullable<DateTime>("ShippedOn"),
				customerId: reader.FieldInt("CustomerID"),
				customerGuid: reader.FieldGuid( "CustomerGUID"),
				referrer: reader.Field("Referrer"),
				skinId: reader.FieldInt("SkinID"),
				lastName: reader.Field("LastName"),
				firstName: reader.Field("FirstName"),
				email: reader.Field("Email"),
				notes: reader.Field("Notes"),
				billingEqualsShipping: reader.FieldBool("BillingEqualsShipping"),
				billingAddress: BuildBillingAddress(reader),
				shippingAddress: BuildShippingAddress(reader),
				shippingResidenceType: reader.FieldInt("ShippingResidenceType"),
				shippingMethodId: reader.FieldInt("ShippingMethodID"),
				shippingMethod: reader.Field("ShippingMethod"),
				shippingCalculationId: reader.FieldInt("ShippingCalculationID"),
				phone: reader.Field("Phone"),
				registerDate: reader.ReadNullable<DateTime>("RegisterDate"),
				affiliateId: reader.FieldInt("AffiliateID"),
				couponCode: reader.Field("CouponCode"),
				couponType: reader.FieldInt("CouponType"),
				couponDescription: reader.Field("CouponDescription"),
				couponDiscountAmount: reader.FieldDecimal("CouponDiscountAmount"),
				couponDiscountPercent: reader.FieldDecimal("CouponDiscountPercent"),
				couponIncludesFreeShipping: reader.FieldBool("CouponIncludesFreeShipping"),
				oktoEmail: reader.FieldBool("OkToEmail"),
				deleted: reader.FieldBool("Deleted"),
				cardType: reader.Field("CardType"),
				cardName: reader.Field("CardName"),
				cardExpirationMonth: reader.Field("CardExpirationMonth"),
				cardExpirationYear: reader.Field("CardExpirationYear"),
				cardStartDate: reader.Field("CardStartDate"),
				cardIssueNumber: reader.Field("CardIssueNumber"),
				subtotal: reader.FieldDecimal("OrderSubtotal"),
				tax: reader.FieldDecimal("OrderTax"),
				shippingCost: reader.FieldDecimal("OrderShippingCosts"),
				total: reader.FieldDecimal("OrderTotal"),
				paymentGateway: reader.Field("PaymentGateway"),
				authorizationCode: reader.Field("AuthorizationCode"),
				authorizationResult: reader.Field("AuthorizationResult"),
				authorizationPNRef: reader.Field("AuthorizationPNRef"),
				transactionCommand: reader.Field("TransactionCommand"),
				orderDate: reader.FieldDateTime("OrderDate"),
				customerLevelId: reader.FieldInt("LevelID"),
				customerLevelName: reader.Field("LevelName"),
				customerLevelDiscountPercent: reader.FieldDecimal("LevelDiscountPercent"),
				customerLevelDiscountAmount: reader.FieldDecimal("LevelDiscountAmount"),
				customerLevelHasFreeShipping: reader.FieldBool("LevelHasFreeShipping"),
				customerLevelAllowsQuantityDiscounts: reader.FieldBool("LevelAllowsQuantityDiscounts"),
				customerLevelHasNoTax: reader.FieldBool("LevelHasNoTax"),
				customerLevelAllowsCoupons: reader.FieldBool("LevelAllowsCoupons"),
				customerLevelDiscountsApplyToExtendedPrices: reader.FieldBool("LevelDiscountsApplyToExtendedPrices"),
				lastIPAddress: reader.Field("LastIPAddress"),
				paymentMethod: reader.Field("PaymentMethod"),
				orderNotes: reader.Field("OrderNotes"),
				poNumber: reader.Field("PONumber"),
				downloadEmailSentOn: reader.ReadNullable<DateTime>("DownloadEmailSentOn"),
				receiptEmailSentOn: reader.ReadNullable<DateTime>("ReceiptEmailSentOn"),
				distributorEmailSentOn: reader.ReadNullable<DateTime>("DistributorEmailSentOn"),
				shippingTrackingNumber: reader.Field("ShippingTrackingNumber"),
				shippedVia: reader.Field("ShippedVIA"),
				customerServiceNotes: reader.Field("CustomerServiceNotes"),
				realtimeRatesRequest: reader.Field("RTShipRequest"),
				realtimeRatesResponse: reader.Field("RTShipResponse"),
				transactionState: reader.Field("TransactionState"),
				avsResult: reader.Field("AVSResult"),
				captureCommand: reader.Field("CaptureTXCommand"),
				captureResult: reader.Field("CaptureTXResult"),
				voidCommand: reader.Field("VoidTXCommand"),
				voidResult: reader.Field("VoidTXResult"),
				refundCommand: reader.Field("RefundTXCommand"),
				refundResult: reader.Field("RefundTXResult"),
				refundReason: reader.Field("RefundReason"),
				cardinalLookupResult: reader.Field("CardinalLookupResult"),
				cardinalAuthenticatResult: reader.Field("CardinalAuthenticateResult"),
				cardinalGatewayParams: reader.Field("CardinalGatewayParms"),
				affiliateCommissionRecorded: reader.FieldBool("AffiliateCommissionRecorded"),
				orderOptions: reader.Field("OrderOptions"),
				weight: reader.FieldDecimal("OrderWeight"),
				carrierReportedRate: reader.Field("CarrierReportedRate"),
				carrierReportedWeight: reader.Field("CarrierReportedWeight"),
				localeSetting: reader.Field("LocaleSetting"),
				finalizationData: reader.Field("FinalizationData"),
				extensionData: reader.Field("ExtensionData"),
				alreadyConfirmed: reader.FieldBool("AlreadyConfirmed"),
				cartType: reader.FieldInt("CartType"),
				last4: reader.Field("Last4"),
				readyToShip: reader.FieldBool("ReadyToShip"),
				isPrinted: reader.FieldBool("IsPrinted"),
				authorizedOn: reader.ReadNullable<DateTime>("AuthorizedOn"),
				capturedOn: reader.ReadNullable<DateTime>("CapturedOn"),
				refundedOn: reader.ReadNullable<DateTime>("RefundedOn"),
				voidedOn: reader.ReadNullable<DateTime>("VoidedOn"),
				fraudedOn: reader.ReadNullable<DateTime>("FraudedOn"),
				editedOn: reader.ReadNullable<DateTime>("EditedOn"),
				trackingUrl: reader.Field("TrackingURL"),
				shippedEmailSentOn: reader.ReadNullable<DateTime>("ShippedEMailSentOn"),
				inventoryWasReduced: reader.FieldBool("InventoryWasReduced"),
				maxMindFraudScore: reader.FieldDecimal("MaxMindFraudScore"),
				maxMindDetails: reader.Field("MaxMindDetails"),
				vatRegistrationId: reader.Field("VatRegistrationID"),
				crypt: reader.FieldInt("Crypt"),
				transactionType: reader.FieldInt("TransactionType"),
				recurringSubscriptionId: reader.Field("RecurringSubscriptionID"),
				recurringSubscriptionCommand: reader.Field("RecurringSubscriptionCommand"),
				recurringSubscriptionResult: reader.Field("RecurringSubscriptionResult"),
				relatedOrderNumber: reader.FieldInt("RelatedOrderNumber"),
				buySafeCommand: reader.Field("BuySafeCommand"),
				buySafeResult: reader.Field("BuySafeResult"),
				receiptHtml: reader.Field("ReceiptHtml"),
				updatedOn: reader.FieldDateTime("UpdatedOn"),
				createdon: reader.FieldDateTime("CreatedOn"),
				orderItems: orderItems);

		Address BuildBillingAddress(SqlDataReader reader)
			=> new Address(
				lastName: reader.Field("BillingLastName"),
				firstName: reader.Field("BillingFirstName"),
				company: reader.Field("BillingCompany"),
				address1: reader.Field("BillingAddress1"),
				address2: reader.Field("BillingAddress2"),
				suite: reader.Field("BillingSuite"),
				city: reader.Field("BillingCity"),
				state: reader.Field("BillingState"),
				zip: reader.Field("BillingZip"),
				country: reader.Field("BillingCountry"),
				phone: reader.Field("BillingPhone"));

		Address BuildShippingAddress(SqlDataReader reader)
			=> new Address(
				lastName: reader.Field("ShippingLastName"),
				firstName: reader.Field("ShippingFirstName"),
				company: reader.Field("ShippingCompany"),
				address1: reader.Field("ShippingAddress1"),
				address2: reader.Field("ShippingAddress2"),
				suite: reader.Field("ShippingSuite"),
				city: reader.Field("ShippingCity"),
				state: reader.Field("ShippingState"),
				zip: reader.Field("ShippingZip"),
				country: reader.Field("ShippingCountry"),
				phone: reader.Field("ShippingPhone"));

		IResult<IEnumerable<OrderItem>> LoadOrderItems(SqlConnection connection, int orderNumber)
		{
			var orderItemIds = new List<int>();
			using(var command = connection.CreateCommand())
			{
				command.CommandText = @"
					SELECT ShoppingCartRecID
					FROM Orders_ShoppingCart
					WHERE OrderNumber = @orderNumber";

				command.Parameters.AddWithValue("@orderNumber", orderNumber);

				using(var reader = command.ExecuteReader())
					while(reader.Read())
						orderItemIds.Add(reader.FieldInt("ShoppingCartRecID"));
			}

			var orderItems = new List<OrderItem>();

			foreach(var orderItemId in orderItemIds)
			{
				var orderItemKitResults = LoadOrderItemKitDetails(connection, orderItemId);
				if(orderItemKitResults is IFailure)
					return Result.Error<IEnumerable<OrderItem>>((IFailure)orderItemKitResults);

				using(var command = connection.CreateCommand())
				{
					command.CommandText = @"
						SELECT *
						FROM Orders_ShoppingCart
						WHERE ShoppingCartRecID = @orderItemId";

					command.Parameters.AddWithValue("@orderItemId", orderItemId);

					using(var reader = command.ExecuteReader())
						while(reader.Read())
							orderItems.Add(BuildOrderItem(reader, orderItemKitResults.Value));
				}
			}

			return Result.Ok(orderItems);
		}

		OrderItem BuildOrderItem(SqlDataReader reader, IEnumerable<OrderItemKitDetail> orderItemKitDetails)
			=> new OrderItem(
				orderNumber: reader.FieldInt("OrderNumber"),
				shoppingCartRecId: reader.FieldInt("ShoppingCartRecID"),
				customerId: reader.FieldInt("CustomerID"),
				productId: reader.FieldInt("ProductID"),
				variantId: reader.FieldInt("VariantID"),
				quantity: reader.FieldInt("Quantity"),
				color: reader.Field("ChosenColor"),
				colorSkuModifier: reader.Field("ChosenColorSKUModifier"),
				size: reader.Field("ChosenSize"),
				sizeSkuModifier: reader.Field("ChosenSizeSKUModifier"),
				productName: LocalizedStringConverter.ParseMlData(reader.Field("OrderedProductName")),
				variantName: LocalizedStringConverter.ParseMlData(reader.Field("OrderedProductVariantName")),
				productSku: reader.Field("OrderedProductSKU"),
				manufacturerPartNumber: reader.Field("OrderedProductManufacturerPartNumber"),
				weight: reader.FieldDecimal("OrderedProductWeight"),
				price: reader.FieldDecimal("OrderedProductPrice"),
				regularPrice: reader.FieldDecimal("OrderedProductRegularPrice"),
				salePrice: reader.FieldDecimal("OrderedProductSalePrice"),
				extendedPrice: reader.FieldDecimal("OrderedProductExtendedPrice"),
				quantityDiscountName: reader.Field("OrderedProductQuantityDiscountName"),
				quantityDiscountId: reader.FieldInt("OrderedProductQuantityDiscountID"),
				quantityDiscountPercent: reader.FieldDecimal("OrderedProductQuantityDiscountPercent"),
				taxable: reader.FieldBool("IsTaxable"),
				isShipSeparately: reader.FieldBool("IsShipSeparately"),
				isDownload: reader.FieldBool("IsDownload"),
				downloadLocation: reader.Field("DownloadLocation"),
				freeShipping: reader.FieldBool("FreeShipping"),
				isSecureAttachment: reader.FieldBool("IsSecureAttachment"),
				textoption: reader.Field("TextOption"),
				cartType: reader.FieldInt("CartType"),
				shippingAddressID: reader.FieldInt("ShippingAddressID"),
				shippingDetail: reader.Field("ShippingDetail"),
				shippingMethodId: reader.FieldInt("ShippingMethodID"),
				shippingMethod: reader.Field("ShippingMethod"),
				distributorId: reader.FieldInt("DistributorID"),
				notes: reader.Field("Notes"),
				distributorEmailSentOn: reader.ReadNullable<DateTime>("distributorEmailSentOn"),
				extensionData: reader.Field("ExtensionData"),
				sizeOptionPrompt: LocalizedStringConverter.ParseMlData(reader.Field("SizeOptionPrompt")),
				colorOptionPrompt: LocalizedStringConverter.ParseMlData(reader.Field("ColorOptionPrompt")),
				textOptionPrompt: LocalizedStringConverter.ParseMlData(reader.Field("TextOptionPrompt")),
				createdOn: reader.FieldDateTime("CreatedOn"),
				customerEntersPrice: reader.FieldBool("CustomerEntersPrice"),
				customerEntersPricePrompt: reader.Field("CustomerEntersPricePrompt"),
				isAKit: reader.FieldBool("IsAKit"),
				isSystem: reader.FieldBool("IsSystem"),
				taxClassId: reader.FieldInt("TaxClassID"),
				taxRate: reader.FieldDecimal("TaxRate"),
				isGift: reader.FieldBool("IsGift"),
				downloadStatus: reader.FieldInt("DownloadStatus"),
				downloadValidDays: reader.FieldInt("DownloadValidDays"),
				downloadCategory: reader.Field("DownloadCategory"),
				downloadReleasedOn: reader.ReadNullable<DateTime>("DownloadReleasedOn"),
				gtin: reader.Field("GTIN"),
				updatedOn: reader.FieldDateTime("UpdatedOn"),
				kitDetails: orderItemKitDetails);

		IResult<IEnumerable<OrderItemKitDetail>> LoadOrderItemKitDetails(SqlConnection connection, int orderItemId)
		{
			using(var command = connection.CreateCommand())
			{
				command.CommandText = "SELECT * FROM Orders_KitCart WHERE ShoppingCartRecID = @orderItemId";
				command.Parameters.AddWithValue("@orderItemId", orderItemId);

				var orderItemKitDetails = new List<OrderItemKitDetail>();

				using(var reader = command.ExecuteReader())
					while(reader.Read())
						orderItemKitDetails.Add(BuildOrderItemKitDetail(reader));

				return Result.Ok(orderItemKitDetails);
			}
		}

		OrderItemKitDetail BuildOrderItemKitDetail(SqlDataReader reader)
			=> new OrderItemKitDetail(
				orderNumber: reader.FieldInt("OrderNumber"),
				kitCartRecId: reader.FieldInt("KitCartRecID"),
				customerId: reader.FieldInt("CustomerID"),
				shoppingCartRecId: reader.FieldInt("ShoppingCartRecId"),
				productId: reader.FieldInt("ProductID"),
				variantId: reader.FieldInt("VariantID"),
				productName: LocalizedStringConverter.ParseMlData(reader.Field("ProductName")),
				variantName: LocalizedStringConverter.ParseMlData(reader.Field("ProductVariantName")),
				kitGroupId: reader.FieldInt("KitGroupID"),
				kitGroupName: LocalizedStringConverter.ParseMlData(reader.Field("KitGroupName")),
				kitGroupIsRequired: reader.FieldBool("KitGroupIsRequired"),
				kitItemId: reader.FieldInt("KitItemID"),
				kitItemName: LocalizedStringConverter.ParseMlData(reader.Field("KitItemName")),
				kitItemPriceDelta: reader.FieldDecimal("KitItemPriceDelta"),
				quantity: reader.FieldInt("Quantity"),
				kitItemWeightDelta: reader.FieldDecimal("KitItemWeightDelta"),
				textOption: reader.Field("TextOption"),
				extensionData: reader.Field("ExtensionData"),
				kitGroupTypeId: reader.FieldInt("KitGroupTypeID"),
				inventoryVariantId: reader.FieldInt("InventoryVariantID"),
				inventoryVariantColor: reader.Field("InventoryVariantColor"),
				inventoryVariantSize: reader.Field("InventoryVariantSize"),
				createdOn: reader.FieldDateTime("CreatedOn"),
				cartType: reader.FieldInt("CartType"),
				kitGroupIsReadonly: reader.FieldBool("KitGroupIsReadOnly"),
				kitItemInventoryQuantityDelta: reader.FieldInt("KitItemInventoryQuantityDelta"),
				updatedOn: reader.FieldDateTime("UpdatedOn"));
	}
}
