// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using AspDotNetStorefront.ClientResource;
using AspDotNetStorefrontCore.Tokens;
using Sprache;

namespace AspDotNetStorefrontCore
{
	public class Parser
	{
		readonly TokenExecutor DefaultTokenExecutor;
		readonly IClientScriptRegistry ClientScriptRegistry;

		public Parser()
		{
			DefaultTokenExecutor = DependencyResolver.Current.GetService<TokenExecutor>();
			ClientScriptRegistry = DependencyResolver.Current.GetService<IClientScriptRegistry>();
		}

		public string GetTokenValue(string tokenKey, Dictionary<string, string> parameters = null)
		{
			var customer = HttpContext.Current.GetCustomer();
			var value = DefaultTokenExecutor.GetTokenValue(customer, tokenKey, parameters);
			return value ?? string.Empty;
		}

		public string ReplaceTokens(string unparsedInput, ParserOptions options = null)
		{
			var httpContext = DependencyResolver.Current.GetService<HttpContextBase>();

			return ReplaceTokens(
				httpContext,
				httpContext.GetCustomer(),
				unparsedInput,
				options);
		}

		public string ReplaceTokens(HttpContextBase httpContext, Customer customer, string unparsedInput, ParserOptions options = null)
		{
			// Initialize default options if none are provided
			options = options ?? new ParserOptions();

			var registeredTokenHandlers = DependencyResolver.Current.GetServices<ITokenHandler>();
			var tokenExecutorFactory = DependencyResolver.Current.GetService<TokenExecutorFactory>();
			var tokenExecutor = tokenExecutorFactory(registeredTokenHandlers.Concat(options.AdditionalTokenHandlers));

			// Parse the input document and get the parsed content segments and any unparsed results back
			var parser = BuildParser();
			var parserResult = parser.TryParse(unparsedInput);

			// Evaluate and combine the content segments
			var content = new StringBuilder();
			var context = new ContentSegmentContext(tokenExecutor, ClientScriptRegistry, httpContext, httpContext.GetCustomer());
			foreach(var segment in parserResult.Value)
				content.Append(segment.GetContent(context));

			// Append any remainder of the input that failed to parse
			if(!parserResult.Remainder.AtEnd)
				content.Append(parserResult.Remainder.Source.Substring(parserResult.Remainder.Position));

			return content.ToString();
		}

