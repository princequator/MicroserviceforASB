//using Azure.Identity;
//using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.ServiceBus;
//using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace user_send_microservice
{
    class Program
    {
        static ITopicClient topicClient;
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            // Fetching secrets from Azure Key Vault.
            string clientSecret = "8-qI~NFUYG_~1XW4PnCKSDQGPxaG0K6J38"; //App registration secret
            string baseUri = "https://azdemoappkv.vault.azure.net/";
            string clientId = "624c4866-5ef2-45ea-82e9-9a810f39629e"; //App registration ID
            var client = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(
                async (string auth, string res, string scope) =>
                {
                    var authContext = new AuthenticationContext(auth);
                    var credential = new ClientCredential(clientId, clientSecret);
                    AuthenticationResult result = await authContext.AcquireTokenAsync(res, credential);
                    if (result == null)
                    {
                        throw new InvalidOperationException("Failed to retrieve token");
                    }
                    return result.AccessToken;
                }
                ));
            var secretData = await client.GetSecretAsync(baseUri, "ServiceBusConnectionString");

            //string ServiceBusConnectionString = "Endpoint=sb://azdemoappsb.servicebus.windows.net/;SharedAccessKeyName=azdemo-sendkey;SharedAccessKey=yPRFqtrXWlujBCFL41zT4sF7L4EL3WFsbJ0RMBujAMQ=";
            string ServiceBusConnectionString = secretData.Value;
            string TopicName = "azdemo-topic";

            topicClient = new TopicClient(ServiceBusConnectionString, TopicName);

            // Send messages.
            await SendUserMessage();

            Console.ReadKey();

            await topicClient.CloseAsync();
        }


        static async Task SendUserMessage()
        {
            List<User> users = GetDummyDataForUser();

            var serializeUser = JsonConvert.SerializeObject(users);

            string messageType = "userData";

            string messageId = Guid.NewGuid().ToString();

            var message = new ServiceBusMessage
            {
                Id = messageId,
                Type = messageType,
                Content = serializeUser
            };

            var serializeBody = JsonConvert.SerializeObject(message);

            // send data to bus

            var busMessage = new Message(Encoding.UTF8.GetBytes(serializeBody));
            busMessage.UserProperties.Add("Type", messageType);
            busMessage.MessageId = messageId;

            await topicClient.SendAsync(busMessage);

            Console.WriteLine("message has been sent");

        }

       public class User
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        public class ServiceBusMessage
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public string Content { get; set; }
        }

        private static List<User> GetDummyDataForUser()
        {
            User user = new User();
            List<User> lstUsers = new List<User>();
            for (int i = 1; i < 3; i++)
            {
                user = new User();
                user.Id = i;
                user.Name = "PrinceSharma" + i;

                lstUsers.Add(user);
            }

            return lstUsers;
        }
    }
}
