// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace AspDotNetStorefrontCore.DataRetention
{
	public interface IDataRetentionService
	{
		void UpsertLastActivity(int customerId);

		void UpsertLastActivity(Guid? customerGuid);

		bool CustomerCanBeAnonymized(int customerId);

		bool CustomerIsAnonymized(int customerId);

		Result<bool> AnonymizeCustomer(int customerId);

		IEnumerable<Result<bool>> AnonymizeOrders(int customerId);

		Result<bool> AnonymizeOrder(int orderNumber);

		bool PendingRemoveAccountRequest(Customer customer);

		void CreateRemoveAccountRequest(Customer customer);

		void SendRemoveAccountAcknowledgement(
			Customer customer,
			string storeName,
			string mailFromAddress);

		void InactiveCustomerMaintenance();
	}
}
