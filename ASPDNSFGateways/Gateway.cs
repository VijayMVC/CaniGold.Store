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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using AspDotNetStorefront.Signifyd;
using AspDotNetStorefrontCore;
using AspDotNetStorefrontGateways.Processors;

namespace AspDotNetStorefrontGateways
{
	/// <summary>
	/// Summary description for Gateway.
	/// </summary>
	public partial class Gateway
	{
		//these gateways have custom logic surrounding them and cannot be removed and fully abstracted
		public const string ro_GWPAYPALPRO = "PAYPALPRO";
		public const string ro_GWPAYPAL = "PAYPAL";
		public const string ro_GWPAYFLOWPRO = "PAYFLOWPRO";
		public const string ro_GWMICROPAY = "MICROPAY";
		public const string ro_GWAMAZONPAYMENTS = "AMAZONPAYMENTS";
		public const string ro_GWPAYPALEMBEDDEDCHECKOUT = AppLogic.ro_PMPayPalEmbeddedCheckout;
		public const string ro_GWTWOCHECKOUT = "TWOCHECKOUT";
		public const string ro_GWACCEPTJS = "ACCEPTJS";
		public const string ro_GWBRAINTREE = "BRAINTREE";
		public const string ro_GWSAGEPAYPI = "SAGEPAYPI";
		public const string AuthorizeNetGatewayName = "AUTHORIZENET";
		public const string eWAYGatewayName = "EWAY";

		//Used for determining how the customer chose to pay through Braintree
		public const string BraintreeCreditCardKey = "CreditCard";
		//Used for determining how the customer chose to pay through Sage Pay PI
		public const string SagePayPiCreditCardKey = "CreditCard";

		public Gateway() { }

		private static String DetermineGatwayToUse(String GatewayPassedIn)
		{
			String GWCleaned = AppLogic.CleanPaymentGateway(GatewayPassedIn);
			if(GWCleaned.Length == 0)
			{
				GWCleaned = AppLogic.ActivePaymentGatewayCleaned();
			}
			return GWCleaned;
		}

		// conceptually, marking an order as "cleared" now means setting it's TransactionState to 'CAPTURED'
		// AND processing all related actions for the order (processsing download files, drop ship notifications, gift card setup, etc, etc, etc...
		public static void ProcessOrderAsCaptured(int orderNumber)
		{
			var order = new Order(orderNumber, Localization.GetDefaultLocale());

			if(order.TransactionIsCaptured())
				return;

			//Mark payment cleared:
			order.SetTransactionState(AppLogic.ro_TXStateCaptured);

			//Make sure inventory was deducted
			order.DeductInventory();

			//Update the Micropay balances if any was purchased
			var micropayProductId = AppLogic.GetMicroPayProductID();
			var originalMicropayTotal = AppLogic.GetMicroPayBalance(order.CustomerID);
			var newMicropayTotal = originalMicropayTotal;

			//Use the raw price for the amount because it may be discounted or on sale in the order
			var price = AppLogic.GetVariantPrice(AppLogic.GetProductsDefaultVariantID(micropayProductId));
			foreach(var item in order.CartItems)
				if(item.ProductID == micropayProductId)
					newMicropayTotal += (price * item.Quantity);

			//Only update the DB if there's been a change
			if(newMicropayTotal != originalMicropayTotal)
			{
				var micropayParams = new SqlParameter[]
				{
						new SqlParameter("@balance", Localization.CurrencyStringForDBWithoutExchangeRate(newMicropayTotal)),
						new SqlParameter("@customerId", order.CustomerID)
				};

				DB.ExecuteSQL("UPDATE Customer SET MicroPayBalance = @balance WHERE CustomerID = @customerId", micropayParams);
			}

			//Handle downloadable items
			order.ReleaseDownloadItems();

			//Distributor notifications
			order.SendDistributorNotifications();

			//Serialize Gift Cards
			order.ProcessOrderGiftCards();

			//Send Email Gift Card Email
			order.SendEmailGiftCardEmails();

			try
			{
				//Added try/catch because it blew up the cart if jurisdiction could not be determined but did not prevent the order
				//call-out commit tax transaction add-ins
				if(AppLogic.AppConfigBool("AvalaraTax.Enabled"))
				{
					var avaTax = new AvaTax();
					avaTax.CommitTax(order);
				}
			}
			catch(Exception exception)
			{
				SysLog.LogException(exception, MessageTypeEnum.GeneralException, MessageSeverityEnum.Error);
			}
		}

		// NOTE: this does NOT execute any monetary transaction, it just sets the transactionstate to indicated refunded!
		public static string ForceRefundStatus(int orderNumber)
		{
			var order = new Order(orderNumber);

			if(order.TransactionIsCaptured())
				order.ReturnInventory();

			//Update transaction state
			var updateParams = new SqlParameter[]
			{
				new SqlParameter("@refundedState", AppLogic.ro_TXStateRefunded),
				new SqlParameter("@orderNumber", orderNumber)
			};

			DB.ExecuteSQL(@"UPDATE Orders 
							SET RefundTXCommand = 'ADMIN FORCED REFUND',
								RefundReason = 'ADMIN FORCED REFUND', 
								TransactionState = @refundedState, 
								RefundedOn = GETDATE(), 
								IsNew = 0 
							WHERE OrderNumber = @orderNumber",
							updateParams);

			return AppLogic.ro_OK;
		}

		/// <summary>
		/// Marks a transaction state as Voided, but does not communicate with a gateway
		/// </summary>
		/// <param name="OrderNumber">Order Number of the order to be voided</param>
		/// <returns>Returns string OK</returns>
		public static string ForceVoidStatus(int OrderNumber)
		{
			return ForceVoidStatus(OrderNumber, AppLogic.ro_TXStateVoided);
		}

		public static string ForceVoidStatus(int OrderNumber, string status)
		{

			int CouponType = 0;
			string CouponCode = "";
			decimal CouponDiscountAmount = 0.0M;

			using(SqlConnection con = new SqlConnection(DB.GetDBConn()))
			{
				con.Open();
				using(IDataReader rs = DB.GetRS("Select CouponType, CouponCode, CouponDiscountAmount from Orders  with (NOLOCK)  where OrderNumber=" + OrderNumber.ToString(), con))
				{
					if(rs.Read())
					{
						CouponType = DB.RSFieldInt(rs, "CouponType");
						CouponCode = DB.RSField(rs, "CouponCode");
						CouponDiscountAmount = DB.RSFieldDecimal(rs, "CouponDiscountAmount");
					}
				}
			}

			// make sure inventory was restored. safe to call repeatedly. proc protects against deducting twice
			DB.ExecuteSQL("aspdnsf_AdjustInventory " + OrderNumber.ToString() + ",1");

			DB.ExecuteSQL("update Orders set VoidTXCommand='ADMIN FORCED VOID', TransactionState=" + DB.SQuote(status) + ", VoidedOn=getdate(), IsNew=0 where OrderNumber=" + OrderNumber.ToString());

			DecrementMicropayProductsInOrder(OrderNumber);

			//Invalidate GiftCards ordered on this order
			GiftCards GCs = new GiftCards(OrderNumber, GiftCardCollectionFilterType.OrderNumber);
			foreach(GiftCard gc in GCs)
			{
				gc.GiftCardTransactions.Add(GiftCardUsageTransaction.CreateTransaction(gc.GiftCardID, GiftCardUsageReasons.FundsRemovedByAdmin, 0, 0, gc.Balance, ""));
				gc.UpdateCard(null, null, null, null, 1, null, null, null, null, null, null, null, null, null, null);
			}

			//Restore Amount to coupon used in paying for the order
			if((CouponTypeEnum)CouponType == CouponTypeEnum.GiftCard)
			{
				GiftCard gc = new GiftCard(CouponCode);
				if(gc.GiftCardID != 0)
				{
					gc.GiftCardTransactions.Add(GiftCardUsageTransaction.CreateTransaction(gc.GiftCardID, GiftCardUsageReasons.FundsAddedByAdmin, 0, 0, CouponDiscountAmount, ""));
				}
			}

			return AppLogic.ro_OK;
		}

		public static int CreateOrderRecord(ShoppingCart cart, int OrderNumber, Address UseBillingAddress)
		{
			StringBuilder sql = new StringBuilder(4096);
			String orderGUID = CommonLogic.GetNewGUID();

			if(OrderNumber == 0)
			{
				OrderNumber = AppLogic.GetNextOrderNumber();
			}

			using(SqlConnection con = new SqlConnection(DB.GetDBConn()))
			{
				con.Open();
				using(IDataReader rsCustomer = DB.GetRS("select * from customer  with (NOLOCK)  where customerid=" + cart.ThisCustomer.CustomerID.ToString(), con))
				{
					rsCustomer.Read();

					String PMCleaned = AppLogic.CleanPaymentMethod(UseBillingAddress.PaymentMethodLastUsed);


					Decimal CartTotal = cart.Total(true);
					Decimal dShippingTotal = cart.ShippingTotal(true, true);
					Decimal dSubTotal = cart.SubTotal(true, false, true, true);
					Decimal dTaxTotal = cart.TaxTotal();
					Decimal NetTotal = CartTotal - CommonLogic.IIF(cart.Coupon.CouponType == CouponTypeEnum.GiftCard, CommonLogic.IIF(CartTotal < cart.Coupon.DiscountAmount, CartTotal, cart.Coupon.DiscountAmount), 0);

					if(NetTotal > System.Decimal.Zero || !AppLogic.AppConfigBool("SkipPaymentEntryOnZeroDollarCheckout"))
					{
						AppLogic.ValidatePM(PMCleaned); // prevent PM Hacks!
					}

					// PayPalExpressMark is just for checkout flow, now that we are recording the order, save it as PayPalExpress
					if(PMCleaned == AppLogic.ro_PMPayPalExpressMark)
					{
						PMCleaned = AppLogic.ro_PMPayPalExpress;
						UseBillingAddress.PaymentMethodLastUsed = AppLogic.ro_PMPayPalExpress;
						UseBillingAddress.UpdateDB();
					}

					sql.Append("insert into Orders(OrderNumber,OrderGUID,VATRegistrationID,TransactionType,CartType,LocaleSetting,OrderWeight,TransactionState,PONumber,StoreVersion,CustomerID,Referrer,OrderNotes,FinalizationData,CustomerServiceNotes,OrderOptions,PaymentMethod,LastIPAddress,CustomerGUID,SkinID,LastName,FirstName,EMail,Phone,Notes,RegisterDate,AffiliateID,CouponCode,CouponType,CouponDescription,CouponDiscountAmount,CouponDiscountPercent,CouponIncludesFreeShipping,OKToEMail,Deleted,BillingEqualsShipping,BillingLastName,BillingFirstName,BillingCompany,BillingAddress1,BillingAddress2,BillingSuite,BillingCity,BillingState,BillingZip,BillingCountry,BillingPhone,ShippingLastName,ShippingFirstName,ShippingCompany,ShippingResidenceType,ShippingAddress1,ShippingAddress2,ShippingSuite,ShippingCity,ShippingState,ShippingZip,ShippingCountry,ShippingPhone,ShippingMethodID,ShippingMethod,ShippingCalculationID,RTShipRequest,RTShipResponse,CardType,CardName,Last4,CardExpirationMonth,CardExpirationYear,CardStartDate,CardIssueNumber,OrderSubtotal,OrderTax,OrderShippingCosts,OrderTotal,AuthorizationResult,AuthorizationCode,AuthorizationPNREF,TransactionCommand, StoreID) values (");
					sql.Append(OrderNumber.ToString() + ",");
					sql.Append(DB.SQuote(orderGUID) + ",");
					sql.Append(DB.SQuote(cart.ThisCustomer.VATRegistrationID) + ",");
					sql.Append("1,"); // always 1 here on create, except for ad-hoc refund type orders
					sql.Append(((int)cart.CartType).ToString() + ",");
					sql.Append(DB.SQuote(cart.ThisCustomer.LocaleSetting) + ",");
					sql.Append(Localization.DecimalStringForDB(cart.WeightTotal()) + ",");

					if(PMCleaned == AppLogic.ro_PMCreditCard || PMCleaned == AppLogic.ro_PMPayPalExpress)
					{
						sql.Append(DB.SQuote(AppLogic.TransactionMode()) + ",");
					}
					else
					{
						sql.Append("NULL,");
					}
					sql.Append(DB.SQuote(UseBillingAddress.PONumber) + ",");
					sql.Append(DB.SQuote(CommonLogic.GetVersion()) + ",");
					sql.Append(cart.ThisCustomer.CustomerID.ToString() + ",");
					sql.Append(DB.SQuote(DB.RSField(rsCustomer, "Referrer")) + ",");
					sql.Append(DB.SQuote(cart.OrderNotes) + ",");
					sql.Append(DB.SQuote(cart.FinalizationData) + ",");
					sql.Append(DB.SQuote(CommonLogic.IIF(cart.CartType == CartTypeEnum.RecurringCart, "Recurring Auto-Ship, Sequence #" + ((CartItem)cart.CartItems[0]).RecurringIndex.ToString(), "")) + ",");
					sql.Append(DB.SQuote(cart.GetOptionsList()) + ",");
					sql.Append(DB.SQuote(UseBillingAddress.PaymentMethodLastUsed) + ",");
					sql.Append(DB.SQuote(DB.RSField(rsCustomer, "LastIPAddress")) + ",");
					sql.Append(DB.SQuote(DB.RSFieldGUID(rsCustomer, "CustomerGUID")) + ",");
					sql.Append(cart.SkinID.ToString() + ",");
					sql.Append(DB.SQuote(DB.RSField(rsCustomer, "LastName")) + ",");
					sql.Append(DB.SQuote(DB.RSField(rsCustomer, "FirstName")) + ",");
					sql.Append(DB.SQuote(DB.RSField(rsCustomer, "EMail")) + ",");
					sql.Append(DB.SQuote(DB.RSField(rsCustomer, "Phone")) + ",");
					sql.Append(DB.SQuote(DB.RSField(rsCustomer, "Notes")) + ",");
					sql.Append(DB.DateQuote(Localization.ToDBDateTimeString(DB.RSFieldDateTime(rsCustomer, "RegisterDate"))) + ",");
					sql.Append(cart.ThisCustomer.AffiliateID.ToString() + ",");
					if(cart.Coupon.CouponCode.Length == 0)
					{
						sql.Append("NULL,");
					}
					else
					{
						sql.Append(DB.SQuote(cart.Coupon.CouponCode) + ",");
					}
					sql.Append(((int)cart.Coupon.CouponType).ToString() + ",");
					if(cart.HasCoupon() && cart.CouponIsValid)
					{
						sql.Append(DB.SQuote(cart.Coupon.Description) + ",");
						sql.Append(Localization.DecimalStringForDB(CommonLogic.IIF(cart.Coupon.CouponType == CouponTypeEnum.GiftCard, CommonLogic.IIF(CartTotal < cart.Coupon.DiscountAmount, CartTotal, cart.Coupon.DiscountAmount), cart.Coupon.DiscountAmount)) + ",");
						sql.Append(Localization.DecimalStringForDB(cart.Coupon.DiscountPercent) + ",");
						if(cart.Coupon.DiscountIncludesFreeShipping)
						{
							sql.Append("1,");
						}
						else
						{
							sql.Append("0,");
						}
					}
					else
					{
						sql.Append("NULL,");
						sql.Append("0,");
						sql.Append("0,");
						sql.Append("0,");
					}
					if(DB.RSFieldBool(rsCustomer, "OKToEMail"))
					{
						sql.Append("1,");
					}
					else
					{
						sql.Append("0,");
					}
					sql.Append("0,");
					if(DB.RSFieldBool(rsCustomer, "BillingEqualsShipping"))
					{
						sql.Append("1,");
					}
					else
					{
						sql.Append("0,");
					}

					sql.Append(DB.SQuote(UseBillingAddress.LastName) + ",");
					sql.Append(DB.SQuote(UseBillingAddress.FirstName) + ",");
					sql.Append(DB.SQuote(UseBillingAddress.Company) + ",");
					sql.Append(DB.SQuote(UseBillingAddress.Address1) + ",");
					sql.Append(DB.SQuote(UseBillingAddress.Address2) + ",");
					sql.Append(DB.SQuote(UseBillingAddress.Suite) + ",");
					sql.Append(DB.SQuote(UseBillingAddress.City) + ",");
					sql.Append(DB.SQuote(UseBillingAddress.State) + ",");
					sql.Append(DB.SQuote(UseBillingAddress.Zip) + ",");
					sql.Append(DB.SQuote(UseBillingAddress.Country) + ",");
					sql.Append(DB.SQuote(UseBillingAddress.Phone) + ",");

					if(cart.HasMultipleShippingAddresses())
					{
						sql.Append("NULL,");
						sql.Append("NULL,");
						sql.Append("NULL,");
						sql.Append("0,");
						sql.Append("NULL,");
						sql.Append("NULL,");
						sql.Append("NULL,");
						sql.Append("NULL,");
						sql.Append("NULL,");
						sql.Append("NULL,");
						sql.Append("NULL,");
						sql.Append("NULL,");
						sql.Append("0,");
						sql.Append("NULL,");
					}
					else
					{
						Address ShippingAddress = new Address();
						ShippingAddress.LoadByCustomer(cart.ThisCustomer.CustomerID, cart.ThisCustomer.PrimaryShippingAddressID, AddressTypes.Shipping);
						sql.Append(DB.SQuote(ShippingAddress.LastName) + ",");
						sql.Append(DB.SQuote(ShippingAddress.FirstName) + ",");
						sql.Append(DB.SQuote(ShippingAddress.Company) + ",");
						sql.Append(((int)ShippingAddress.ResidenceType).ToString() + ",");
						sql.Append(DB.SQuote(ShippingAddress.Address1) + ",");
						sql.Append(DB.SQuote(ShippingAddress.Address2) + ",");
						sql.Append(DB.SQuote(ShippingAddress.Suite) + ",");
						sql.Append(DB.SQuote(ShippingAddress.City) + ",");
						sql.Append(DB.SQuote(ShippingAddress.State) + ",");
						sql.Append(DB.SQuote(ShippingAddress.Zip) + ",");
						sql.Append(DB.SQuote(ShippingAddress.Country) + ",");
						sql.Append(DB.SQuote(ShippingAddress.Phone) + ",");
						if(cart.IsAllDownloadComponents())
						{
							sql.Append("0,");
							sql.Append(DB.SQuote("Download") + ",");
						}
						else if(cart.IsAllSystemComponents())
						{
							sql.Append("0,");
							sql.Append(DB.SQuote("System") + ",");
						}
						else
						{
							//Try to find an item with shipping info, as not all will necessarily have it.  The ones that do should all be identical.
							var shippingItem = cart
								.CartItems
								.Where(ci => ci.ShippingMethodID != 0)
								.FirstOrDefault();

							if(shippingItem == null)
								shippingItem = cart.FirstItem();

							sql.Append(shippingItem.ShippingMethodID.ToString() + ",");
							sql.Append(DB.SQuote(shippingItem.ShippingMethod) + ",");
						}
					}

					sql.Append(((int)Shipping.GetActiveShippingCalculationID()).ToString() + ",");
					sql.Append(DB.SQuote(DB.RSField(rsCustomer, "RTShipRequest")) + ",");
					sql.Append(DB.SQuote(DB.RSField(rsCustomer, "RTShipResponse")) + ",");

					sql.Append(DB.SQuote(UseBillingAddress.CardType) + ",");
					sql.Append(DB.SQuote(UseBillingAddress.CardName) + ",");

					String Last4 = String.Empty;
					if((PMCleaned == AppLogic.ro_PMCreditCard || PMCleaned == AppLogic.ro_PMPayPalExpress))
					{
						Last4 = AppLogic.SafeDisplayCardNumberLast4(UseBillingAddress.CardNumber, String.Empty, 0);
					}

					sql.Append(DB.SQuote(Last4) + ",");
					sql.Append(DB.SQuote(UseBillingAddress.CardExpirationMonth) + ",");
					sql.Append(DB.SQuote(UseBillingAddress.CardExpirationYear) + ",");
					sql.Append(DB.SQuote(UseBillingAddress.CardStartDate) + ",");
					sql.Append(DB.SQuote(Security.MungeString(UseBillingAddress.CardIssueNumber)) + ",");

					sql.Append(Localization.CurrencyStringForDBWithoutExchangeRate(dSubTotal) + ",");
					sql.Append(Localization.CurrencyStringForDBWithoutExchangeRate(dTaxTotal) + ",");
					sql.Append(Localization.CurrencyStringForDBWithoutExchangeRate(dShippingTotal) + ",");
					sql.Append(Localization.CurrencyStringForDBWithoutExchangeRate(CartTotal) + ",");
					sql.Append(DB.SQuote(AppLogic.ro_TBD) + ","); // must update later to RawResponseString if TX is ok!
					sql.Append(DB.SQuote(AppLogic.ro_TBD) + ",");   // must update later if TX is ok!
					sql.Append(DB.SQuote(AppLogic.ro_TBD) + ",");  // must update later if TX is ok!
					sql.Append(DB.SQuote(AppLogic.ro_TBD) + ",");  // must update later if TX is ok!
					sql.Append(AppLogic.StoreID() + ")");  // Specify a stored id where the order is created.

					DB.ExecuteSQL(sql.ToString());

					// we can now store the cc if required, as we have to get the salt field from the order record just created!
					string saltKey = Order.StaticGetSaltKey(OrderNumber);

					String CC = String.Empty;
					if(PMCleaned == AppLogic.ro_PMCreditCard || PMCleaned == AppLogic.ro_PMPayPalExpress)
					{
						if(cart.ThisCustomer.MasterShouldWeStoreCreditCardInfo)
						{
							CC = Security.MungeString(UseBillingAddress.CardNumber, saltKey);
						}
						else
						{
							CC = AppLogic.ro_CCNotStoredString;
						}
					}

					DB.ExecuteSQL("update Orders set CardNumber=" + DB.SQuote(CC) + " where OrderNumber=" + OrderNumber.ToString());

					PromotionManager.FinalizePromotionsOnOrderComplete(cart, OrderNumber);

					// **** Partial fix for embedded checkout methods ****
					if(PMCleaned == AppLogic.ro_PMPayPalEmbeddedCheckout)
					{
						var checkoutAccountStatusProvider = DependencyResolver.Current.GetService<ICheckoutAccountStatusProvider>();
						var checkoutAccountStatus = checkoutAccountStatusProvider.GetCheckoutAccountStatus(cart.ThisCustomer, cart.ThisCustomer.EMail);
						if(checkoutAccountStatus.State == CheckoutAccountState.Unvalidated)
							DB.ExecuteSQL(
								"update Customer set IsRegistered = 1 where CustomerId = @customerId;",
								new SqlParameter("@customerId", cart.ThisCustomer.CustomerID));
					}
				}
			}

			return OrderNumber;
		}

