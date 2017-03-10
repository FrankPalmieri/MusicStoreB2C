using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using MusicStoreB2C.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicStoreB2C.Filters;

namespace MusicStoreB2C.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ValidateModel]
    public class TasksController : Controller
    {
        // In this service we're using an in-memory list to store tasks, just to keep things simple.
        // This means that all of your tasks will be lost each time you run the service
        private static List<Models.TaskItem> db = new List<Models.TaskItem>();

        private readonly AppSettings _appSettings;
        private readonly ILogger<TasksController> _logger;

        public TasksController(IOptions<AppSettings> options, ILoggerFactory loggerFactory)
        {
            _appSettings = options.Value;
            _logger = loggerFactory.CreateLogger<TasksController>();
        }

        [HttpGet]
        public IEnumerable<Models.TaskItem> Get()
        {
            // string owner = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            string owner = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            IEnumerable<Models.TaskItem> userTasks = db.Where(t => t.Owner == owner);
            return userTasks;
        }

        [HttpPost]
        public void Post(Models.TaskItem task)
        {
            if (task.Text == null || task.Text == string.Empty)
                throw new WebException("Please provide a task description");

            // string owner = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            string owner = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            task.Owner = owner;
            task.Completed = false;
            task.DateModified = DateTime.UtcNow;
            db.Add(task);
        }

        [HttpDelete("{id}")]
        public void Delete(int id)
        {
            // string owner = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            string owner = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            Models.TaskItem task = db.Where(t => t.Owner.Equals(owner) && t.Id.Equals(id)).FirstOrDefault();
            db.Remove(task);
        }
    }
}
