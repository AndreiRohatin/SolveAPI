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
using System.Collections.Concurrent;

namespace SolveAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminsController : BaseController
    {

        public AdminsController()
        {
        }


        // GET: api/Admins
        [HttpGet]
        public TransactionResult GetAdmins()
        {
            TransactionResult retObj = validateAdminOperations();
            if (!retObj.IsSuccesful) return retObj;

            retObj.Data         = DatabaseManager.Accounts.Values.Where(o=>o.AccountType == AccountType.Admin);
            retObj.IsSuccesful  = true;
            retObj.Status       = 200;
            retObj.Message      = $"Retrieved all admins";
            return retObj;
        }

        // GET: api/Admins/5
        [HttpGet("{id}")]
        public TransactionResult GetAdmin(string id)
        {
            TransactionResult retObj = validateAdminOperations(id);
            if (!retObj.IsSuccesful) return retObj;

            Account selectedAccount = null;
            if(!DatabaseManager.Accounts.TryGetValue(id, out selectedAccount))
            {
                retObj.Status       = 500;
                retObj.Message      = "Internal Error";
                retObj.Data         = null;
                retObj.IsSuccesful  = false;
                return retObj;
            }
            retObj.Data             = selectedAccount;
            retObj.IsSuccesful      = true;
            retObj.Status           = 200;
            retObj.Message          = $"Retrieved admin with id{id}";
            return retObj;

        }

        // POST: api/Admins/5
        [HttpPost("{id}")]
        public async Task<TransactionResult> ModifyAdmin(Dictionary<string,string> accessModifiers)
        {
            TransactionResult retObj = new TransactionResult();
            if (accessModifiers == null || !accessModifiers.ContainsKey("id") || !accessModifiers.ContainsKey("action")){
                retObj.Status       = 400;
                retObj.Message      = "Invalid request";
                retObj.IsSuccesful  = false;
                return retObj;
            }
            string id = accessModifiers["id"];
            string action = accessModifiers["action"];
            accessModifiers.Remove("id");
            accessModifiers.Remove("action");
            retObj = validateAdminOperations(id);
            if (!retObj.IsSuccesful) return retObj;
            try
            {
                switch (action)
                {
                    //change only access modifiers
                    case "modify":
                        {
                            if(accessModifiers == null)
                            {
                                retObj.Status       = 400;
                                retObj.Message      = "No data recieved";
                                retObj.IsSuccesful  = false;
                                return retObj;
                            }
                            await DatabaseManager.Client.SetAsync($"users/{id}/AccessModifiers", accessModifiers);
                            retObj.Status       = 200;
                            retObj.IsSuccesful  = true;
                            retObj.Message      = $"Successfuly updated admin with id: {id}";
                            return retObj;
                        }
                    //also update account type
                    case "create":
                        {
                            if (accessModifiers == null)
                            {
                                retObj.Status       = 400;
                                retObj.Message      = "No data recieved";
                                retObj.IsSuccesful  = false;
                                return retObj;
                            }
                            //change access modifiers
                            await DatabaseManager.Client.SetAsync($"users/{id}/AccessModifiers", accessModifiers);
                            //create admin
                            await DatabaseManager.Client.SetAsync($"users/{id}/AccountType", AccountType.Admin);
                            retObj.Status       = 200;
                            retObj.IsSuccesful  = true;
                            retObj.Message      = $"Successfuly updated admin with id: {id}";
                            return retObj;
                        }
                    //delete access modifiers & account type
                    case "delete":
                        {
                            //downgrade to basic user
                            await DatabaseManager.Client.SetAsync($"users/{id}/AccountType", AccountType.User);
                            await DatabaseManager.Client.SetAsync($"users/{id}/AccessModifiers", Account.CreateInitialAccess());
                            retObj.Status       = 200;
                            retObj.IsSuccesful  = true;
                            retObj.Message      = $"Successfuly downgraded admin with id: {id}";
                            return retObj;
                        }
                    default:
                        {
                            retObj.Status       = 400;
                            retObj.Message      = "Unkown action";
                            retObj.IsSuccesful  = false;
                            return retObj;
                        }
                }
            }
            catch
            {
                retObj.Status       = 502;
                retObj.Message      = "Error while sending data to the supplier";
                retObj.IsSuccesful  = false;
                return retObj;
            }
        }
    }
}
