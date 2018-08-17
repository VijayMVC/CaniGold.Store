// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace AspDotNetStorefrontCore
{
	public class CardType
	{
		#region Variable Declaration

		private int[] _validLengths;
		private string[] _validPrefixes;

		#endregion

		#region Constructor

		private CardType(string[] validPrefixes, int[] validLengths)
		{
			_validPrefixes = validPrefixes;
			_validLengths = validLengths;
		}

		#endregion

		#region Properties

		public int[] ValidLengths
		{
			get { return _validLengths; }
		}

		public string[] ValidPrefixes
		{
			get { return _validPrefixes; }
		}

		#endregion

		public static readonly CardType Visa = new CardType(new string[] { "4" }, new int[] { 13, 16 });
		public static readonly CardType MasterCard = new CardType(new string[] { "51", "52", "53", "54", "55", "22", "23", "24", "25", "26", "27" }, new int[] { 16 });
		public static readonly CardType Amex = new CardType(new string[] { "34", "37" }, new int[] { 15 });
		public static readonly CardType DinersClub = new CardType(new string[] { "300", "301", "302", "303", "304", "305", "36", "38" }, new int[] { 14 });
		public static readonly CardType Discover = new CardType(new string[] { "6011", "65", "644", "645", "646", "647", "648", "649", "622" }, new int[] { 16 });
		public static readonly CardType EnRoute = new CardType(new string[] { "2014", "2149" }, new int[] { 15 });
		public static readonly CardType JCB = new CardType(new string[] { "35", "2131", "1800" }, new int[] { 15, 16 });

		public static CardType Parse(string s)
		{
			if(s.StartsWith("Visa", StringComparison.InvariantCultureIgnoreCase))
			{
				return CardType.Visa;
			}
			if(s.StartsWith("MasterCard", StringComparison.InvariantCultureIgnoreCase))
			{
				return CardType.MasterCard;
			}
			if(s.StartsWith("Ame", StringComparison.InvariantCultureIgnoreCase))
			{
				return CardType.Amex;
			}
			if(s.StartsWith("Diners", StringComparison.InvariantCultureIgnoreCase))
			{
				return CardType.DinersClub;
			}
			if(s.StartsWith("Diners", StringComparison.InvariantCultureIgnoreCase))
			{
				return CardType.DinersClub;
			}
			if(s.StartsWith("Discover", StringComparison.InvariantCultureIgnoreCase))
			{
				return CardType.Discover;
			}
			if(s.StartsWith("EnRoute", StringComparison.InvariantCultureIgnoreCase))
			{
				return CardType.EnRoute;
			}
			if(s.StartsWith("JCB", StringComparison.InvariantCultureIgnoreCase))
			{
				return CardType.JCB;
			}

			return null;
		}

		public static CardType ParseFromNumber(string number)
		{
			var validationExpression = new CreditCardValidationExpression();
			return new[]
				{
					Amex,
					Visa,
					MasterCard,
					Discover
				}
				.Where(card => Regex.IsMatch(number, validationExpression.GetExpression(card)))
				.FirstOrDefault();
		}
	}
}
