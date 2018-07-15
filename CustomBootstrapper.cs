using System;
using System.IO;
using System.Reflection;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Responses;
using Nancy.Session;
using Nancy.ViewEngines;
using Nancy.TinyIoc;
namespace r2warsTorneo
{
	public class CustomBootstrapper : DefaultNancyBootstrapper
	{
		protected override void ConfigureApplicationContainer(TinyIoCContainer container)
		{
			base.ConfigureApplicationContainer(container);
			ResourceViewLocationProvider.RootNamespaces.Add(Assembly.GetAssembly(typeof(MainModule)), "r2warsTorneo.Views");
		}

		protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
		{
			base.ApplicationStartup(container, pipelines);
			CookieBasedSessions.Enable(pipelines);
		}

		/*protected override void ConfigureConventions(NancyConventions conventions)
		{
			base.ConfigureConventions(conventions);
            conventions.StaticContentsConventions.Add(AddStaticResourcePath("/content", Assembly.GetAssembly(typeof(MainModule)), "r2wars.Web.Views.content"));
		}*/

		protected override NancyInternalConfiguration InternalConfiguration
		{
			get
			{
				return NancyInternalConfiguration.WithOverrides(OnConfigurationBuilder);
			}
		}

		void OnConfigurationBuilder(NancyInternalConfiguration x)
		{
			x.ViewLocationProvider = typeof(ResourceViewLocationProvider);
		}
        protected override byte[] FavIcon
        {
            get { return null; }
        }
		public static Func<NancyContext, string, Response> AddStaticResourcePath(string requestedPath, Assembly assembly, string namespacePrefix)
		{
			return (context, s) =>
			       	{
			       		var path = context.Request.Path;
						if (!path.StartsWith(requestedPath))
						{
							return null;
						}

						string resourcePath;
						string name;

						var adjustedPath = path.Substring(requestedPath.Length + 1);
						if (adjustedPath.IndexOf('/') >= 0)
						{
							name = Path.GetFileName(adjustedPath);
							resourcePath = namespacePrefix + "." + adjustedPath.Substring(0, adjustedPath.Length - name.Length - 1).Replace('/', '.');
						}
						else
						{
							name = adjustedPath;
							resourcePath = namespacePrefix;
						}
						return new EmbeddedFileResponse(assembly, resourcePath, name);
			       	};
		}
	}
}