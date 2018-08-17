// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using AspDotNetStorefront.StringResource;

namespace AspDotNetStorefrontCore.DataRetention
{
	public class DataRetentionService : IDataRetentionService
	{
		readonly AppConfigProvider AppConfigProvider;
		readonly IStringResourceProvider StringResourceProvider;

		public DataRetentionService(AppConfigProvider appConfigProvider, IStringResourceProvider stringResourceProvider)
		{
			AppConfigProvider = appConfigProvider;
			StringResourceProvider = stringResourceProvider;
		}

		public bool CustomerCanBeAnonymized(int customerId)
		{
			var customerCanBeAnonymized = DB.GetSqlN(
				@"with isadmin (N)
				as
				(
					select count(customerid) N
					from customer
					where isadmin > 0
					and customerid = @customerId
				),
				recurringorders (N)
				as
				(
					select count(customerid) N
					from shoppingcart
					where carttype = @cartType
					and originalrecurringordernumber is not null
					and customerid = @customerId
				),
				outstandingorders (N)
				as
				(
					select count(ordernumber) N
					from orders
					where
					(
						transactionstate = @transactionStatePending
						or transactionstate = @transactionStateAuthorized
						or (shippedon is null and transactionstate = @transactionStateCaptured)
						or datediff(dd, orderdate, getdate()) <= 7
						or datediff(dd, authorizedon, getdate()) <= 7
						or datediff(dd, capturedon, getdate()) <= 7
						or datediff(dd, refundedon, getdate()) <= 7
						or datediff(dd, voidedon, getdate()) <= 7
						or datediff(dd, editedon, getdate()) <= 7
					)
					and customerid = @customerId
				)

				select (outstandingorders.N + recurringorders.N + isadmin.N) N
				from isadmin, outstandingorders, recurringorders;",

				new SqlParameter("@cartType", (int)CartTypeEnum.RecurringCart),
				new SqlParameter("@transactionStatePending", AppLogic.ro_TXStatePending),
				new SqlParameter("@transactionStateAuthorized", AppLogic.ro_TXStateAuthorized),
				new SqlParameter("@transactionStateCaptured", AppLogic.ro_TXStateCaptured),
				new SqlParameter("@customerId", customerId));

			return customerCanBeAnonymized == 0;
		}

		public Result<bool> AnonymizeCustomer(int customerId)
		{
			if(customerId == 0)
				return Result.Ok(false);

			if(!CustomerCanBeAnonymized(customerId))
				return Result.Ok(false);

			try
			{
				DB.ExecuteSQL(
					@"update customer
					set email = substring(@replacementText, 0, 100),
					firstname = substring(@replacementText, 0, 100),
					lastname = substring(@replacementText, 0, 100),
					phone = substring(@replacementText, 0, 25),
					oktoemail = 0,
					lastipaddress = null,
					rtshiprequest = null,
					rtshipresponse = null,
					finalizationdata = null,
					active = 0,
					password = '',
					saltkey = 0
					where customerid = @customerId;

					update address
					set email = substring(@replacementText, 0, 100),
					firstname = substring(@replacementText, 0, 100),
					lastname = substring(@replacementText, 0, 100),
					company = null,
					address1 = substring(@replacementText, 0, 100),
					address2 = null,
					phone = substring(@replacementText, 0, 25),
					cardname = null
					where customerid = @customerId;

					update customerdataretention set removaldate = getdate() where customerid = @customerId;

					delete from customersession where customerid = @customerId;",

					new SqlParameter("@replacementText", GetReplacementText()),
					new SqlParameter("@customerId", customerId));
			}
			catch(Exception exception)
			{
				return Result.Fail(false, exception);
			}

			return Result.Ok(true);
		}

		public IEnumerable<Result<bool>> AnonymizeOrders(int customerId)
		{
			var results = new List<Result<bool>>();

			if(customerId == 0)
			{
				results.Add(Result.Ok(false));
				return results;
			}

			using(var connection = DB.dbConn())
			{
				connection.Open();
				using(var reader = DB.GetRS(
					@"select ordernumber
					from orders
					where customerid = @customerId",
					connection,
					new SqlParameter("@customerId", customerId)))
				{
					while(reader.Read())
					{
						results.Add(
							AnonymizeOrder(
								DB.RSFieldInt(reader, "orderNumber")));
					}
				}
			}

			return results;
		}

