
using AzureDocsUpdatesFnApp.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AzureDocsUpdatesFnApp
{
    public static class EmailNotificationFn
    {
        private static IReadOnlyDictionary<string, string> productMap;

        /*
         * Assumes input JSON that looks like the following:
         * 
            [
                {
                    "items": [
                        {
                            "Title": "Managed Service Identity in App Service and Azure Functions",
                            "Date": "2018-04-26T",
                            "Url": "MicrosoftDocs/azure-docs/contents/articles/app-service/app-service-managed-service-identity.md"
                        },
                        {
                            "Title": "Build a .NET Core and SQL Database web app in Azure App Service",
                            "Date": "2018-04-26T",
                            "Url": "MicrosoftDocs/azure-docs/contents/articles/app-service/app-service-web-tutorial-dotnetcore-sqldb.md"
                        }
                    ],
                    "product": "app-service"
                },
                {
                    "items": [
                        {
                            "Title": "Develop locally with the Azure Cosmos DB Emulator",
                            "Date": "2018-04-26T",
                            "Url": "MicrosoftDocs/azure-docs/contents/articles/cosmos-db/local-emulator.md"
                        },
                        {
                            "Title": "Azure Cosmos DB: .NET Change Feed Processor API, SDK & resources ",
                            "Date": "2018-04-26T",
                            "Url": "MicrosoftDocs/azure-docs/contents/articles/cosmos-db/sql-api-sdk-dotnet-changefeed.md"
                        }
                    ],
                    "product": "cosmos-db"
                }
            ]
        */
        [FunctionName("SendEmailFn")]
        public static IActionResult SendEmail(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequest req, 
            ExecutionContext context,
            TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            string emailHeader = @"<!doctype html>
                        <html lang=""en"">
                        <head>
                            <title> Azure Documentation Updates | Microsoft Azure </title >
                           </head>
                           <body>
                               <h1> Azure Documentation Updates</h1>";
            string emailFooter = @"</body></html>";

            // Read the JSON input in the request body. This JSON should be used to construct the HTML body.
            string requestBody = new StreamReader(req.Body).ReadToEnd();

            dynamic data = JsonConvert.DeserializeObject(requestBody);

            StringBuilder bodyBuilder = new StringBuilder();
            bodyBuilder.Append(emailHeader);

            if (productMap == null)
            {
                productMap = ProductTitleRepository.GetMapping(context.FunctionAppDirectory);
            }

            foreach (var product in data)
            {
                StringBuilder productBuilder = new StringBuilder();
                productBuilder.Append($"<div id=\"{product.product}\">");
                productBuilder.Append("<h3>");

                // Get friendly name for product
                string productName = GetProductFriendlyName((string)product.product);
 
                productBuilder.Append(productName);

                productBuilder.Append("<h3>");
                productBuilder.Append("<ul>");

                foreach (var item in product.items)
                {
                    string date = item.Date;
                    date = date.TrimEnd('T');

                    string itemUrl = item.Url;

                    string itemLink = $"https://docs.microsoft.com/en-us/azure/{itemUrl.Replace("MicrosoftDocs/azure-docs/contents/articles/", string.Empty).Replace(".md", string.Empty)}";

                    productBuilder.Append("<li>");
                    productBuilder.Append($"{date}: ");
                    productBuilder.Append($"<a target=\"_blank\" href=\"{itemLink}\">{item.Title}</a>");
                    productBuilder.Append("</li>");
                }

                productBuilder.Append("</ul>");
                productBuilder.Append("</h3>");

                bodyBuilder.Append(productBuilder.ToString());
            }

            bodyBuilder.Append(emailFooter);
            
            return new OkObjectResult(bodyBuilder.ToString());
        }

        private static string GetProductFriendlyName(string product)
        {
            string productName = product.TrimStart('{').TrimEnd('}');
            string productFriendlyName = productMap[productName];
            return productFriendlyName;
        }
    }
}
