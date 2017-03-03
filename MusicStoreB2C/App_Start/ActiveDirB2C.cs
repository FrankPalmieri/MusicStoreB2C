using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Security.Claims;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Dynamic;
using System.Collections;

namespace MusicStoreB2C.App_Start
{
    public class ActiveDirB2C
    {
        private static string signUpPolicyId;
        private static string signInPolicyId;
        private static string signUpOrInPolicyId;
        private static string profilePolicyId;
        private static string resetPasswordPolicyId;
        private static string clientId;
        private static string clientSecret;
        private static string GraphResourceId;
        private static string GraphEndpoint = "https://graph.windows.net/";
        private static string GraphSuffix = "";
        private static string GraphVersion = "api-version=1.6";
        private static string RedirectUri;
        private static string AadInstance;
        private static string tenant;

        private B2CGraphClient b2cGraph;
        //
        // Summary:
        //     Invoked if exceptions are thrown during request processing. The exceptions will
        //     be re-thrown after this event unless suppressed.
        public Func<FailureContext, Task> OnResetPasswordRequested { get; set; }
        public Func<FailureContext, Task> OnCanceledSignIn { get; set; }
        public Func<FailureContext, Task> OnOtherFailure { get; set; }

        public static string ClientId { get { return clientId; } }
        public static string ClientSecret { get { return clientSecret; } }
        public static string Tenant { get { return tenant; } }

        public static string SignUpPolicyId { get { return signUpPolicyId; } set { signUpPolicyId = value; } }
        public static string SignInPolicyId { get { return signInPolicyId; } set { signInPolicyId = value; } }
        public static string SignUpOrInPolicyId { get { return signUpOrInPolicyId; } set { signUpOrInPolicyId = value; } }
        public static string ProfilePolicyId { get { return profilePolicyId; } set { profilePolicyId = value; } }
        public static string ResetPasswordPolicyId { get { return resetPasswordPolicyId; } set { resetPasswordPolicyId = value; } }

        // Private variables for Startup
        private readonly ILogger _logger;
        public ActiveDirB2C(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ActiveDirB2C>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IConfiguration Configuration)
        {
            // Configure the OWIN pipeline to use cookie auth.
            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            // App config settings
            clientId = Configuration["AzureAD:ClientId"];
            clientSecret = Configuration["AzureAD:ClientSecret"];
            GraphResourceId = Configuration["AzureAD:GraphResourceId"];
            GraphEndpoint = Configuration["AzureAD:GraphEndpoint"];
            GraphSuffix = Configuration["AzureAD:GraphSuffix"];
            GraphVersion = Configuration["AzureAD:GraphVersion"];
            AadInstance = Configuration["AzureAD:AadInstance"];
            tenant = Configuration["AzureAD:Tenant"];
            RedirectUri = Configuration["AzureAD:RedirectUri"];

            // B2C policy identifiers
            SignUpPolicyId = Configuration["AzureAD:SignUpPolicyId"];
            SignInPolicyId = Configuration["AzureAD:SignInPolicyId"];
            SignUpOrInPolicyId = Configuration["AzureAD:SignUpOrInPolicyId"];
            ProfilePolicyId = Configuration["AzureAD:ProfileEditingPolicyId"];
            ResetPasswordPolicyId = Configuration["AzureAD:ResetPasswordPolicyId"];

            // Configure the OWIN pipeline to use OpenID Connect auth.
            app.UseOpenIdConnectAuthentication(CreateOptionsFromPolicy(SignUpPolicyId));
            app.UseOpenIdConnectAuthentication(CreateOptionsFromPolicy(SignInPolicyId));
            app.UseOpenIdConnectAuthentication(CreateOptionsFromPolicy(SignUpOrInPolicyId, true));
            app.UseOpenIdConnectAuthentication(CreateOptionsFromPolicy(ProfilePolicyId));
            app.UseOpenIdConnectAuthentication(CreateOptionsFromPolicy(ResetPasswordPolicyId));

            b2cGraph = new B2CGraphClient(ActiveDirB2C.ClientId, ActiveDirB2C.ClientSecret, ActiveDirB2C.Tenant);
        }

