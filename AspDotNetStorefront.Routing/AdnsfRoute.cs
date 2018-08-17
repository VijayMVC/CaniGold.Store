// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;
using System.Web.Routing;

namespace AspDotNetStorefront.Routing
{
	public class AdnsfRoute : Route
	{
		static readonly ReflectionCacheContainer ReflectionCache = ReflectionCacheContainer.Create();

		readonly IEnumerable<RouteDataValueTransform> RouteDataValueTransforms;
		readonly IEnumerable<string> QueryStringRouteDataValues;

		public AdnsfRoute(string url, RouteValueDictionary defaults = null, RouteValueDictionary constraints = null, RouteValueDictionary dataTokens = null, IRouteHandler routeHandler = null, IEnumerable<RouteDataValueTransform> routeDataValueTransforms = null, IEnumerable<string> queryStringRouteDataValues = null)
			: base(url, defaults, constraints, dataTokens, routeHandler)
		{
			RouteDataValueTransforms = routeDataValueTransforms ?? Enumerable.Empty<RouteDataValueTransform>();
			QueryStringRouteDataValues = queryStringRouteDataValues ?? Enumerable.Empty<string>();
		}

		public override RouteData GetRouteData(HttpContextBase httpContext)
		{
			// Invoke the internal RouteParser class via reflection to create a ParsedRoute instance from the route's URL template.
			var parsedRouteProxy = ReflectionCache.Parse(Url);

			// Build the full requested path
			var requestedVirtualPath = string.Format(
				"{0}{1}",
				httpContext.Request.AppRelativeCurrentExecutionFilePath.Substring(2),
				httpContext.Request.PathInfo);

			// Parse the requested url into a RouteValueDictionary, including the route's defaults
			var routeDataValues = parsedRouteProxy.Match(requestedVirtualPath, Defaults);
			if(routeDataValues == null)
				return null;

			// Apply customizaed ADNSF behavior
			ApplyQueryStringToRouteDataValues(httpContext.Request.QueryString, routeDataValues);
			ApplyRouteDataValueTransformations(routeDataValues, RouteDirection.IncomingRequest);

			// Validate all constraints are met
			if(Constraints != null)
				foreach(var constraint in Constraints)
				{
					var constraintResult = ProcessConstraint(httpContext, constraint.Value, constraint.Key, routeDataValues, RouteDirection.IncomingRequest);
					if(!constraintResult)
						return null;
				}

			// Prepare the RouteData object to return
			var routeData = new RouteData(this, RouteHandler);

			// Copy in the route data values, including the query string includes and transformations
			foreach(var routeDataEntry in routeDataValues)
				routeData.Values.Add(routeDataEntry.Key, routeDataEntry.Value);

			// Copy in any data tokens
			if(DataTokens != null)
				foreach(var dataToken in DataTokens)
					routeData.DataTokens.Add(dataToken.Key, dataToken.Value);

			return routeData;
		}

		public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
		{
			// We need to modify the route data values for purposes of generating a URL, but we don't want to modify the collection
			// that's passed in. We'll clone it and modify the clone.
			var routeDataValues = new RouteValueDictionary(values);

			// Apply customizaed ADNSF behavior
			ApplyQueryStringToRouteDataValues(requestContext.HttpContext.Request.QueryString, routeDataValues);
			ApplyRouteDataValueTransformations(routeDataValues, RouteDirection.UrlGeneration);

			// Invoke the internal RouteParser class via reflection to create a ParsedRoute instance from the route's URL template.
			var parsedRouteProxy = ReflectionCache.Parse(Url);

			// Invoke the internal Bind method to create a URL and new route data values from the provided route data.
			var boundUrlProxy = parsedRouteProxy.Bind(requestContext.RouteData.Values, routeDataValues, Defaults, Constraints);
			if(boundUrlProxy == null)
				return null;

			// Run the constraints against the new route data values
			if(Constraints != null)
				foreach(var constraint in Constraints)
				{
					var constraintResult = ProcessConstraint(requestContext.HttpContext, constraint.Value, constraint.Key, boundUrlProxy.Values, RouteDirection.UrlGeneration);
					if(!constraintResult)
						return null;
				}

			// Construct the return value from the bound URL and this route
			var virtualPathData = new VirtualPathData(this, boundUrlProxy.Url);

			// Copy in any data tokens
			if(DataTokens != null)
				foreach(var dataToken in DataTokens)
					virtualPathData.DataTokens[dataToken.Key] = dataToken.Value;

			return virtualPathData;
		}

