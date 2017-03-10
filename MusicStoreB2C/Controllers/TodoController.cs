using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MusicStoreB2C.Models;
using MusicStoreB2C.Filters;
using Microsoft.AspNetCore.Diagnostics;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Net;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace MusicStoreB2C.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ValidateModel]
    public class TodoController : Controller
    {
        private readonly ITodoRepository _todoRepository;
        public TodoController(ITodoRepository todoRepository)
        {
            _todoRepository = todoRepository;
        }


        #region snippet_GetAll 
        [HttpGet]
        public IEnumerable<TodoItem> GetAll()
        {
            // return _todoRepository.GetAll();
            string owner = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            IEnumerable<Models.TodoItem> toDos = _todoRepository.GetAll().Where(t => t.Owner == owner);
            return toDos;
        }


        [HttpGet("{id}", Name = "GetTodo")]
        public IActionResult GetById(long id)
        {
            var item = _todoRepository.Find(id);
            if (item == null)
            {
                return NotFound(new ItemError { Key = id });
            }
            return new ObjectResult(item);
        }
        #endregion
        #region snippet_Create 
        [HttpPost]
        public IActionResult Create([FromBody] TodoItem item)
        {
            if (item == null)
            {
                return BadRequest(new ApiError { Message = "No item found in body" });
            }
            if (item.Name == null || item.Name == string.Empty)
                throw new WebException("Please provide a todo name");

            string owner = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            item.Owner = owner;
            item.IsComplete = false;
            item.DateModified = DateTime.UtcNow;

            _todoRepository.Add(item);

            return CreatedAtRoute("GetTodo", new { id = item.Key }, item);
        }
        #endregion


        #region snippet_Update 
        [HttpPut("{id}")]
        public IActionResult Update(long id, [FromBody] TodoItem item)
        {
            if (item == null || item.Key != id)
            {
                return BadRequest(new ApiError { Message = "No item found in body or item key does not match id" });
            }

            var todo = _todoRepository.Find(id);
            if (todo == null)
            {
                return NotFound(new ItemError { Key = id });
            }

            todo.IsComplete = item.IsComplete;
            todo.Name = item.Name;

            _todoRepository.Update(todo);
            return new NoContentResult();
        }

        [HttpPatch("{id}")]
        public IActionResult PartialUpdate(long id, [FromBody] JObject item)
        {
            if (item == null || item["key"].Value<long>() != id)
            {
                return BadRequest(new ApiError { Message = "No item found in body or item key does not match id" });
            }

            var todo = _todoRepository.Find(id);
            if (todo == null)
            {
                return NotFound(new ItemError { Key = id });
            }
            var isComplete = item["isComplete"];
            if (isComplete != null)
            {
                todo.IsComplete = isComplete.Value<bool>();
                todo.DateModified = DateTime.UtcNow;
            }
            var name = item["name"];
            if (name != null)
            {
                todo.Name = item["name"].Value<string>();
                todo.DateModified = DateTime.UtcNow;
            }

            _todoRepository.Update(todo);
            return new NoContentResult();
        }
        #endregion


        #region snippet_Delete 
        [HttpDelete("{id}")]
        public IActionResult Delete(long id)
        {
            var todo = _todoRepository.Find(id);
            if (todo == null)
            {
                return NotFound(new ItemError { Key = id });
            }
            string owner = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            // Todo - allow admin to delete any item
            if (todo.Owner != owner)
            {
                return BadRequest(new ApiError { Message = "No item found in body or item key does not match id" });
            }
            _todoRepository.Remove(id);
            return new NoContentResult();
        }
        #endregion
    }
}