        // Added support for calling GraphAPI per this link:
        // http://stackoverflow.com/questions/40721201/microsoft-graph-api-returns-forbidden-response-when-trying-to-use-me-memberof-i
        // - Added Authority configuration, changed ResponseType from IdToken to CodeIdToken, added GetClaimsFromUserInfoEndpoint = true
        // and SaveTokens = true

        private OpenIdConnectOptions CreateOptionsFromPolicy(string policy, bool automaticChallenge = false)
        {
            policy = policy.ToLower();
            var authority = string.Format(AadInstance, Tenant, policy);
            var options = new OpenIdConnectOptions
            {
                // For each policy, give OWIN the policy-specific metadata address, and
                // set the authentication type to the id of the policy
                MetadataAddress = string.Format(AadInstance, Tenant, policy),
                AuthenticationScheme = policy,
                AutomaticChallenge = automaticChallenge,
                Authority = authority,
                CallbackPath = new PathString(string.Format("/{0}", policy)),
                // Per https://github.com/Azure-Samples/active-directory-dotnet-webapp-openidconnect-aspnetcore-b2c/issues/6
                SignedOutCallbackPath = new PathString(string.Format("/signout-callback-{0}", policy)),
                RemoteSignOutPath = new PathString(string.Format("/signout-{0}", policy)),

                // These are standard OpenID Connect parameters, with values pulled from config.json
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                PostLogoutRedirectUri = RedirectUri,

                Events = new OpenIdConnectEvents
                {
                    OnAuthenticationFailed = AuthenticationFailed,
                    OnAuthorizationCodeReceived = AuthorizationCodeReceived,
                    OnMessageReceived = MessageReceived,
                    OnRedirectToIdentityProvider = RedirectToIdentityProvider,
                    OnRedirectToIdentityProviderForSignOut = RedirectToIdentityProviderForSignOut,
                    OnRemoteSignOut = RemoteSignOut,
                    OnRemoteFailure = RemoteFailure,
                    OnTicketReceived = TicketReceived,
                    OnTokenResponseReceived = TokenResponseReceived,
                    OnTokenValidated = TokenValidated,
                    OnUserInformationReceived = UserInformationReceived,
                },
                //
                // TODO Need to understand why CodeIdToken doesn't work here
                //
                // ResponseType = OpenIdConnectResponseType.CodeIdToken,
                ResponseType = OpenIdConnectResponseType.IdToken,
                Scope = {"openid", "offline_access"},

                // This piece is optional - it is used for displaying the user's name in the navigation bar.
                TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                },
                GetClaimsFromUserInfoEndpoint = true,
                SaveTokens = true
            };
            return options;
        }

        private Task UserInformationReceived(UserInformationReceivedContext context)
        {
            _logger.LogDebug("UserInformationReceived!");
            return Task.FromResult(0);
        }

