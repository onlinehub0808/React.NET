/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;
using System.Linq;
using System.Text;






#if LEGACYASPNET
using System.Web;
using IHtmlHelper = System.Web.Mvc.HtmlHelper;
using IUrlHelper = System.Web.Mvc.UrlHelper;
#else
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Html;
using IHtmlString = Microsoft.AspNetCore.Html.IHtmlContent;
using Microsoft.AspNetCore.Mvc;
#endif

#if LEGACYASPNET
namespace React.Web.Mvc
#else
namespace React.AspNet
#endif
{
	/// <summary>
	/// HTML Helpers for utilising React from an ASP.NET MVC application.
	/// </summary>
	public static class HtmlHelperExtensions
	{
		[ThreadStatic]
		private static StringWriter _sharedStringWriter;

		/// <summary>
		/// Gets the React environment
		/// </summary>
		private static IReactEnvironment Environment
		{
			get
			{
				return ReactEnvironment.GetCurrentOrThrow;
			}
		}
		public static IHtmlString React<T>(
			this IHtmlHelper htmlHelper,
			string componentName,
			T props,
			string htmlTag = null,
			string containerId = null,
			bool clientOnly = false,
			bool serverOnly = false,
			string containerClass = null,
			Action<Exception, string, string> exceptionHandler = null,
			IRenderFunctions renderFunctions = null
		)
		{
			try
			{
				var reactComponent = Environment.CreateComponent(componentName, props, containerId, clientOnly, serverOnly);
				if (!string.IsNullOrEmpty(htmlTag))
				{
					reactComponent.ContainerTag = htmlTag;
				}

				if (!string.IsNullOrEmpty(containerClass))
				{
					reactComponent.ContainerClass = containerClass;
				}

				return RenderToString(writer => reactComponent.RenderHtml(writer, clientOnly, serverOnly, exceptionHandler, renderFunctions));
			}
			finally
			{
				Environment.ReturnEngineToPool();
			}
		}

		public static IHtmlString ReactWithInit<T>(
			this IHtmlHelper htmlHelper,
			string componentName,
			T props,
			string htmlTag = null,
			string containerId = null,
			bool clientOnly = false,
			bool serverOnly = false,
			string containerClass = null,
			Action<Exception, string, string> exceptionHandler = null,
			IRenderFunctions renderFunctions = null
		)
		{
			try
			{
				var reactComponent = Environment.CreateComponent(componentName, props, containerId, clientOnly);
				if (!string.IsNullOrEmpty(htmlTag))
				{
					reactComponent.ContainerTag = htmlTag;
				}

				if (!string.IsNullOrEmpty(containerClass))
				{
					reactComponent.ContainerClass = containerClass;
				}

				return RenderToString(writer =>
				{
					reactComponent.RenderHtml(writer, clientOnly, serverOnly, exceptionHandler: exceptionHandler, renderFunctions);
					writer.WriteLine();
					WriteScriptTag(writer, bodyWriter => reactComponent.RenderJavaScript(bodyWriter, waitForDOMContentLoad: true));
				});

			}
			finally
			{
				Environment.ReturnEngineToPool();
			}
		}
		public static IHtmlString ReactInitJavaScript(this IHtmlHelper htmlHelper, bool clientOnly = false)
		{
			try
			{
				return RenderToString(writer =>
				{
					WriteScriptTag(writer, bodyWriter => Environment.GetInitJavaScript(bodyWriter, clientOnly));
				});
			}
			finally
			{
				Environment.ReturnEngineToPool();
			}
		}
		public static IHtmlString ReactGetScriptPaths(this IHtmlHelper htmlHelper, IUrlHelper urlHelper = null)
		{
			string nonce = Environment.Configuration.ScriptNonceProvider != null
				? $" nonce=\"{Environment.Configuration.ScriptNonceProvider()}\""
				: "";

			return new HtmlString(string.Join("", Environment.GetScriptPaths()
				.Select(scriptPath => $"<script{nonce} src=\"{(urlHelper == null ? scriptPath : urlHelper.Content(scriptPath))}\"></script>")));
		}

		/// <summary>
		/// Returns style tags based on the webpack asset manifest
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="urlHelper">Optional IUrlHelper instance. Enables the use of tilde/relative (~/) paths inside the expose-components.js file.</param>
		/// <returns></returns>
		public static IHtmlString ReactGetStylePaths(this IHtmlHelper htmlHelper, IUrlHelper urlHelper = null)
		{
			return new HtmlString(string.Join("", Environment.GetStylePaths()
				.Select(stylePath => $"<link rel=\"stylesheet\" href=\"{(urlHelper == null ? stylePath : urlHelper.Content(stylePath))}\" />")));
		}

		private static IHtmlString RenderToString(Action<StringWriter> withWriter)
		{
			var stringWriter = _sharedStringWriter;
			if (stringWriter != null)
			{
				stringWriter.GetStringBuilder().Clear();
			}
			else
			{
				_sharedStringWriter = stringWriter = new StringWriter(new StringBuilder(128));
			}

			withWriter(stringWriter);
			return new HtmlString(stringWriter.ToString());
		}

		private static void WriteScriptTag(TextWriter writer, Action<TextWriter> bodyWriter)
		{
			writer.Write("<script");
			if (Environment.Configuration.ScriptNonceProvider != null)
			{
				writer.Write(" nonce=\"");
				writer.Write(Environment.Configuration.ScriptNonceProvider());
				writer.Write("\"");
			}

			writer.Write(">");

			bodyWriter(writer);

			writer.Write("</script>");
		}
	}
}
