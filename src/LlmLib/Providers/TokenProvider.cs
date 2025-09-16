namespace LlmLib.Providers;

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;

/// <summary>
/// Provides methods to acquire tokens for authentication.
/// </summary>
public static class TokenProvider
{
    /// <summary>
    /// The public client application used to acquire tokens.
    /// </summary>
    public static readonly IPublicClientApplication App = PublicClientApplicationBuilder
        .Create(ClientId)
        .WithAuthority(new Uri(AuthorityUri))
        .WithDefaultRedirectUri()
        .WithParentActivityOrWindow(WinWindowProvider.GetConsoleOrTerminalWindow)
        .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
        .Build();

    private const string ClientId = "68df66a4-cad9-4bfd-872b-c6ddde00d6b2";
    private const string MsitTenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
    private const string AuthorityUri = $"https://login.microsoftonline.com/{MsitTenantId}";
    private const string SubstrateLLMAPIScope = $"https://substrate.office.com/llmapi/LLMAPI.dev";
    private static readonly IEnumerable<string> Scopes = new List<string>()
            {
                SubstrateLLMAPIScope,
            };

    /// <summary>
    /// Asynchronously gets an authentication token.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the authentication token.</returns>
    public static async Task<string> GetTokenAsync()
    {
        AuthenticationResult? result = null;

        // Try to use the previously signed-in account from the cache
        IEnumerable<IAccount> accounts = await App.GetAccountsAsync().ConfigureAwait(false);
        IAccount? existingAccount = accounts.FirstOrDefault();

        try
        {
            if (existingAccount != null)
            {
                result = await App.AcquireTokenSilent(Scopes, existingAccount).ExecuteAsync().ConfigureAwait(false);
            }
            else
            {
                // Next, try to sign in silently with the account that the user is signed into Windows
                result = await App.AcquireTokenSilent(Scopes, PublicClientApplication.OperatingSystemAccount)
                                    .ExecuteAsync().ConfigureAwait(false);
            }
        }
        catch (MsalUiRequiredException)
        {
            // Can't get a token silently, go interactive
            result = await App.AcquireTokenInteractive(Scopes).ExecuteAsync().ConfigureAwait(false);
        }

        return result.AccessToken;
    }
}

