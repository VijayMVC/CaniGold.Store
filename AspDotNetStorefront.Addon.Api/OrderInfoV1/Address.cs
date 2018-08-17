// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
namespace AspDotNetStorefront.Addon.Api.OrderInfoV1
{
	public class Address
	{
		public string LastName { get; }
		public string FirstName { get; }
		public string Company { get; }
		public string Address1 { get; }
		public string Address2 { get; }
		public string Suite { get; }
		public string City { get; }
		public string State { get; }
		public string Zip { get; }
		public string Country { get; }
		public string Phone { get; }

		public Address(
			string lastName,
			string firstName,
			string company,
			string address1,
			string address2,
			string suite,
			string city,
			string state,
			string zip,
			string country,
			string phone)
		{
			LastName = lastName;
			FirstName = firstName;
			Company = company;
			Address1 = address1;
			Address2 = address2;
			Suite = suite;
			City = city;
			State = state;
			Zip = zip;
			Country = country;
			Phone = phone;
		}
	}
}
