using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MusicStoreClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// This is a modified version of ToDoListClient from
    /// https://github.com/Azure-Samples/active-directory-dotnet-native-desktop 
    /// to try to support Azure AD B2C instead of Azure AD
    /// </summary>
    public partial class MainWindow : Window
    {
        private Configuration LocalConfig;
        private B2CUser b2cUser;
        private HttpClient httpClient = new HttpClient();
        private static string todoListBaseAddress;

        public MainWindow()
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var exePath = new FileInfo(config.FilePath).Directory;
                // Map the new configuration file.
                ExeConfigurationFileMap configFileMap =  new ExeConfigurationFileMap();
                configFileMap.ExeConfigFilename = System.IO.Path.Combine(exePath.ToString(), "App.Local.config");
                var fInfo = new FileInfo(configFileMap.ExeConfigFilename);
                if (fInfo.Exists)
                {
                    LocalConfig = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
                }

                todoListBaseAddress = GetConfigValue("todo:TodoListBaseAddress");

                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private string GetConfigValue(string setting)
        {
            // If there are local config values, use those
            // otherwise, use the application configuration
            string cfgValue = null;
            if (LocalConfig != null)
                cfgValue = LocalConfig.AppSettings.Settings[setting].Value;
            if (cfgValue == null)
            {
                cfgValue = ConfigurationManager.AppSettings[setting];
            }
            return cfgValue;
        }

        protected async void OnInitializedAsync(object sender, EventArgs e)
        {
            Globals.AadInstance = GetConfigValue("b2c:AADInstance");
            Globals.RedirectUri = GetConfigValue("b2c:RedirectUri");
            Globals.Tenant = GetConfigValue("b2c:Tenant");
            Globals.ClientId = GetConfigValue("b2c:ClientId");
            Globals.SignInPolicy = GetConfigValue("b2c:SignInPolicy");
            Globals.SignUpPolicy = GetConfigValue("b2c:SignUpPolicy");
            Globals.SignUpOrSignInPolicy = GetConfigValue("b2c:SignUpOrSignInPolicy");
            Globals.EditProfilePolicy = GetConfigValue("b2c:ProfilePolicy");
            b2cUser = new B2CUser();

            int result = await b2cUser.InitializeAsync();
            if (b2cUser.IsSignedIn())
            {
                SignInButton.Content = "Clear Cache";
                UserName.Text = b2cUser.Username;
                UserId.Text = b2cUser.UserId;
                GetTodoList(true);
            }
            else
            {
                UserName.Text = "Sign In to get ToDo items";
                UserId.Text = String.Empty;
            }
        }

        private async void SignIn(object sender = null, RoutedEventArgs args = null)
        {
            try
            {
                if (SignInButton.Content.ToString() == "Clear Cache")
                {
                    // await is not really doing much here but we want to call the webservice later to
                    // perform the signout and that will be async - for future expansion :-)
                    var blah = await b2cUser.SignOut();
                    UserName.Text = "Sign In to get ToDo items";
                    UserId.Text = String.Empty;
                    TodoList.ItemsSource = null;
                    SignInButton.Content = "Sign In";
                }
                else
                {
                    var blah = await b2cUser.SignUpOrIn();
                    if (b2cUser.IsSignedIn() == true)
                    {
                        SignInButton.Content = "Clear Cache";
                        UserName.Text = b2cUser.Username;
                        UserId.Text = b2cUser.UserId;
                        GetTodoList();
                    }
                }
            }
            catch (Exception ex)
            {
                // An unexpected error occurred.
                string message = ex.Message;
                if (ex.InnerException != null)
                {
                    message += "Inner Exception : " + ex.InnerException.Message;
                }

                MessageBox.Show(message);
            }
        }
        private async void AddTodoItem(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(TodoText.Text))
                {
                    MessageBox.Show("Please enter a value for the To Do item name");
                    return;
                }

                //
                // Get an access token to call the To Do service.
                //
                //AuthenticationResult result = null;
                //try
                //{
                //    result = await authContext.AcquireTokenAsync(todoListResourceId, clientId, redirectUri, new PlatformParameters(PromptBehavior.Never));
                //}
                //catch (AdalException ex)
                //{
                //    // There is no access token in the cache, so prompt the user to sign-in.
                //    if (ex.ErrorCode == "user_interaction_required")
                //    {
                //        MessageBox.Show("Please sign in first");
                //        SignInButton.Content = "Sign In";
                //    }
                //    else
                //    {
                //        // An unexpected error occurred.
                //        string message = ex.Message;
                //        if (ex.InnerException != null)
                //        {
                //            message += "Error Code: " + ex.ErrorCode + "Inner Exception : " + ex.InnerException.Message;
                //        }

                //        MessageBox.Show(message);
                //    }

                //    return;
                //}

                //
                // Call the To Do service.
                //

                // Once the token has been returned by ADAL, add it to the http authorization header, before making the call to access the To Do service.
                // httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);

                // TODO This doesn't seem correct - user is not setup on the server side - we want to use a code token
                // not sure if we need the ID token too or if the user is in the code token
                // httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", b2cUser.IDToken);

                // Send Todo item as Json in Body, to POST to the todo list web api.

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                List<KeyValuePair<string, string>> toDoArray = new List<KeyValuePair<string, string>>();
                toDoArray.Add(new KeyValuePair<string, string>("Name", TodoText.Text) );
                var item = new TodoItem();
                item.Name = TodoText.Text;
                item.IsComplete = false;
                item.Owner = b2cUser.Username;
                string s = serializer.Serialize(item);
                HttpContent content = new StringContent(s, Encoding.UTF8, "application/json");

                // Call the To Do list service.
                HttpResponseMessage response = await httpClient.PostAsync(todoListBaseAddress + "/api/todo", content);

                if (response.IsSuccessStatusCode)
                {
                    TodoText.Text = "";
                    GetTodoList();
                }
                else
                {
                    MessageBox.Show("An error occurred : " + response.ReasonPhrase);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private async void GetTodoList(bool isAppStarting = false)
        {
            try
            {
                //
                // Get an access token to call the To Do service.
                //
                //AuthenticationResult result = null;
                //try
                //{
                //    result = await authContext.AcquireTokenAsync(todoListResourceId, clientId, redirectUri, new PlatformParameters(PromptBehavior.Never));
                //    SignInButton.Content = "Clear Cache";
                //}
                //catch (AdalException ex)
                //{
                //    // There is no access token in the cache, so prompt the user to sign-in.
                //    if (ex.ErrorCode == "user_interaction_required")
                //    {
                //        if (!isAppStarting)
                //        {
                //            MessageBox.Show("Please sign in to view your To-Do list");
                //            SignInButton.Content = "Sign In";
                //        }
                //    }
                //    else
                //    {
                //        // An unexpected error occurred.
                //        string message = ex.Message;
                //        if (ex.InnerException != null)
                //        {
                //            message += "Error Code: " + ex.ErrorCode + "Inner Exception : " + ex.InnerException.Message;
                //        }
                //        MessageBox.Show(message);
                //    }

                //    return;
                //}

                // Once the token has been returned by ADAL, add it to the http authorization header, before making the call to access the To Do list service.
                // httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", b2cUser.IDToken);

                // Call the To Do list service.
                HttpResponseMessage response = await httpClient.GetAsync(todoListBaseAddress + "/api/todo");

                if (response.IsSuccessStatusCode)
                {

                    // Read the response and databind to the GridView to display To Do items.
                    string s = await response.Content.ReadAsStringAsync();
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    List<TodoItem> toDoArray = serializer.Deserialize<List<TodoItem>>(s);

                    TodoList.ItemsSource = toDoArray.Select(t => new { t.Name });
                }
                else
                {
                    MessageBox.Show("An error occurred : " + response.ReasonPhrase);
                }

                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // This function clears cookies from the browser control used by ADAL.
        private void ClearCookies()
        {
            const int INTERNET_OPTION_END_BROWSER_SESSION = 42;
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_END_BROWSER_SESSION, IntPtr.Zero, 0);
        }

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int lpdwBufferLength);


    }
}
