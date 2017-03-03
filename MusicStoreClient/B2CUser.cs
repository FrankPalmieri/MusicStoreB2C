using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.Identity.Client;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace MusicStoreClient
{
    public static class Globals
    {
        private static string aadInstance = String.Empty; // "https://login.microsoftonline.com/";
        private static string redirectUri = String.Empty; // typically "urn:ietf:wg:oauth:2.0:oob"

        // TODO: Replace these five default with your own configuration values
        private static string tenant = String.Empty; // "<b2cdir>.onmicrosoft.com" -  domain name
        private static string clientId = String.Empty; // applicationId
        private static string signInPolicy = String.Empty; // "b2c_1_sign_in_short";
        private static string signUpPolicy = String.Empty; // "b2c_1_sign_up";
        private static string signUpOrSignInPolicy = String.Empty; // "b2c_1_sign_up_or_sign_in_short";
        private static string editProfilePolicy = String.Empty; // "b2c_1_profile_editing";

        public static string AadInstance { get => aadInstance; set => aadInstance = value; }
        public static string RedirectUri { get => redirectUri; set => redirectUri = value; }
        public static string Tenant { get => tenant; set => tenant = value; }
        public static string ClientId { get => clientId; set => clientId = value; }
        public static string SignInPolicy { get => signInPolicy; set => signInPolicy = value; }
        public static string SignUpPolicy { get => signUpPolicy; set => signUpPolicy = value; }
        public static string SignUpOrSignInPolicy { get => signUpOrSignInPolicy; set => signUpOrSignInPolicy = value; }
        public static string EditProfilePolicy { get => editProfilePolicy; set => editProfilePolicy = value; }
        public static string Authority { get => string.Concat(AadInstance, Tenant); }
    }
    internal class B2CUser
    {
        // Create MS Identity Client Object
        private PublicClientApplication pca = null;
        private AuthenticationResult result;

        public string Username
        {
            get
            {
                if (IsSignedIn() == false)
                    return String.Empty;
                else
                    return result.User.Name;
            }
        }

        public string UserId
        {
            get
            {
                if (IsSignedIn() == false)
                    return String.Empty;
                else
                    return result.User.UniqueId;
            }
        }

        public string IDToken
        {
            get
            {
                if (IsSignedIn() == false)
                    return String.Empty;
                else
                    return result.IdToken;
            }
        }
        public string Policy
        {
            get
            {
                if (IsSignedIn() == false)
                    return String.Empty;
                else
                {
                    var token = new JwtSecurityToken(result.Token);
                    string policy = string.Empty;
                    foreach (var claim in token.Claims)
                    {
                        if (claim.Type.Contains("tfp"))
                            policy = claim.Value;
                    }
                    return policy;
#if validate_token
                    // https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-reference-tokens#validating-tokens
                    // https://social.msdn.microsoft.com/Forums/sqlserver/en-US/893a6142-1508-4aa2-9da3-dab3b1f1a6b9/b2c-jwt-token-signature-validation?forum=WindowsAzureAD


                    var tokenHandler = new JwtSecurityTokenHandler();
                    var validationParameters = new TokenValidationParameters()
                    {
                        ValidAudience = "d049536e-9bea-4517-bd15-3188f470ed28",
                        ValidIssuer = "https://login.microsoftonline.com/454e32fa-c294-4055-a7ec-29d58738a2d4/v2.0/",
                        ValidateIssuerSigningKey
                    };
                    _jwtPayload = JsonConvert.DeserializeObject < dictionary < string, object= "" >> (Jose.JWT.Payload(jwt));

     TokenValidationParameters validationParams = new TokenValidationParameters() { ValidAudiences = [my validAudiences] };

                    OpenIdConnectCachingSecurityTokenProvider issuerCredentialProvider = new OpenIdConnectCachingSecurityTokenProvider(_jwtPayload["iss"] + ".well-known/openid-configuration?p=" + _jwtPayload["acr"]);
                    await issuerCredentialProvider.RetrieveMetadata();
                    IEnumerable<SecurityToken> securityTokens = issuerCredentialProvider.SecurityTokens;
                    validationParams.IssuerSigningTokens = securityTokens;
                    validationParams.ValidIssuer = issuerCredentialProvider.Issuer;

                    var validatedToken = new JwtSecurityToken() as SecurityToken;
                    var claimsPrincipal = tokenHandler.ValidateToken(result.Token, validationParameters, out validatedToken);

                    var policy = claimsPrincipal.FindFirst("tfp");
                    return policy.ToString();
#endif
                }

            }
        }

        public string CacheFilePath { get; private set; } = @".\TokenCache.dat";

        internal async Task<int> InitializeAsync()
        {
            result = null;
            AuthenticationResult silentResult = null;
            try
            {
                // If the user has has a token cached with any policy, we'll display them as signed-in.
                TokenCacheItem tci = pca.UserTokenCache.ReadItems(Globals.ClientId).Where(i => i.Scope.Contains(Globals.ClientId) && !string.IsNullOrEmpty(i.Policy)).FirstOrDefault();
                string existingPolicy = tci == null ? null : tci.Policy;
                silentResult = await pca.AcquireTokenSilentAsync(new string[] { Globals.ClientId }, string.Empty, Globals.Authority, existingPolicy, false);
                result = silentResult;
                return 0;
            }
            catch (MsalException ex)
            {
                if (ex.ErrorCode == "failed_to_acquire_token_silently")
                {
                    // There are no tokens in the cache.  Proceed without calling the To Do list service.
                }
                else
                {
                    // An unexpected error occurred.
                    string message = ex.Message;
                    if (ex.InnerException != null)
                    {
                        message += "Inner Exception : " + ex.InnerException.Message;
                    }
                    MessageBox.Show(message);
                }
                return -1;
            }
        }

        internal async Task<int> EditAccountSettings()
        {
            AuthenticationResult profileResult = null;
            try
            {
                profileResult = await pca.AcquireTokenAsync(new string[] { Globals.ClientId },
                    string.Empty, UiOptions.ForceLogin, null, null, Globals.Authority,
                    Globals.EditProfilePolicy);
                //UsernameLabel.Content = result.User.Name;
                result = profileResult;
                return 0;
            }
            catch (MsalException ex)
            {
                if (ex.ErrorCode != "authentication_canceled")
                {
                    // An unexpected error occurred.
                    string message = ex.Message;
                    if (ex.InnerException != null)
                    {
                        message += "Inner Exception : " + ex.InnerException.Message;
                    }

                    MessageBox.Show(message);
                }

                return -1;
            }

        }

        public B2CUser()
        {
            pca = new PublicClientApplication(Globals.ClientId)
            {
                // MSAL implements an in-memory cache by default.  Since we want tokens to persist when the user closes the app, 
                // we've extended the MSAL TokenCache and created a simple FileCache in this app.
                UserTokenCache = new TokenFileCache()
                {
                    CacheFilePath = this.CacheFilePath
                },
            };
            result = null;
        }

        internal async Task<int> SignIn()
        {
            result = null;
            AuthenticationResult signInResult = null;
            try
            {
                signInResult = await pca.AcquireTokenAsync(new string[] { Globals.ClientId },
                    string.Empty, UiOptions.ForceLogin, null, null, Globals.Authority,
                Globals.SignInPolicy);
                result = signInResult;
            }
            catch (MsalException ex)
            {
                if (ex.ErrorCode != "authentication_canceled")
                {
                    // An unexpected error occurred.
                    string message = ex.Message;
                    if (ex.InnerException != null)
                    {
                        message += "Inner Exception : " + ex.InnerException.Message;
                    }

                    MessageBox.Show(message);
                }

                return -1;
            }
            return 0;
        }

        internal async Task<int> SignUpOrIn()
        {
            result = null;
            AuthenticationResult signUpInResult = null;
            try
            {
                signUpInResult = await pca.AcquireTokenAsync(new string[] { Globals.ClientId },
                    string.Empty, UiOptions.ForceLogin, null, null, Globals.Authority,
                Globals.SignUpOrSignInPolicy);
                result = signUpInResult;
            }
            catch (MsalException ex)
            {
                if (ex.ErrorCode != "authentication_canceled")
                {
                    // An unexpected error occurred.
                    string message = ex.Message;
                    if (ex.InnerException != null)
                    {
                        message += "Inner Exception : " + ex.InnerException.Message;
                    }

                    MessageBox.Show(message);
                }

                return -1;
            }
            return 0;
        }

        internal bool IsSignedIn()
        {
            if (result == null)
                return false;
            // TODO: Make sure token is valid?
            return true;
        }

        internal async Task<int> SignOut()
        {
            await Task.Yield();

            // Clear any remnants of the user's session.
            pca.UserTokenCache.Clear(Globals.ClientId);

            // This is a helper method that clears browser cookies in the browser control that MSAL uses, it is not part of MSAL.
            ClearCookies();

            // Clear out the result value so object knows it is signed out too
            result = null;

            return 0;
        }

        // This function clears cookies from the browser control used by MSAL.
        private void ClearCookies()
        {
            const int INTERNET_OPTION_END_BROWSER_SESSION = 42;
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_END_BROWSER_SESSION, IntPtr.Zero, 0);
        }

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int lpdwBufferLength);
    }
}