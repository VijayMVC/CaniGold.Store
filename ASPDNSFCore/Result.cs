// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;

namespace AspDotNetStorefrontCore
{
	public class Result
	{
		public static Result Ok()
			=> new Result(true, null);

		public static Result Fail(Exception error)
			=> new Result(false, error);

		public static Result<T> Ok<T>(T value)
			=> new Result<T>(true, value, null);

		public static Result<T> Fail<T>(T value, Exception error)
			=> new Result<T>(false, value, error);

		public static Result<T> Fail<T>(Exception error)
			=> new Result<T>(false, default(T), error);

		public bool Success { get; }
		public Exception Error { get; }

		public Result(bool success, Exception error)
		{
			Success = success;
			Error = error;
		}
	}

	public class Result<T> : Result
	{
		public T Value { get; }

		public Result(bool success, T value, Exception error)
			: base(success, error)
		{
			Value = value;
		}
	}
}