		Parser<IEnumerable<IContentSegment>> BuildParser()
		{
			// This is easiest to read from the bottom (most generic) to the top (most specific).

			Parser<string> double_quoted_text =
				from open in Parse.Char('"')
				from content in Parse.CharExcept('"').Many().Text()
				from close in Parse.Char('"')
				select content;

			Parser<string> single_quoted_text =
				from open in Parse.Char('\'')
				from content in Parse.CharExcept('\'').Many().Text()
				from close in Parse.Char('\'')
				select content;

			Parser<string> quoted_text =
				double_quoted_text.Or(single_quoted_text);

			Parser<string> token_attribute_value =
				quoted_text;

			Parser<string> token_attribute_key =
				Parse.LetterOrDigit.Or(Parse.Chars('_', '-')).AtLeastOnce().Text().Token();

			Parser<KeyValuePair<string, string>> token_attribute =
				from key in token_attribute_key
				from eq in Parse.Char('=').Token()
				from value in token_attribute_value
				select new KeyValuePair<string, string>(key, value);

			Parser<string> token_open =
				from tag in Parse.String("(!").Text()
				from ws in Parse.WhiteSpace.Many()
				select tag;

			Parser<string> token_close =
				from ws in Parse.WhiteSpace.Many()
				from tag in Parse.String("!)").Text()
				select tag;

			Parser<Token> token =
				from open in token_open
				from name in Parse.LetterOrDigit.Or(Parse.Chars('_', '-')).AtLeastOnce().Text().Token().Named("token name")
				from attributes in token_attribute.Many().Named("token attributes")
				from close in token_close
				select new Token(name, attributes);

			Parser<KeyValuePair<string, string>> registered_script_attribute =
				from name in Parse.AnyChar.Except(Parse.WhiteSpace.Or(Parse.Char('=')).Or(Parse.Char('>'))).AtLeastOnce().Text().Token().Named("token attribute name")
				from eq in Parse.Char('=').Token()
				from value in quoted_text.Or(Parse.AnyChar.Except(Parse.WhiteSpace).AtLeastOnce().Text()).Token().Named("token attribute value")
				select new KeyValuePair<string, string>(name, value);

			Parser<string> script_open =
				Parse.String("<script").Text();

			Parser<string> script_close =
				Parse.String("</script>").Text();

			Parser<Literal> script_content_literal =
				from content in Parse.AnyChar.Except(token_open.Or(script_close)).AtLeastOnce().Text()
				select new Literal(content);

			Parser<IContentSegment> script_content =
				((Parser<IContentSegment>)token)
					.Or(script_content_literal);

			Parser<IContentSegment> registered_script =
				from tagOpen in script_open
				from attributes in registered_script_attribute.Many()
				from tagClose in Parse.Char('>')
				from content in script_content.Many()
				from scriptClose in script_close
				let registered = attributes
					.Where(kvp => StringComparer.OrdinalIgnoreCase.Equals(kvp.Key, "aspdnsf-registered"))
					.Select(kvp => kvp.Value)
					.LastOrDefault()
				let bundle = attributes
					.Where(kvp => StringComparer.OrdinalIgnoreCase.Equals(kvp.Key, "aspdnsf-registered-bundle"))
					.Select(kvp => kvp.Value)
					.LastOrDefault()
				let name = attributes
					.Where(kvp => StringComparer.OrdinalIgnoreCase.Equals(kvp.Key, "aspdnsf-registered-name"))
					.Select(kvp => kvp.Value)
					.LastOrDefault()
				let dependencies = attributes
					.Where(kvp => StringComparer.OrdinalIgnoreCase.Equals(kvp.Key, "aspdnsf-registered-dependencies"))
					.Select(kvp => kvp.Value.Split(','))
					.LastOrDefault()
				let source = attributes
					.Where(kvp => StringComparer.OrdinalIgnoreCase.Equals(kvp.Key, "src"))
					.Select(kvp => kvp.Value)
					.LastOrDefault()
				let async = attributes
					.Where(kvp => StringComparer.OrdinalIgnoreCase.Equals(kvp.Key, "async"))
					.Select(kvp => StringComparer.OrdinalIgnoreCase.Equals(bool.TrueString, kvp.Value))
					.LastOrDefault()
				where StringComparer.OrdinalIgnoreCase.Equals(registered, bool.TrueString)
				select string.IsNullOrWhiteSpace(source)
					? (IContentSegment)new RegisteredInlineScript(name, new NestedContentSegment(content), dependencies)
					: string.IsNullOrWhiteSpace(bundle)
						? (IContentSegment)new RegisteredScriptReference(source, async, dependencies)
						: (IContentSegment)new RegisteredBundledScript(bundle, source, dependencies);

			Parser<NestedContentSegment> unregistered_script =
				from tagOpen in script_open
				from content in script_content.Many()
				from tagClose in script_close
				select new NestedContentSegment(new IContentSegment[] { new Literal(tagOpen) }.Concat(content).Concat(new IContentSegment[] { new Literal(tagClose) }));

			Parser<Literal> literal =
				from content in Parse.AnyChar.Except(token_open.Or(script_open)).AtLeastOnce().Text()
				select new Literal(content);

			Parser<IContentSegment> content_segment =
				((Parser<IContentSegment>)token)
					.Or(registered_script)
					.Or(unregistered_script)
					.Or(literal);

			return content_segment.Many();
		}

		/// <summary>
		/// An interface to convert parsed content into a string.
		/// </summary>
		interface IContentSegment
		{
			string GetContent(ContentSegmentContext context);
		}

		/// <summary>
		/// Provides common services and data for content segments to render their content.
		/// </summary>
		class ContentSegmentContext
		{
			public TokenExecutor TokenExecutor { get; }
			public IClientScriptRegistry ClientScriptRegistry { get; }
			public HttpContextBase HttpContext { get; }
			public Customer Customer { get; }

