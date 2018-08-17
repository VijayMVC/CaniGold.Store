// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Web.Mvc;

namespace AspDotNetStorefront.Models
{
	public class AcceptJsViewModel
	{
		public string ClientKey { get; }
		public string ApiLoginId { get; }
		public string ScriptUrlHostedForm { get; }
		public string ScriptUrlOwnForm { get; }

		public AcceptJsViewModel(
			string clientKey,
			string apiLoginId,
			string scriptUrlHostedForm,
			string scriptUrlOwnForm)
		{
			ClientKey = clientKey;
			ApiLoginId = apiLoginId;
			ScriptUrlHostedForm = scriptUrlHostedForm;
			ScriptUrlOwnForm = scriptUrlOwnForm;
		}
	}

	public class AcceptJsCreditCardViewModel
	{
		public string LastFour { get; set;  }
		public string DataValue { get; set; }
		public string DataDescriptor { get; set; }
		public string ExpirationDate { get; set; }
	}

	public class AcceptJsEcheckViewModel
	{
		public AcceptJsViewModel AcceptJsViewModel { get; }
		public ECheckViewModel ECheckViewModel { get; }
		public CheckoutECheckViewModel CheckoutECheckViewModel { get; }
		public string DataValue { get; set; }
		public string DataDescriptor { get; set; }
		public SelectList AccountTypes { get; }
		public AcceptJsEcheckViewModel(
			AcceptJsViewModel acceptJsViewModel,
			ECheckViewModel eCheckViewModel,
			CheckoutECheckViewModel checkoutECheckViewModel,
			SelectList accountTypes)
		{
			AcceptJsViewModel = acceptJsViewModel;
			ECheckViewModel = eCheckViewModel;
			CheckoutECheckViewModel = checkoutECheckViewModel;
			AccountTypes = accountTypes;
}
	}

	public class AcceptJsEcheckPostModel
	{
		public string DataValue { get; set; }
		public string DataDescriptor { get; set; }
		public string ECheckDisplayAccountNumberLastFour { get; set; }
		public string ECheckDisplayAccountType { get; set; }
	}
}