		void ApplyQueryStringToRouteDataValues(NameValueCollection queryString, RouteValueDictionary routeDataValues)
		{
			// Pull in any query string values that match the QueryStringRouteDataValues collection.
			// Don't overwrite any existing route data values, though.
			var queryStringPairsToApply = queryString
				.Keys
				.Cast<string>()
				.Intersect(QueryStringRouteDataValues, StringComparer.OrdinalIgnoreCase)
				.Where(key => !routeDataValues.ContainsKey(key));

			foreach(var queryKey in queryStringPairsToApply)
				routeDataValues[queryKey] = queryString[queryKey];
		}

		void ApplyRouteDataValueTransformations(RouteValueDictionary routeDataValues, RouteDirection direction)
		{
			// Apply the transformations of the route values based on the key
			foreach(var transformer in RouteDataValueTransforms)
				transformer(routeDataValues, direction);
		}

		#region Reflected Proxy Classes

		/*
			==== DO NOT MODIFY THESE CLASSES ====
			These proxy classes exist only to expose internal .NET framework classes via reflection so we can use the
			same API's that the .NET framework's Route class uses. They are not intended as extension points to
			customize ADNSF functionality.

			All of these classes follow the same pattern: they wrap a reflected instance of the proxied type and invoke
			expose a subset of the members of that type, which are themselves invoked via reflection.
		*/

		// Adapted from https://github.com/Whathecode/Framework-Class-Library-Extension/blob/master/Whathecode.System/DelegateHelper.cs
		// MIT licensed
		public static TDelegate CreateOpenInstanceDelegate<TDelegate>(MethodInfo methodInfo)
			where TDelegate : class
		{
			var delegateInfo = typeof(TDelegate).GetMethod("Invoke");
			var delegateParameters = delegateInfo.GetParameters();

			// Convert instance type when necessary.
			var delegateInstanceType = delegateParameters
				.Select(p => p.ParameterType)
				.First();

			var instanceParameter = Expression.Parameter(delegateInstanceType);
			var convertedInstanceParameter = ConvertExpression(instanceParameter, methodInfo.DeclaringType);

			var originalParameterExpressions = delegateParameters
				.Select(d => d.ParameterType)
				.Skip(1)
				.Select(Expression.Parameter)
				.ToArray();

			var methodTypes = methodInfo
				.GetParameters()
				.Select(m => m.ParameterType);

			// Convert the parameters from the delegate parameter type to the required type when necessary.
			var convertedParameterExpressions = originalParameterExpressions.Zip(methodTypes, ConvertExpression);

			// Create method call.
			var methodCall = Expression.Call(
				convertedInstanceParameter,
				methodInfo,
				convertedParameterExpressions);

			return Expression.Lambda<TDelegate>(
					ConvertExpression(methodCall, delegateInfo.ReturnType), // Convert return type when necessary.
					new[] { instanceParameter }.Concat(originalParameterExpressions))
				.Compile();
		}

		static Expression ConvertExpression(Expression expression, Type toType)
			=> toType == expression.Type
				? expression
				: Expression.Convert(expression, toType);

		/// <summary>
		/// Caches expensive reflection calls
		/// </summary>
		public class ReflectionCacheContainer
		{
			public delegate object ParseDelegate(string routeUrl);

			public static ReflectionCacheContainer Create()
			{
				var routeParserType = Type.GetType("System.Web.Routing.RouteParser, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

				return new ReflectionCacheContainer(
					parseProxy: (ParseDelegate)routeParserType
						.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static)
						.CreateDelegate(typeof(ParseDelegate)));
			}

			public ParseDelegate ParseProxy { get; }
			public ConcurrentDictionary<Type, ParsedRouteReflectedProxy.ReflectionCache> ParsedRoutes { get; }
			public ConcurrentDictionary<Type, BoundUrlReflectedProxy.ReflectionCache> BoundUrls { get; }

			ReflectionCacheContainer(ParseDelegate parseProxy)
			{
				ParseProxy = parseProxy;
				ParsedRoutes = new ConcurrentDictionary<Type, ParsedRouteReflectedProxy.ReflectionCache>();
				BoundUrls = new ConcurrentDictionary<Type, BoundUrlReflectedProxy.ReflectionCache>();
			}

			public ParsedRouteReflectedProxy Parse(string routeUrl)
			{
				var parsedRoute = ParseProxy(routeUrl);
				if(parsedRoute == null)
					return null;

				var parsedRouteType = parsedRoute.GetType();

				ParsedRouteReflectedProxy.ReflectionCache parsedRouteReflectionCache;
				if(!ParsedRoutes.TryGetValue(parsedRouteType, out parsedRouteReflectionCache))
				{
					parsedRouteReflectionCache = ParsedRouteReflectedProxy.ReflectionCache.Create(parsedRouteType);
					ParsedRoutes.TryAdd(parsedRouteType, parsedRouteReflectionCache);
				}

				return new ParsedRouteReflectedProxy(
					instance: parsedRoute,
					reflection: parsedRouteReflectionCache,
					boundUrls: BoundUrls);
			}
		}

