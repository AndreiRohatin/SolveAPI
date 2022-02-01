#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using SolveAPI.Models;

namespace SolveAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : BaseController
    {

        public TasksController()
        {
        }

        // GET: api/Tasks
        [HttpGet]
        public TransactionResult GetTasks()
        {
            TransactionResult retObj = validateAuth();
            if (!retObj.IsSuccesful) return retObj;
            //there is no point in returning all tasks from all users
            //return all tasks from the current user
            if (CurrentUser != null && !string.IsNullOrWhiteSpace(CurrentUser.UID) && DatabaseManager.Tasks.ContainsKey(CurrentUser.UID))
            {
                retObj.Data         = DatabaseManager.Tasks[CurrentUser.UID].Values.ToList();
                retObj.IsSuccesful  = true;
                retObj.Status       = 200;
                retObj.Message      = $"Retrieved all requested tasks for user with id: {CurrentUser.UID}";
                return retObj;
            }
            retObj.Status       = 500;
            retObj.Message      = "Internal Error";
            retObj.IsSuccesful  = false;
            return retObj;
        }

        // GET: api/Tasks/5
        [HttpGet("{id}")]
        public TransactionResult GetTask(string id)
        {
            TransactionResult retObj = validateUserOperations(id);
            if (!retObj.IsSuccesful) return retObj;
            if (CurrentUser != null && !string.IsNullOrWhiteSpace(CurrentUser.UID) && DatabaseManager.Tasks.ContainsKey(CurrentUser.UID))
            {
                SolveAPI.Models.Task currentTask = null;
                if(DatabaseManager.Tasks[CurrentUser.UID].TryGetValue(id,out currentTask))
                {
                    retObj.Data         = currentTask;
                    retObj.IsSuccesful  = true;
                    retObj.Status       = 200;
                    retObj.Message      = $"Retrieved requested task with id: {id}";
                    return retObj;
                }
                retObj.Status = 400;
                retObj.Message = $"There is no task with id: {id}";
                retObj.IsSuccesful = false;
                return retObj;
            }
            retObj.Status       = 500;
            retObj.Message      = "Internal Error";
            retObj.IsSuccesful  = false;
            return retObj;
        }

        // PUT: api/Tasks/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<TransactionResult> PutTask(string id, SolveAPI.Models.Task task)
        {
            TransactionResult retObj = validateUserOperations(id);
            if (!retObj.IsSuccesful) return retObj;
            if (!DatabaseManager.ValidateTask(task))
            {
                retObj.Status       = 400;
                retObj.IsSuccesful  = false;
                retObj.Message      = "Task object is not valid";
                return retObj;
            }
            //if the user doesn't have task or the task he wants to update doesn't have sent id we should notify him
            if (!DatabaseManager.Tasks.ContainsKey(CurrentUser.UID) || !DatabaseManager.Tasks[CurrentUser.UID].ContainsKey(id))
            {
                retObj.Status       = 400;
                retObj.IsSuccesful  = false;
                retObj.Message      = "No tasks for current user / Task with current id doesn't exist in list";
                return retObj;
            }
            try
            {
                FirebaseResponse response = await DatabaseManager.Client.SetAsync($"tasks/{CurrentUser.UID}/{id}", task);
                retObj.IsSuccesful  = true;
                retObj.Status       = 200;
                retObj.Message      = "Account updated successfully";
                retObj.Data         = task;
            }
            catch
            {
                retObj.IsSuccesful  = false;
                retObj.Status       = 500;
                retObj.Message      = "Error while adding task";
            }
            return retObj;
        }

        // POST: api/Tasks
        [HttpPost]
        public async Task<TransactionResult> PostTask(SolveAPI.Models.Task task)
        {
            TransactionResult retObj = validateAuth();
            if (!retObj.IsSuccesful) return retObj;
            if (!DatabaseManager.ValidateTask(task))
            {
                retObj.Status       = 400;
                retObj.IsSuccesful  = false;
                retObj.Message      = "Task object is not valid";
            }
            try
            {
                FirebaseResponse response = await DatabaseManager.Client.PushAsync($"tasks/{CurrentUser.UID}", task);
                retObj.IsSuccesful  = true;
                retObj.Status       = 200;
                retObj.Message      = "Account updated successfully";
                retObj.Data         = task;
            }
            catch
            {
                retObj.IsSuccesful  = false;
                retObj.Status       = 500;
                retObj.Message      = "Error while adding task";
            }
            return retObj;
        }

        // DELETE: api/Tasks/5
        [HttpDelete("{id}")]
        public async Task<TransactionResult> DeleteTask(string id)
        {
            TransactionResult retObj = this.validateUserOperations(id);
            if (retObj.IsSuccesful == false) return retObj;
            if (DatabaseManager.Tasks.ContainsKey(CurrentUser.UID) && !DatabaseManager.Tasks[CurrentUser.UID].ContainsKey(id))
            {
                retObj.Status       = 400;
                retObj.IsSuccesful  = false;
                retObj.Message      = "No tasks for current user / Task with current id doesn't exist in list";
                return retObj;
            }
            try
            {
                FirebaseResponse response = await DatabaseManager.Client.DeleteAsync($"/users/{id}");
                retObj.IsSuccesful  = true;
                retObj.Status       = 200;
                retObj.Message      = "Task deleted successfully";
            }
            catch
            {
                retObj.IsSuccesful = false;
                retObj.Status = 500;
                retObj.Message = $"Error while deleting account at path : /users/{id}";
            }
            return retObj;
        }

    }
}
