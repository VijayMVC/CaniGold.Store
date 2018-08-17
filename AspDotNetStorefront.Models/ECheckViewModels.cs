// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.ComponentModel.DataAnnotations;

namespace AspDotNetStorefront.Models
{
	/// <summary>
	/// Support for EChecks.
	/// </summary>
	public class ECheckViewModel
	{
		/// <summary>
		/// The bank account number. 17 digit(max) numeric string.
		/// </summary>
		[Display(Name = "echeck.accountnumber.label", Prompt = "echeck.accountnumber.example")]
		[Required(ErrorMessage = "echeck.accountnumber.required")]
		[StringLength(17, ErrorMessage = "echeck.accountnumber.length")]
		public string AccountNumber { get; set; }

		/// <summary>
		/// The ABA routing number. 9 digit(max) numeric string.
		/// </summary>
		[Display(Name = "echeck.routingnumber.label", Prompt = "echeck.routingnumber.example")]
		[Required(ErrorMessage = "echeck.routingnumber.required")]
		[StringLength(9, ErrorMessage = "echeck.routingnumber.length")]
		public string RoutingNumber { get; set; }

		/// <summary>
		/// The name of the person who holds the bank account. 22 char max.
		/// </summary>
		[Display(Name = "echeck.nameonaccount.label", Prompt = "echeck.nameonaccount.example")]
		[Required(ErrorMessage = "echeck.nameonaccount.required")]
		[StringLength(22, ErrorMessage = "echeck.nameonaccount.length")]
		public string NameOnAccount { get; set; }

		/// <summary>
		/// The type of bank account used for the eCheck.Net transaction.
		/// Valid values are: checking, savings, or businessChecking.
		/// </summary>
		[Display(Name = "echeck.accounttype.label", Prompt = "echeck.accounttype.example")]
		[Required(ErrorMessage = "echeck.accounttype.required")]
		[StringLength(40)]
		public string AccountType { get; set; }
	}

	public class CheckoutECheckViewModel
	{
		[StringLength(4)]
		public string ECheckDisplayAccountNumberLastFour { get; set; }
		[StringLength(40)]
		public string ECheckDisplayAccountType { get; set; }

		public CheckoutECheckViewModel(
			string eCheckDisplayAccountNumberLastFour,
			string eCheckDisplayAccountType)
		{
			ECheckDisplayAccountNumberLastFour = eCheckDisplayAccountNumberLastFour;
			ECheckDisplayAccountType = eCheckDisplayAccountType;
		}
	}
}
