using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace NET_Proxy
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            //Ignore invalid ssl certificates
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(delegate { return true; }); ;
            GlobalConfiguration.Configure(CustomHttpProxy.Register);
        }
    }

    public static class CustomHttpProxy
    {
        //Register endpoint
        public static void Register(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute(
                name: "Proxy",
                routeTemplate: "{*path}",
                handler: HttpClientFactory.CreatePipeline(
                    innerHandler: new HttpClientHandler(),
                    handlers: new DelegatingHandler[]
                    {
                    new ProxyHandler()
                    }
                ),
                defaults: new { path = RouteParameter.Optional },
                constraints: null
            );
        }
    }

    public class ProxyHandler : DelegatingHandler
    {
        private static HttpClient client = new HttpClient();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            //Capture origin uri with query parameters and activate https on default port
            var forwardUri = new UriBuilder(request.RequestUri.AbsoluteUri)
            {
                Scheme = Uri.UriSchemeHttps,
                Port = -1
            };

            //Set remote uri
            forwardUri.Host = ConfigurationManager.AppSettings["SG_ENDPOINT"];
            request.RequestUri = forwardUri.Uri;

            if (request.Method == HttpMethod.Get)
            {
                request.Content = null;
            }

            //Set header with remote ur
            request.Headers.Add("X-Forwarded-Host", request.Headers.Host);
            request.Headers.Host = "gcs.softguard.com";
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            return response;
        }
    }
}
