using System;
using System.Configuration;
using System.Linq;
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
            forwardUri.Port = Convert.ToInt32(ConfigurationManager.AppSettings["SG_PORT"]);
            forwardUri.Scheme = ConfigurationManager.AppSettings["SG_SCHEME"];
            request.RequestUri = forwardUri.Uri;

            //Change http method
            var contentType = string.Empty;
            if (request.Headers.Contains("Access-Control-Request-Method"))
            {
                request.Method = new HttpMethod(request.Headers.GetValues("Access-Control-Request-Method").First());
                request.Headers.Remove("Access-Control-Request-Method");
                contentType = request.Headers.GetValues("Access-Control-Request-Headers").First();
                request.Headers.Remove("Access-Control-Request-Headers");
            }

            //Save origin to include it in the response
            var origin = string.Empty;
            if (request.Headers.Contains("Origin"))
            {
                origin = request.Headers.GetValues("Origin").First();
                request.Headers.Remove("Origin");
                request.Headers.Remove("Referer");
            }

            if (request.Method == HttpMethod.Get)
            {
                request.Content = null;
            }

            //Set header with remote url
            request.Headers.Add("X-Forwarded-Host", request.Headers.Host);
            request.Headers.Host = ConfigurationManager.AppSettings["SG_HOST"];
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!string.IsNullOrEmpty(origin))
            {
                response.Headers.Add("Access-Control-Allow-Origin", origin);
            }

            if (!string.IsNullOrEmpty(contentType))
            {
                response.Headers.Add("Access-Control-Allow-Headers", contentType);
            }
            
            return response;
        }
    }
}
