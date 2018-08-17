// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;

namespace AspDotNetStorefront.Addon.Api
{
	public interface IResult
	{
		bool IsSuccess { get; }
	}

	public interface ISuccess : IResult
	{ }

	public interface IFailure : IResult
	{
		Error Error { get; }
	}

	public interface IResult<out TValue> : IResult
	{
		TValue Value { get; }
	}

	public interface ISuccess<TValue> : IResult<TValue>, ISuccess
	{ }

	public interface IFailure<TValue> : IResult<TValue>, IFailure
	{ }

	public static class Result
	{
		class Success : ISuccess
		{
			public bool IsSuccess
				=> true;

			public void Deconstruct(out bool isSuccess)
			{
				isSuccess = IsSuccess;
			}
		}

		class Success<TValue> : ISuccess<TValue>
		{
			public bool IsSuccess
				=> true;

			public TValue Value { get; }

			public Success(TValue value)
			{
				Value = value;
			}

			public void Deconstruct(out bool isSuccess)
			{
				isSuccess = IsSuccess;
			}

			public void Deconstruct(out bool isSuccess, out TValue value)
			{
				isSuccess = IsSuccess;
				value = Value;
			}
		}

		class Failure : IFailure
		{
			public bool IsSuccess
				=> false;

			public Error Error { get; }

			public Failure(Error error)
			{
				Error = error;
			}

			public void Deconstruct(out bool isSuccess)
			{
				isSuccess = IsSuccess;
			}

			public void Deconstruct(out bool isSuccess, out Error error)
			{
				isSuccess = IsSuccess;
				error = Error;
			}
		}

		class Failure<TValue> : IFailure<TValue>
		{
			public bool IsSuccess
				=> false;

			public TValue Value
				=> default(TValue);

			public Error Error { get; }

			public Failure(Error error)
			{
				Error = error;
			}

			public void Deconstruct(out bool isSuccess)
			{
				isSuccess = IsSuccess;
			}

			public void Deconstruct(out bool isSuccess, out TValue value)
			{
				isSuccess = IsSuccess;
				value = Value;
			}

			public void Deconstruct(out bool isSuccess, out Error error)
			{
				isSuccess = IsSuccess;
				error = Error;
			}
		}

		public static IResult Ok()
			=> new Success();

		public static IResult<TValue> Ok<TValue>(TValue value)
			=> new Success<TValue>(value);

		public static IResult Error(Error error)
			=> new Failure(error);

		public static IResult<TValue> Error<TValue>(Error error)
			=> new Failure<TValue>(error);

		public static IResult<TValue> Error<TValue>(IFailure failure)
			=> Error<TValue>(failure.Error);
	}
}