			public ContentSegmentContext(TokenExecutor tokenExecutor, IClientScriptRegistry clientScriptRegistry, HttpContextBase httpContext, Customer customer)
			{
				TokenExecutor = tokenExecutor;
				ClientScriptRegistry = clientScriptRegistry;
				HttpContext = httpContext;
				Customer = customer;
			}
		}

		/// <summary>
		/// A content segment that contains other content segments.
		/// </summary>
		class NestedContentSegment : IContentSegment
		{
			public IEnumerable<IContentSegment> Children { get; }

			public NestedContentSegment(IEnumerable<IContentSegment> children)
			{
				Children = children ?? Enumerable.Empty<IContentSegment>();
			}

			public string GetContent(ContentSegmentContext context)
			{
				return string.Concat(
					Children.Select(child => child.GetContent(context)));
			}
		}

		/// <summary>
		/// A token to be executed by a <see cref="TokenExecutor"/> .
		/// </summary>
		class Token : IContentSegment
		{
			public string Name { get; }
			public IDictionary<string, string> Parameters { get; }

			public Token(string name, IEnumerable<KeyValuePair<string, string>> parameters)
			{
				Name = name;
				Parameters = parameters.ToDictionary(
					attribute => attribute.Key,
					attribute => attribute.Value);
			}

			public string GetContent(ContentSegmentContext context)
			{
				return context
					.TokenExecutor
					.GetTokenValue(
						context.Customer,
						Name,
						Parameters);
			}
		}

		/// <summary>
		/// An inline script to be registered with an <see cref="IClientScriptRegistry"/>.
		/// </summary>
		class RegisteredInlineScript : IContentSegment
		{
			public string Name { get; }
			public NestedContentSegment Content { get; }
			public IEnumerable<string> Dependencies { get; }

			public RegisteredInlineScript(string name, NestedContentSegment content, IEnumerable<string> dependencies)
			{
				Name = name;
				Content = content;
				Dependencies = dependencies;
			}

			public string GetContent(ContentSegmentContext context)
			{
				return context
					.ClientScriptRegistry
					.RegisterInlineScript(
						context.HttpContext,
						Content.GetContent(context),
						Name,
						addScriptTag: true,
						dependencies: Dependencies);
			}
		}

		/// <summary>
		/// A local script reference to be registered with an <see cref="IClientScriptRegistry"/>.
		/// </summary>
		class RegisteredBundledScript : IContentSegment
		{
			public string BundleUrl { get; }
			public string Url { get; }
			public IEnumerable<string> Dependencies { get; }

			public RegisteredBundledScript(string bundleUrl, string url, IEnumerable<string> dependencies)
			{
				BundleUrl = bundleUrl;
				Url = url;
				Dependencies = dependencies;
			}

			public string GetContent(ContentSegmentContext context)
			{
				return context
					.ClientScriptRegistry
					.RegisterScriptBundle(
						context.HttpContext,
						BundleUrl,
						new[] { Url },
						Dependencies);
			}
		}

		/// <summary>
		/// A script reference to be registered with an <see cref="IClientScriptRegistry"/>.
		/// </summary>
		class RegisteredScriptReference : IContentSegment
		{
			public string Url { get; }
			public bool Async { get; }
			public IEnumerable<string> Dependencies { get; }

			public RegisteredScriptReference(string url, bool async, IEnumerable<string> dependencies)
			{
				Url = url;
				Async = async;
				Dependencies = dependencies;
			}

			public string GetContent(ContentSegmentContext context)
			{
				return context
					.ClientScriptRegistry
					.RegisterScriptReference(
						context.HttpContext,
						Url,
						Async,
						Dependencies);
			}
		}

		/// <summary>
		/// A block of text to be rendered as-is.
		/// </summary>
		class Literal : IContentSegment
		{
			public string Content { get; }

			public Literal(string content)
			{
				Content = content;
			}

			public string GetContent(ContentSegmentContext context)
			{
				return Content;
			}
		}
	}

	public class ParserOptions
	{
		public IEnumerable<ITokenHandler> AdditionalTokenHandlers { get; }

		public ParserOptions(IEnumerable<ITokenHandler> additionalTokenHandlers = null)
		{
			AdditionalTokenHandlers = additionalTokenHandlers ?? Enumerable.Empty<ITokenHandler>();
		}
	}
}
