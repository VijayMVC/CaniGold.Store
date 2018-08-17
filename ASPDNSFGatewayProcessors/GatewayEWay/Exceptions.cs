// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Runtime.Serialization;

namespace GatewayEWay
{
	public class Exceptions
	{
		[Serializable]
		public class UnknownErrorFailure : Exception
		{
			public UnknownErrorFailure() { }
			public UnknownErrorFailure(string message) : base(message) { }
			public UnknownErrorFailure(string message, Exception inner) : base(message, inner) { }
			protected UnknownErrorFailure
			(
			  SerializationInfo info,
			  StreamingContext context
			) : base(info, context) { }
		}

		[Serializable]
		public class MissingConfigurationFailure : Exception
		{
			public MissingConfigurationFailure() { }
			public MissingConfigurationFailure(string message) : base(message) { }
			public MissingConfigurationFailure(string message, Exception inner) : base(message, inner) { }
			protected MissingConfigurationFailure
			(
			  SerializationInfo info,
			  StreamingContext context
			) : base(info, context) { }
		}

		[Serializable]
		public class GatewayReportedFailure : Exception
		{
			public GatewayReportedFailure() { }
			public GatewayReportedFailure(string message) : base(message) { }
			public GatewayReportedFailure(string message, Exception inner) : base(message, inner) { }
			protected GatewayReportedFailure
			(
			  SerializationInfo info,
			  StreamingContext context
			) : base(info, context) { }
		}
	}
}
