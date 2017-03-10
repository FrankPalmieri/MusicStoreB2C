using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MusicStoreB2C.Controllers
{
    [Authorize]
    public class TasksUIController : Controller
    {
        private static string serviceUrl;
        private readonly ILogger<TasksUIController> _logger;
        private readonly AppSettings _appSettings;

        public TasksUIController(IOptions<AppSettings> options, ILoggerFactory loggerFactory)
        {
            _appSettings = options.Value;
            _logger = loggerFactory.CreateLogger<TasksUIController>();
            serviceUrl = Startup.TaskServiceUrl;
        }
    
        // GET: TodoList
        public async Task<ActionResult> Index()
        {
            try {
                var blah = User.Identities.First().BootstrapContext as string;
                // var bootstrapContext = ClaimsPrincipal.Current.Identities.First().BootstrapContext as ClaimsIdentity;

                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, serviceUrl + "/api/tasks");

                // Add the token acquired from ADAL to the request headers
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", blah);
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    String responseString = await response.Content.ReadAsStringAsync();
                    JArray tasks = JArray.Parse(responseString);
                    ViewBag.Tasks = tasks;
                    return View();
                }
                else
                {
                    // If the call failed with access denied, show the user an error indicating they might need to sign-in again.
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return new RedirectResult("/Error?message=Error: " + response.ReasonPhrase + " You might need to sign in again.");
                    }
                }

                return new RedirectResult("/Error?message=An Error Occurred Reading To Do List: " + response.StatusCode);
            }
            catch (Exception ex)
            {
                return new RedirectResult("/Error?message=An Error Occurred Reading To Do List: " + ex.Message);
            }
        }

        // POST: TodoList/Create
        [HttpPost]
        public async Task<ActionResult> Create(string description)
        {
            try
            {
                var blah = User.Identities.First().BootstrapContext as string;
                // var bootstrapContext = ClaimsPrincipal.Current.Identities.First().BootstrapContext as ClaimsIdentity;

                HttpContent content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("Text", description) });
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, serviceUrl + "/api/tasks/");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", /* bootstrapContext.Token*/ blah);
                request.Content = content;
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return new RedirectResult("/TasksUI");
                }
                else
                {
                    // If the call failed with access denied, show the user an error indicating they might need to sign-in again.
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return new RedirectResult("/Error?message=Error: " + response.ReasonPhrase + " You might need to sign in again.");
                    }
                }

                return new RedirectResult("/Error?message=Error reading your To-Do List.");
            }
            catch (Exception ex)
            {
                return new RedirectResult("/Error?message=Error reading your To-Do List.  " + ex.Message);
            }
        }

        // POST: /TodoList/Delete
        [HttpPost]
        public async Task<ActionResult> Delete(string id)
        {
            try
            {
                var blah = User.Identities.First().BootstrapContext as string;
                // var bootstrapContext = ClaimsPrincipal.Current.Identities.First().BootstrapContext as ClaimsIdentity;

                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, serviceUrl + "/api/tasks/" + id);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", /* bootstrapContext.Token*/ blah);
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return new RedirectResult("/TasksUI");
                }
                else
                {
                    // If the call failed with access denied, then drop the current access token from the cache, 
                    // and show the user an error indicating they might need to sign-in again.
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return new RedirectResult("/Error?message=Error: " + response.ReasonPhrase + " You might need to sign in again.");
                    }
                }

                return new RedirectResult("/Error?message=Error deleting your To-Do Item.");
            }
            catch (Exception ex)
            {
                return new RedirectResult("/Error?message=Error deleting your To-Do Item.  " + ex.Message);
            }
        }
    }
}