		// this routine does NOT go through all the normal cart to order conversion mechanics. It sets up everything explicitely.
		// NOTE: cardnumber COULD be just last4 coming in on the UseBillingAddress.CardNumber. That is ok. it is up to the gateway to allow it or fail it
		public static String MakeAdHocOrder(String PaymentGatewayToUse, int OriginalOrderNumber, String OriginalTransactionID, Customer OrderCustomer, Address UseBillingAddress, String CardExtraCode, Decimal OrderTotal, AppLogic.TransactionTypeEnum OrderType, String OrderDescription, out int NewOrderNumber)
		{
			String status = AppLogic.ro_OK;
			NewOrderNumber = 0;
			String GWCleaned = DetermineGatwayToUse(PaymentGatewayToUse);

			if(!GatewayLoader.GetProcessor(GWCleaned).SupportsAdHocOrders() || GWCleaned == "TWOCHECKOUT")
			{
				status = "Error: Gateway does not support ad-hoc orders!";
				return status;
			}

			NewOrderNumber = AppLogic.GetNextOrderNumber();

			// try to run the card first:
			String AVSResult = String.Empty;
			String AuthorizationResult = String.Empty;
			String AuthorizationCode = String.Empty;
			String AuthorizationTransID = String.Empty;
			String TransactionCommand = String.Empty;
			String TransactionResponse = String.Empty;
			bool IsRefund = false;
			if(OrderType.Equals(AppLogic.TransactionTypeEnum.CREDIT))
			{
				IsRefund = true;
			}

			if(IsRefund)
			{
				// NOTE: the "OrderTotal" that was passed in here is the actual RefundAmount (not the original OrderTotal!)
				status = Gateway.ProcessRefund(OrderCustomer.CustomerID, OriginalOrderNumber, NewOrderNumber, OrderTotal, OrderDescription, UseBillingAddress);
			}
			else
			{
				if(GWCleaned == ro_GWBRAINTREE)
					return "Error: Gateway does not support ad-hoc charges!";   //We support partial refunds, but not ad-hoc orders for Braintree

				if(GWCleaned == ro_GWSAGEPAYPI)
					return "Error: Gateway does not support ad-hoc charges!";   //We support partial refunds, but not ad-hoc orders for SagePayPi

				var transactionMode = (TransactionModeEnum)Enum.Parse(typeof(TransactionModeEnum), AppLogic.AppConfig("TransactionMode").ToLowerInvariant().Replace(" ", ""));

				status = Gateway.ProcessCard(
					cart: null,
					gateway: PaymentGatewayToUse,
					customerId: OrderCustomer.CustomerID,
					orderNumber: NewOrderNumber,
					billingAddress: UseBillingAddress,
					cardExtraCode: CardExtraCode,
					shippingAddress: null,
					orderTotal: OrderTotal,
					useLiveTransactions: AppLogic.AppConfigBool("UseLiveTransactions"),
					cavv: string.Empty,
					eci: string.Empty,
					xid: string.Empty,
					transactionMode: transactionMode,
					avsResult: out AVSResult,
					authorizationResult: out AuthorizationResult,
					authorizationCode: out AuthorizationCode,
					authorizationTransID: out AuthorizationTransID,
					transactionCommand: out TransactionCommand,
					transactionResponse: out TransactionResponse,
					gatewayUsed: out GWCleaned);

				if(AVSResult == null)
				{
					AVSResult = String.Empty;
				}
				if(AuthorizationResult == null)
				{
					AuthorizationResult = String.Empty;
				}
				if(AuthorizationCode == null)
				{
					AuthorizationCode = String.Empty;
				}
				if(AuthorizationTransID == null)
				{
					AuthorizationTransID = String.Empty;
				}
				if(TransactionCommand == null)
				{
					TransactionCommand = String.Empty;
				}
				if(TransactionResponse == null)
				{
					TransactionResponse = String.Empty;
				}
			}

			String TransCMD = TransactionCommand;
			if(TransCMD.Length != 0 && UseBillingAddress.CardNumber != null && UseBillingAddress.CardNumber.Length != 0)
			{
				String tmp1 = AppLogic.SafeDisplayCardNumber(UseBillingAddress.CardNumber, String.Empty, 0);
				TransCMD = TransCMD.Replace(UseBillingAddress.CardNumber, tmp1);
			}
			if(TransCMD.Length != 0 && CardExtraCode != null && CardExtraCode.Length != 0)
			{
				String tmp2 = AppLogic.SafeDisplayCardExtraCode(CardExtraCode);
				TransCMD = TransCMD.Replace(CardExtraCode, tmp2);
			}
			// we dont' need it anymore. NUKE IT!
			TransactionCommand = "1".PadLeft(TransactionCommand.Length);
			TransactionCommand = String.Empty;

			String TransRES = AuthorizationResult;
			if(TransRES.Length != 0 && UseBillingAddress.CardNumber != null && UseBillingAddress.CardNumber.Length != 0)
			{
				String tmp3 = AppLogic.SafeDisplayCardNumber(UseBillingAddress.CardNumber, String.Empty, 0);
				TransRES = TransRES.Replace(UseBillingAddress.CardNumber, tmp3);
			}
			if(TransRES.Length != 0 && CardExtraCode != null && CardExtraCode.Length != 0)
			{
				String tmp4 = AppLogic.SafeDisplayCardExtraCode(CardExtraCode);
				TransRES = TransRES.Replace(CardExtraCode, tmp4);
			}
			// we dont' need it anymore. NUKE IT!
			AuthorizationResult = "1".PadLeft(AuthorizationResult.Length);
			AuthorizationResult = String.Empty;

			if(status == AppLogic.ro_OK)
			{
				// ok, we have a good charge/or refund, so now make the proper records in orders and orders_shoppingcart tables!

				StringBuilder sql = new StringBuilder(4096);

				String AdHocNotes = String.Format("This is a {0} order type for original order number {1}", OrderType, OriginalOrderNumber.ToString());

				int ShipCalcID = (int)Shipping.GetActiveShippingCalculationID();

				sql.Append("insert into Orders(OrderNumber,VATRegistrationID,TransactionType,ParentOrderNumber,CartType,PaymentGateway,LocaleSetting,StoreVersion,CustomerID,CustomerServiceNotes,PaymentMethod,LastIPAddress,CustomerGUID,SkinID,LastName,FirstName,EMail,Phone,AffiliateID,OKToEMail,BillingEqualsShipping,BillingLastName,BillingFirstName,BillingCompany,BillingAddress1,BillingAddress2,BillingSuite,BillingCity,BillingState,BillingZip,BillingCountry,BillingPhone,ShippingLastName,ShippingFirstName,ShippingCompany,ShippingAddress1,ShippingAddress2,ShippingSuite,ShippingCity,ShippingState,ShippingZip,ShippingCountry,ShippingPhone,CardType,CardName,Last4,CardExpirationMonth,CardExpirationYear,CardStartDate,CardIssueNumber,OrderSubtotal,OrderTax,OrderShippingCosts,OrderTotal,AuthorizationResult,AuthorizationCode,AuthorizationPNREF,TransactionCommand,ShippingCalculationID) values (");
				sql.Append(NewOrderNumber.ToString() + ",");
				sql.Append(DB.SQuote(OrderCustomer.VATRegistrationID) + ",");
				sql.Append(CommonLogic.IIF(IsRefund, "2", "1") + ",");
				if(OriginalOrderNumber != 0)
				{
					sql.Append(OriginalOrderNumber.ToString() + ",");
				}
				else
				{
					sql.Append("NULL,");
				}
				sql.Append(((int)CartTypeEnum.ShoppingCart).ToString() + ",");
				sql.Append(DB.SQuote(GWCleaned) + ",");
				sql.Append(DB.SQuote(OrderCustomer.LocaleSetting) + ",");
				sql.Append(DB.SQuote(CommonLogic.GetVersion()) + ",");
				sql.Append(OrderCustomer.CustomerID.ToString() + ",");
				sql.Append(DB.SQuote(AdHocNotes + ". " + OrderDescription) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.PaymentMethodLastUsed) + ",");
				sql.Append(DB.SQuote(OrderCustomer.LastIPAddress) + ",");
				sql.Append(DB.SQuote(OrderCustomer.CustomerGUID) + ",");
				sql.Append(OrderCustomer.SkinID.ToString() + ",");
				sql.Append(DB.SQuote(OrderCustomer.LastName) + ",");
				sql.Append(DB.SQuote(OrderCustomer.FirstName) + ",");
				sql.Append(DB.SQuote(OrderCustomer.EMail) + ",");
				sql.Append(DB.SQuote(OrderCustomer.Phone) + ",");
				sql.Append(OrderCustomer.AffiliateID.ToString() + ",");
				if(OrderCustomer.OKToEMail)
				{
					sql.Append("1,");
				}
				else
				{
					sql.Append("0,");
				}
				sql.Append("1,"); // BillingEqualsShipping

				// billing:
				sql.Append(DB.SQuote(UseBillingAddress.LastName) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.FirstName) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.Company) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.Address1) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.Address2) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.Suite) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.City) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.State) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.Zip) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.Country) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.Phone) + ",");

				// shipping:
				sql.Append(DB.SQuote(UseBillingAddress.LastName) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.FirstName) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.Company) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.Address1) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.Address2) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.Suite) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.City) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.State) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.Zip) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.Country) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.Phone) + ",");

				sql.Append(DB.SQuote(UseBillingAddress.CardType) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.CardName) + ",");
				String Last4 = String.Empty;
				String PMCleaned = AppLogic.ro_PMCreditCard;
				if(PMCleaned == AppLogic.ro_PMCreditCard || PMCleaned == AppLogic.ro_PMPayPalExpress)
				{
					Last4 = AppLogic.SafeDisplayCardNumberLast4(UseBillingAddress.CardNumber, "Address", UseBillingAddress.AddressID);
				}
				sql.Append(DB.SQuote(Last4) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.CardExpirationMonth) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.CardExpirationYear) + ",");
				sql.Append(DB.SQuote(UseBillingAddress.CardStartDate) + ",");
				sql.Append(DB.SQuote(Security.MungeString(UseBillingAddress.CardIssueNumber)) + ",");
				sql.Append(Localization.CurrencyStringForDBWithoutExchangeRate(OrderTotal) + ",");
				sql.Append(Localization.CurrencyStringForDBWithoutExchangeRate(0.0M) + ",");
				sql.Append(Localization.CurrencyStringForDBWithoutExchangeRate(0.0M) + ",");
				sql.Append(Localization.CurrencyStringForDBWithoutExchangeRate(OrderTotal) + ",");
				sql.Append(DB.SQuote(AuthorizationResult) + ","); // must update later to RawResponseString if TX is ok!
				sql.Append(DB.SQuote(AuthorizationCode) + ",");   // must update later if TX is ok!
				sql.Append(DB.SQuote(AuthorizationTransID) + ",");  // must update later if TX is ok!
				sql.Append(DB.SQuote(TransCMD) + ",");  // must update later if TX is ok!
				sql.Append(DB.SQuote(ShipCalcID.ToString()));
				sql.Append(")");

				DB.ExecuteSQL(sql.ToString());

				// now set trans state and info, we do this as a separate update, to be consistent with the code in how MakeOrder does it
				sql.Length = 0;
				sql.Append("update orders set ");
				sql.Append("PaymentGateway=" + DB.SQuote(GWCleaned) + ", ");
				sql.Append("AVSResult=" + DB.SQuote(AVSResult) + ", ");

				// we can now store the cc if required, as we have to get the salt field from the order record just created!
				// only store the CC# if this gateway needs it for void/capture/refund later 
				// i.e. (even if the store has store cc true, don't store it unless the gateway needs it)
				String CC = String.Empty;
				if(PMCleaned == AppLogic.ro_PMCreditCard || PMCleaned == AppLogic.ro_PMPayPalExpress)
				{
					if(OrderCustomer.MasterShouldWeStoreCreditCardInfo)
					{
						CC = Security.MungeString(UseBillingAddress.CardNumber, Order.StaticGetSaltKey(NewOrderNumber));
					}
					else
					{
						CC = AppLogic.ro_CCNotStoredString;
					}
					sql.Append("CardNumber=" + DB.SQuote(CC) + ", ");
				}

				sql.Append("AuthorizationResult=" + DB.SQuote(TransRES) + ", ");
				sql.Append("AuthorizationCode=" + DB.SQuote(AuthorizationCode) + ", ");
				sql.Append("AuthorizationPNREF=" + DB.SQuote(AuthorizationTransID) + ", ");
				sql.Append("TransactionCommand=" + DB.SQuote(TransCMD));
				sql.Append(" where OrderNumber=" + NewOrderNumber.ToString());
				DB.ExecuteSQL(sql.ToString());

				sql.Length = 0;
				sql.Append("insert into Orders_ShoppingCart(ShippingAddressID,ShippingDetail,ShoppingCartRecID,OrderNumber,CustomerID,ProductID,VariantID,Quantity,OrderedProductName,OrderedProductSKU,OrderedProductPrice,OrderedProductRegularPrice,ColorOptionPrompt,SizeOptionPrompt,TextOptionPrompt,CustomerEntersPricePrompt,Notes) values(");
				sql.Append("0,NULL,0,");
				sql.Append(NewOrderNumber.ToString() + ",");
				sql.Append(OrderCustomer.CustomerID.ToString() + ",");
				sql.Append(AppLogic.AdHocProductID.ToString() + ",");
				sql.Append(AppLogic.AdHocVariantID.ToString() + ",");
				sql.Append("1,");
				sql.Append(DB.SQuote(CommonLogic.IIF(IsRefund, "Ad Hoc Refund", "Ad Hoc Charge")) + ",");
				sql.Append(DB.SQuote(CommonLogic.IIF(IsRefund, "ADHOCREFUND", "ADHOCCHARGE")) + ",");
				sql.Append(Localization.CurrencyStringForGatewayWithoutExchangeRate(OrderTotal) + ",");
				sql.Append(Localization.CurrencyStringForGatewayWithoutExchangeRate(OrderTotal) + ",");
				sql.Append(DB.SQuote(String.Empty) + ",");
				sql.Append(DB.SQuote(String.Empty) + ",");
				sql.Append(DB.SQuote(String.Empty) + ",");
				sql.Append(DB.SQuote(String.Empty) + ",");
				sql.Append(DB.SQuote(AdHocNotes));
				sql.Append(")");
				DB.ExecuteSQL(sql.ToString());

				if(!IsRefund)
				{
					bool isAuthOnly = AppLogic.AppConfig("TransactionMode").Equals("AUTH", StringComparison.InvariantCultureIgnoreCase);
					List<SqlParameter> spa = new List<SqlParameter>(){
						new SqlParameter("IsNew", (isAuthOnly ? 1 : 0)), //auth only transactions should be marked as new so they can be processed by admins
						new SqlParameter("TransactionState", isAuthOnly ? AppLogic.ro_TXStateAuthorized : AppLogic.ro_TXStateCaptured), //the gateway will have followed the transactionmode appconfig, so we should mark the ad hoc order accordingly.
						new SqlParameter("OrderNumber", NewOrderNumber)
					};

					DB.ExecuteSQL("update orders set IsNew=@IsNew, TransactionState=@TransactionState, AuthorizedOn=getdate() where OrderNumber=@OrderNumber", spa.ToArray());
				}
				else
				{
					// copy over refundtxcommand from "original order" to become the transaction command for this "new" refund order, just in case it's needed later:
					String ParentOrderRefundCommand = String.Empty;

					using(SqlConnection con = new SqlConnection(DB.GetDBConn()))
					{
						con.Open();
						using(IDataReader rs = DB.GetRS("select RefundTXCommand from Orders  with (NOLOCK)  where OrderNumber=" + OriginalOrderNumber.ToString(), con))
						{
							if(rs.Read())
							{
								ParentOrderRefundCommand = DB.RSField(rs, "RefundTXCommand");
							}
						}
					}

					DB.ExecuteSQL("update orders set TransactionCommand=" + DB.SQuote(ParentOrderRefundCommand) + ", IsNew=0, TransactionState=" + DB.SQuote(AppLogic.ro_TXStateRefunded) + ", AuthorizedOn=getdate(), CapturedOn=getdate(), RefundedOn=getdate() where OrderNumber=" + NewOrderNumber.ToString());
				}
			}
			return status;
		}

		// note if RecurringSubscriptionID is not empty, the caller must setup additional RecurringSubscription Fields in the order tables
		// after they call this routine
		public static String MakeRecurringOrder(ShoppingCart cart, int OrderNumber, String RecurringSubscriptionID, String XID)
		{
			String Status = MakeOrder(String.Empty, AppLogic.TransactionMode(), cart, OrderNumber, String.Empty, String.Empty, XID, RecurringSubscriptionID);
			return Status;
		}


		// returns AppLogic.ro_OK or error Msg.
		//
		// if AppLogic.ro_OK then order was created successfully, and the cart is now empty (unless it's a recurring cart, in which dates are updated to next recurring date)
		//
		// if error msg, then shopping cart remains unchanged as it was before call
		//
		// if PaymentGatewayToUse is empty, we'll use the active store payment gateway
		//
		// NOTE: if RecurringSubscriptionID is not empty we just use this routine to localize "order creation" for an already approved
		// gateway autobill recurring order, so in this case, NO money is to change hands, and no live gateay all is to be made, the order should
		// be forced to captured state. The caller must patch up the ordertotal, shipping, and tax fields based on the recurring autobilling information
		// that was received back from the gateway. This routine will NOT do that part for AutoBill orders.
		//
		/// <summary>
		/// Processes the payment then creates an order, returns AppLogic.ro_OK or error Msg.
		/// </summary>
		/// <param name="PaymentGatewayToUse">Specify the payment gateay to use, if none is specified then the deafult is used</param>
		/// <param name="TransactionMode">Set to AUTH or AUTH-CAPTURE</param>
		/// <param name="cart">ShoppingCart object being processed into the order</param>
		/// <param name="OrderNumber">Ordernumber for the new order</param>
		/// <param name="CAVV"></param>
		/// <param name="ECI"></param>
		/// <param name="XID"></param>
		/// <param name="RecurringSubscriptionID">If specified the we just use this routine to localize "order creation" for an already approved gateway autobill recurring order, so in this case, NO money is to change hands, and no live gateay all is to be made, the order should be forced to captured state. The caller must patch up the ordertotal, shipping, and tax fields based on the recurring autobilling information that was received back from the gateway. This routine will NOT do that part for AutoBill orders.</param>
		/// <returns>AppLogic.ro_OK or error Msg.</returns>
		public static String MakeOrder(String PaymentGatewayToUse, String TransactionMode, ShoppingCart cart, int OrderNumber, String CAVV, String ECI, String XID, String RecurringSubscriptionID)
		{
			return MakeOrder(PaymentGatewayToUse, TransactionMode, cart, OrderNumber, CAVV, ECI, XID, RecurringSubscriptionID, new Dictionary<string, string>());
		}

		/// <summary>
		/// Processes the payment then creates an order, returns AppLogic.ro_OK or error Msg.
		/// </summary>
		/// <param name="PaymentGatewayToUse">Specify the payment gateay to use, if none is specified then the deafult is used</param>
		/// <param name="TransactionMode">Set to AUTH or AUTH-CAPTURE</param>
		/// <param name="cart">ShoppingCart object being processed into the order</param>
		/// <param name="OrderNumber">Ordernumber for the new order</param>
		/// <param name="CAVV"></param>
		/// <param name="ECI"></param>
		/// <param name="XID"></param>
		/// <param name="RecurringSubscriptionID">If specified the we just use this routine to localize "order creation" for an already approved gateway autobill recurring order, so in this case, NO money is to change hands, and no live gateay all is to be made, the order should be forced to captured state. The caller must patch up the ordertotal, shipping, and tax fields based on the recurring autobilling information that was received back from the gateway. This routine will NOT do that part for AutoBill orders.</param>
		/// <param name="TransactionContext">Transaction params</param>
		/// <returns>AppLogic.ro_OK or error Msg.</returns>
		public static String MakeOrder(String PaymentGatewayToUse, String TransactionMode, ShoppingCart cart, int OrderNumber, String CAVV, String ECI, String XID, String RecurringSubscriptionID, IDictionary<string, string> TransactionContext)
		{
			if(OrderNumber == 0)
			{
				OrderNumber = AppLogic.GetNextOrderNumber();
			}

			string AVSResult = string.Empty;
			string AuthorizationResult = string.Empty;
			string AuthorizationCode = string.Empty;
			string AuthorizationTransID = string.Empty;
			string TransactionCommand = string.Empty;
			string TransactionResponse = string.Empty;
			string serializedAmazonOrderTrackingDetail = string.Empty;
			var orderTrackingDetail = new Processors.AmazonPaymentsOrderTrackingDetail();
			// Use the cart customer because when processing recurring orders the current customer is Admin.
			var customer = cart.ThisCustomer;

			Address UseBillingAddress = new Address();
			UseBillingAddress.LoadByCustomer(customer.CustomerID, customer.PrimaryBillingAddressID, AddressTypes.Billing);

			// if only one address, let's get it so the gateway can display it:
			var firstItemShippingAddressId = cart.FirstItemShippingAddressID();
			var cartHasSingleShippingAddress = !cart.HasMultipleShippingAddresses() && firstItemShippingAddressId > 0;
			Address UseShippingAddress = null;
			if(cartHasSingleShippingAddress)
			{
				UseShippingAddress = new Address();
				UseShippingAddress.LoadByCustomer(customer.CustomerID, firstItemShippingAddressId, AddressTypes.Shipping);
			}

			string GW = DetermineGatwayToUse(PaymentGatewayToUse);

			string status = AppLogic.ro_OK;
			string PM = AppLogic.CleanPaymentMethod(UseBillingAddress.PaymentMethodLastUsed);
			if(RecurringSubscriptionID.Length != 0)
			{
				PM = AppLogic.ro_PMBypassGateway;
			}

			var cartTotal = cart.Total(true);
			var orderTotal = cartTotal;

			if(cart.Coupon.CouponType == CouponTypeEnum.GiftCard)
				orderTotal = cartTotal < cart.Coupon.DiscountAmount
					? 0
					: cartTotal - cart.Coupon.DiscountAmount;

			var setToCapturedState = false;

			var maxMindFraudResult = new MaxMindFraudCheck.MaxMindResult();

			// Signifyd Fraud Protection
			var signifydApi = DependencyResolver.Current.GetService<SignifydCaseApi>();
			var signifydConfigurationProvider = DependencyResolver.Current.GetService<SignifydConfigurationProvider>();
			var signifydConfiguration = signifydConfigurationProvider.Create();
			var signifydCasePayload = new SignifydCasePayload();

			// We need to have the latest updated session data to get the CardExtraCode.
			// If we're in checkout the cart customer and current customer will be the same, so we use the current customer.
			// Otherwise we're in recurring processing, but if the admin has an active session and was partway through 
			// checkout before starting processing, the currentCustomer would be Admin and we'd accidentally get the Admin's CardExtraCode,
			// so we new up the cart customer to get the latest session data.
			// We're doing all this to avoid the performance penalty of newing up the customer unless we have to.
			var currentCustomer = HttpContext.Current.GetCustomer();
			var CardExtraCode = cart.ThisCustomer.CustomerID == currentCustomer.CustomerID
				? AppLogic.GetCardExtraCodeFromSession(currentCustomer)
				: AppLogic.GetCardExtraCodeFromSession(new Customer(cart.ThisCustomer.CustomerID));

			#region Avalara - Prevent Order if Tax Can't be Calculated
			//Avalara - Fail order if tax can't be calculated
			if(AppLogic.AppConfigBool("AvalaraTax.Enabled") && AppLogic.AppConfigBool("AvalaraTax.PreventOrderIfAddressValidationFails"))
			{
				AvaTax avaTax = new AvaTax();
				try
				{
					//Address Validation isn't enough to be sure tax can't be calculated
					//So we have to check this again to ensure all is well.  Yet another call to GetTaxRate :(
					//Exception means something is very wrong, anything else is ok
					avaTax.GetTaxRate(customer, cart.CartItems, cart.OrderOptions);
				}
				catch(Exception ex)
				{
					status = String.Format(AppLogic.GetString("Avalara.AddressValidate.FixAddressBeforeCheckout", customer.LocaleSetting), avaTax.ValidateAddress(customer));
					SysLog.LogMessage(
						status,
						ex.Message,
						MessageTypeEnum.GeneralException,
						MessageSeverityEnum.Error);
					//log as failed transaction
					DB.ExecuteSQL(@"insert into FailedTransaction(CustomerID,OrderNumber,IPAddress,OrderDate,PaymentGateway,PaymentMethod,TransactionCommand,TransactionResult)
                            values(@CustomerID,@OrderNumber,@IPAddress,@OrderDate,@PaymentGateway,@PaymentMethod,@TransactionCommand,@TransactionResult)", new SqlParameter[] {
								new SqlParameter("@CustomerID", customer.CustomerID.ToString()),
								new SqlParameter("@OrderNumber", OrderNumber.ToString()),
								new SqlParameter("@IPAddress", customer.LastIPAddress),
								new SqlParameter("@OrderDate", DateTime.Now),
								new SqlParameter("@PaymentGateway", GW),
								new SqlParameter("@PaymentMethod", PM),
								new SqlParameter("@TransactionCommand", "AppConfig AvalaraTax.PreventOrderIfAddressValidationFails Stopped Order Processing"),
								new SqlParameter("@TransactionResult",ex.Message)});
					//bail immediately
					return status;
				}
			}
			#endregion

			if(orderTotal == System.Decimal.Zero)
			{
				AuthorizationTransID = "ZeroCostOrder";
				status = AppLogic.ro_OK; // nothing to charge!
			}
			else
			{
				#region Select Payment Method
				if(PM == AppLogic.ro_PMCreditCard)
				{
					// initialize capture state
					setToCapturedState = AppLogic.TransactionModeIsAuthCapture();

					if(AppLogic.AppConfigBool("Signifyd.Enabled"))
					{
						// store values for use when the case is actually created at the bottom of MakeOrder()
						signifydCasePayload.Gateway = GW;
						signifydCasePayload.Customer = customer;
						signifydCasePayload.BillingAddress = UseBillingAddress;
						signifydCasePayload.ShippingAddress = UseShippingAddress;
						signifydCasePayload.OrderNumber = OrderNumber;
						signifydCasePayload.OrderTotal = orderTotal;

						signifydApi.ValidateConfiguration(signifydCasePayload, signifydConfiguration);

						// when using Signifyd, all orders are created a AUTH only until we get a callback from them,
						// at which time we authorize the card (if approved and Transaction Mode = AUTHCAPTURE)
						setToCapturedState = false;
					}

					//MaxMind Fraud Protection
					var maxMindResult = new MaxMindFraudCheck().GetFraudScore(
						orderNumber: OrderNumber,
						customer: customer,
						billingAddress: UseBillingAddress,
						shippingAddress: UseShippingAddress,
						orderAmount: orderTotal,
						paymentMethod: PM,
						firstItemShippingAddressId: cart.FirstItemShippingAddressID());

					maxMindFraudResult = maxMindResult.Value;

					// MaxMind will return a Success if it's not enabled
					if(maxMindResult.Success && !maxMindResult.Value.FraudThresholdExceeded)
					{
						status = ProcessCard(
							cart: cart,
							gateway: GW,
							customerId: customer.CustomerID,
							orderNumber: OrderNumber,
							billingAddress: UseBillingAddress,
							cardExtraCode: CardExtraCode,
							shippingAddress: UseShippingAddress,
							orderTotal: orderTotal,
							useLiveTransactions: AppLogic.AppConfigBool("UseLiveTransactions"),
							cavv: CAVV,
							eci: ECI,
							xid: XID,
							transactionMode: setToCapturedState ? TransactionModeEnum.authcapture : TransactionModeEnum.auth,
							avsResult: out AVSResult,
							authorizationResult: out AuthorizationResult,
							authorizationCode: out AuthorizationCode,
							authorizationTransID: out AuthorizationTransID,
							transactionCommand: out TransactionCommand,
							transactionResponse: out TransactionResponse,
							gatewayUsed: out GW);

						if(AppLogic.AppConfigBool("Signifyd.Enabled"))
						{
							// update values based on auth result
							signifydCasePayload.TransactionId = AuthorizationTransID;
							signifydCasePayload.AVSResult = AVSResult;
							signifydCasePayload.CAVVResponseCode = string.Empty; // gateway process card implementations are not returning the CAVV result, so this has to be an empty string here
						}
					}
					else
					{
						DB.ExecuteSQL(@"
							insert into FailedTransaction (
								CustomerID,
								OrderNumber,
								IPAddress,
								MaxMindFraudScore,
								MaxMindDetails,
								OrderDate,
								PaymentGateway,
								PaymentMethod,
								TransactionCommand,
								TransactionResult)
							values(
								@CustomerID,
								@OrderNumber,
								@LastIPAddress,
								@FraudScore,
								@FraudDetails,
								getdate(),
								@PaymentGateway,
								@PaymentMethod,
								@TransactionCommand,
								@TransactionResult)",
							new[]
							{
								new SqlParameter("@CustomerId", customer.CustomerID),
								new SqlParameter("@OrderNumber", OrderNumber),
								new SqlParameter("@LastIPAddress", customer.LastIPAddress),
								new SqlParameter("@FraudScore", maxMindFraudResult.FraudScore),
								new SqlParameter("@FraudDetails", maxMindFraudResult.FraudDetails),
								new SqlParameter("@PaymentGateway", "MAXMIND"),
								new SqlParameter("@PaymentMethod", PM),
								new SqlParameter("@TransactionCommand", $"MAXMIND FRAUD SCORE={Localization.DecimalStringForDB(maxMindFraudResult.FraudScore)},"),
								new SqlParameter("@TransactionResult", AppLogic.ro_NotApplicable)
							});
						status = "MAXMIND FRAUD CHECK FAILED";
					}
				}
				else if(PM == AppLogic.ro_PMECheck)
				{
					status = ProcessECheck(
								cart: cart,
								customerId: customer.CustomerID,
								orderNumber: OrderNumber,
								useLiveTransactions: AppLogic.AppConfigBool("UseLiveTransactions"),
								transactionMode: setToCapturedState ? TransactionModeEnum.authcapture : TransactionModeEnum.auth,
								billingAddress: UseBillingAddress,
								shippingAddress: UseShippingAddress,
								orderTotal: orderTotal,
								avsResult: out AVSResult,
								authorizationResult: out AuthorizationResult,
								authorizationCode: out AuthorizationCode,
								authorizationTransId: out AuthorizationTransID,
								transactionCommand: out TransactionCommand,
								transactionResponse: out TransactionResponse);

					setToCapturedState = true;
				}
				else if(PM == AppLogic.ro_PMAmazonPayments)
				{
					setToCapturedState = TransactionMode != AppLogic.ro_TXModeAuthOnly;

					orderTrackingDetail.OrderReference.Id = UseBillingAddress.CardNumber;
					Address updatedShippingAddress;

					status = new AmazonPaymentsGateway()
						.AuthorizeOrder(
							orderTrackingDetail: ref orderTrackingDetail,
							orderNumber: OrderNumber,
							customerId: customer.CustomerID,
							orderTotal: orderTotal,
							useLiveTransactions: AppLogic.AppConfigBool("UseLiveTransactions"),
							authAndCapture: setToCapturedState,
							updatedShippingAddress: out updatedShippingAddress);

					if(updatedShippingAddress != null)
					{
						// update address fields from AmazonPaymentsGateway if they are updated there
						customer.PrimaryShippingAddress.FirstName = updatedShippingAddress.FirstName;
						customer.PrimaryShippingAddress.LastName = updatedShippingAddress.LastName;
						customer.PrimaryShippingAddress.Address1 = updatedShippingAddress.Address1;
						customer.PrimaryShippingAddress.Address2 = updatedShippingAddress.Address2;
						customer.PrimaryShippingAddress.Phone = updatedShippingAddress.Phone;
					}

					if(status != AppLogic.ro_OK)
					{
						UseBillingAddress.CardNumber = string.Empty;
						UseBillingAddress.UpdateDB();
					}

					var serializer = new Processors.AmazonPaymentsOrderTrackingDetailSerializer();
					serializedAmazonOrderTrackingDetail = serializer.SerializeAmazonOrderTrackingDetail(orderTrackingDetail);
				}
				else if(PM == AppLogic.ro_PMPayPalExpress || PM == AppLogic.ro_PMPayPalExpressMark)
				{
					String PayPalToken = CAVV;  //hack: needed place to pass token and payerID.
					String PayerID = ECI;
					// Note that we are reseting GW here for this order. (output parameter)
					// This is so that refund/void etc. will go through the proper gateway.
					status = ProcessExpressCheckout(cart, orderTotal, OrderNumber, PayPalToken, PayerID, TransactionMode, out AuthorizationResult, out AuthorizationTransID, out GW);
					setToCapturedState = false;
					if(TransactionMode != AppLogic.ro_TXModeAuthOnly
						|| AppLogic.AppConfigBool("PayPal.ForceCapture"))
					{
						setToCapturedState = true;
					}
				}
				else if(PM == AppLogic.ro_PMMicropay)
				{
					status = MicropayController.ProcessTransaction(OrderNumber, customer.CustomerID, orderTotal, AppLogic.AppConfigBool("UseLiveTransactions"), AppLogic.TransactionMode(), UseBillingAddress, String.Empty, UseShippingAddress, CAVV, ECI, XID, out AVSResult, out AuthorizationResult, out AuthorizationCode, out AuthorizationTransID, out TransactionCommand, out TransactionResponse);
					setToCapturedState = AppLogic.TransactionModeIsAuthCapture();
				}
				else if(PM == AppLogic.ro_PMPurchaseOrder)
				{
					status = AppLogic.ro_OK;
					setToCapturedState = false;
				}
				else if(PM == AppLogic.ro_PMCheckByMail)
				{
					status = AppLogic.ro_OK;
					setToCapturedState = false;
				}
				else if(PM == AppLogic.ro_PMCOD)
				{
					status = AppLogic.ro_OK;
					setToCapturedState = false;
				}
				else if(PM == AppLogic.ro_PMRequestQuote)
				{
					status = AppLogic.ro_OK;
					setToCapturedState = false;
				}
				else if(PM == AppLogic.ro_PMBypassGateway)
				{
					status = AppLogic.ro_OK;
					setToCapturedState = true;
				}
				else if(PM == AppLogic.ro_PMPayPalEmbeddedCheckout)
				{
					status = AppLogic.ro_OK;
					setToCapturedState = false;
				}
				else
				{
					//PM has somehow ended up as something invalid.  Don't process the order.
					status = AppLogic.GetString("admin.common.PaymentMethodErrorPrompt", customer.LocaleSetting);
				}
				#endregion
			}
			if(orderTotal == System.Decimal.Zero || RecurringSubscriptionID.Length != 0)
			{
				setToCapturedState = true; // zero dollar orders always get set to captured state right away!
			}
			#region If Status OK - Create Order
			if(status == AppLogic.ro_OK)
			{
				CreateOrderRecord(cart, OrderNumber, UseBillingAddress);

				//Address cleanup for offsite payment methods where we lock down the address until after the order is placed
				if(AppLogic.ro_OffsitePayMentMethods.Contains(PM))
				{
					//This should have the full address details from the offsite vendor now.  Allow it to be used/edited in the future.
					Address.ReleaseOffsiteAddress(customer, customer.PrimaryShippingAddress);
				}

				Address.CleanupAbandonOffsiteAddresses(customer);

				if(UseBillingAddress.CardNumber == null)
				{
					UseBillingAddress.CardNumber = string.Empty;
				}
				if(CardExtraCode == null)
				{
					CardExtraCode = string.Empty;
				}
				if(TransactionCommand == null)
				{
					TransactionCommand = string.Empty;
				}
				if(AuthorizationResult == null)
				{
					AuthorizationResult = string.Empty;
				}
				if(AVSResult == null)
				{
					AVSResult = string.Empty;
				}
				if(AuthorizationCode == null)
				{
					AuthorizationCode = string.Empty;
				}
				if(AuthorizationTransID == null)
				{
					AuthorizationTransID = string.Empty;
				}
				if(RecurringSubscriptionID == null)
				{
					RecurringSubscriptionID = string.Empty;
				}

				string TransCMD = TransactionCommand;
				if(TransCMD.Length != 0 && UseBillingAddress.CardNumber != null && UseBillingAddress.CardNumber.Length != 0)
				{
					string tmp1 = AppLogic.SafeDisplayCardNumber(UseBillingAddress.CardNumber, String.Empty, 0);
					TransCMD = TransCMD.Replace(UseBillingAddress.CardNumber, tmp1);
				}
				if(TransCMD.Length != 0 && CardExtraCode != null && CardExtraCode.Length != 0)
				{
					string tmp2 = AppLogic.SafeDisplayCardExtraCode(CardExtraCode);
					TransCMD = TransCMD.Replace(CardExtraCode, tmp2);
				}

				if(GW == ro_GWSAGEPAYPI)
				{
					var order = new Order(OrderNumber, customer.LocaleSetting);
					order.CaptureTXCommand = TransactionCommand;
					order.CaptureTXResult = TransactionResponse;
				}

				// we dont' need it anymore. NUKE IT!
				TransactionCommand = "1".PadLeft(TransactionCommand.Length);
				TransactionCommand = string.Empty;

				string TransRES = AuthorizationResult;
				if(TransRES.Length != 0 && UseBillingAddress.CardNumber != null && UseBillingAddress.CardNumber.Length != 0)
				{
					String tmp3 = AppLogic.SafeDisplayCardNumber(UseBillingAddress.CardNumber, String.Empty, 0);
					TransRES = TransRES.Replace(UseBillingAddress.CardNumber, tmp3);
				}
				if(TransRES.Length != 0 && CardExtraCode != null && CardExtraCode.Length != 0)
				{
					String tmp4 = AppLogic.SafeDisplayCardExtraCode(CardExtraCode);
					TransRES = TransRES.Replace(CardExtraCode, tmp4);
				}
				// we dont' need it anymore. NUKE IT!
				AuthorizationResult = "1".PadLeft(AuthorizationResult.Length);
				AuthorizationResult = String.Empty;

				var cleanedFraudDetails = maxMindFraudResult.FraudDetails;
				if(!string.IsNullOrEmpty(cleanedFraudDetails) && !string.IsNullOrEmpty(UseBillingAddress.CardNumber))
					cleanedFraudDetails = cleanedFraudDetails
						.Replace(UseBillingAddress.CardNumber, AppLogic.SafeDisplayCardNumber(UseBillingAddress.CardNumber, string.Empty, 0))
						.Replace(CardExtraCode, AppLogic.SafeDisplayCardExtraCode(CardExtraCode));

				// we dont' need it anymore. NUKE IT!
				maxMindFraudResult.FraudDetails = "1".PadLeft(maxMindFraudResult.FraudDetails.Length);
				maxMindFraudResult.FraudDetails = string.Empty;

				// DO NOT set a captured state here, it will be set by ProcessOrderAsCaptured later in this routine, if appropriate:
				// we just handle the auth  here!
				string transState = AppLogic.isPendingPM(PM)
					? AppLogic.ro_TXStatePending
					: AppLogic.ro_TXStateAuthorized;

				string sql2 = string.Empty;
				if(PM == AppLogic.ro_PMAmazonPayments)
				{
					sql2 =
						string.Format("UPDATE Orders SET PaymentGateway={0}, TransactionCommand={1}, AuthorizationPNREF={2}, AuthorizationResult={3}, AuthorizationCode={4}, VoidTXCommand={5}, VoidTXResult={6}, CaptureTXCommand={7}, CaptureTXResult={8}, RefundTXCommand={9}, RefundTXResult={10}, TransactionState={11}, AuthorizedOn=GETDATE() WHERE OrderNumber={12}",
						DB.SQuote(GW), DB.SQuote(serializedAmazonOrderTrackingDetail), DB.SQuote(orderTrackingDetail.Authorization.Id), DB.SQuote(orderTrackingDetail.Authorization.State), DB.SQuote(orderTrackingDetail.Authorization.ReasonCode), DB.SQuote(orderTrackingDetail.Cancel.Id), DB.SQuote(orderTrackingDetail.Cancel.State), DB.SQuote(orderTrackingDetail.Capture.Id), DB.SQuote(orderTrackingDetail.Capture.State), DB.SQuote(orderTrackingDetail.Refund.Id), DB.SQuote(orderTrackingDetail.Refund.State), DB.SQuote(transState), OrderNumber.ToString());
				}
				else
				{
					sql2 =
						string.Format("update Orders set PaymentGateway={0}, AVSResult={1}, AuthorizationResult={2}, AuthorizationCode={3}, AuthorizationPNREF={4}, MaxMindFraudScore={5}, MaxMindDetails={6}, TransactionState={7}, AuthorizedOn=getdate(), TransactionCommand={8}, RecurringSubscriptionID={9} where OrderNumber={10}",
						DB.SQuote(GW), DB.SQuote(AVSResult), DB.SQuote(TransRES), DB.SQuote(AuthorizationCode), DB.SQuote(AuthorizationTransID),
							Localization.DecimalStringForDB(maxMindFraudResult.FraudScore), DB.SQuote(cleanedFraudDetails), DB.SQuote(transState),
						DB.SQuote(TransCMD), DB.SQuote(RecurringSubscriptionID), OrderNumber.ToString());
				}
				DB.ExecuteSQL(sql2);

				if(RecurringSubscriptionID.Length != 0)
				{
					// remember to set special TransactionType for a gateway recurring autobill:
					sql2 = String.Format("update Orders set ParentOrderNumber={0}, TransactionType={1} where OrderNumber={2}", cart.OriginalRecurringOrderNumber.ToString(), ((int)AppLogic.TransactionTypeEnum.RECURRING_AUTO).ToString(), OrderNumber.ToString());
					DB.ExecuteSQL(sql2);
				}

				// order was ok, clean up shopping cart and move cart to order cart:
				if(cart.HasCoupon())
				{
					Order o = new Order(OrderNumber, customer.LocaleSetting);
					GiftCard gc = new GiftCard(cart.Coupon.CouponCode);
					decimal TransAmt = CommonLogic.IIF(o.Total() > gc.Balance, gc.Balance, o.Total());
					gc.AddTransaction(TransAmt, customer.CustomerID, OrderNumber);
				}

				//When processing Recurring order need to limit to the current _originalRecurringOrderNumber
				String RecurringOrderSql = String.Empty;
				if(cart.OriginalRecurringOrderNumber != 0)
				{
					RecurringOrderSql = String.Format(" and OriginalRecurringOrderNumber={0}", cart.OriginalRecurringOrderNumber);
				}

				CreateOrderShoppingCartRecords(OrderNumber, cart, customer);

				if(cart.HasGiftCards())
				{
					GiftCard.SyncOrderNumber(OrderNumber);
				}
				//For multi shipping address orders fix up the ShippingDetail if different from the primary Shipping address. 
				var sql4 = "select ShoppingCartRecID, ShippingAddressID from ShoppingCart "
					+ String.Format(" where CartType={0} and CustomerID={1} ", (int)cart.CartType, customer.CustomerID)
					+ RecurringOrderSql;

				using(SqlConnection con = new SqlConnection(DB.GetDBConn()))
				{
					con.Open();
					using(IDataReader rso = DB.GetRS(sql4, con))
					{
						while(rso.Read())
						{
							int addressID = DB.RSFieldInt(rso, "ShippingAddressID");
							int cartID = DB.RSFieldInt(rso, "ShoppingCartRecID");
							if(addressID == 0)
							{
								addressID = customer.PrimaryShippingAddressID;
							}
							Address shipAddress = new Address();
							shipAddress.LoadFromDB(addressID);
							string sql = "update orders_shoppingcart set ShippingAddressID=" + addressID.ToString() + ", ShippingDetail=" + DB.SQuote(shipAddress.AsXml) + " where ShoppingCartRecID=" + cartID.ToString();
							DB.ExecuteSQL(sql);
						}
					}
				}

				String sql5 = "insert into orders_kitcart(OrderNumber,CartType,KitCartRecID,CustomerID,ShoppingCartRecID,ProductID,VariantID,ProductName,productVariantName,KitGroupID,KitGroupTypeID,InventoryVariantID,InventoryVariantColor,InventoryVariantSize,KitGroupName,KitGroupIsRequired,KitItemID,KitItemName,KitItemPriceDelta,KitItemWeightDelta,TextOption,Quantity)"
					+ String.Format(" select {0},CartType,KitCartRecID,CustomerID,ShoppingCartRecID,KitCart.ProductID,KitCart.VariantID,Product.Name,ProductVariant.Name,KitCart.KitGroupID,KitCart.KitGroupTypeID,KitItem.InventoryVariantID,KitItem.InventoryVariantColor,KitItem.InventoryVariantSize,KitGroup.Name,KitGroup.IsRequired,KitCart.KitItemID,KitItem.Name,KitItem.PriceDelta,KitItem.WeightDelta,KitCart.TextOption,Quantity", OrderNumber)
					+ " FROM ((((KitCart   with (NOLOCK)  INNER JOIN KitGroup   with (NOLOCK)  ON KitCart.KitGroupID = KitGroup.KitGroupID)"
					+ " INNER JOIN KitItem  with (NOLOCK)  ON KitCart.KitItemID = KitItem.KitItemID)"
					+ " INNER JOIN Product   with (NOLOCK)  ON KitCart.ProductID = Product.ProductID)"
					+ " INNER JOIN ProductVariant   with (NOLOCK)  ON KitCart.VariantID = ProductVariant.VariantID)"
					+ String.Format(" WHERE CartType={0} and customerid={1} and ShoppingCartRecID <> 0", (int)cart.CartType, customer.CustomerID)
					+ RecurringOrderSql;
				DB.ExecuteSQL(sql5);

				// Move Kit image uploads to their permanent location
				using(var connection = new SqlConnection(DB.GetDBConn()))
				{
					connection.Open();
					var updatedImageInfo = new Dictionary<int, string>();
					using(var command = connection.CreateCommand())
					{
						command.CommandText = @"
							select KitCartRecId, CustomerId, ProductId, KitItemID, TextOption 
							from Orders_KitCart
							where OrderNumber = @orderNumber
							and KitGroupTypeID = @kitGroupTypeId";
						command.Parameters.AddWithValue("@orderNumber", OrderNumber);
						command.Parameters.AddWithValue("@kitGroupTypeId", KitGroupData.FILE_OPTION);

						using(var reader = command.ExecuteReader())
							while(reader.Read())
							{
								var appRelativePathTempPath = DB.RSField(reader, "TextOption");
								if(string.IsNullOrEmpty(appRelativePathTempPath))
									continue;

								var temporaryFilePath = CommonLogic.SafeMapPath(appRelativePathTempPath.TrimStart('/'));
								if(!File.Exists(temporaryFilePath))
									continue;

								var fileExtension = Path.GetExtension(temporaryFilePath);
								var moveFileName = string.Format("{0}_{1}_{2}_{3}{4}",
									DB.RSFieldInt(reader, "CustomerId"),
									DB.RSFieldInt(reader, "ProductId"),
									DB.RSFieldInt(reader, "KitItemID"),
									DB.RSFieldInt(reader, "KitCartRecId"),
									fileExtension);

								var moveFilePath = string.Concat(
									AppLogic.GetImagePath(
										entityOrObjectName: "Orders",
										size: string.Empty,
										fullPath: false),
									moveFileName);

								var moveFileFullPath = Path.Combine(
									AppLogic.GetImagePath(
										entityOrObjectName: "Orders",
										size: string.Empty,
										fullPath: true),
									moveFileName);

								try
								{
									File.Move(temporaryFilePath, moveFileFullPath);
									updatedImageInfo.Add(DB.RSFieldInt(reader, "KitCartRecId"), moveFilePath);
								}
								catch(Exception exception)
								{
									SysLog.LogException(exception, MessageTypeEnum.GeneralException, MessageSeverityEnum.Error);
								}
							}
					}

					foreach(var updatedImage in updatedImageInfo)
					{
						using(var command = connection.CreateCommand())
						{
							command.CommandText = "update Orders_KitCart set TextOption = @textOption where kitCartRecid = @kitCartRecId";
							command.Parameters.AddWithValue("@textOption", updatedImage.Value);
							command.Parameters.AddWithValue("@kitCartRecId", updatedImage.Key);
							command.ExecuteNonQuery();
						}
					}
				}

				// download products
				if(cart.HasDownloadComponents())
				{
					bool autoRelease = AppLogic.AppConfig("Download.ReleaseOnAction").EqualsIgnoreCase("auto");

					DownloadItem downloadItem = new DownloadItem();
					foreach(CartItem c in cart.CartItems.Where(w => w.IsDownload))
					{
						downloadItem.Create(OrderNumber, c);
						if(autoRelease)
						{
							downloadItem.Load(c.ShoppingCartRecordID);
							downloadItem.SendDownloadEmailNotification(false);
							downloadItem.Release(false);
						}
					}
				}

				bool m_CustomerLevelAllowsQuantityDiscounts = QuantityDiscount.CustomerLevelAllowsQuantityDiscounts(customer.CustomerLevelID);
				// now set extended pricing info in the order cart to take into account all levels, quantities, etc...so the order object doesn't have to recompute cart stuff
				foreach(CartItem c in cart.CartItems)
				{
					if(!c.CustomerEntersPrice && !AppLogic.IsAKit(c.ProductID) && !c.IsUpsell)
					{
						int Q = c.Quantity;
						bool IsOnSale = false;
						decimal pr = 0.0M;
						if(cart.CartType == CartTypeEnum.RecurringCart || c.ProductID == 0)
						{
							pr = c.Price; // price is grandfathered
						}
						else
						{
							pr = AppLogic.DetermineLevelPrice(c.VariantID, customer.CustomerLevelID, out IsOnSale);
						}
						pr = pr * Q;
						Decimal DIDPercent = 0.0M;
						QuantityDiscount.QuantityDiscountType fixedPriceDID = QuantityDiscount.QuantityDiscountType.Percentage;
						if(m_CustomerLevelAllowsQuantityDiscounts)
						{
							DIDPercent = QuantityDiscount.GetQuantityDiscountTablePercentageForLineItem(c, out fixedPriceDID);
							if(DIDPercent != 0.0M)
							{
								if(fixedPriceDID.Equals(QuantityDiscount.QuantityDiscountType.FixedAmount))
								{
									pr = (c.Price - DIDPercent) * (Decimal)Q;
								}
								else
								{
									pr = (1.0M - (DIDPercent / 100.0M)) * pr;
								}
							}
						}
						decimal regular_pr = System.Decimal.Zero;
						decimal sale_pr = System.Decimal.Zero;
						decimal extended_pr = System.Decimal.Zero;
						if(cart.CartType != CartTypeEnum.RecurringCart)
						{
							regular_pr = AppLogic.GetVariantPrice(c.VariantID);
							sale_pr = AppLogic.GetVariantSalePrice(c.VariantID);
							extended_pr = AppLogic.GetVariantExtendedPrice(c.VariantID, customer.CustomerLevelID);

							// Adjust for color and size price modifiers
							Decimal PrMod = AppLogic.GetColorAndSizePriceDelta(c.ChosenColor, c.ChosenSize, c.TaxClassID, customer, true, true);

							if(PrMod != System.Decimal.Zero)
							{
								pr += Decimal.Round(PrMod * (1.0M - (DIDPercent / 100.0M)), 2, MidpointRounding.AwayFromZero) * Q;
							}
							if(pr < System.Decimal.Zero)
							{
								pr = System.Decimal.Zero;
							}
						}
						else
						{
							regular_pr = c.Price;
							sale_pr = System.Decimal.Zero;
							extended_pr = System.Decimal.Zero;
						}

						DB.ExecuteSQL("update orders_ShoppingCart set OrderedProductPrice=" + Localization.DecimalStringForDB(pr) + ", OrderedProductRegularPrice=" + Localization.CurrencyStringForDBWithoutExchangeRate(regular_pr) + ", OrderedProductSalePrice=" + Localization.CurrencyStringForDBWithoutExchangeRate(sale_pr) + ", OrderedProductExtendedPrice=" + Localization.CurrencyStringForDBWithoutExchangeRate(extended_pr) + " where OrderNumber=" + OrderNumber.ToString() + " and ShoppingCartRecID=" + c.ShoppingCartRecordID.ToString());
					}
					else if(!c.CustomerEntersPrice && !AppLogic.IsAKit(c.ProductID) && c.IsUpsell)
					{
						int Q = c.Quantity;
						decimal pr = c.Price * Q;
						DB.ExecuteSQL("update orders_ShoppingCart set OrderedProductPrice=" + Localization.DecimalStringForDB(pr) + ", OrderedProductRegularPrice=" + Localization.CurrencyStringForDBWithoutExchangeRate(c.Price) + " where OrderNumber=" + OrderNumber.ToString() + " and ShoppingCartRecID=" + c.ShoppingCartRecordID.ToString());
					}
					else
					{
						int Q = c.Quantity;
						decimal pr = c.Price * Q;
						DB.ExecuteSQL("update orders_ShoppingCart set OrderedProductPrice=" + Localization.DecimalStringForDB(pr) + " where OrderNumber=" + OrderNumber.ToString() + " and ShoppingCartRecID=" + c.ShoppingCartRecordID.ToString());
					}
				}

				// make sure inventory was deducted. safe to call repeatedly. proc protects against deducting twice
				DB.ExecuteSQL("aspdnsf_AdjustInventory " + OrderNumber.ToString() + ",-1");

				// TFS 821: Create an order shipment record for each shipment
				List<int> shippingAddressIds = Shipping.GetDistinctShippingAddressIDs(cart.CartItems);
				foreach(var shippingAddressId in shippingAddressIds)
				{
					var shippingAddress = new Address();
					shippingAddress.LoadFromDB(shippingAddressId);

					decimal shippingTotal = Prices.ShippingTotalForAddress(cart.CartItems, customer, shippingAddress, includeTax: true);
					DB.ExecuteSQL("insert into OrderShipment (OrderNumber, AddressID, ShippingTotal) select distinct OrderNumber, ShippingAddressID, @shippingTotal from Orders_ShoppingCart where OrderNumber = @orderNumber and ShippingAddressID = @shippingAddressID",
						new[] {
						new SqlParameter("@orderNumber", OrderNumber),
						new SqlParameter("@shippingAddressID", shippingAddressId),
						new SqlParameter("@shippingTotal", shippingTotal),
					});
				}

				// clear cart
				String RecurringVariantsList = AppLogic.GetRecurringVariantsList();

				if(cart.CartType == CartTypeEnum.ShoppingCart)
				{
					// clear "normal" items out of cart, but leave any recurring items or wishlist items still in there:
					DB.ExecuteSQL("delete from kitcart where CartType=" + ((int)CartTypeEnum.ShoppingCart).ToString() + CommonLogic.IIF(RecurringVariantsList.Length != 0, " and VariantID not in (" + RecurringVariantsList + ")", "") + " and customerid=" + customer.CustomerID.ToString());
					string query = string.Format("delete from ShoppingCart where ShoppingCartRecID in(select a.ShoppingCartRecID from ShoppingCart a with (nolock) inner join (select distinct a.ProductID,a.StoreID from ShoppingCart a with (nolock) left join ProductStore b with (nolock) on a.ProductID = b.ProductID " +
					"where ({0} = 0 or b.StoreID = a.StoreID)) b on a.ProductID = b.ProductID and a.StoreID = b.StoreID where CartType={1} and CustomerID={2} and ({3} = 0 or a.StoreID = {4}))", CommonLogic.IIF(AppLogic.GlobalConfigBool("AllowProductFiltering") == true, 1, 0), ((int)CartTypeEnum.ShoppingCart).ToString() +
					CommonLogic.IIF(RecurringVariantsList.Length != 0, " and VariantID not in (" + RecurringVariantsList + ")", ""), customer.CustomerID, CommonLogic.IIF(AppLogic.GlobalConfigBool("AllowShoppingcartFiltering") == true, 1, 0), AppLogic.StoreID());

					DB.ExecuteSQL(query);
				}

				if(RecurringVariantsList.Length != 0 && cart.ContainsRecurringAutoShip)
				{
					// WE HAVE RECURRING ITEMS! They should be left in the cart, so the next recurring process will still find them.

					DateTime NextRecurringShipDate = System.DateTime.Now.AddMonths(1); // default just for safety, should never be used
					if(cart.OriginalRecurringOrderNumber == 0)
					{
						// this is a completely NEW recurring order, so set the recurring master parameters:
						String ThisOrderDate = Localization.ToNativeDateTimeString(System.DateTime.Now);
						foreach(CartItem c in cart.CartItems)
						{
							if(c.IsRecurring)
							{
								switch(c.RecurringIntervalType)
								{
									case DateIntervalTypeEnum.Day:
										NextRecurringShipDate = System.DateTime.Now.AddDays(c.RecurringInterval);
										break;
									case DateIntervalTypeEnum.Week:
										NextRecurringShipDate = System.DateTime.Now.AddDays(7 * c.RecurringInterval);
										break;
									case DateIntervalTypeEnum.Month:
										NextRecurringShipDate = System.DateTime.Now.AddMonths(c.RecurringInterval);
										break;
									case DateIntervalTypeEnum.Year:
										NextRecurringShipDate = System.DateTime.Now.AddYears(c.RecurringInterval);
										break;
									case DateIntervalTypeEnum.NumberOfDays:
										NextRecurringShipDate = System.DateTime.Now.AddDays(c.RecurringInterval);
										break;
									case DateIntervalTypeEnum.Weekly:
										NextRecurringShipDate = System.DateTime.Now.AddDays(7);
										break;
									case DateIntervalTypeEnum.BiWeekly:
										NextRecurringShipDate = System.DateTime.Now.AddDays(14);
										break;
									case DateIntervalTypeEnum.EveryFourWeeks:
										NextRecurringShipDate = System.DateTime.Now.AddDays(28);
										break;
									case DateIntervalTypeEnum.Monthly:
										NextRecurringShipDate = System.DateTime.Now.AddMonths(1);
										break;
									case DateIntervalTypeEnum.Quarterly:
										NextRecurringShipDate = System.DateTime.Now.AddMonths(3);
										break;
									case DateIntervalTypeEnum.SemiYearly:
										NextRecurringShipDate = System.DateTime.Now.AddMonths(6);
										break;
									case DateIntervalTypeEnum.Yearly:
										NextRecurringShipDate = System.DateTime.Now.AddYears(1);
										break;
									default:
										NextRecurringShipDate = System.DateTime.Now.AddMonths(c.RecurringInterval);
										break;
								}

								if(AppLogic.AppConfigBool("Recurring.LimitCustomerToOneOrder"))
								{
									int MigrateDays = RecurringOrderMgr.ProcessAutoBillMigrateExisting(customer.CustomerID);
									if(MigrateDays != 0)
									{
										NextRecurringShipDate = NextRecurringShipDate.AddDays((double)MigrateDays);
									}
								}

								DB.ExecuteSQL("update ShoppingCart set BillingAddressID=" + UseBillingAddress.AddressID.ToString() + ",RecurringIndex=1, CartType=" + ((int)CartTypeEnum.RecurringCart).ToString() + ", CreatedOn=" + DB.DateQuote(Localization.ToDBShortDateString(DateTime.Parse(ThisOrderDate))) + ", NextRecurringShipDate=" + DB.DateQuote(Localization.ToDBShortDateString(NextRecurringShipDate)) + ", OriginalRecurringOrderNumber=" + OrderNumber.ToString() + " where (OriginalRecurringOrderNumber is null or OriginalRecurringOrderNumber=0) and VariantID=" + c.VariantID.ToString() + " and CartType=" + ((int)CartTypeEnum.ShoppingCart).ToString() + " and customerid=" + customer.CustomerID.ToString());
							}
						}
						DB.ExecuteSQL("update kitcart set CartType=" + ((int)CartTypeEnum.RecurringCart).ToString() + ", CreatedOn=" + DB.DateQuote(Localization.ToDBShortDateString(DateTime.Parse(ThisOrderDate))) + ", OriginalRecurringOrderNumber=" + OrderNumber.ToString() + " where (OriginalRecurringOrderNumber is null or OriginalRecurringOrderNumber=0) and VariantID in (" + RecurringVariantsList + ") and CartType=" + ((int)CartTypeEnum.ShoppingCart).ToString() + " and customerid=" + customer.CustomerID.ToString());



						//Recurring PayPal Express logic
						ExpressAPIType expressApiType = PayPalController.GetAppropriateExpressType();

						if((PM == AppLogic.ro_PMPayPalExpress && expressApiType == ExpressAPIType.PayPalExpress))
						{
							String ecRecurringProfileStatus = String.Empty;

							if(PM == AppLogic.ro_PMPayPalExpress)
								ecRecurringProfileStatus = MakeExpressCheckoutRecurringProfile(cart, OrderNumber, CAVV, ECI, NextRecurringShipDate);

							if(ecRecurringProfileStatus != AppLogic.ro_OK)
							{
								try
								{
									// send email notification to admin
									string emailSubject = String.Format(AppLogic.GetString("recurringorder.ppecfailed.subject", customer.SkinID, customer.LocaleSetting), AppLogic.AppConfig("StoreName"));
									string emailBody = String.Format(AppLogic.GetString("recurringorder.ppecfailed.body", customer.SkinID, customer.LocaleSetting), OrderNumber.ToString());

									if(!AppLogic.AppConfigBool("TurnOffStoreAdminEMailNotifications"))
									{
										String SendToList = AppLogic.AppConfig("GotOrderEMailTo").Replace(",", ";");
										if(SendToList.IndexOf(';') != -1)
										{
											foreach(String s in SendToList.Split(';'))
											{
												AppLogic.SendMail(subject: emailSubject,
													body: emailBody + AppLogic.AppConfig("MailFooter"),
													useHtml: true,
													fromAddress: AppLogic.AppConfig("GotOrderEMailFrom"),
													fromName: AppLogic.AppConfig("GotOrderEMailFromName"),
													toAddress: s.Trim(),
													toName: s.Trim(),
													bccAddresses: String.Empty,
													server: AppLogic.MailServer());
											}
										}
										else
										{
											AppLogic.SendMail(subject: emailSubject,
												body: emailBody + AppLogic.AppConfig("MailFooter"),
												useHtml: true,
												fromAddress: AppLogic.AppConfig("GotOrderEMailFrom"),
												fromName: AppLogic.AppConfig("GotOrderEMailFromName"),
												toAddress: SendToList.Trim(),
												toName: SendToList.Trim(),
												bccAddresses: String.Empty,
												server: AppLogic.MailServer());
										}
									}
								}
								catch
								{
									SysLog.LogMessage(String.Format(AppLogic.GetStringForDefaultLocale("recurringorder.ppecfailed.body"),
										OrderNumber.ToString()),
										ecRecurringProfileStatus,
										MessageTypeEnum.Informational,
										MessageSeverityEnum.Message);
								}
							}
						}

						if(AppLogic.AppConfigBool("Recurring.UseGatewayInternalBilling") && RecurringSubscriptionID.Length == 0
							&& (GW == Gateway.eWAYGatewayName || GW == "USAEPAY" || GW == "AUTHORIZENET" || GW == Gateway.ro_GWPAYFLOWPRO || GW == Gateway.ro_GWPAYPALEMBEDDEDCHECKOUT || GW == Gateway.ro_GWPAYPAL)
							&& PM == AppLogic.ro_PMCreditCard)
						{
							// Call gateway recurring subscription setup here to get SubscriptionID, etc
							// NOTES:
							//
							// 1) We have already charged the customer's card for this initial (starting) occurrence as part of creating this order prior to this point
							//
							// 2) ALL Cart items must have exact same recurring interval at this point!! If a single cart contained multiple
							//    products with different recurring intervals, they need to have been split out into different carts & orders
							//    before this point
							//
							// 3) TBD it is UNCLEAR at this point what to do if the create subscription call fails, as we've already created the
							//    order and charged the customer
							//
							CartItem firstcartrecurringitem = ((CartItem)cart.CartItems[0]);
							foreach(CartItem c in cart.CartItems)
							{
								if(c.IsRecurring)
								{
									firstcartrecurringitem = c;
									break;
								}
							}

							// This works if the cart has a single recurring item, or multiple items with same schedule.
							// TBD handling carts with multiple recurring items with differing schedules.
							ShoppingCart cartRecur = new ShoppingCart(customer.SkinID, customer, CartTypeEnum.RecurringCart, OrderNumber,
									false); // false will load recurring items that are not due yet, which we need to do here

							Decimal CartTotalRecur = Decimal.Round(cartRecur.Total(true), 2, MidpointRounding.AwayFromZero);
							Decimal RecurringAmount = CartTotalRecur - CommonLogic.IIF(cartRecur.Coupon.CouponType == CouponTypeEnum.GiftCard, CommonLogic.IIF(CartTotalRecur < cartRecur.Coupon.DiscountAmount, CartTotalRecur, cartRecur.Coupon.DiscountAmount), 0);

							String RecurringSubscriptionSetupStatus = String.Empty;
							String RecurringSubscriptionCommand = String.Empty;
							String RecurringSubscriptionResult = String.Empty;

							// dynamically load the gateway processor class via the name

							GatewayProcessor processor = GatewayLoader.GetProcessor(GW);

							if(processor != null)
							{
								if(PM == AppLogic.ro_PMPayPalExpress)
									XID = AuthorizationTransID;

								if(GW == Gateway.ro_GWPAYPAL && PM == AppLogic.ro_PMCreditCard)
									XID = CardExtraCode;


								RecurringSubscriptionSetupStatus = processor.RecurringBillingCreateSubscription(firstcartrecurringitem.ProductName,
													customer,
													UseBillingAddress,
													UseShippingAddress,
													RecurringAmount,
													NextRecurringShipDate,
													firstcartrecurringitem.RecurringInterval,
													firstcartrecurringitem.RecurringIntervalType,
													OrderNumber,
													XID,
													TransactionContext,
													out RecurringSubscriptionID,
													out RecurringSubscriptionCommand,
													out RecurringSubscriptionResult);
							}

							// wipe card #, if not done by the gateway, just to be safe
							if(RecurringSubscriptionCommand.Length != 0 && UseBillingAddress != null && UseBillingAddress.CardNumber != null && UseBillingAddress.CardNumber.Length > 0)
							{
								RecurringSubscriptionCommand = RecurringSubscriptionCommand.Replace(UseBillingAddress.CardNumber, AppLogic.SafeDisplayCardNumber(UseBillingAddress.CardNumber, "Orders", 0));
							}
							if(RecurringSubscriptionResult.Length != 0 && UseBillingAddress != null && UseBillingAddress.CardNumber != null && UseBillingAddress.CardNumber.Length > 0)
							{
								RecurringSubscriptionResult = RecurringSubscriptionResult.Replace(UseBillingAddress.CardNumber, AppLogic.SafeDisplayCardNumber(UseBillingAddress.CardNumber, "Orders", 0));
							}
							if(RecurringSubscriptionSetupStatus == AppLogic.ro_OK)
							{
								DB.ExecuteSQL("update ShoppingCart set RecurringSubscriptionID=" + DB.SQuote(RecurringSubscriptionID) + " where OriginalRecurringOrderNumber=" + OrderNumber.ToString() + " and CartType=" + ((int)CartTypeEnum.RecurringCart).ToString());
								String sqlsub = String.Format("update Orders set RecurringSubscriptionID={0}, RecurringSubscriptionCommand={1}, RecurringSubscriptionResult={2} where OrderNumber={3}", DB.SQuote(RecurringSubscriptionID), DB.SQuote(RecurringSubscriptionCommand), DB.SQuote(RecurringSubscriptionResult), OrderNumber.ToString());
								DB.ExecuteSQL(sqlsub);
							}
							else
							{
								// Card has been processed, Recurring subscription has failed. Admin needs to be notified.
								SysLog.LogMessage(
									String.Format(AppLogic.GetString("Order.RecurringOrderSubscriptionFailed", customer.LocaleSetting), OrderNumber.ToString()),
									RecurringSubscriptionSetupStatus,
									MessageTypeEnum.Informational,
									MessageSeverityEnum.Error);

								//Send admin email, recurring sub needs to be set up manually
								AppLogic.SendMail(
									subject: String.Format(AppLogic.GetString("Order.RecurringOrderSubscriptionFailed", customer.LocaleSetting), OrderNumber.ToString()),
									body: AppLogic.GetString("Order.RecurringOrderSubscriptionNeedsManualCreation", customer.LocaleSetting) + " - " + RecurringSubscriptionSetupStatus,
									useHtml: false);
								RecurringSubscriptionResult = "FAILED\r\n" + RecurringSubscriptionSetupStatus;
								String sqlsub = String.Format("update Orders set RecurringSubscriptionCommand={0}, RecurringSubscriptionResult={1} where OrderNumber={2}", DB.SQuote(RecurringSubscriptionCommand), DB.SQuote(RecurringSubscriptionResult), OrderNumber.ToString());
								DB.ExecuteSQL(sqlsub);
							}
						}
					}
					else
					{
						if(RecurringSubscriptionID.Length != 0 && XID.Length != 0)
						{
							DB.ExecuteSQL("update Orders set AuthorizationPNREF=" + DB.SQuote(XID) + ", ParentOrderNumber=" + cart.OriginalRecurringOrderNumber.ToString() + " where OrderNumber=" + OrderNumber.ToString());
						}
						else
						{
							DB.ExecuteSQL("update Orders set ParentOrderNumber=" + cart.OriginalRecurringOrderNumber.ToString() + " where OrderNumber=" + OrderNumber.ToString());
						}

						// this is a REPEAT recurring order process:
						NextRecurringShipDate = System.DateTime.Now.AddMonths(1); // default just for safety, should never be used, as it should be reset below!

						// don't reset their ship dates to today plus interval, use what "would" have been the proper order date
						// for this order, and then add the interval (in case the store administrator is processing this order early!)
						DateTime ProperNextRecurringShipDateStartsOn = ((CartItem)cart.CartItems[0]).NextRecurringShipDate;
						if(ProperNextRecurringShipDateStartsOn.Equals(System.DateTime.MinValue))
						{
							// safety check:
							ProperNextRecurringShipDateStartsOn = System.DateTime.Now;
						}

						foreach(CartItem c in cart.CartItems)
						{
							switch(c.RecurringIntervalType)
							{
								case DateIntervalTypeEnum.Day:
									NextRecurringShipDate = ProperNextRecurringShipDateStartsOn.AddDays(c.RecurringInterval);
									break;
								case DateIntervalTypeEnum.Week:
									NextRecurringShipDate = ProperNextRecurringShipDateStartsOn.AddDays(7 * c.RecurringInterval);
									break;
								case DateIntervalTypeEnum.Month:
									NextRecurringShipDate = ProperNextRecurringShipDateStartsOn.AddMonths(c.RecurringInterval);
									break;
								case DateIntervalTypeEnum.Year:
									NextRecurringShipDate = ProperNextRecurringShipDateStartsOn.AddYears(c.RecurringInterval);
									break;
								case DateIntervalTypeEnum.NumberOfDays:
									NextRecurringShipDate = ProperNextRecurringShipDateStartsOn.AddDays(c.RecurringInterval);
									break;
								case DateIntervalTypeEnum.Weekly:
									NextRecurringShipDate = ProperNextRecurringShipDateStartsOn.AddDays(7);
									break;
								case DateIntervalTypeEnum.BiWeekly:
									NextRecurringShipDate = ProperNextRecurringShipDateStartsOn.AddDays(14);
									break;
								case DateIntervalTypeEnum.EveryFourWeeks:
									NextRecurringShipDate = ProperNextRecurringShipDateStartsOn.AddDays(28);
									break;
								case DateIntervalTypeEnum.Monthly:
									NextRecurringShipDate = ProperNextRecurringShipDateStartsOn.AddMonths(1);
									break;
								case DateIntervalTypeEnum.Quarterly:
									NextRecurringShipDate = ProperNextRecurringShipDateStartsOn.AddMonths(3);
									break;
								case DateIntervalTypeEnum.SemiYearly:
									NextRecurringShipDate = ProperNextRecurringShipDateStartsOn.AddMonths(6);
									break;
								case DateIntervalTypeEnum.Yearly:
									NextRecurringShipDate = ProperNextRecurringShipDateStartsOn.AddYears(1);
									break;
								default:
									NextRecurringShipDate = ProperNextRecurringShipDateStartsOn.AddMonths(c.RecurringInterval);
									break;
							}
							DB.ExecuteSQL("update ShoppingCart set BillingAddressID=" + UseBillingAddress.AddressID.ToString() + ",RecurringIndex=RecurringIndex+1, NextRecurringShipDate=" + DB.DateQuote(Localization.ToDBShortDateString(NextRecurringShipDate)) + " where originalrecurringordernumber=" + cart.OriginalRecurringOrderNumber.ToString() + " and VariantID=" + c.VariantID.ToString() + " and CartType=" + ((int)CartTypeEnum.RecurringCart).ToString() + " and customerid=" + customer.CustomerID.ToString());
						}
					}
				}

				// clear CouponCode
				if(AppLogic.AppConfigBool("ClearCouponAfterOrdering"))
				{
					DB.ExecuteSQL("update customer set CouponCode=NULL where customerid=" + customer.CustomerID.ToString());
				}

				PromotionManager.RemoveUnusedPromotionsForOrder(OrderNumber);

				//  now we have to update their quantity discount fields in their "order cart", so we have them available for later
				// receipts (e.g. you may delete that quantity discount table tomorrow, but the customer wants to get their receipt again
				// next month, and we would have to reproduce the exact order conditions that they had on order, and we couldn't do that
				// if the discount table has been deleted, unless we store the discount info along with the order)
				if(QuantityDiscount.CustomerLevelAllowsQuantityDiscounts(customer.CustomerLevelID))
				{
					DB.ExecuteStoredProcInt("aspdnsf_updOrderitemQuantityDiscount", new SqlParameter[] { DB.CreateSQLParameter("@OrderNumber", SqlDbType.Int, 4, OrderNumber, ParameterDirection.Input) });
				}

				// now update their CustomerLevel info in the order record, if necessary:
				if(customer.CustomerID != 0)
				{
					using(SqlConnection con = new SqlConnection(DB.GetDBConn()))
					{
						con.Open();
						using(IDataReader rs_l = DB.GetRS("select * from CustomerLevel   with (NOLOCK)  where CustomerLevelID=" + customer.CustomerLevelID.ToString(), con))
						{
							if(rs_l.Read())
							{
								StringBuilder sql_l = new StringBuilder(4096);
								sql_l.Append("update orders set ");
								sql_l.Append("LevelID=" + customer.CustomerLevelID.ToString() + ",");
								sql_l.Append("LevelName=" + DB.SQuote(customer.CustomerLevelName) + ",");
								sql_l.Append("LevelDiscountPercent=" + Localization.DecimalStringForDB(DB.RSFieldDecimal(rs_l, "LevelDiscountPercent")) + ",");
								sql_l.Append("LevelDiscountAmount=" + Localization.CurrencyStringForDBWithoutExchangeRate(DB.RSFieldDecimal(rs_l, "LevelDiscountAmount")) + ",");
								sql_l.Append("LevelHasFreeShipping=" + CommonLogic.IIF(DB.RSFieldBool(rs_l, "LevelHasFreeShipping"), 1, 0).ToString() + ",");
								sql_l.Append("LevelAllowsQuantityDiscounts=" + CommonLogic.IIF(DB.RSFieldBool(rs_l, "LevelAllowsQuantityDiscounts"), 1, 0).ToString() + ",");
								sql_l.Append("LevelHasNoTax=" + CommonLogic.IIF(DB.RSFieldBool(rs_l, "LevelHasNoTax"), 1, 0).ToString() + ",");
								sql_l.Append("LevelAllowsCoupons=" + CommonLogic.IIF(DB.RSFieldBool(rs_l, "LevelAllowsCoupons"), 1, 0).ToString() + ",");
								sql_l.Append("LevelDiscountsApplyToExtendedPrices=" + CommonLogic.IIF(DB.RSFieldBool(rs_l, "LevelDiscountsApplyToExtendedPrices"), 1, 0).ToString() + " ");
								sql_l.Append("where OrderNumber=" + OrderNumber.ToString());
								DB.ExecuteSQL(sql_l.ToString());
							}
						}
					}
				}

				// call-out order packing add-ins
				if(AppLogic.AppConfigBool("AvalaraTax.Enabled"))
				{
					//create tax transaction for the order
					Order order = new Order(OrderNumber);

					//Add Shipping to Order Collection
					foreach(int ShipToId in cart.CartItems.Select(ci => ci.ShippingAddressID).Distinct())
					{
						CartItemCollection ciSingleShipmentCollection = new CartItemCollection();
						ciSingleShipmentCollection.AddRange(cart.CartItems.Where(ci => ci.ShippingAddressID == ShipToId).ToList());

						Address shipToAddress = new Address();
						shipToAddress.LoadFromDB(ShipToId);

						MultiShipOrder_Shipment shipment = new MultiShipOrder_Shipment();
						shipment.DestinationAddress = shipToAddress.AsXml;
						shipment.OrderNumber = OrderNumber;
						shipment.ShippingAmount = Prices.ShippingTotal(true, true, ciSingleShipmentCollection, customer, cart.OrderOptions);
						shipment.ShippingMethodId = ciSingleShipmentCollection[0].ShippingMethodID;
						shipment.ShippingAddressId = ciSingleShipmentCollection[0].ShippingAddressID;
						shipment.BillingAddressId = ciSingleShipmentCollection[0].BillingAddressID;

						shipment.Save();
					}

					try
					{
						// Added try/catch because it blew up the cart if jurisdiction could not be determined but did not prevent the order
						AvaTax avaTax = new AvaTax();
						avaTax.OrderPlaced(order);
					}
					catch(Exception Ex)
					{
						SysLog.LogException(Ex, MessageTypeEnum.GeneralException, MessageSeverityEnum.Error);
					}
				}

				if(setToCapturedState)
				{
					ProcessOrderAsCaptured(OrderNumber);
				}

				AppLogic.eventHandler("NewOrder").CallEvent("&NewOrder=true&OrderNumber=" + OrderNumber.ToString());

				// create the case on signifyd at the bottom of MakeOrder to ensure we are immediately ready for the call back
				if(AppLogic.AppConfigBool("Signifyd.Enabled") && !string.IsNullOrEmpty(signifydCasePayload.Gateway))
					signifydApi.CreateCaseAndGuarantee(
						signifydCasePayload: signifydCasePayload,
						signifydConfiguration: signifydConfiguration);
			}
			#endregion

			return status;
		}

		public static void CreateOrderShoppingCartRecords(int orderNumber, ShoppingCart cart, Customer customer)
		{
			//move the shopping cart records to orders_shoppingcart records.
			//check whether the product has manufacturerpartnumber as well as the variant, if either of them has it then get the manufacturerpartnumber 
			//of the product not the variant

			var cartParams = new[]
			{
				new SqlParameter("@orderNumber", orderNumber),
				new SqlParameter("@originalOrderNumber", cart.OriginalRecurringOrderNumber),
				new SqlParameter("@vatCountryID", AppLogic.AppConfigNativeInt("VAT.CountryID")),
				new SqlParameter("@productFilter", AppLogic.GlobalConfigBool("AllowProductFiltering")),
				new SqlParameter("@cartType", (int)cart.CartType),
				new SqlParameter("@customerID", customer.CustomerID),
				new SqlParameter("@cartFilter", AppLogic.GlobalConfigBool("AllowShoppingcartFiltering")),
				new SqlParameter("@storeID", AppLogic.StoreID())
			};

			var sql = @"
				with ProductStore_CTE
				as
				(
					select distinct 
						sc.ProductID, sc.StoreID from ShoppingCart sc 
					left join ProductStore ps 
						on sc.ProductID = ps.ProductID 
					where 
						@productFilter = 0 
						or ps.StoreID = sc.StoreID
				) 
				
				insert into Orders_ShoppingCart(
					OrderNumber,
					DistributorID,
					CartType,
					ShippingMethodID,
					ShippingMethod,
					Notes,
					ShippingAddressID,
					ExtensionData,
					ShoppingCartRecID,
					CustomerID,
					ProductID,
					VariantID,
					Quantity,
					ChosenColor,
					ChosenColorSKUModifier,
					ChosenSize,
					ChosenSizeSKUModifier,
					TextOption,
					ColorOptionPrompt,
					SizeOptionPrompt,
					TextOptionPrompt,
					CustomerEntersPricePrompt,
					OrderedProductName,
					OrderedProductVariantName,
					OrderedProductSKU,
					OrderedProductManufacturerPartNumber,
					OrderedProductWeight,
					OrderedProductPrice,
					CustomerEntersPrice,
					IsTaxable,
					IsShipSeparately,
					IsDownload,
					FreeShipping,
					IsAKit,
					IsSystem,
					TaxClassId,
					TaxRate,
					IsGift,
					GTIN)
						
				select 
					@orderNumber,
					pd.DistributorID,
					sc.CartType,
					sc.ShippingMethodID,
					sc.ShippingMethod,
					sc.Notes,
					sc.ShippingAddressID,
					sc.ExtensionData,
					sc.ShoppingCartRecID,
					sc.CustomerID,
					sc.ProductID,
					sc.VariantID,
					sc.Quantity,
					sc.ChosenColor,
					sc.ChosenColorSKUModifier,
					sc.ChosenSize,
					sc.ChosenSizeSKUModifier,
					sc.TextOption,
					p.ColorOptionPrompt,
					p.SizeOptionPrompt,
					p.TextOptionPrompt,
					pv.CustomerEntersPricePrompt,
					p.Name,
					pv.Name,
					sc.ProductSKU,
					isnull(p.ManufacturerPartNumber, '') + isnull(pv.ManufacturerPartNumber, '') ManufacturerPartNumber,
					sc.ProductWeight,
					sc.ProductPrice,
					sc.CustomerEntersPrice,
					sc.IsTaxable,
					sc.IsShipSeparately,
					sc.IsDownload,
					sc.FreeShipping,
					sc.IsAKit,
					sc.IsSystem,
					sc.TaxClassId,
					(isnull(cr.taxrate, 0) + isnull(sr.taxrate, 0) + isnull(zr.taxrate, 0)) TaxRate, 
					sc.IsGift, 
					sc.GTIN
				from 
					ShoppingCart sc
					left outer join Product p
						on sc.ProductId = p.ProductId
					left outer join ProductDistributor pd
						on p.ProductID = pd.ProductID
					left join ProductVariant pv 
						on sc.VariantID = pv.VariantID
					left join Address a 
						on sc.ShippingAddressID = a.AddressID
					left join Country c 
						on c.Name = a.Country
					left join State s 
						on s.Abbreviation = a.State 
						and s.CountryID = c.CountryID
					left join CountryTaxRate cr 
						on cr.CountryID = isnull(c.CountryID, @vatCountryID) 
						and cr.TaxClassId = sc.TaxClassId
					left join StateTaxRate sr 
						on sr.StateID = s.StateID 
						and sr.TaxClassId = sc.TaxClassId 
					left join ZipTaxRate zr 
						on zr.ZipCode = a.Zip 
						and zr.TaxClassId = sc.TaxClassId
					inner join ProductStore_CTE
						on sc.ProductID = ProductStore_CTE.ProductID 
						and sc.StoreID = ProductStore_CTE.StoreID
				where 
					sc.Quantity > 0
					and sc.CartType = @cartType 
					and sc.CustomerID = @customerID 
					and (@cartFilter = 0 or sc.StoreID = @storeID) 
					and	(@originalOrderNumber = 0 or sc.OriginalRecurringOrderNumber = @originalOrderNumber)";

			DB.ExecuteSQL(sql, cartParams);
		}

		// cart "could" be null, so be careful!
		static string ProcessECheck(
			ShoppingCart cart,
			int customerId,
			int orderNumber,
			bool useLiveTransactions,
			TransactionModeEnum transactionMode,
			Address billingAddress,
			Address shippingAddress,
			decimal orderTotal,
			out string avsResult,
			out string authorizationResult,
			out string authorizationCode,
			out string authorizationTransId,
			out string transactionCommand,
			out string transactionResponse)
		{
			var gateway = AppLogic.ActivePaymentGatewayCleaned();
			var status = string.Empty;

			avsResult = string.Empty;
			authorizationResult = string.Empty;
			authorizationCode = string.Empty;
			authorizationTransId = string.Empty;
			transactionCommand = string.Empty;
			transactionResponse = string.Empty;

			// dynamically load the gateway processor class via the name
			var processor = GatewayLoader.GetProcessor(gateway);
			if(processor != null)
				status = processor.ProcessECheck(orderNumber, customerId, orderTotal, useLiveTransactions, transactionMode, billingAddress, shippingAddress, out avsResult, out authorizationResult, out authorizationCode, out authorizationTransId, out transactionCommand, out transactionResponse);
			else
				status = $"NO GATEWAY SET OR A ECHECKS NOT IMPLEMENTED FOR THAT GATEWAY (GATEWAY={gateway})";

			//Accept.js transaction values can only be attempted once.  If something fails, the customer needs to re-enter their info
			if(status != AppLogic.ro_OK
				&& gateway == Gateway.ro_GWACCEPTJS
				&& cart != null)
			{
				AcceptJsClearOpaqueData(cart.ThisCustomer.ThisCustomerSession);
			}

			if(status != AppLogic.ro_OK)
				LogFailedTransaction(
					customerId: customerId,
					orderNumber: orderNumber,
					gateway: gateway,
					paymentMethod: AppLogic.ro_PMECheck,
					transactionCommand: transactionCommand,
					transactionResponse: transactionResponse);

			return status;
		}

		// cart could be null, so be careful
		static string ProcessCard(ShoppingCart cart, string gateway, int customerId, int orderNumber, Address billingAddress, string cardExtraCode, Address shippingAddress, decimal orderTotal, bool useLiveTransactions, string cavv, string eci, string xid, TransactionModeEnum transactionMode, out string avsResult, out string authorizationResult, out string authorizationCode, out string authorizationTransID, out string transactionCommand, out string transactionResponse, out string gatewayUsed)
		{
			var GW = DetermineGatwayToUse(gateway);
			gatewayUsed = GW;
			var status = "NO GATEWAY SET, GATEWAY=" + DB.SQuote(GW);

			avsResult = String.Empty;
			authorizationResult = String.Empty;
			authorizationCode = String.Empty;
			authorizationTransID = String.Empty;
			transactionCommand = String.Empty;
			transactionResponse = String.Empty;

			if(cart != null)
			{
				if(!cart.ThisCustomer.IsAdminUser && (AppLogic.ExceedsFailedTransactionsThreshold(cart.ThisCustomer) || AppLogic.IPIsRestricted(cart.ThisCustomer.LastIPAddress)))
				{
					return AppLogic.GetString("gateway.FailedTransactionThresholdExceeded", cart.ThisCustomer.SkinID, cart.ThisCustomer.LocaleSetting);
				}
			}
			if(billingAddress.PaymentMethodLastUsed.StartsWith(AppLogic.ro_PMPayPal, StringComparison.InvariantCultureIgnoreCase))
			{
				status = PayPalController.ProcessPaypal(orderNumber, customerId, orderTotal, useLiveTransactions, AppLogic.ro_TXModeAuthCapture, billingAddress, shippingAddress, cavv, eci, xid, out avsResult, out authorizationResult, out authorizationCode, out authorizationTransID, out transactionCommand, out transactionResponse);
			}
			else
			{
				GatewayTransaction gwtran = new GatewayTransaction(orderNumber, customerId, orderTotal, billingAddress, shippingAddress, cavv, cardExtraCode, eci, xid, transactionMode);

				// Set some variables needed by Sage Pay PI for recurring
				if(cart?.CartType == CartTypeEnum.RecurringCart && GW == Gateway.ro_GWSAGEPAYPI)
				{
					gwtran.TransactionId = GetReferenceTransactionId(cart.OriginalRecurringOrderNumber);
					gwtran.RecurringOrderNumberOriginal = cart.OriginalRecurringOrderNumber;
					gwtran.TransactionType = cart.CartType == CartTypeEnum.RecurringCart
						? "Repeat"
						: string.Empty;
				}

				// run the transaction.
				// all exception handling occurs within the transaction class
				gwtran.Process();

				avsResult = gwtran.AVSResult;
				authorizationResult = gwtran.AuthorizationResult;
				authorizationCode = gwtran.AuthorizationCode;
				authorizationTransID = gwtran.AuthorizationTransactionID;
				transactionCommand = gwtran.TransactionCommand;
				transactionResponse = gwtran.TransactionResponse;
				status = gwtran.Status;
				gatewayUsed = gwtran.GatewayUsed;
			}

			if(status != AppLogic.ro_OK && status != AppLogic.ro_3DSecure)
			{
				// record failed TX:
				try
				{
					String txout = transactionCommand;
					String txresponse = transactionResponse;
					if(billingAddress.CardNumber != null && billingAddress.CardNumber.Length != 0)
					{
						String tmp = AppLogic.SafeDisplayCardNumber(billingAddress.CardNumber, "Address", billingAddress.AddressID);
						if(!string.IsNullOrEmpty(txout) && txout.Length != 0)
						{
							txout = txout.Replace(billingAddress.CardNumber, tmp);
						}
						if(!string.IsNullOrEmpty(txresponse) && txresponse.Length != 0)
						{
							txresponse = txresponse.Replace(billingAddress.CardNumber, tmp);
						}
					}
					if(cardExtraCode != null && cardExtraCode.Length != 0)
					{
						String tmp = AppLogic.SafeDisplayCardExtraCode(cardExtraCode);
						if(!string.IsNullOrEmpty(txout) && txout.Length != 0)
						{
							txout = txout.Replace(cardExtraCode, tmp);
						}
						if(!string.IsNullOrEmpty(txresponse) && txresponse.Length != 0)
						{
							txresponse = txresponse.Replace(cardExtraCode, tmp);
						}
					}
					String IP = "";
					if(cart != null)
					{
						IP = cart.ThisCustomer.LastIPAddress;
					}
					String sql = "insert into FailedTransaction(CustomerID,OrderNumber,IPAddress,OrderDate,PaymentGateway,PaymentMethod,TransactionCommand,TransactionResult) values(" + customerId.ToString() + "," + orderNumber.ToString() + "," + DB.SQuote(IP) + ",getdate()," + DB.SQuote(GW) + "," + DB.SQuote(AppLogic.ro_PMCreditCard) + "," + DB.SQuote(txout) + "," + DB.SQuote(txresponse) + ")";
					DB.ExecuteSQL(sql);
				}
				catch
				{
					throw new Exception(AppLogic.GetString("gateway.ConfigurationIssue", cart.ThisCustomer.LocaleSetting));
				}
			}

			//Accept.js transaction values can only be attempted once.  If something fails, the customer needs to re-enter their info
			if(status != AppLogic.ro_OK
				&& gateway == Gateway.ro_GWACCEPTJS
				&& cart != null)
			{
				AcceptJsClearOpaqueData(cart.ThisCustomer.ThisCustomerSession);
			}

			return status;
		}

		public static string DispatchCapture(String GW, int OrderNumber)
		{
			Order o = new Order(OrderNumber, Localization.GetDefaultLocale());
			String Status = string.Empty;

			if(GW == Gateway.ro_GWAMAZONPAYMENTS)
			{
				// handle AmazonPayments as "pseudo gateway"
				var orderTrackingDetail = new AmazonPaymentsOrderTrackingDetail();
				var processor = new AmazonPaymentsGateway();
				var serializer = new AmazonPaymentsOrderTrackingDetailSerializer();
				var serializedAmazonOrderTrackingDetail = string.Empty;

				using(var conn = new SqlConnection(DB.GetDBConn()))
				{
					conn.Open();
					using(var rs = DB.GetRS("SELECT TransactionCommand FROM Orders(NOLOCK) WHERE OrderNumber=@orderNumber", new[] { new SqlParameter("orderNumber", o.OrderNumber.ToString()) }, conn))
					{
						if(rs.Read())
						{
							try
							{
								orderTrackingDetail = serializer.DeserializeAmazonOrderTrackingDetail(DB.RSField(rs, "TransactionCommand"));
							}
							catch
							{
								return AppLogic.GetString("gateway.amazonpayments.ordertrackingdetailnotfound");
							}
						}
					}
				}

				Status = processor.CaptureOrder(ref orderTrackingDetail, o.OrderNumber, o.CustomerID, o.Total());

				if(Status == AppLogic.ro_OK)
				{
					serializedAmazonOrderTrackingDetail = serializer.SerializeAmazonOrderTrackingDetail(orderTrackingDetail);

					string sql = string.Format("UPDATE Orders SET TransactionCommand={0}, AuthorizationPNREF={1}, AuthorizationResult={2}, AuthorizationCode={3}, VoidTXCommand={4}, VoidTXResult={5}, CaptureTXCommand={6}, CaptureTXResult={7}, RefundTXCommand={8}, RefundTXResult={9} WHERE OrderNumber={10}",
						DB.SQuote(serializedAmazonOrderTrackingDetail), DB.SQuote(orderTrackingDetail.Authorization.Id), DB.SQuote(orderTrackingDetail.Authorization.State), DB.SQuote(orderTrackingDetail.Authorization.ReasonCode), DB.SQuote(orderTrackingDetail.Cancel.Id), DB.SQuote(orderTrackingDetail.Cancel.State), DB.SQuote(orderTrackingDetail.Capture.Id), DB.SQuote(orderTrackingDetail.Capture.State), DB.SQuote(orderTrackingDetail.Refund.Id), DB.SQuote(orderTrackingDetail.Refund.State), o.OrderNumber.ToString());

					DB.ExecuteSQL(sql);
				}
			}
			else
			{
				// dynamically load the gateway processor class via the name
				GatewayProcessor processor = GatewayLoader.GetProcessor(GW);
				if(processor != null)
				{
					Status = processor.CaptureOrder(o);
				}
				else
				{
					Status = "Unknown PaymentGateway in Capture";
				}
			}

			if(Status.ToUpper(CultureInfo.InvariantCulture) == AppLogic.ro_OK)
			{
				Gateway.ProcessOrderAsCaptured(OrderNumber);
			}
			return Status;
		}

		public static String ForceCapture(int OrderNumber)
		{
			// update transaction state
			Gateway.ProcessOrderAsCaptured(OrderNumber);
			return AppLogic.ro_OK;
		}

		public static String ProcessRefund(int CustomerID, int OriginalOrderNumber, int NewOrderNumber, Decimal RefundAmount, String RefundReason, Address UseBillingAddress)
		{
			// get GW for this order, not the generic GW 
			string GW = String.Empty;
			decimal OrderTotal = 0.0M;
			int CouponType = 0;
			string CouponCode = "";
			decimal CouponDiscountAmount = 0.0M;
			string Status = string.Empty;

			using(SqlConnection con = new SqlConnection(DB.GetDBConn()))
			{
				con.Open();
				using(IDataReader rs = DB.GetRS("Select PaymentGateway, PaymentMethod, OrderTotal, OrderTax, CouponType, CouponCode, CouponDiscountAmount from Orders  with (NOLOCK)  where OrderNumber=@orderNumber", new[] { new SqlParameter("orderNumber", OriginalOrderNumber.ToString()) }, con))
				{
					if(rs.Read())
					{
						GW = AppLogic.CleanPaymentGateway(DB.RSField(rs, "PaymentGateway"));

						string PM = AppLogic.CleanPaymentMethod(DB.RSField(rs, "PaymentMethod"));
						if(PM == AppLogic.ro_PMMicropay)
						{
							GW = Gateway.ro_GWMICROPAY;
						}
						else if(PM == AppLogic.ro_PMAmazonPayments)
						{
							GW = Gateway.ro_GWAMAZONPAYMENTS;
						}
						else if(PM == AppLogic.ro_PMPayPalEmbeddedCheckout)
						{
							GW = Gateway.ro_GWPAYFLOWPRO;
						}

						OrderTotal = DB.RSFieldDecimal(rs, "OrderTotal");
						CouponType = DB.RSFieldInt(rs, "CouponType");
						CouponCode = DB.RSField(rs, "CouponCode");
						CouponDiscountAmount = DB.RSFieldDecimal(rs, "CouponDiscountAmount");
					}
				}
			}


			if(GW == "")
			{
				GW = AppLogic.ActivePaymentGatewayCleaned();
				Status = "NO GATEWAY SET, GATEWAY=" + DB.SQuote(GW);
			}


			if(GW == Gateway.ro_GWAMAZONPAYMENTS)
			{
				// handle AmazonPayments as "pseudo gateway"
				var orderTrackingDetail = new AmazonPaymentsOrderTrackingDetail();
				var processor = new AmazonPaymentsGateway();
				var serializer = new AmazonPaymentsOrderTrackingDetailSerializer();
				var serializedAmazonOrderTrackingDetail = string.Empty;

				using(var conn = new SqlConnection(DB.GetDBConn()))
				{
					conn.Open();
					using(var rs = DB.GetRS("SELECT TransactionCommand FROM Orders(NOLOCK) WHERE OrderNumber=" + OriginalOrderNumber.ToString(), conn))
					{
						if(rs.Read())
						{
							try
							{
								orderTrackingDetail = serializer.DeserializeAmazonOrderTrackingDetail(DB.RSField(rs, "TransactionCommand"));
							}
							catch
							{
								return AppLogic.GetString("gateway.amazonpayments.ordertrackingdetailnotfound");
							}
						}
					}
				}

				Status = processor.RefundOrder(ref orderTrackingDetail, OriginalOrderNumber, RefundAmount, RefundReason);

				if(Status == AppLogic.ro_OK)
				{
					serializedAmazonOrderTrackingDetail = serializer.SerializeAmazonOrderTrackingDetail(orderTrackingDetail);

					string sql = string.Format("UPDATE Orders SET TransactionCommand={0}, AuthorizationPNREF={1}, AuthorizationResult={2}, AuthorizationCode={3}, VoidTXCommand={4}, VoidTXResult={5}, CaptureTXCommand={6}, CaptureTXResult={7}, RefundTXCommand={8}, RefundTXResult={9}, AuthorizedOn=GETDATE() WHERE OrderNumber={10}",
						DB.SQuote(serializedAmazonOrderTrackingDetail), DB.SQuote(orderTrackingDetail.Authorization.Id), DB.SQuote(orderTrackingDetail.Authorization.State), DB.SQuote(orderTrackingDetail.Authorization.ReasonCode), DB.SQuote(orderTrackingDetail.Cancel.Id), DB.SQuote(orderTrackingDetail.Cancel.State), DB.SQuote(orderTrackingDetail.Capture.Id), DB.SQuote(orderTrackingDetail.Capture.State), DB.SQuote(orderTrackingDetail.Refund.Id), DB.SQuote(orderTrackingDetail.Refund.State), OriginalOrderNumber.ToString());

					DB.ExecuteSQL(sql);
				}
			}
			else
			{
				// dynamically load the gateway processor class via the name
				GatewayProcessor processor = GatewayLoader.GetProcessor(GW);
				if(processor != null)
				{
					Status = processor.RefundOrder(OriginalOrderNumber, NewOrderNumber, RefundAmount, RefundReason, UseBillingAddress);
				}
				else
				{
					Status = "Unknown PaymentGateway in RefundOrder";
				}
			}

			if(Status.ToUpper(CultureInfo.InvariantCulture) == AppLogic.ro_OK)
			{
				// was this a full refund) {
				if((RefundAmount == System.Decimal.Zero || RefundAmount == OrderTotal))
				{
					// make sure inventory was restored. safe to call repeatedly. proc protects against deducting twice
					DB.ExecuteSQL("aspdnsf_AdjustInventory " + OriginalOrderNumber.ToString() + ",1");

					DecrementMicropayProductsInOrder(OriginalOrderNumber);

					// update transactionstate
					DB.ExecuteSQL("update Orders set RefundReason=" + DB.SQuote(RefundReason) + ", TransactionState=" + DB.SQuote(AppLogic.ro_TXStateRefunded) + ", RefundedOn=getdate(), IsNew=0 where OrderNumber=" + OriginalOrderNumber.ToString());

					//Invalidate GiftCards ordered on this order
					GiftCards GCs = new GiftCards(OriginalOrderNumber, GiftCardCollectionFilterType.OrderNumber);
					foreach(GiftCard gc in GCs)
					{
						gc.GiftCardTransactions.Add(GiftCardUsageTransaction.CreateTransaction(gc.GiftCardID, GiftCardUsageReasons.FundsRemovedByAdmin, 0, 0, gc.Balance, ""));
						gc.UpdateCard(null, null, null, null, 1, null, null, null, null, null, null, null, null, null, null);
					}

					//Restore Amount to coupon used in paying for the order
					if((CouponTypeEnum)CouponType == CouponTypeEnum.GiftCard)
					{
						GiftCard gc = new GiftCard(CouponCode);
						if(gc.GiftCardID != 0)
						{
							gc.GiftCardTransactions.Add(GiftCardUsageTransaction.CreateTransaction(gc.GiftCardID, GiftCardUsageReasons.FundsAddedByAdmin, 0, 0, CouponDiscountAmount, ""));
						}
					}

					if(AppLogic.AppConfigBool("AvalaraTax.Enabled"))
					{
						Address originAddress = new Address
						{
							Address1 = AppLogic.AppConfig("RTShipping.OriginAddress"),
							Address2 = AppLogic.AppConfig("RTShipping.OriginAddress2"),
							City = AppLogic.AppConfig("RTShipping.OriginCity"),
							Country = AppLogic.AppConfig("RTShipping.OriginCountry"),
							State = AppLogic.AppConfig("RTShipping.OriginState"),
							Suite = String.Empty,
							Zip = AppLogic.AppConfig("RTShipping.OriginZip"),

							//pass in the ShippingTaxClassID
							NickName = AppLogic.AppConfigUSInt("ShippingTaxClassID").ToString(),
						};

						AvaTax avaTax = new AvaTax();
						avaTax.IssueRefund(new Order(OriginalOrderNumber), originAddress, 0);
					}
				}
				else
				{
					if(AppLogic.AppConfigBool("AvalaraTax.Enabled"))
					{
						Address originAddress = new Address
						{
							Address1 = AppLogic.AppConfig("RTShipping.OriginAddress"),
							Address2 = AppLogic.AppConfig("RTShipping.OriginAddress2"),
							City = AppLogic.AppConfig("RTShipping.OriginCity"),
							Country = AppLogic.AppConfig("RTShipping.OriginCountry"),
							State = AppLogic.AppConfig("RTShipping.OriginState"),
							Suite = String.Empty,
							Zip = AppLogic.AppConfig("RTShipping.OriginZip"),

							//pass in the ShippingTaxClassID
							NickName = AppLogic.AppConfigUSInt("ShippingTaxClassID").ToString(),
						};

						AvaTax avaTax = new AvaTax();
						avaTax.IssueRefund(new Order(OriginalOrderNumber), originAddress, RefundAmount);
					}
				}
			}


			return Status;
		}

		public static String DispatchVoid(String GW, int OrderNumber)
		{
			int CouponType = 0;
			string CouponCode = "";
			decimal CouponDiscountAmount = 0.0M;

			using(SqlConnection con = new SqlConnection(DB.GetDBConn()))
			{
				con.Open();
				using(IDataReader rs = DB.GetRS("Select PaymentGateway, PaymentMethod, OrderTotal, CouponType, CouponCode, CouponDiscountAmount from Orders  with (NOLOCK)  where OrderNumber=" + OrderNumber.ToString(), con))
				{
					if(rs.Read())
					{
						CouponType = DB.RSFieldInt(rs, "CouponType");
						CouponCode = DB.RSField(rs, "CouponCode");
						CouponDiscountAmount = DB.RSFieldDecimal(rs, "CouponDiscountAmount");
					}
				}
			}

			String Status = String.Empty;

			if(GW == Gateway.ro_GWAMAZONPAYMENTS)
			{
				// handle AmazonPayments as "pseudo gateway"
				var orderTrackingDetail = new AmazonPaymentsOrderTrackingDetail();
				var processor = new AmazonPaymentsGateway();
				var serializer = new AmazonPaymentsOrderTrackingDetailSerializer();
				var serializedAmazonOrderTrackingDetail = string.Empty;

				using(var conn = new SqlConnection(DB.GetDBConn()))
				{
					conn.Open();
					using(var rs = DB.GetRS("SELECT TransactionCommand FROM Orders(NOLOCK) WHERE OrderNumber=@orderNumber", new[] { new SqlParameter("orderNumber", OrderNumber.ToString()) }, conn))
					{
						if(rs.Read())
						{
							try
							{
								orderTrackingDetail = serializer.DeserializeAmazonOrderTrackingDetail(DB.RSField(rs, "TransactionCommand"));
							}
							catch
							{
								return AppLogic.GetString("gateway.amazonpayments.ordertrackingdetailnotfound");
							}
						}
					}
				}

				Status = processor.CancelOrder(ref orderTrackingDetail, OrderNumber);

				if(Status == AppLogic.ro_OK)
				{
					serializedAmazonOrderTrackingDetail = serializer.SerializeAmazonOrderTrackingDetail(orderTrackingDetail);

					string sql = string.Format("UPDATE Orders SET TransactionCommand={0}, AuthorizationPNREF={1}, AuthorizationResult={2}, AuthorizationCode={3}, VoidTXCommand={4}, VoidTXResult={5}, CaptureTXCommand={6}, CaptureTXResult={7}, RefundTXCommand={8}, RefundTXResult={9} WHERE OrderNumber={10}",
						DB.SQuote(serializedAmazonOrderTrackingDetail), DB.SQuote(orderTrackingDetail.Authorization.Id), DB.SQuote(orderTrackingDetail.Authorization.State), DB.SQuote(orderTrackingDetail.Authorization.ReasonCode), DB.SQuote(orderTrackingDetail.Cancel.Id), DB.SQuote(orderTrackingDetail.Cancel.State), DB.SQuote(orderTrackingDetail.Capture.Id), DB.SQuote(orderTrackingDetail.Capture.State), DB.SQuote(orderTrackingDetail.Refund.Id), DB.SQuote(orderTrackingDetail.Refund.State), OrderNumber.ToString());

					DB.ExecuteSQL(sql);
				}
			}
			else
			{
				// dynamically load the gateway processor class via the name
				GatewayProcessor processor = GatewayLoader.GetProcessor(GW);
				if(processor != null)
				{
					Status = processor.VoidOrder(OrderNumber);
				}
				else
				{
					Status = "Unknown PaymentGateway in Void";
				}
			}

			if(Status.ToUpper(CultureInfo.InvariantCulture) == AppLogic.ro_OK)
			{
				AppLogic.eventHandler("OrderVoided").CallEvent("&OrderVoided=true&OrderNumber=" + OrderNumber.ToString());

				// make sure inventory was restored. safe to call repeatedly. proc protects against deducting twice
				DB.ExecuteSQL("aspdnsf_AdjustInventory " + OrderNumber.ToString() + ",1");

				DecrementMicropayProductsInOrder(OrderNumber);

				// update transactionstate
				DB.ExecuteSQL("update Orders set TransactionState=" + DB.SQuote(AppLogic.ro_TXStateVoided) + ", VoidedOn=getdate(), IsNew=0 where ordernumber=" + OrderNumber.ToString());

				//Invalidate GiftCards ordered on this order
				GiftCards GCs = new GiftCards(OrderNumber, GiftCardCollectionFilterType.OrderNumber);
				foreach(GiftCard gc in GCs)
				{
					gc.GiftCardTransactions.Add(GiftCardUsageTransaction.CreateTransaction(gc.GiftCardID, GiftCardUsageReasons.FundsRemovedByAdmin, 0, 0, gc.Balance, ""));
					gc.UpdateCard(null, null, null, null, 1, null, null, null, null, null, null, null, null, null, null);
				}

				//Restore Amount to coupon used in paying for the order
				if((CouponTypeEnum)CouponType == CouponTypeEnum.GiftCard)
				{
					GiftCard gc = new GiftCard(CouponCode);
					if(gc.GiftCardID != 0)
					{
						gc.GiftCardTransactions.Add(GiftCardUsageTransaction.CreateTransaction(gc.GiftCardID, GiftCardUsageReasons.FundsAddedByAdmin, 0, 0, CouponDiscountAmount, ""));
					}
				}
			}
			return Status;
		}

		public static void DecrementMicropayProductsInOrder(int OrderNumber)
		{
			Order ord = new Order(OrderNumber, Localization.GetDefaultLocale());
			//Update (subtract back from) the customer's Micropay balances if any was purchased as part of this order
			if(ord.TransactionIsCaptured())
			{
				int MicropayProductID = AppLogic.GetMicroPayProductID();
				int MicropayVariantID = AppLogic.GetProductsDefaultVariantID(MicropayProductID);
				decimal mpTotal = AppLogic.GetMicroPayBalance(ord.CustomerID);

				//Use the raw price for the amount because 
				// it may be discounted or on sale in the order
				decimal amount = AppLogic.GetVariantPrice(MicropayVariantID);
				foreach(CartItem c in ord.CartItems)
				{
					if(c.ProductID == MicropayProductID)
					{
						mpTotal -= (amount * c.Quantity);
					}
				}
				if(mpTotal < System.Decimal.Zero)
				{
					mpTotal = System.Decimal.Zero;
				}
				DB.ExecuteSQL(String.Format("update Customer set MicroPayBalance={0} where CustomerID={1}", Localization.CurrencyStringForDBWithoutExchangeRate(mpTotal), ord.CustomerID));
			}
		}

		public static string StartExpressCheckout(ShoppingCart cart, bool boolBypassOrderReview = false, IDictionary<string, string> checkoutOptions = null)
		{
			checkoutOptions = checkoutOptions ?? new Dictionary<string, string>();

			switch(PayPalController.GetAppropriateExpressType())
			{
				case ExpressAPIType.PayFlowPro:
					return PayFlowProController.StartEC(cart, boolBypassOrderReview, checkoutOptions);
				case ExpressAPIType.PayPalExpress:
				case ExpressAPIType.PayPalAcceleratedBording:
				case ExpressAPIType.NoValidAPIType:
				default:
					return PayPalController.StartEC(cart, boolBypassOrderReview, checkoutOptions);
			}
		}

		public static String GetExpressCheckoutDetails(String PayPalToken, int CustomerID)
		{
			switch(PayPalController.GetAppropriateExpressType())
			{
				case ExpressAPIType.PayFlowPro:
					return PayFlowProController.GetECDetails(PayPalToken, CustomerID);
				case ExpressAPIType.PayPalExpress:
				case ExpressAPIType.PayPalAcceleratedBording:
				case ExpressAPIType.NoValidAPIType:
				default:
					return PayPalController.GetECDetails(PayPalToken, CustomerID);
			}
		}

		public static String ProcessExpressCheckout(ShoppingCart cart, decimal OrderTotal, int OrderNumber, String PayPalToken, String PayerID, String TransactionMode, out String AuthorizationResult, out String AuthorizationTransID, out String Gateway)
		{
			switch(PayPalController.GetAppropriateExpressType())
			{
				case ExpressAPIType.PayFlowPro:
					Gateway = ro_GWPAYFLOWPRO;
					return PayFlowProController.ProcessEC(cart, OrderTotal, OrderNumber, PayPalToken, PayerID, TransactionMode, out AuthorizationResult, out AuthorizationTransID);
				case ExpressAPIType.PayPalExpress:
				case ExpressAPIType.PayPalAcceleratedBording:
				case ExpressAPIType.NoValidAPIType:
				default:
					Gateway = ro_GWPAYPAL;
					return PayPalController.ProcessEC(cart, OrderTotal, OrderNumber, PayPalToken, PayerID, TransactionMode, out AuthorizationResult, out AuthorizationTransID);
			}
		}

		public static String MakeExpressCheckoutRecurringProfile(ShoppingCart cart, int orderNumber, String payPalToken, String payerID, DateTime nextRecurringShipDate)
		{
			switch(PayPalController.GetAppropriateExpressType())
			{
				case ExpressAPIType.PayPalExpress:
				case ExpressAPIType.PayPalAcceleratedBording:
				case ExpressAPIType.NoValidAPIType:
				default:
					return PayPalController.MakeECRecurringProfile(cart, orderNumber, payPalToken, payerID, nextRecurringShipDate);
			}
		}


		// ----------------------------------------------------------------------------------------------------------------------------------------
		// the following routines are master order mgmt/state transition routines, located here to centralize the code & logic. These routines used
		// to be in orderframe.aspx.cs and other places. They are centralized here now so that the WSI can also use the same logic for
		// order management processing requests.
		// NOTE: the Order object in memory is NOT updated after the call, only the master db records/tables are updated! if you need current
		// in memory Order object status, you should load a new one after the call, if successful
		// ----------------------------------------------------------------------------------------------------------------------------------------

		// returns AppLogic.ro_OK on success, otherwise error description. on error, order state is unchanged.
		public static string OrderManagement_DoVoid(Order order)
		{
			return OrderManagement_DoVoid(order, false);
		}

		public static string OrderManagement_DoVoid(Order order, bool force)
		{
			var status = AppLogic.ro_OK;
			if(order.OrderNumber == 0 || order.IsEmpty)
				return "Order Not Found";

			if(order.VoidedOn == System.DateTime.MinValue)
			{
				var paymentMethod = AppLogic.CleanPaymentMethod(order.PaymentMethod);
				var gateway = AppLogic.CleanPaymentGateway(order.PaymentGateway);

				if(force)
				{
					status = ForceVoidStatus(order.OrderNumber, AppLogic.ro_TXStateForceVoided);
				}
				else if(paymentMethod == AppLogic.ro_PMCreditCard
					|| paymentMethod == AppLogic.ro_PMMicropay
					|| paymentMethod == AppLogic.ro_PMPayPalExpress
					|| paymentMethod == AppLogic.ro_PMCheckByMail
					|| paymentMethod == AppLogic.ro_PMAmazonPayments
					|| paymentMethod == AppLogic.ro_PMPayPalEmbeddedCheckout)
				{
					if(paymentMethod == AppLogic.ro_PMMicropay)
					{
						gateway = ro_GWMICROPAY;
					}
					else if(paymentMethod == AppLogic.ro_PMAmazonPayments)
					{
						gateway = ro_GWAMAZONPAYMENTS;
					}
					else if(paymentMethod == AppLogic.ro_PMPayPalEmbeddedCheckout)
					{
						gateway = ro_GWPAYFLOWPRO;
					}

					if(paymentMethod == AppLogic.ro_PMCheckByMail)
					{
						// internal payment methods do not get dispatched, but are forced
						status = ForceVoidStatus(order.OrderNumber);
					}
					else
					{
						status = DispatchVoid(gateway, order.OrderNumber);
					}

					if(status == AppLogic.ro_OK)
					{
						try
						{
							var customer = new Customer(order.CustomerID);
							AppLogic.SendMail(
								subject: AppLogic.GetString("Order.OrderWasVoided",
									customer.SkinID,
									customer.LocaleSetting),
								body: AppLogic.RunXmlPackage(
									AppLogic.AppConfig("XmlPackage.OrderCanceledEmail"),
									null,
									null,
									1,
									string.Empty,
									"ordernumber=" + order.OrderNumber.ToString(),
									false,
									false),
								useHtml: true,
								fromAddress: AppLogic.AppConfig("MailMe_FromAddress"),
								fromName: AppLogic.AppConfig("MailMe_FromName"),
								toAddress: customer.EMail,
								toName: customer.FullName(),
								bccAddresses: string.Empty,
								server: AppLogic.MailServer());
						}
						catch(Exception exception)
						{
							SysLog.LogException(exception, MessageTypeEnum.GeneralException, MessageSeverityEnum.Error);
						}
					}
				}
				else
				{
					status = string.Format("Void not supported for the {0} payment method!", order.PaymentMethod);
				}
			}
			else
			{
				status = string.Format("The payment for this order was already voided on {0}.", Localization.ToNativeDateTimeString(order.VoidedOn));
			}

			if(AppLogic.AppConfigBool("AvalaraTax.Enabled"))
			{
				var avaTax = new AvaTax();
				avaTax.VoidTax(order);
			}

			return status;
		}

		public static string OrderManagement_UpdateTransaction(Order order)
		{
			var status = AppLogic.ro_OK;
			if(order.OrderNumber == 0 || order.IsEmpty)
				return "Order Not Found";

			return status;
		}


		// returns AppLogic.ro_OK on success, otherwise error description. on error, order state is unchanged.
		public static string OrderManagement_DoCapture(Order order)
		{
			var status = AppLogic.ro_OK;
			if(order.OrderNumber == 0 || order.IsEmpty)
				return "Order Not Found";

			var paymentMethod = AppLogic.CleanPaymentMethod(order.PaymentMethod);

			if(order.CapturedOn == System.DateTime.MinValue)
			{
				if(order.TransactionState == AppLogic.ro_TXStateAuthorized || order.TransactionState == AppLogic.ro_TXStatePending)
				{
					status = AppLogic.ro_OK; // will be ok for all non credit card orders

					var gateway = AppLogic.CleanPaymentGateway(order.PaymentGateway);

					if(paymentMethod == AppLogic.ro_PMCreditCard
						|| paymentMethod == AppLogic.ro_PMMicropay
						|| paymentMethod == AppLogic.ro_PMPayPalExpress
						|| paymentMethod == AppLogic.ro_PMAmazonPayments
						|| paymentMethod == AppLogic.ro_PMPayPalEmbeddedCheckout)
					{
						if(paymentMethod == AppLogic.ro_PMMicropay)
						{
							gateway = ro_GWMICROPAY;
						}
						else if(paymentMethod == AppLogic.ro_PMAmazonPayments)
						{
							gateway = ro_GWAMAZONPAYMENTS;
						}
						else if(paymentMethod == AppLogic.ro_PMPayPalEmbeddedCheckout)
						{
							gateway = ro_GWPAYFLOWPRO;
						}
						status = DispatchCapture(gateway, order.OrderNumber);
					}
					else
					{
						status = ForceCapture(order.OrderNumber);
					}
				}
				else
				{
					status = string.Format("The transaction state ({0}) is not AUTH.", order.TransactionState);
				}
			}
			else
			{
				status = string.Format("The payment for this order was already captured on {0}.", Localization.ToNativeDateTimeString(order.CapturedOn));
			}

			//Low inventory notification
			if(AppLogic.AppConfigBool("SendLowStockWarnings") && status == AppLogic.ro_OK)
			{
				var purchasedVariants = new List<int>();
				foreach(CartItem ci in order.CartItems)
				{
					purchasedVariants.Add(ci.VariantID);
				}

				AppLogic.LowInventoryWarning(purchasedVariants);
			}

			return status;
		}

		static public string OrderManagement_DoFullRefund(Order order, string refundReason)
		{
			var status = AppLogic.ro_OK;
			if(order.OrderNumber == 0 || order.IsEmpty)
				return "Order Not Found";

			string paymentMethod = AppLogic.CleanPaymentMethod(order.PaymentMethod);
			if(order.RefundedOn == System.DateTime.MinValue)
			{
				if(order.CapturedOn != System.DateTime.MinValue)
				{
					if(order.TransactionState == AppLogic.ro_TXStateCaptured)
					{
						if(paymentMethod == AppLogic.ro_PMCreditCard
							|| paymentMethod == AppLogic.ro_PMMicropay
							|| paymentMethod == AppLogic.ro_PMPayPalExpress
							|| paymentMethod == AppLogic.ro_PMAmazonPayments
							|| paymentMethod == AppLogic.ro_PMPayPalEmbeddedCheckout)
						{
							status = ProcessRefund(order.CustomerID, order.OrderNumber, 0, order.Total(), refundReason, null);
						}
						else
						{
							status = ForceRefundStatus(order.OrderNumber);
						}
						if(status == AppLogic.ro_OK)
						{
							try
							{
								var customer = new Customer(order.CustomerID, true);
								AppLogic.SendMail(
									subject: "Order Was Refunded",
									body: AppLogic.RunXmlPackage(
										AppLogic.AppConfig("XmlPackage.RefundEmail"),
										null,
										customer,
										1,
										string.Empty,
										"ordernumber=" + order.OrderNumber.ToString(),
										false,
										false),
									useHtml: true,
									fromAddress: AppLogic.AppConfig("MailMe_FromAddress"),
									fromName: AppLogic.AppConfig("MailMe_FromName"),
									toAddress: customer.EMail,
									toName: customer.FullName(),
									bccAddresses: string.Empty,
									server: AppLogic.MailServer());
							}
							catch(Exception exception)
							{
								SysLog.LogException(exception, MessageTypeEnum.GeneralException, MessageSeverityEnum.Error);
							}
						}
					}
					else
					{
						status = string.Format(
							"This transaction has not yet been Captured. Use Void if required. The transaction state ({0}) is not {1}.",
							order.TransactionState,
							AppLogic.ro_TXModeAuthCapture);
					}
				}
				else
				{
					status = "The payment for this order has not yet been cleared.";
				}
			}
			else
			{
				status = string.Format("This transaction has already been refunded on {0}!", Localization.ToNativeDateTimeString(order.RefundedOn));
			}

			return status;
		}

		static public string OrderManagement_DoForceFullRefund(Order order)
		{
			var status = AppLogic.ro_OK;
			if(order.OrderNumber == 0 || order.IsEmpty)
				return "Order Not Found";

			status = ForceRefundStatus(order.OrderNumber);

			if(AppLogic.AppConfigBool("AvalaraTax.Enabled"))
			{
				var originAddress = new Address
				{
					Address1 = AppLogic.AppConfig("RTShipping.OriginAddress"),
					Address2 = AppLogic.AppConfig("RTShipping.OriginAddress2"),
					City = AppLogic.AppConfig("RTShipping.OriginCity"),
					Country = AppLogic.AppConfig("RTShipping.OriginCountry"),
					State = AppLogic.AppConfig("RTShipping.OriginState"),
					Suite = String.Empty,
					Zip = AppLogic.AppConfig("RTShipping.OriginZip"),

					//pass in the ShippingTaxClassID
					NickName = AppLogic.AppConfigUSInt("ShippingTaxClassID").ToString(),
				};

				var avaTax = new AvaTax();
				avaTax.IssueRefund(order, originAddress, 0);
			}

			return status;
		}

		static public String OrderManagement_MarkAsFraud(Order ord, String ViewInLocaleSetting)
		{
			String Status = AppLogic.ro_OK;
			if(ord.OrderNumber == 0 || ord.IsEmpty)
			{
				Status = String.Format("Order Not Found");
			}
			else
			{
				try
				{
					// ignore duplicates:
					Customer c = new Customer(ord.CustomerID, true);
					if(c.LastIPAddress.Length != 0)
					{
						DB.ExecuteSQL("insert RestrictedIP(IPAddress) values(" + DB.SQuote(c.LastIPAddress) + ")");
					}
				}
				catch { }
				Order.MarkOrderAsFraud(ord.OrderNumber, true);

				if(ord.TransactionIsCaptured())
				{
					if(AppLogic.AppConfigBool("AvalaraTax.Enabled"))
					{
						Address originAddress = new Address
						{
							Address1 = AppLogic.AppConfig("RTShipping.OriginAddress"),
							Address2 = AppLogic.AppConfig("RTShipping.OriginAddress2"),
							City = AppLogic.AppConfig("RTShipping.OriginCity"),
							Country = AppLogic.AppConfig("RTShipping.OriginCountry"),
							State = AppLogic.AppConfig("RTShipping.OriginState"),
							Suite = String.Empty,
							Zip = AppLogic.AppConfig("RTShipping.OriginZip"),

							//pass in the ShippingTaxClassID
							NickName = AppLogic.AppConfigUSInt("ShippingTaxClassID").ToString(),
						};

						AvaTax avaTax = new AvaTax();
						avaTax.IssueRefund(ord, originAddress, 0);
					}
				}
				else
				{
					if(AppLogic.AppConfigBool("AvalaraTax.Enabled"))
					{
						AvaTax avaTax = new AvaTax();
						avaTax.VoidTax(ord);
					}
				}
			}

			return Status;
		}

		static public String OrderManagement_ClearFraud(Order ord, String ViewInLocaleSetting)
		{
			String Status = AppLogic.ro_OK;
			if(ord.OrderNumber == 0 || ord.IsEmpty)
			{
				Status = String.Format("Order Not Found");
			}
			else
			{
				Customer c = new Customer(ord.CustomerID, true);
				if(c.LastIPAddress.Length != 0)
				{
					DB.ExecuteSQL("delete from RestrictedIP where IPAddress=" + DB.SQuote(c.LastIPAddress));
				}
				Order.MarkOrderAsFraud(ord.OrderNumber, false);
			}
			return Status;
		}

		static public String OrderManagement_ClearNewStatus(Order ord, String ViewInLocaleSetting)
		{
			String Status = AppLogic.ro_OK;
			if(ord.OrderNumber == 0 || ord.IsEmpty)
			{
				Status = String.Format("Order Not Found");
			}
			else
			{
				DB.ExecuteSQL("Update orders set IsNew=0 where OrderNumber=" + ord.OrderNumber.ToString());
			}
			return Status;
		}

		static public String OrderManagement_BlockIP(Order ord, String ViewInLocaleSetting)
		{
			String Status = AppLogic.ro_OK;
			if(ord.OrderNumber == 0 || ord.IsEmpty)
			{
				Status = String.Format("Order Not Found");
			}
			else
			{
				try
				{
					if(ord.LastIPAddress.Length != 0)
					{
						// ignore duplicates:
						DB.ExecuteSQL("insert RestrictedIP(IPAddress) values(" + DB.SQuote(ord.LastIPAddress) + ")");
					}
				}
				catch { }
			}
			return Status;
		}

		static public String OrderManagement_AllowIP(Order ord, String ViewInLocaleSetting)
		{
			String Status = AppLogic.ro_OK;
			if(ord.OrderNumber == 0 || ord.IsEmpty)
			{
				Status = String.Format("Order Not Found");
			}
			else
			{
				if(ord.LastIPAddress.Length != 0)
				{
					DB.ExecuteSQL("delete from RestrictedIP where IPAddress=" + DB.SQuote(ord.LastIPAddress));
				}
			}
			return Status;
		}

		static public string OrderManagement_SendDistributorNotification(Order order, bool overrideDelay)
		{
			var status = AppLogic.ro_OK;

			if(order.OrderNumber == 0 || order.IsEmpty)
				return "Order Not Found";

			if(order.HasDistributorComponents())
				order.SendDistributorNotifications(overrideDelay);
			else
				status = "<p><b>NO DISTRIBUTOR ITEMS. DISTRIBUTOR E-MAIL(S) NOT SENT.</b></p>";

			if(order.IsAllDownloadComponents() && order.isAllDistributorComponents())
				Order.MarkOrderAsShipped(order.OrderNumber, "DISTRIBUTOR", string.Empty, DateTime.Now, false, true);

			return status;
		}

		static public String OrderManagement_ChangeOrderEMail(Order ord, String ViewInLocaleSetting, String NewEMail)
		{
			String Status = AppLogic.ro_OK;
			if(ord.OrderNumber == 0 || ord.IsEmpty)
			{
				Status = String.Format("Order Not Found");
			}
			else
			{
				if(NewEMail.Length != 0)
				{
					DB.ExecuteSQL("update Orders set EMail=" + DB.SQuote(NewEMail) + " where OrderNumber=" + ord.OrderNumber.ToString());

					// now, try to reassign the order to the customer who owns that e-mail address, IF and ONLY IF that e-mail address
					// is mapped to ONLY ONE customer record:
					if(DB.GetSqlN("select count(*) as N from Customer  with (NOLOCK)  where EMail=" + DB.SQuote(NewEMail) + " and Deleted=0") == 1)
					{
						// ok, we have one exact customer match, use it:
						int CustomerID = 0;

						using(SqlConnection con = new SqlConnection(DB.GetDBConn()))
						{
							con.Open();
							using(IDataReader rsCustomer = DB.GetRS("select CustomerID from Customer  with (NOLOCK)  where EMail=" + DB.SQuote(NewEMail) + " and Deleted=0", con))
							{
								if(rsCustomer.Read())
								{
									CustomerID = DB.RSFieldInt(rsCustomer, "CustomerID");
								}
							}
						}

						if(CustomerID != 0)
						{
							DB.ExecuteSQL("update Orders set CustomerID=" + CustomerID.ToString() + " where OrderNumber=" + ord.OrderNumber.ToString());
							DB.ExecuteSQL("update Orders_ShoppingCart set CustomerID=" + CustomerID.ToString() + " where OrderNumber=" + ord.OrderNumber.ToString());
							DB.ExecuteSQL("update Orders_KitCart set CustomerID=" + CustomerID.ToString() + " where OrderNumber=" + ord.OrderNumber.ToString());
						}
					}
					else
					{
						Status = AppLogic.GetString("admin.order.ChangeEmailError", ViewInLocaleSetting);
					}
				}
			}
			return Status;
		}

		static public String OrderManagement_MarkAsReadyToShip(Order ord, String ViewInLocaleSetting)
		{
			String Status = AppLogic.ro_OK;
			if(ord.OrderNumber == 0 || ord.IsEmpty)
			{
				Status = String.Format("Order Not Found");
			}
			else
			{
				DB.ExecuteSQL("update Orders set ReadyToShip=1 where OrderNumber=" + ord.OrderNumber.ToString());
			}
			return Status;
		}

		static public String OrderManagement_ClearReadyToShip(Order ord, String ViewInLocaleSetting)
		{
			String Status = AppLogic.ro_OK;
			if(ord.OrderNumber == 0 || ord.IsEmpty)
			{
				Status = String.Format("Order Not Found");
			}
			else
			{
				DB.ExecuteSQL("update Orders set ReadyToShip=0 where OrderNumber=" + ord.OrderNumber.ToString());
			}
			return Status;
		}

		static public string OrderManagement_MarkAsShipped(Order order, string shippedVia, string trackingNumber, DateTime shippedOn)
		{
			if(order.OrderNumber == 0 || order.IsEmpty)
				return "Order Not Found";

			order.DeductInventory();
			Order.MarkOrderAsShipped(
				orderNumber: order.OrderNumber,
				shippedVia: shippedVia,
				shippingTrackingNumber: trackingNumber,
				shippedOn: shippedOn,
				isRecurring: false,
				disableEmail: false);

			return AppLogic.ro_OK;
		}

		static public String OrderManagement_SetPrivateNotes(Order ord, String ViewInLocaleSetting, String Notes)
		{
			String Status = AppLogic.ro_OK;
			if(ord.OrderNumber == 0 || ord.IsEmpty)
			{
				Status = String.Format("Order Not Found");
			}
			else
			{
				DB.ExecuteSQL("Update orders set Notes=" + DB.SQuote(Notes) + " where OrderNumber=" + ord.OrderNumber.ToString());
			}
			return Status;
		}

		static public String OrderManagement_SetCustomerServiceNotes(Order ord, String ViewInLocaleSetting, String Notes)
		{
			String Status = AppLogic.ro_OK;
			if(ord.OrderNumber == 0 || ord.IsEmpty)
			{
				Status = String.Format("Order Not Found");
			}
			else
			{
				DB.ExecuteSQL("Update orders set CustomerServiceNotes=" + DB.SQuote(Notes) + " where OrderNumber=" + ord.OrderNumber.ToString());
			}
			return Status;
		}

		static public String OrderManagement_SetOrderWeight(Order ord, String ViewInLocaleSetting, Decimal NewWeight)
		{
			String Status = AppLogic.ro_OK;
			if(ord.OrderNumber == 0 || ord.IsEmpty)
			{
				Status = String.Format("Order Not Found");
			}
			else
			{
				DB.ExecuteSQL("update Orders set OrderWeight=" + Localization.DecimalStringForDB(NewWeight) + " where OrderNumber=" + ord.OrderNumber.ToString());
			}
			return Status;
		}

		static public string OrderManagement_SendToFedexShippingMgr(Order order)
		{
			if(order.OrderNumber == 0 || order.IsEmpty)
				return "Order Not Found";

			order.DeductInventory();

			//Clear out any old job if this is a re-send
			DB.ExecuteSQL("DELETE FROM ShippingImportExport WHERE OrderNumber = @orderNumber", new SqlParameter("@orderNumber", order.OrderNumber));

			var fedexSql = @"INSERT INTO ShippingImportExport(OrderNumber,
				CustomerID,
				CompanyName,
				CustomerLastName,
				CustomerFirstName,
				CustomerEmail,
				CustomerPhone,
				Address1,
				Address2,
				City,
				State,
				Zip,
				Country,
				ServiceCarrierCode,
				Cost,
				Weight) 
				VALUES(@orderNumber,
				@customerId,
				@company
				@lastName
				@firstName
				@email
				@phone
				@address1
				@address2
				@city
				@state
				@zip
				@country
				@carrierCode
				@cost
				@weight)";

			var shippingMethod = order.ShippingMethod;
			if(shippingMethod.IndexOf("|") != -1)
				shippingMethod = shippingMethod.Substring(0, shippingMethod.IndexOf("|"));

			var fedexParams = new SqlParameter[]
			{
					new SqlParameter("@orderNumber", order.OrderNumber),
					new SqlParameter("@customerId", order.CustomerID),
					new SqlParameter("@company", order.ShippingAddress.m_Company.Substring(0, 50)),
					new SqlParameter("@lastName", string.Format("{0} {1}", order.ShippingAddress.m_FirstName, order.ShippingAddress.m_LastName).Substring(0, 50)),
					new SqlParameter("@firstName", "Combined in last name per FedEx"),
					new SqlParameter("@email", order.ShippingAddress.m_EMail.Substring(0, 50)),
					new SqlParameter("@phone", order.ShippingAddress.m_Phone.Substring(0, 50)),
					new SqlParameter("@address1", order.ShippingAddress.m_Address1.Substring(0, 100)),
					new SqlParameter("@address2", order.ShippingAddress.m_Address2.Substring(0, 100)),
					new SqlParameter("@city", order.ShippingAddress.m_City.Substring(0, 100)),
					new SqlParameter("@state", order.ShippingAddress.m_State.Substring(0, 100)),
					new SqlParameter("@zip", order.ShippingAddress.m_Zip.Substring(0, 10)),
					new SqlParameter("@country", order.ShippingAddress.m_Country.Substring(0, 100)),
					new SqlParameter("@carrierCode", shippingMethod.Substring(0, 50)),
					new SqlParameter("@cost", order.ShippingTotal()),
					new SqlParameter("@weight", order.OrderWeight)
			};

			DB.ExecuteSQL(fedexSql, fedexParams);

			var orderSql = @"UPDATE Orders 
								SET ReadyToShip = 1, 
									IsNew = 0, 
									ShippedVIA = @shippingMethod, 
									ShippingTrackingNumber = 'Pending From FedEx ShipManager', 
									ShippedOn = GETDATE() 
								WHERE OrderNumber = @orderNumber";

			var orderParams = new SqlParameter[]
			{
					new SqlParameter("@shippingMethod", order.ShippingMethod),
					new SqlParameter("@orderNumber", order.OrderNumber)
			};

			DB.ExecuteSQL(orderSql, orderParams);

			return AppLogic.ro_OK;
		}

		static public string OrderManagement_MarkAsPrinted(Order order)
		{
			if(order.OrderNumber == 0 || order.IsEmpty)
				return "Order Not Found";

			DB.ExecuteSQL("UPDATE Orders SET IsPrinted = 1 WHERE OrderNumber = @orderNumber", new SqlParameter("@orderNumber", order.OrderNumber));

			return AppLogic.ro_OK;
		}

		static public string OrderManagement_SendReceipt(Order ord, string ViewInLocaleSetting)
		{
			string status = AppLogic.ro_OK;

			if(ord.OrderNumber == 0 || ord.IsEmpty)
			{
				status = string.Format("Order Not Found");
			}
			else
			{
				var customer = new Customer(ord.CustomerID, true);
				var orderStoreId = Order.GetOrderStoreID(ord.OrderNumber);
				var mailServer = AppLogic.AppConfig("MailMe_Server", orderStoreId, true);

				if(!string.IsNullOrEmpty(ord.EMail) &&
					!string.IsNullOrEmpty(mailServer) &&
					mailServer != AppLogic.ro_TBD)
				{
					var subjectLine = new StringBuilder();

					if(AppLogic.AppConfigBool("UseLiveTransactions", orderStoreId, true))
						subjectLine.AppendFormat("{0} {1}", AppLogic.AppConfig("StoreName", orderStoreId, true), AppLogic.GetString("admin.common.Receipt", ord.SkinID, ord.LocaleSetting));
					else
						subjectLine.AppendFormat("{0} {1}", AppLogic.AppConfig("StoreName", orderStoreId, true), string.Format(AppLogic.GetString("common.cs.2", ord.SkinID, ord.LocaleSetting), string.Empty));

					if(ord.PaymentMethod.Equals("REQUEST QUOTE", StringComparison.InvariantCultureIgnoreCase))
						subjectLine.AppendFormat(" {0}", AppLogic.GetString("order.cs.18", ord.SkinID, ord.LocaleSetting));

					AppLogic.SendMail(subject: subjectLine.ToString(),
						body: ord.Receipt(customer, true) + AppLogic.AppConfig("MailFooter", orderStoreId, true),
						useHtml: true,
						fromAddress: AppLogic.AppConfig("ReceiptEMailFrom", orderStoreId, true),
						fromName: AppLogic.AppConfig("ReceiptEMailFromName", orderStoreId, true),
						toAddress: ord.EMail,
						toName: ord.EMail,
						bccAddresses: string.Empty,
						replyToAddress: AppLogic.AppConfig("ReceiptEMailFrom", orderStoreId, true));

					DB.ExecuteSQL("UPDATE Orders SET ReceiptEMailSentOn = GETDATE() WHERE OrderNumber = @orderNumber", new SqlParameter("@orderNumber", ord.OrderNumber));
				}
				else
				{
					status = "NO MAIL SERVER INFO OR NO CUSTOMER E-MAIL ADDRESS FOUND. RECEIPT E-MAIL NOT SENT";
				}
			}
			return status;
		}

		static public String OrderManagement_SetTracking(Order ord, String ViewInLocaleSetting, String ShippedVIA, String TrackingNumber)
		{
			String Status = AppLogic.ro_OK;
			if(ord.OrderNumber == 0 || ord.IsEmpty)
			{
				Status = String.Format("Order Not Found");
			}
			else
			{
				DB.ExecuteSQL("Update orders set ShippedVIA=" + DB.SQuote(ShippedVIA) + ", ShippingTrackingNumber=" + DB.SQuote(TrackingNumber) + " where OrderNumber=" + ord.OrderNumber.ToString());
			}
			return Status;
		}

		static string GetReferenceTransactionId(int originalOrderNumber)
		{
			return DB.GetSqlS(
				@"select AuthorizationPNREF as S from Orders with (NOLOCK) where OrderNumber = @orderNumber",
				new SqlParameter("orderNumber", originalOrderNumber));
		}

		static public string ListFormCollectionKeyValuePairs(FormCollection collection)
		{
			var collectionValues = new StringBuilder();
			collectionValues.AppendLine("Posted Form Data");
			foreach(var key in collection.AllKeys)
			{
				var value = collection[key];
				collectionValues.AppendLine($"{key} | {collection[key]}");
			}
			return collectionValues.ToString();
		}

		static void AcceptJsClearOpaqueData(CustomerSession customerSession)
		{
			customerSession.ClearVal(AppLogic.AcceptJsDataDescriptor);
			customerSession.ClearVal(AppLogic.AcceptJsDataValue);
		}

		static void LogFailedTransaction(int customerId, int orderNumber, string gateway, string paymentMethod, string transactionCommand, string transactionResponse)
		{
			// For customer IP Address
			var customer = new Customer(customerId);

			DB.ExecuteSQL(
				@"insert into FailedTransaction(
					CustomerID,
					OrderNumber,
					IPAddress,
					OrderDate,
					PaymentGateway,
					PaymentMethod,
					TransactionCommand,
					TransactionResult,
					CustomerEMailed
					)
				values(
					@CustomerID,
					@OrderNumber,
					@IPAddress,
					getdate(),
					@PaymentGateway,
					@PaymentMethod,
					@TransactionCommand,
					@TransactionResult,
					0)",
				new SqlParameter("@CustomerID", customerId),
				new SqlParameter("@OrderNumber", orderNumber),
				new SqlParameter("@IPAddress", customer.LastIPAddress),
				new SqlParameter("@PaymentGateway", gateway),
				new SqlParameter("@PaymentMethod", paymentMethod),
				new SqlParameter("@TransactionCommand", transactionCommand),
				new SqlParameter("@TransactionResult", transactionResponse));
		}

	}
}