		public Result<bool> AnonymizeOrder(int orderNumber)
		{
			if(orderNumber == 0)
				return Result.Ok(false);

			try
			{
				DB.ExecuteSQL(
					@"update multishiporder_shipment
					set destinationaddress = ''
					where ordernumber = @orderNumber;

					update orders set
					firstname = substring(@replacementText, 0, 100),
					lastname = substring(@replacementText, 0, 100),
					email = substring(@replacementText, 0, 100),
					billinglastname = substring(@randomReplacementText, 0, len(billinglastname) + 1),
					billingfirstname = substring(@randomReplacementText, 0, len(billingfirstname) + 1),
					billingaddress1 = substring(@randomReplacementText, 0, len(billingaddress1) + 1),
					billingaddress2 = substring(@randomReplacementText, 0, len(billingaddress2) + 1),
					billingphone = substring(@randomReplacementText, 0, len(billingphone) + 1),
					shippinglastname = substring(@randomReplacementText, 0, len(shippinglastname) + 1),
					shippingfirstname = substring(@randomReplacementText, 0, len(shippingfirstname) + 1),
					shippingaddress1 = substring(@randomReplacementText, 0, len(shippingaddress1) + 1),
					shippingaddress2 = substring(@randomReplacementText, 0, len(shippingaddress2) + 1),
					shippingphone = substring(@randomReplacementText, 0, len(shippingphone) + 1),
					phone = substring(@replacementText, 0, 25),
					cardname = substring(@randomReplacementText, 0, len(cardname) + 1),
					transactioncommand = null,
					lastipaddress = null,
					rtshiprequest = null,
					rtshipresponse = null,
					capturetxcommand = null,
					capturetxresult = null,
					voidtxcommand = null,
					voidtxresult = null,
					refundtxcommand = null,
					refundtxresult = null,
					refundreason = null,
					cardinallookupresult = null,
					cardinalauthenticateresult = null,
					cardinalgatewayparms = null,
					finalizationdata = null,
					buysafecommand = '',
					maxminddetails = null,
					receipthtml = null,
					authorizationresult = null
					where ordernumber = @orderNumber;

					update orders_shoppingcart
					set shippingdetail = null
					where orderNumber = @orderNumber;

					update ordertransaction
					set transactioncommand = substring(@randomReplacementText, 0, len(transactioncommand) + 1),
					transactionresult = substring(@randomReplacementText, 0, len(transactionresult) + 1)
					where ordernumber = @orderNumber;",

					new SqlParameter("@randomReplacementText", GetRandomReplacementText()),
					new SqlParameter("@replacementText", GetReplacementText()),
					new SqlParameter("@orderNumber", orderNumber));
			}
			catch(Exception exception)
			{
				return Result.Fail(false, exception);
			}

			return Result.Ok(true);
		}

		public void UpsertLastActivity(int customerId)
		{
			using(var connection = DB.dbConn())
			{
				connection.Open();

				var command = new SqlCommand(
					@"update customerdataretention
					set customerlastactiveon = getdate()
					where customerid = @customerId;",
					connection);
				command.Parameters.AddWithValue("@customerId", customerId);
				var rowsAffected = command.ExecuteNonQuery();

				if (rowsAffected == 0)
				{
					command = new SqlCommand(
						@"insert customerdataretention (customerid, customerlastactiveon)
						values(@customerId, getdate());",
						connection);
					command.Parameters.AddWithValue("@customerId", customerId);
					command.ExecuteNonQuery();
				}
			}
		}

		public void UpsertLastActivity(Guid? customerGuid)
		{
			if(customerGuid == null)
				return;

			using(var connection = DB.dbConn())
			{
				connection.Open();

				var command = new SqlCommand(
					@"update customerdataretention
					set customerlastactiveon = getdate() from customer c
					join customerdataretention cr on c.customerid = cr.customerid
					and c.customerguid = @customerGuid;",
					connection);
				command.Parameters.AddWithValue("@customerGuid", customerGuid);
				var rowsAffected = command.ExecuteNonQuery();

				if(rowsAffected == 0)
				{
					command = new SqlCommand(
						@"insert customerdataretention (customerid, customerlastactiveon)
						select customerid, getdate() from customer where customerguid = @customerGuid;",
						connection);
					command.Parameters.AddWithValue("@customerGuid", customerGuid);
					command.ExecuteNonQuery();
				}
			}
		}

		public bool CustomerIsAnonymized(int customerId)
		{
			var isAnonymized = DB.GetSqlN(
				@"select count(customerid) N
				from customerdataretention
				where customerid = @customerId and removaldate is not null",
				new SqlParameter("@customerId", customerId));

			return isAnonymized > 0;
		}

		string GetReplacementText()
		{
			return StringResourceProvider.GetString("dataretentionpolicies.replacementtext");
		}

		string GetRandomReplacementText()
		{
			var bytes = new byte[1024];
			new Random().NextBytes(bytes);
			return Convert.ToBase64String(bytes)
				.Replace("=", string.Empty)
				.Replace("+", string.Empty)
				.Replace("/", string.Empty);
		}

		public bool PendingRemoveAccountRequest(Customer customer)
		{
			var customerId = DB.GetSqlN(
				@"select customerid N
				from customerdataretention
				where customerid = @customerId and (removalrequestdate is not null or removaldate is not null)",
				new SqlParameter("@customerId", customer.CustomerID));

			return customerId > 0;
		}

		public void CreateRemoveAccountRequest(Customer customer)
		{
			UpsertLastActivity(customer.CustomerID);

			DB.ExecuteSQL(
				@"update customerdataretention
				set removalrequestdate = getdate()
				where customerid = @customerId;",
				new SqlParameter("@customerId", customer.CustomerID));
		}

