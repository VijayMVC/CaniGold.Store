// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.Profile;
using System.Web.Security;
using AspDotNetStorefront.Models;
using AspDotNetStorefront.Routing;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Classes
{
	public class AccountControllerHelper
	{
		readonly AccountSettings Settings;

		public AccountControllerHelper(AccountSettings settings)
		{
			Settings = settings;
		}

		public string GetQueryStringErrorMessage(int? errorMsgQueryStringValue)
		{
			if(errorMsgQueryStringValue == null || errorMsgQueryStringValue == 0)
				return null;

			return new ErrorMessage(errorMsgQueryStringValue.Value).Message;
		}

		public bool IsPasswordResetAvailable()
		{
			return !String.IsNullOrEmpty(AppLogic.MailServer()) && AppLogic.MailServer() != AppLogic.ro_TBD;
		}

		public Result Login(Customer signedInCustomer, ProfileBase profile, string username, string password, int skinId, bool registering = false)
		{
			var targetCustomer = new Customer(username, true);
			if(!targetCustomer.IsRegistered)
			{
				return new Result(
					state: ResultState.Error,
					message: AppLogic.GetString("lat_signin_process.aspx.1", skinId, signedInCustomer.LocaleSetting));
			}

			if(!Membership.ValidateUser(username, password))
			{
				Security.LogEvent("Store Login Failed", "Attempted login failed for email address " + username, 0, 0, 0);

				if((targetCustomer.BadLoginCount + 1) >= Settings.MaxBadLogins)
				{
					targetCustomer.UpdateCustomer(
						lockedUntil: DateTime.Now.AddMinutes(Settings.BadLoginLockTimeout),
						badLogin: -1);

					return new Result(
						state: ResultState.Error,
						message: AppLogic.GetString("lat_signin_process.aspx.3", skinId, signedInCustomer.LocaleSetting));
				}
				else
					targetCustomer.UpdateCustomer(
						badLogin: 1);

				return new Result(
					state: ResultState.Error,
					message: AppLogic.GetString("lat_signin_process.aspx.1", skinId, signedInCustomer.LocaleSetting));
			}

			if(targetCustomer.LockedUntil > DateTime.Now)
				return new Result(
					state: ResultState.Error,
					message: AppLogic.GetString("lat_signin_process.aspx.3", skinId, targetCustomer.LocaleSetting));

			if(!targetCustomer.Active)
				return new Result(
					state: ResultState.Error,
					message: AppLogic.GetString("lat_signin_process.aspx.2", skinId, targetCustomer.LocaleSetting));

			if(((targetCustomer.IsAdminSuperUser || targetCustomer.IsAdminUser)
					&& targetCustomer.PwdChanged.AddDays(Settings.AdminPasswordChangeDays) < DateTime.Now)
				|| targetCustomer.PwdChangeRequired)
				return new Result(
					state: ResultState.PasswordChangeRequired,
					message: AppLogic.GetString("lat_signin_process.aspx.4", skinId, targetCustomer.LocaleSetting));

			// Don't transfer information from one registered account to another
			if(!signedInCustomer.IsRegistered || registering)
			{
				if(Settings.DynamicRelatedProductsEnabled)
					targetCustomer.ReplaceProductViewFromAnonymous();

				AppLogic.ExecuteSigninLogic(signedInCustomer, targetCustomer);
				targetCustomer.ThisCustomerSession.UpdateCustomerSession(null, null);
			}

			// reset the cookie value if present for affiliate
			var affiliateIDFromCookie = int.Parse(
				CommonLogic.IsInteger(profile.GetPropertyValue(Customer.ro_AffiliateCookieName).ToString())
					? profile.GetPropertyValue(Customer.ro_AffiliateCookieName).ToString()
					: "0");

			int? affiliateIdParameter = null;
			if(AppLogic.IsValidAffiliate(affiliateIDFromCookie))
			{
				// reset it's value
				profile.SetPropertyValue(Customer.ro_AffiliateCookieName, affiliateIDFromCookie.ToString());
				affiliateIdParameter = affiliateIDFromCookie;
			}

			if(targetCustomer.IsAdminUser)
				Security.LogEvent("Store Login Success", "", targetCustomer.CustomerID, targetCustomer.CustomerID, targetCustomer.ThisCustomerSession.SessionID);

			var lockeduntil = DateTime.Now.AddMinutes(-1);
			targetCustomer.UpdateCustomer(
				affiliateId: affiliateIdParameter,
				lockedUntil: lockeduntil,
				badLogin: -1,
				passwordChangeRequired: false);

			return new Result(
				state: ResultState.Success);
		}

		public Result RequestNewPassword(Customer signedInCustomer, string email, int skinId)
		{
			email = (email ?? String.Empty).Trim().ToLower();

			if(String.IsNullOrEmpty(email))
				return new Result(
					state: ResultState.Error,
					message: AppLogic.GetString("lostpassword.aspx.4", skinId, signedInCustomer.LocaleSetting));

			var resetCustomer = new Customer(email);
			if(!resetCustomer.IsRegistered)
				return new Result(
					state: ResultState.Error,
					message: AppLogic.GetString("signin.aspx.25", skinId, signedInCustomer.LocaleSetting));

			try
			{
				var user = Membership.GetUser(email);
				var newPassword = user.ResetPassword();

				while(newPassword.Contains("*")) // *'s in passwords fail because of replacement - keep generating new passwords until no *'s
					newPassword = user.ResetPassword();

				AppLogic.SendMail(subject: Settings.StoreName + " " + AppLogic.GetString("lostpassword.aspx.6", skinId, signedInCustomer.LocaleSetting), body: AppLogic.RunXmlPackage(Settings.XmlPackageLostPassword, null, signedInCustomer, skinId, string.Empty, "newpwd=" + newPassword + "&thiscustomerid=" + signedInCustomer.CustomerID.ToString(), false, false), useHtml: true, fromAddress: Settings.MailFromAddress, fromName: Settings.MailFromAddress, toAddress: email, toName: email, bccAddresses: "", server: AppLogic.MailServer());

				return new Result(
					state: ResultState.Success,
					message: AppLogic.GetString("lostpassword.aspx.2", skinId, signedInCustomer.LocaleSetting));
			}
			catch
			{
				return new Result(
					state: ResultState.Error,
					message: AppLogic.GetString("lostpassword.aspx.3", skinId, signedInCustomer.LocaleSetting));
			}
		}

		public Result ChangePassword(Customer signedInCustomer, string username, string oldPassword, string newPassword, string newPasswordConfirmation, int skinId)
		{
			var targetCustomer = new Customer(username, true);

			if(!targetCustomer.IsRegistered)
				return new Result(
					state: ResultState.Error,
					message: AppLogic.GetString("lat_signin_process.aspx.1", skinId, signedInCustomer.LocaleSetting));

			if(!Membership.ValidateUser(username, oldPassword))
			{
				if(targetCustomer.IsAdminUser)
					targetCustomer.UpdateCustomer(
						badLogin: 1);

				return new Result(
					state: ResultState.Error,
					message: AppLogic.GetString("signin.aspx.29", skinId, signedInCustomer.LocaleSetting));
			}

			if(targetCustomer.IsAdminUser)
				Security.LogEvent("Admin Password Changed Success", "", targetCustomer.CustomerID, targetCustomer.CustomerID, 0);

			var user = Membership.GetUser(username);

			var passwordValidationResult = ValidatePassword(signedInCustomer, targetCustomer, oldPassword, newPassword, newPasswordConfirmation, skinId);
			if(passwordValidationResult.State != ResultState.Success)
				return passwordValidationResult;

			var changePasswordResult = user.ChangePassword(oldPassword, newPassword);
			if(!changePasswordResult)
				return new Result(
					state: ResultState.Error);

			AppLogic.ExecuteSigninLogic(signedInCustomer, targetCustomer);
			targetCustomer.ThisCustomerSession.UpdateCustomerSession(null, null);

			// clear impersonation
			targetCustomer.ThisCustomerSession.ClearVal("IGD");
			targetCustomer.ThisCustomerSession.ClearVal("IGD_EDITINGORDER");

			return new Result(
				state: ResultState.Success,
				message: AppLogic.GetString("signin.aspx.24", skinId, signedInCustomer.LocaleSetting));
		}

		Result ValidatePassword(Customer signedInCustomer, Customer targetCustomer, string oldPassword, string newPassword, string newPasswordConfirmation, int skinId)
		{
			if(newPassword.Replace("*", "").Trim().Length == 0)
				return new Result(
					state: ResultState.Error);

			if(newPassword == oldPassword)
				return new Result(
					state: ResultState.Error,
					message: AppLogic.GetString("signin.aspx.30", skinId, signedInCustomer.LocaleSetting));

			if(newPassword != newPasswordConfirmation)
				return new Result(
					state: ResultState.Error,
					message: AppLogic.GetString("signin.newpassword.mismatch", skinId, signedInCustomer.LocaleSetting));

			if(targetCustomer.PasswordPreviouslyUsed(newPassword))
				return new Result(
					state: ResultState.Error,
					message: String.Format(AppLogic.GetString("signin.aspx.31", skinId, signedInCustomer.LocaleSetting), Settings.NumberOfPreviouslyUsedPasswords));

			if(targetCustomer.BadLoginCount >= Settings.MaxBadLogins)
				return new Result(
					state: ResultState.Error,
					message: AppLogic.GetString("lat_signin_process.aspx.3", skinId, signedInCustomer.LocaleSetting));

			if(!targetCustomer.Active)
				return new Result(
					state: ResultState.Error,
					message: AppLogic.GetString("lat_signin_process.aspx.2", skinId, signedInCustomer.LocaleSetting));

			try
			{
				if(targetCustomer.IsAdminUser || targetCustomer.IsAdminSuperUser || Settings.UseStrongPasswords)
				{
					if(!Regex.IsMatch(newPassword, Settings.StrongPasswordValidatorExpression, RegexOptions.Compiled))
						return new Result(
							state: ResultState.Error,
							message: AppLogic.GetString("signin.aspx.26", skinId, signedInCustomer.LocaleSetting));
				}
				else
				{
					if(!Regex.IsMatch(newPassword, Settings.PasswordValidatorExpression, RegexOptions.Compiled))
						return new Result(
							state: ResultState.Error,
							message: AppLogic.GetString("signin.newpassword.normalRegexFailure", skinId, signedInCustomer.LocaleSetting));
				}
			}
			catch(ArgumentException)
			{
				AppLogic.SendMail("Invalid Password Validation Pattern", "", false, Settings.MailFromAddress, Settings.MailFromAddress, Settings.MailToAddress, Settings.MailToAddress, "", "", AppLogic.MailServer());
				throw new Exception("Password validation expression is invalid, please notify site administrator");
			}

			return new Result(
				state: ResultState.Success,
				message: AppLogic.GetString("signin.aspx.24", skinId, signedInCustomer.LocaleSetting));
		}

		public AccountIndexViewModel BuildAccountIndexViewModel(AccountViewModel account, Customer customer, UrlHelper urlHelper)
		{
			var billingAddress = new Address();
			billingAddress.LoadByCustomer(customer.CustomerID, customer.PrimaryBillingAddressID, AddressTypes.Billing);

			var primaryBillingAddress = new AccountAddressViewModel
			{
				Id = billingAddress.AddressID,
				FirstName = billingAddress.FirstName,
				LastName = billingAddress.LastName,
				Company = billingAddress.Company,
				Address1 = billingAddress.Address1,
				Address2 = billingAddress.Address2,
				Suite = billingAddress.Suite,
				City = billingAddress.City,
				State = billingAddress.State,
				Zip = billingAddress.Zip,
				Country = billingAddress.Country,
				Phone = billingAddress.Phone
			};

			var shippingAddress = new Address();
			shippingAddress.LoadByCustomer(customer.CustomerID, customer.PrimaryShippingAddressID, AddressTypes.Billing);

			var primaryShippingAddress = new AccountAddressViewModel
			{
				Id = shippingAddress.AddressID,
				FirstName = shippingAddress.FirstName,
				LastName = shippingAddress.LastName,
				Company = shippingAddress.Company,
				Address1 = shippingAddress.Address1,
				Address2 = shippingAddress.Address2,
				Suite = shippingAddress.Suite,
				City = shippingAddress.City,
				State = shippingAddress.State,
				Zip = shippingAddress.Zip,
				Country = shippingAddress.Country,
				Phone = shippingAddress.Phone
			};

			return new AccountIndexViewModel(
				account: account,
				primaryBillingAddress: primaryBillingAddress,
				primaryShippingAddress: primaryShippingAddress,
				orders: BuildOrderViewModels(customer, Settings.StoreId),
				paymentMethodLastUsed: billingAddress.DisplayPaymentMethodInfo(customer, billingAddress.PaymentMethodLastUsed),
				showWishListButton: Settings.ShowWishlistButtons,
				showVatRegistrationId: Settings.VatEnabled,
				showSaveCreditCardNumber: Settings.StoreCreditCards,
				saveCreditCardNumberNote: Settings.StoreCreditCards
					&& customer.HasActiveRecurringOrders(true)
					&& !Settings.UseGatewayInternalBilling
						? AppLogic.GetString("account.aspx.70")
						: null,
				showRecurringOrders: ShoppingCart.NumItems(customer.CustomerID, CartTypeEnum.RecurringCart) > 0,
				showWallets: new AspDotNetStorefrontGateways.Processors.AuthorizeNet().IsCimEnabled,
				customerLevel: customer.CustomerLevelID != 0
					? customer.CustomerLevelName
					: string.Empty,
				hasMicropayBalance: customer.IsRegistered
					&& AppLogic.MicropayIsEnabled()
					&& AppLogic.GetMicroPayProductID() != 0,
				showMicropayLink: AppLogic.AppConfigBool("MicroPay.ShowAddToBalanceLink")
					&& AppLogic.MicropayIsEnabled()
					&& AppLogic.GetMicroPayProductID() != 0,
				micropayLink: urlHelper.BuildProductLink(AppLogic.GetMicroPayProductID()),
				micropayBalance: customer.MicroPayBalance,
				localeSetting: customer.LocaleSetting,
				currencySetting: customer.CurrencySetting,
				requireEmailConfirmation: AppLogic.AppConfigBool("RequireEmailConfirmation"),
				displayOver13Selector: AppLogic.AppConfigBool("RequireOver13Checked"),
				showAccountRemovalButton: AppLogic.AppConfigBool("DataRetentionPolicies.Enabled")
					&& !customer.IsAdminSuperUser
					&& !customer.IsAdminUser);
		}

		public IEnumerable<AccountOrderViewModel> BuildOrderViewModels(Customer customer, int storeId)
		{
			var parameters = new[]
				{
					new System.Data.SqlClient.SqlParameter("stateAuthorized", AppLogic.ro_TXStateAuthorized),
					new System.Data.SqlClient.SqlParameter("stateCaptured", AppLogic.ro_TXStateCaptured),
					new System.Data.SqlClient.SqlParameter("statePending", AppLogic.ro_TXStatePending),
					new System.Data.SqlClient.SqlParameter("customerId", customer.CustomerID),
					new System.Data.SqlClient.SqlParameter("allowCustomerFiltering", AppLogic.GlobalConfigBool("AllowCustomerFiltering") ? 1 : 0),
					new System.Data.SqlClient.SqlParameter("storeId", storeId),
				};

			var orderCommand = @"
					Select 
						DownloadEMailSentOn,
						OrderNumber, 
						QuoteCheckout, 
						ShippedVIA, 
						ShippingTrackingNumber
					from dbo.Orders with (nolock)
					where 
						TransactionState in (@stateAuthorized, @stateCaptured, @statePending)
						and CustomerID = @customerId
						and (@allowCustomerFiltering = 0 or StoreID = @storeId)
					order by OrderDate desc";

			using(var connection = DB.dbConn())
			{
				connection.Open();

				using(var rs = DB.GetRS(orderCommand, parameters, connection))
				{
					while(rs.Read())
					{
						var order = new Order(DB.RSFieldInt(rs, "OrderNumber"));
						var downloadEMailSentOn = DB.RSFieldDateTime(rs, "DownloadEMailSentOn");
						var quoteCheckout = DB.RSFieldByte(rs, "QuoteCheckout");
						var shippedVia = DB.RSField(rs, "ShippedVIA");
						var shippingTrackingNumber = DB.RSField(rs, "ShippingTrackingNumber");

						yield return new AccountOrderViewModel
						{
							OrderNumber = order.OrderNumber,
							OrderDate = order.OrderDate,
							PaymentStatus = BuildPaymentStatus(
								paymentMethod: order.PaymentMethod,
								cardNumber: order.CardNumber,
								transactionState: order.TransactionState,
								orderTotal: order.Total(),
								skinId: customer.SkinID,
								localeSetting: customer.LocaleSetting),
							TransactionStateNotificationType =
								order.TransactionState == AppLogic.ro_TXStateAuthorized
								|| order.TransactionState == AppLogic.ro_TXStatePending
									? "primary"
									: order.TransactionState == AppLogic.ro_TXStateCaptured
										? "success"
										: "danger",
							TransactionState = order.TransactionState,
							ShippingStatus = BuildShippingStatus(
								orderNumber: order.OrderNumber,
									shippedOn: order.ShippedOn == DateTime.MinValue ? string.Empty : order.ShippedOn.ToString(),
								shippedVIA: shippedVia,
								shippingTrackingNumber: shippingTrackingNumber,
								transactionState: order.TransactionState,
								downloadEMailSentOn: downloadEMailSentOn,
								skinId: customer.SkinID,
								localeSetting: customer.LocaleSetting),
							CustomerServiceNotes = BuildCustServiceNotes(
								customerServiceNotes: order.CustomerServiceNotes,
								skinId: customer.SkinID,
								localeSetting: customer.LocaleSetting),
							OrderTotal = BuildOrderTotal(
								quoteCheckout: quoteCheckout,
								paymentMethod: order.PaymentMethod,
								orderTotal: order.Total(),
								couponType: (int)order.GetCoupon().CouponType,
								couponDiscountAmount: order.GetCoupon().DiscountAmount,
								skinId: customer.SkinID,
								localeSetting: customer.LocaleSetting,
								currencyCode: customer.CurrencySetting),
							CanReorder = Settings.ReorderEnabled
								&& string.IsNullOrEmpty(order.RecurringSubscriptionID)
								&& !order.HasKitItems()
						};
					}
				}
			}
		}

		public Customer CreateAccount(AccountViewModel model, Customer customer)
		{
			int registeredCustomerId;
			string registeredCustomerGuid;
			Customer.MakeAnonCustomerRecord(out registeredCustomerId, out registeredCustomerGuid);

			var registeredCustomer = new Customer(registeredCustomerId);
			UpdateAccount(model, registeredCustomer);

			return registeredCustomer;
		}

		public void UpdateAccount(AccountViewModel account, Customer customer)
		{
			customer.UpdateCustomer(
				isRegistered: true,
				email: account.Email.ToLowerInvariant().Trim(),
				firstName: account.FirstName,
				lastName: account.LastName,
				phone: account.Phone,
				okToEmail: account.IsOkToEmail,
				over13Checked: account.IsOver13,
				vatRegistrationId: account.VatRegistrationId,
				storeCreditCardInDb: account.SaveCreditCardNumber,
				clearSavedCCNumbers: customer.StoreCCInDB && !account.SaveCreditCardNumber  //If cards were stored before and the box is now unchecked, clear old saved data
			);

			if(!string.IsNullOrEmpty(account.Password))
			{
				var password = new Password(account.Password);

				customer.UpdateCustomer(
					saltedAndHashedPassword: password.SaltedPassword,
					saltKey: password.Salt
				);
			}
		}

		public PasswordValidationResult ValidateAccountPassword(Customer customer, string password, string passwordConfirmation)
		{
			if(password != passwordConfirmation)
				return PasswordValidationResult.DoesNotMatch;

			try
			{
				if(customer.IsAdminUser || customer.IsAdminSuperUser || Settings.UseStrongPasswords)
				{
					if(!Regex.IsMatch(password, Settings.StrongPasswordValidatorExpression, RegexOptions.Compiled))
						return PasswordValidationResult.NotStrong;
				}
				else
				{
					if(!Regex.IsMatch(password, Settings.PasswordValidatorExpression, RegexOptions.Compiled))
						return PasswordValidationResult.DoesNotMeetMinimum;
				}
			}
			catch(System.ArgumentException)
			{
				AppLogic.SendMail("Invalid Password Validation Pattern", "", false, Settings.MailFromAddress, Settings.MailFromAddress, Settings.MailToAddress, Settings.MailToAddress, "", "", AppLogic.MailServer());
				throw new Exception("Password validation expression is invalid, please notify site administrator");
			}

			// The new password is the same as the current password
			if(customer.CheckLogin(password))
				return PasswordValidationResult.SameAsCurrent;

			// The new password has been used before
			if(customer.PasswordPreviouslyUsed(password))
				return PasswordValidationResult.SameAsPrevious;

			return PasswordValidationResult.Ok;
		}

		public VatRegistrationValidationResult ValidateVatRegistrationId(AccountViewModel model, Customer customer)
		{
			if(!Settings.VatEnabled || string.IsNullOrEmpty(model.VatRegistrationId))
				return new VatRegistrationValidationResult(ok: true);

			if(AppLogic.VATRegistrationIDIsValid(customer, model.VatRegistrationId))
				return new VatRegistrationValidationResult(ok: true);
			else
				return new VatRegistrationValidationResult(ok: false);
		}

		string BuildPaymentStatus(string paymentMethod, string cardNumber, string transactionState, decimal orderTotal, int skinId, string localeSetting)
		{
			if(orderTotal == Decimal.Zero)
				return AppLogic.GetString("order.cs.16", skinId, localeSetting);

			var paymentStatus =
				paymentMethod.Length != 0
				? string.Format("{0} {1}",
					AppLogic.GetString("account.aspx.43", skinId, localeSetting),
					paymentMethod.Replace(AppLogic.ro_PMMicropay, AppLogic.GetString("account.aspx.11", skinId, localeSetting)))
				: string.Format("{0} {1}",
					AppLogic.GetString("account.aspx.43", skinId, localeSetting),
					cardNumber.StartsWith(AppLogic.ro_PMPayPal, StringComparison.InvariantCultureIgnoreCase)
						? AppLogic.GetString("account.aspx.44", skinId, localeSetting)
						: AppLogic.GetString("account.aspx.45", skinId, localeSetting));

			return paymentStatus;
		}

		string BuildShippingStatus(int orderNumber, string shippedOn, string shippedVIA, string shippingTrackingNumber, string transactionState, DateTime downloadEMailSentOn, int skinId, string localeSetting)
		{
			var shippingStatus = String.Empty;

			if(AppLogic.OrderHasShippableComponents(orderNumber))
			{
				if(shippedOn != "")
				{
					shippingStatus = AppLogic.GetString("account.aspx.48", skinId, localeSetting);
					if(shippedVIA.Length != 0)
					{
						shippingStatus += " " + AppLogic.GetString("account.aspx.49", skinId, localeSetting) + " " + shippedVIA;
					}

					shippingStatus += " " + AppLogic.GetString("account.aspx.50", skinId, localeSetting) + " " + Localization.ParseNativeDateTime(shippedOn).ToString(new System.Globalization.CultureInfo(localeSetting));
					if(shippingTrackingNumber.Length != 0)
					{
						shippingStatus += " " + AppLogic.GetString("account.aspx.51", skinId, localeSetting) + " ";

						var TrackURL = Shipping.GetTrackingURL(shippingTrackingNumber);
						if(TrackURL.Length != 0)
						{
							shippingStatus += "<a href=\"" + TrackURL + "\" target=\"_blank\">" + shippingTrackingNumber + "</a>";
						}
						else
						{
							shippingStatus += shippingTrackingNumber;
						}
					}
				}
				else
				{
					shippingStatus = AppLogic.GetString("account.aspx.52", skinId, localeSetting);
				}
			}

			if(AppLogic.OrderHasDownloadComponents(orderNumber, true))
			{
				var downloadUrl = DependencyResolver.Current.GetService<UrlHelper>().Action(ActionNames.Index, ControllerNames.Downloads);
				shippingStatus += string.Format("<div><a href=\"{0}\">{1}</a></div>", downloadUrl, AppLogic.GetString("download.aspx.1", skinId, localeSetting));
			}

			return shippingStatus;
		}

		string BuildOrderTotal(int quoteCheckout, string paymentMethod, decimal orderTotal, int couponType, decimal couponDiscountAmount, int skinId, string localeSetting, string currencyCode)
		{
			if(couponType == 2)
				orderTotal = orderTotal < couponDiscountAmount ? 0 : orderTotal - couponDiscountAmount;

			return (quoteCheckout == 1 || AppLogic.CleanPaymentMethod(paymentMethod) == AppLogic.ro_PMRequestQuote)
				? AppLogic.GetString("account.aspx.54", skinId, localeSetting)
				: Localization.CurrencyStringForDisplayWithExchangeRate(orderTotal, currencyCode);
		}

		string BuildCustServiceNotes(string customerServiceNotes, int skinId, string localeSetting)
		{
			if(!Settings.ShowCustomerServiceNotesInReceipts)
				return string.Empty;

			return customerServiceNotes.Length == 0
				? AppLogic.GetString("order.cs.29", skinId, localeSetting)
				: customerServiceNotes;
		}

		public class Result
		{
			public readonly ResultState State;
			public readonly string Message;

			public Result(ResultState state, string message = null)
			{
				State = state;
				Message = message;
			}
		}

		public enum ResultState
		{
			Success,
			Error,
			PasswordChangeRequired,
		}

		public enum PasswordValidationResult
		{
			Ok,
			DoesNotMatch,
			DoesNotMeetMinimum,
			NotStrong,
			SameAsCurrent,
			SameAsPrevious
		}

		public class VatRegistrationValidationResult
		{
			public readonly bool Ok;
			public readonly string Message;

			public VatRegistrationValidationResult(bool ok, string message = null)
			{
				Ok = ok;
				Message = message;
			}
		}
	}
}
