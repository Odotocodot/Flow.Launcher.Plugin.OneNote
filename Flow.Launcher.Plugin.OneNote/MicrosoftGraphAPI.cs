// using Microsoft.Identity.Client;
// using Microsoft.Graph;
// using System.Diagnostics;
// using System.Threading.Tasks;
// using System.Net.Http.Headers;
// using System.Windows.Controls;
// using System.Windows;
// using System.Collections.Generic;
// using System.Linq;

// public sealed partial class MainPage : Page
// {

//     //Set the scope for API call to user.read
//     private string[] scopes = new string[] { "user.read" };

//     // Below are the clientId (Application Id) of your app registration and the tenant information.
//     // You have to replace:
//     // - the content of ClientID with the Application Id for your app registration
//     private const string ClientId = "[Application Id pasted from the application registration portal]";

//     private const string Tenant = "common"; // Alternatively "[Enter your tenant, as obtained from the Azure portal, e.g. kko365.onmicrosoft.com]"
//     private const string Authority = "https://login.microsoftonline.com/" + Tenant;

//     // The MSAL Public client app
//     private static IPublicClientApplication PublicClientApp;

//     private static string MSGraphURL = "https://graph.microsoft.com/v1.0/";
//     private static AuthenticationResult authResult;

//     public MainPage()
//     {
//         this.InitializeComponent();
//     }

//     /// <summary>
//     /// Call AcquireTokenAsync - to acquire a token requiring user to sign in
//     /// </summary>
//     private async void CallGraphButton_Click(object sender, RoutedEventArgs e)
//     {
//         try
//         {
//             // Sign in user using MSAL and obtain an access token for Microsoft Graph
//             GraphServiceClient graphClient = await SignInAndInitializeGraphServiceClient(scopes);

//             // Call the /me endpoint of Graph
//             User graphUser = await graphClient.Me.Request().GetAsync();

//             // Go back to the UI thread to make changes to the UI
//             await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
//             {
//                 ResultText.Text = "Display Name: " + graphUser.DisplayName + "\nBusiness Phone: " + graphUser.BusinessPhones.FirstOrDefault()
//                                   + "\nGiven Name: " + graphUser.GivenName + "\nid: " + graphUser.Id
//                                   + "\nUser Principal Name: " + graphUser.UserPrincipalName;
//                 DisplayBasicTokenInfo(authResult);
//                 this.SignOutButton.Visibility = Visibility.Visible;
//             });
//         }
//         catch (MsalException msalEx)
//         {
//             await DisplayMessageAsync($"Error Acquiring Token:{System.Environment.NewLine}{msalEx}");
//         }
//         catch (Exception ex)
//         {
//             await DisplayMessageAsync($"Error Acquiring Token Silently:{System.Environment.NewLine}{ex}");
//             return;
//         }
//     }
//             /// <summary>
//     /// Signs in the user and obtains an access token for Microsoft Graph
//     /// </summary>
//     /// <param name="scopes"></param>
//     /// <returns> Access Token</returns>
//     private static async Task<string> SignInUserAndGetTokenUsingMSAL(string[] scopes)
//     {
//         // Initialize the MSAL library by building a public client application
//         PublicClientApp = PublicClientApplicationBuilder.Create(ClientId)
//             .WithAuthority(Authority)
//             .WithUseCorporateNetwork(false)
//             .WithRedirectUri("https://login.microsoftonline.com/common/oauth2/nativeclient")
//              .WithLogging((level, message, containsPii) =>
//              {
//                  Debug.WriteLine($"MSAL: {level} {message} ");
//              }, LogLevel.Warning, enablePiiLogging: false, enableDefaultPlatformLogging: true)
//             .Build();

//         // It's good practice to not do work on the UI thread, so use ConfigureAwait(false) whenever possible.
//         IEnumerable<IAccount> accounts = await PublicClientApp.GetAccountsAsync().ConfigureAwait(false);
//         IAccount firstAccount = accounts.FirstOrDefault();

//         try
//         {
//             authResult = await PublicClientApp.AcquireTokenSilent(scopes, firstAccount)
//                                               .ExecuteAsync();
//         }
//         catch (MsalUiRequiredException ex)
//         {
//             // A MsalUiRequiredException happened on AcquireTokenSilentAsync. This indicates you need to call AcquireTokenAsync to acquire a token
//             Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");

//             authResult = await PublicClientApp.AcquireTokenInteractive(scopes)
//                                               .ExecuteAsync()
//                                               .ConfigureAwait(false);

//         }
//         return authResult.AccessToken;
//     }
// }