		public void SendRemoveAccountAcknowledgement(
			Customer customer,
			string storeName,
			string mailFromAddress)
		{
			var removeAccountEmail = new Topic("removeaccountemail").Contents;

			AppLogic.SendMail(
				subject: $"{storeName} {StringResourceProvider.GetString("remove.account.emailsubject")}",
				body: removeAccountEmail,
				useHtml: true,
				fromAddress: mailFromAddress,
				fromName: mailFromAddress,
				toAddress: customer.EMail,
				toName: $"{customer.FirstName} {customer.LastName}",
				bccAddresses: string.Empty,
				server: AppLogic.MailServer());
		}

		public void InactiveCustomerMaintenance()
		{
			// Anonymize orders and customers who have not been active in the last N months
			var anonymizeAfterMonths = AppLogic.AppConfigNativeInt("DataRetentionPolicies.MonthsBeforeUserDataAnonymized");

			if(anonymizeAfterMonths == 0)
				return;

			if(anonymizeAfterMonths < 0)
			{
				SysLog.LogMessage("Data Retention is mis-configured", "The Setting 'DataRetentionPolicies.MonthsBeforeUserDataAnonymized' has a negative value.  It either needs to be a positive number of months or 0 to disable aged user data anonymization.", MessageTypeEnum.Informational, MessageSeverityEnum.Error);
				return;
			}

			using(var connection = DB.dbConn())
			{
				connection.Open();

				foreach(var inactiveCustomerId in GetInactiveCustomers(connection, anonymizeAfterMonths))
				{
					var result = AnonymizeCustomer(inactiveCustomerId);
					if(!result.Success)
						SysLog.LogMessage("Customer anonymization failed", $"Customer {inactiveCustomerId} could not be anonymized due to {result.Error}", MessageTypeEnum.Informational, MessageSeverityEnum.Alert);
				}

				foreach(var inactiveOrderId in GetInactiveOrders(connection, anonymizeAfterMonths))
				{
					var result = AnonymizeOrder(inactiveOrderId);
					if(!result.Success)
						SysLog.LogMessage("Order anonymization failed", $"Order {inactiveOrderId} could not be anonymized due to {result.Error}", MessageTypeEnum.Informational, MessageSeverityEnum.Alert);
				}
			}
		}

		IEnumerable<int> GetInactiveCustomers(SqlConnection connection, int months)
		{
			var inactiveCustomerIds = new List<int>();

			if(months <= 0)
				return inactiveCustomerIds;

			using(var reader = DB.GetRS(
				@"with inactiveCustomers
				as
				(
					select c.customerid from customer c
					left join CustomerDataRetention ctr on c.customerid = ctr.customerid
					where (c.isregistered = 1 or c.active = 1)
					and c.isadmin = 0
					and @months > 0
					and datediff(mm, isnull(ctr.customerlastactiveon, c.updatedon), getdate()) >= @months
					and ctr.removaldate is null
				),
				inactiveWithoutOrders
				as
				(
					select c.customerid from inactiveCustomers c
					left join orders o on c.customerid = o.customerid
					where o.ordernumber is null
				),
				inactiveWithOrders
				as
				(
					select c.customerid from inactiveCustomers c
					inner join orders o on c.customerid = o.customerid
					group by c.customerid 
					having datediff(mm, max(o.orderdate), getdate()) >= @months
					and datediff(mm, max(o.authorizedon), getdate()) >= @months
					and datediff(mm, max(o.capturedon), getdate()) >= @months
					and coalesce(datediff(mm, max(o.refundedon), getdate()), @months) >= @months
					and coalesce(datediff(mm, max(o.voidedon), getdate()), @months) >= @months
					and coalesce(datediff(mm, max(o.editedon), getdate()), @months) >= @months
				)
				select customerid from 
				inactiveWithoutOrders
				union 
				select customerid from 
				inactiveWithOrders",
			connection,
			new SqlParameter("@months", months)))
			{
				while(reader.Read())
					inactiveCustomerIds.Add(
						reader.FieldInt("customerid"));
			}

			return inactiveCustomerIds;
		}

		IEnumerable<int> GetInactiveOrders(SqlConnection connection, int months)
		{
			var inactiveOrderIds = new List<int>();

			if(months <= 0)
				return inactiveOrderIds;

			using(var reader = DB.GetRS(
				@"select distinct o.ordernumber from orders o
				left join customer c on o.customerid = c.customerid
				left join customerdataretention ctr on o.customerid = ctr.customerid
				where isnull(c.isadmin, 0) = 0
				and ctr.removaldate is null
				and @months > 0
				and datediff(mm, o.orderdate, getdate()) >= @months
				and datediff(mm, o.authorizedon, getdate()) >= @months
				and datediff(mm, o.capturedon, getdate()) >= @months
				and coalesce(datediff(mm, o.refundedon, getDate()), @months) >= @months
				and coalesce(datediff(mm, o.voidedon, getDate()), @months) >= @months
				and coalesce(datediff(mm, o.editedon, getDate()), @months) >= @months;",
			connection,
			new SqlParameter("@months", months)))
			{
				while(reader.Read())
					inactiveOrderIds.Add(
						reader.FieldInt("ordernumber"));
			}

			return inactiveOrderIds;
		}
	}
}
