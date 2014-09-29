using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

using Google.Apis.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Reseller.v1;
using Google.Apis.Reseller.v1.Data;

namespace GoogleApi.Reseller
{
    class Program
    {
        private const string ClientSertificatePath = @"The path to the certificate file from step 5";
        private const string ClientSertificatePassword = "notasecret";
        private const string ClientAccountEmail = "The client email address from step 5";
        private const string ClientImpersonateUser = "Your Google Apps user from step 1";
        private const string ApplicationName = "Reseller Sample";
        private const int ServiceMaxResults = 100;

        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine(ApplicationName);
            Console.WriteLine("====================");
            try
            {
                new Program().Run();
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }

            Console.WriteLine("Press any Key to Continue...");
            Console.ReadKey();
        }

        private void Run()
        {
            // Setup the service
            var service = Setup();
            var listOfSubscriptions = new List<Subscription>();

            Console.WriteLine("Getting a list of subscriptions...");
            
            GetSubscriptions(ref service, ref listOfSubscriptions, string.Empty);

            foreach (var s in listOfSubscriptions)
            {
                Console.WriteLine(s.SubscriptionId + " - " + s.CustomerId);
            }

            Console.WriteLine("Total: {0}", listOfSubscriptions.Count);
        }

        /// <summary>
        /// Creates the reseller service object
        /// with the provided client settings and certificate
        /// </summary>
        /// <returns></returns>
        private ResellerService Setup()
        {
            // Create a new certificate from the client certificate file
            var certificate = new X509Certificate2(ClientSertificatePath, ClientSertificatePassword, X509KeyStorageFlags.Exportable);

            // Create new credentials based on the certificate and the client settings
            var credentials = new ServiceAccountCredential(
               new ServiceAccountCredential.Initializer(ClientAccountEmail)
               {
                   // Set the scope of the request, AppOrderReadonly does not work here
                   Scopes = new[] { ResellerService.Scope.AppsOrder },
                   User = ClientImpersonateUser
               }.FromCertificate(certificate));

            // Return a new service with the client credentials
            return new ResellerService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credentials,
                ApplicationName = ApplicationName,
            });
        }

        /// <summary>
        /// Gets a list of subscriptions
        /// </summary>
        /// <param name="service">The ResellerService object</param>
        /// <param name="listOfSubscriptions">A list of Subscription to store the returned data</param>
        /// <param name="nextPageToken">The string containing the next page token</param>
        private void GetSubscriptions(ref ResellerService service, ref List<Subscription> listOfSubscriptions, string nextPageToken)
        {
            listOfSubscriptions = listOfSubscriptions ?? new List<Subscription>();

            // Create the subscriptions list request
            var listResults = service.Subscriptions.List();

            // Set the max results and page token
            listResults.MaxResults = ServiceMaxResults;
            listResults.PageToken = !string.IsNullOrEmpty(nextPageToken) ? nextPageToken : string.Empty;

            // Execute the request
            var result = listResults.Execute();

            if (result.SubscriptionsValue != null)
            {
                // Add all the subscriptions to the list
                listOfSubscriptions.AddRange(result.SubscriptionsValue);

                // If the next page token exists
                if (result.NextPageToken != null)
                {
                    // Call yourself again passing the next page token
                    GetSubscriptions(ref service, ref listOfSubscriptions, result.NextPageToken);
                }
            }
        }
    }
}