		/// <summary>
		/// Proxies the ParsedRoute class to support <see cref="AdnsfRoute"/>. DO NOT MODIFY.
		/// </summary>
		public class ParsedRouteReflectedProxy
		{
			public delegate RouteValueDictionary MatchDelegate(object instance, string virtualPath, RouteValueDictionary defaultValues);
			public delegate object BindDelegate(object instance, RouteValueDictionary currentValues, RouteValueDictionary values, RouteValueDictionary defaultValues, RouteValueDictionary constraints);

			readonly object Instance;
			readonly ReflectionCache Reflection;
			readonly ConcurrentDictionary<Type, BoundUrlReflectedProxy.ReflectionCache> BoundUrls;

			public ParsedRouteReflectedProxy(object instance, ReflectionCache reflection, ConcurrentDictionary<Type, BoundUrlReflectedProxy.ReflectionCache> boundUrls)
			{
				Instance = instance;
				Reflection = reflection;
				BoundUrls = boundUrls;
			}

			public RouteValueDictionary Match(string virtualPath, RouteValueDictionary defaultValues)
				=> Reflection.MatchProxy(Instance, virtualPath, defaultValues);

			public BoundUrlReflectedProxy Bind(RouteValueDictionary currentValues, RouteValueDictionary values, RouteValueDictionary defaultValues, RouteValueDictionary constraints)
			{
				var boundUrl = Reflection.BindProxy(Instance, currentValues, values, defaultValues, constraints);
				if(boundUrl == null)
					return null;

				var boundUrlType = boundUrl.GetType();

				BoundUrlReflectedProxy.ReflectionCache boundUrlReflectionCache;
				if(!BoundUrls.TryGetValue(boundUrlType, out boundUrlReflectionCache))
				{
					boundUrlReflectionCache = BoundUrlReflectedProxy.ReflectionCache.Create(boundUrlType);
					BoundUrls.TryAdd(boundUrlType, boundUrlReflectionCache);
				}

				return new BoundUrlReflectedProxy(
					instance: boundUrl,
					reflection: boundUrlReflectionCache);
			}

			public class ReflectionCache
			{
				public static ReflectionCache Create(Type parsedRouteType)
					=> new ReflectionCache(
						match: CreateOpenInstanceDelegate<MatchDelegate>(parsedRouteType.GetMethod("Match", BindingFlags.Public | BindingFlags.Instance)),
						bind: CreateOpenInstanceDelegate<BindDelegate>(parsedRouteType.GetMethod("Bind", BindingFlags.Public | BindingFlags.Instance)));

				public readonly MatchDelegate MatchProxy;
				public readonly BindDelegate BindProxy;

				ReflectionCache(MatchDelegate match, BindDelegate bind)
				{
					MatchProxy = match;
					BindProxy = bind;
				}
			}
		}

		/// <summary>
		/// Proxies the BoundUrl class to support <see cref="AdnsfRoute"/>. DO NOT MODIFY.
		/// </summary>
		public class BoundUrlReflectedProxy
		{
			public delegate string StringPropertyGetDelegate(object instance);
			public delegate RouteValueDictionary RouteValueDictionaryPropertyGetDelegate(object instance);

			public string Url
				=> Reflection.UrlProxy(Instance);

			public RouteValueDictionary Values
				=> Reflection.ValuesProxy(Instance);

			readonly object Instance;
			readonly ReflectionCache Reflection;

			public BoundUrlReflectedProxy(object instance, ReflectionCache reflection)
			{
				Instance = instance;
				Reflection = reflection;
			}

			public class ReflectionCache
			{
				public static ReflectionCache Create(Type boundUrlType)
					=> new ReflectionCache(
						url: CreateOpenInstanceDelegate<StringPropertyGetDelegate>(boundUrlType.GetProperty("Url").GetMethod),
						values: CreateOpenInstanceDelegate<RouteValueDictionaryPropertyGetDelegate>(boundUrlType.GetProperty("Values").GetMethod));

				public readonly StringPropertyGetDelegate UrlProxy;
				public readonly RouteValueDictionaryPropertyGetDelegate ValuesProxy;

				public ReflectionCache(StringPropertyGetDelegate url, RouteValueDictionaryPropertyGetDelegate values)
				{
					UrlProxy = url;
					ValuesProxy = values;
				}
			}
		}

		#endregion
	}
}