        private async Task TokenValidated(TokenValidatedContext context)
        {
            _logger.LogDebug("TokenValidated!");

            // Acquire a Token for the Graph API and cache it using ADAL.
            string userObjectId = (context.Ticket.Principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier"))?.Value;

            var userJson = await b2cGraph.GetUserByObjectId(userObjectId);
            var user = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(userJson);

            // Get the users roles and groups from the Graph Api. Then return the roles and groups in a new identity
            ClaimsIdentity identity = await GetUsersRoles(userObjectId);

            // Add the roles to the Principal User
            context.Ticket.Principal.AddIdentity(identity);
            // return Task.FromResult(0);
        }

        private Task TokenResponseReceived(TokenResponseReceivedContext context)
        {
            _logger.LogDebug("TokenResponseReceived!");
            return Task.FromResult(0);
        }

        private Task TicketReceived(TicketReceivedContext context)
        {
            _logger.LogDebug("TicketReceived!");
            return Task.FromResult(0);
        }

        private Task RemoteSignOut(RemoteSignOutContext context)
        {
            _logger.LogDebug("RemoteSignOut!");
            return Task.FromResult(0);
        }

        private Task RedirectToIdentityProviderForSignOut(RedirectContext context)
        {
            _logger.LogDebug("RedirectToIdentityProviderForSignOut!");
            return Task.FromResult(0);
        }

        private Task RedirectToIdentityProvider(RedirectContext context)
        {
            _logger.LogDebug("RedirectToIdentityProvider!");
            return Task.FromResult(0);
        }

        private Task MessageReceived(MessageReceivedContext context)
        {
            _logger.LogDebug("MessageReceived!");
            return Task.FromResult(0);
        }

        private Task AuthorizationCodeReceived(AuthorizationCodeReceivedContext context)
        {
            _logger.LogDebug("AuthorizationCodeReceived!");
            return Task.FromResult(0);
        }


        // Get user's roles as the Application
        /// <summary>
        /// Returns user's roles and groups as a ClaimsIdentity
        /// </summary>
        /// <param name="accessToken">accessToken retrieved using the client credentials and the resource (Hint: NOT the accessToken from the signin event)</param>
        /// <param name="userId">The user's unique identifier from the signin event</param>
        /// <returns>ClaimsIdentity</returns>
        private async Task<ClaimsIdentity> GetUsersRoles(string userId)
        {
            ClaimsIdentity identity = new ClaimsIdentity("LocalIds");

            string resource = userId + "/memberOf";

            var responseJson = await b2cGraph.GetUserByObjectId(resource);

            // The Json has a odata.metadate header and a value which is list of 0 or more claims.
            dynamic response = JsonConvert.DeserializeObject<dynamic>(responseJson);

            // Make sure the header is what we expect (note must use [] because of '.' in name)
            if ((response["odata.metadata"].ToString().Contains("https://graph.windows.net") == true) &&
                (response["odata.metadata"].ToString().Contains(tenant) == true) &&
                (response["odata.metadata"].ToString().Contains("$metadata#directoryObjects") == true))
            {
                var claims = new List<Claim>();
                // Get the list of claims out of the value into a list of dynamic objects
                var responseClaims = response.value.ToObject<List<dynamic>>();

                if ((responseClaims != null) && (responseClaims.Count > 0))
                {
                    foreach (dynamic item in responseClaims)
                    {
                        string displayName = item.displayName;

                        if ((item["odata.type"] == "Microsoft.DirectoryServices.DirectoryRole") && (item.objectType == "Role"))
                        {
                            if (item.roleDisabled == false)
                                claims.Add(new Claim(ClaimTypes.Role, displayName));
                        }
                        else if ((item.GetType().GetProperty("securityEnabled") != null) &&
                                 (item.securityEnabled == true))
                        {
                            claims.Add(new Claim(ClaimTypes.Role, displayName));
                        }
                        else if((item["odata.type"] == "Microsoft.DirectoryServices.Group") && (item.objectType == "Group"))
                        {
                              claims.Add(new Claim("Group", displayName));
                        }
                        else
                        {
                            // Not something we expect - log it and go on
                            _logger.LogError("Unknown directoryObject found!");
                        }
                    }
                }
                identity.AddClaims(claims);
            }
            return identity;
        }



        private Task AuthenticationFailed(AuthenticationFailedContext context)
        {
            _logger.LogDebug("AuthenticationFailed!");
            return Task.FromResult(0);
        }

        // Used for avoiding yellow-screen-of-death

        // FAP : See http://stackoverflow.com/questions/41224543/how-to-get-azure-aad-b2c-forgot-password-link-to-work/41772017
        // for discussion around handling of password reset - code below modified per this link but changed because ASP.Net Core
        // AD B2C integration is different than ASP.Net 4.X
        private Task RemoteFailure(FailureContext context)
        {
            Task result = Task.FromResult(0);
            _logger.LogDebug("RemoteFailure!");
            context.HandleResponse();
            if (context.Failure is OpenIdConnectProtocolException && context.Failure.Message.Contains("AADB2C90118"))
            {
                // If the user clicked the reset password link, redirect to the reset password route 
                _logger.LogDebug("User clicked reset password link, redirect to ResetPassword route!");
                if (OnResetPasswordRequested != null)
                {
                    result = OnResetPasswordRequested(context);
                }
                else
                {
                    context.Response.Redirect("/Account/ResetPassword");
                }
            }
            else if (context.Failure is OpenIdConnectProtocolException && context.Failure.Message.Contains("access_denied"))
            {
                // If the user canceled the sign in, redirect back to the home page 
                _logger.LogDebug("User canceled out of sign in or was denied access, redirect to Home");
                if (OnResetPasswordRequested != null)
                {
                    result = OnCanceledSignIn(context);
                }
                else
                {
                    context.Response.Redirect("/Home/Index");
                }
            }
            else
            {
                _logger.LogDebug("Not sure why we got here - show error page with message: {0}", context.Failure.Message);
                if (OnOtherFailure != null)
                {
                    result = OnCanceledSignIn(context);
                }
                else
                {
                    context.Response.Redirect("/Home/Error?message=" + context.Failure.Message);
                }
            }

            return result;
        }

        //
        // This function returns the user properties from AD B2C
        //

        public async Task<dynamic> GetUserProperties(ClaimsPrincipal user)
        {
            // Return an empty property list on error
            dynamic userProps = new ExpandoObject();
            try
            {
                // Acquire a Token for the Graph API and cache it using ADAL.
                string userObjectId = (user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier"))?.Value;
                Guid temp; // for test parse - ignore value

                if ((string.IsNullOrWhiteSpace(userObjectId) == false) && (Guid.TryParse(userObjectId, out temp) == true))
                {
                    userProps.objectId = userObjectId;
                    string userJson = await b2cGraph.GetUserByObjectId(userObjectId);

                    if (string.IsNullOrWhiteSpace(userJson) == false)
                    {
                        dynamic userParsedProps = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(userJson);
                        foreach (var prop in userParsedProps)
                        {                          
                            if (((IDictionary<string, object>)userProps).ContainsKey(prop.Name))
                                ((IDictionary<string, object>)userProps)[prop.Name] = prop.Value;
                            else
                                ((IDictionary<string, object>)userProps).Add(prop.Name, prop.Value); 
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO: May want to make sure this is a 404 (no thumbnail) or other expected
                // error before swallowing and returning the black thumbnail
                _logger.LogDebug("GetUserProperties Error: " + ex.Message);
            }

            return Task.FromResult(userProps);
        }

        //
        // This function returns the JPEG/GIF/PNG thumbnail image from the AD Thumbnail Photo 
        // property as a base64 string which can be embedded in the HTML page per this article:
        // https://devio.wordpress.com/2011/01/13/embedding-images-in-html-using-c/
        //

        public Task<string> GetUserThumbnail(ClaimsPrincipal user)
        {
            // return a black pixel as the image if there is an error
            string result = "data:image/jpeg; base64,R0lGODlhAQABAIAAAAAAAAAAACH5BAAAAAAALAAAAAABAAEAAAICTAEAOw==";
            try
            {
                // Acquire a Token for the Graph API and cache it using ADAL.
                string userObjectId = (user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier"))?.Value;
                string photoBase64 = string.Empty;
                if (string.IsNullOrWhiteSpace(userObjectId) == false)
                {
                    photoBase64 = b2cGraph.GetUserPropsByObjectId(userObjectId, "/thumbnailPhoto").Result;
                    if (string.IsNullOrWhiteSpace(photoBase64))
                    {
                        photoBase64 = result;
                    }
                }
                result = photoBase64;
            }
            catch (Exception ex)
            {
                // TODO: May want to make sure this is a 404 (no thumbnail) or other expected
                // error before swallowing and returning the black thumbnail
                _logger.LogDebug("GetUserThumbnail Error: " + ex.Message);
            }
            return Task.FromResult(result);
        }
    }
}
