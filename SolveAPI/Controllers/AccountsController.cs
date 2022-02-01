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
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json.Linq;

namespace SolveAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : BaseController
    {

        public AccountsController()
        {
        }

        // GET: api/Accounts
        [HttpGet]
        public TransactionResult GetAccounts()
        {
            TransactionResult retObj = validateAuth();
            if (!retObj.IsSuccesful) return retObj;

            retObj.Data         = DatabaseManager.Accounts.Values;
            retObj.IsSuccesful  = true;
            retObj.Status       = 200;
            retObj.Message      = $"Retrieved all accounts";
            return retObj;
        }

        // GET: api/Accounts/5
        [HttpGet("{id}")]
        public TransactionResult GetAccount(string id)
        {
            TransactionResult retObj = validateUserOperations(id);
            if(!retObj.IsSuccesful) return retObj;
            Account account;
            if(DatabaseManager.Accounts.TryGetValue(id,out account))
            {
                retObj.IsSuccesful  = true;
                retObj.Status       = 200;
                retObj.Message      = $"Retrieved account with id:{id}";
                retObj.Data         = account;
            }
            else
            {
                retObj.IsSuccesful  = false;
                retObj.Status       = 500;
                retObj.Message      = $"No account with id: {id}";
            }
            return retObj;
        }

        // PUT: api/Accounts/5
        [HttpPut("{id}")]
        public async Task<TransactionResult> PutAccount(string id, Account account)
        {
            //treat cases common for more actions
            TransactionResult retObj = this.validateAdminOperations(id);
            //if the user is not an admin we need to allow him to modify his own properties
            if(retObj.IsSuccesful == false || !isAuthenticated || CurrentUser.UID != id) return retObj;

            //validate more on particular cases
            if (string.IsNullOrWhiteSpace(id) || !DatabaseManager.ValidateAccount(account))
            {
                retObj.IsSuccesful  = false;
                retObj.Status       = 400;
                retObj.Message      = "Wrong input data";
                return retObj;
            }

            if(CurrentUser.AccessModifiers != account.AccessModifiers && CurrentUser.AccountType != AccountType.Admin) 
            {
                retObj.IsSuccesful  = false;
                retObj.Status       = 403;
                retObj.Message      = "Access not granted";
                return retObj;

            }
            try
            {
                FirebaseResponse response = await DatabaseManager.Client.SetAsync($"users/{id}", account);
                retObj.IsSuccesful  = true;
                retObj.Status       = 200;
                retObj.Message      = "Account updated successfully";
                retObj.Data         = account;
            }
            catch
            {
                retObj.IsSuccesful  = false;
                retObj.Status       = 500;
                retObj.Message      = $"Error while sending account at path : /users/{id}";
            }
            return retObj;
        }

        // DELETE: api/Accounts/5
        [HttpDelete("{id}")]
        public async Task<TransactionResult> DeleteAccount(string id)
        {
            TransactionResult retObj = validateAdminOperations(id);
            if (retObj.IsSuccesful == false) return retObj;
            if (!DatabaseManager.Accounts.ContainsKey(id))
            {
                retObj.Status       = 400;
                retObj.IsSuccesful  = false;
                retObj.Message      = $"No user registered with id: {id}";
                return retObj;
            }
            try
            {
                FirebaseResponse response = await DatabaseManager.Client.DeleteAsync($"/users/{id}");
                retObj.IsSuccesful  = true;
                retObj.Status       = 200;
                retObj.Message      = "Account deleted successfully";
            }
            catch
            {
                retObj.IsSuccesful  = false;
                retObj.Status       = 500;
                retObj.Message      = $"Error while deleting account at path : /users/{id}";
            }
            return retObj;
        }



        [Route("/login")]
        [HttpPost]
        public async Task<TransactionResult> Login(Dictionary<string,string> credentials)
        {
            TransactionResult retObj = new TransactionResult();
            if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
            {
                retObj.IsSuccesful = false;
                retObj.Data = null;
                retObj.Status = 400;
                retObj.Message = "User already signed in";
                return retObj;
            }


            if (credentials == null)
            {
                //we didn't get any data from the user, send a message about this
                retObj.IsSuccesful = false;
                retObj.Status = 400;
                retObj.Message = "Null credentials";
                retObj.Data = null;
                retObj.UserId = null;
                return retObj;
            }
            if (!credentials.ContainsKey("email") || !credentials.ContainsKey("password") || string.IsNullOrWhiteSpace(credentials["email"]) || string.IsNullOrWhiteSpace(credentials["password"]))
            {
                //we didn't get any data from the user, send a message about this
                retObj.IsSuccesful = false;
                retObj.Status = 400;
                retObj.Message = "No email/password";
                retObj.Data = null;
                return retObj;
            }
            //send email and password for further validation
            retObj = await DatabaseManager.AuthenticateUser(credentials["email"], credentials["password"]);
            if (retObj.IsSuccesful)
            {
                //we found a user coresponding for the email/password
                //store session cookie for him
                Account? currUser = retObj.Data as Account;
                var claims = new List<Claim>();
                if (currUser != null)
                {
                    //cast done succesfully, we can give claims to the user by using AccessModifiers
                    if (currUser.UID != null) claims.Add(new Claim("UID", currUser.UID));
                    claims.Add(new Claim(ClaimTypes.Role, currUser.AccountType.ToString()));
                    claims.Add(new Claim(ClaimTypes.Email, credentials["email"]));
                    if (currUser.AccessModifiers != null)
                    {
                        foreach (string key in currUser.AccessModifiers.Keys) claims.Add(new Claim(key, currUser.AccessModifiers[key].ToString()));
                    }
                    //dont send the password hash back
                    currUser.PasswordHash = string.Empty;
                    CurrentUser = currUser;
                }
                //proceed forward with the login
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    AllowRefresh = true,
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(20),
                    IsPersistent = true
                };
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsPrincipal(claimsIdentity)), authProperties);
                retObj.Data = currUser;

            }
            return retObj;
        }

        [Route("/logout")]
        [HttpGet]
        public async Task<TransactionResult> Logout()
        {
            //logout doesn't need to be secure
            TransactionResult retObj = new TransactionResult();
            //check if someone is signed in 
            if (User == null || User.Identity == null || !User.Identity.IsAuthenticated)
            {
                retObj.Data = null;
                retObj.Message = "Nobody to sign out";
                retObj.Status = 400;
                retObj.IsSuccesful = false;
                return retObj;
            }
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            retObj.Data = null;
            retObj.Status = 200;
            retObj.IsSuccesful = true;
            retObj.Message = "Succesfully signed out";
            return retObj;
        }
        [Route("/register")]
        [HttpPost]
        private async Task<TransactionResult> Register(Account account)
        {
            TransactionResult retObj = new TransactionResult();
            if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
            {

                retObj.IsSuccesful = false;
                retObj.Data = null;
                retObj.Status = 400;
                retObj.Message = "User already signed in";
                return retObj;
            }
            retObj = await DatabaseManager.RegisterUser(account);
            if (retObj.IsSuccesful)
            {
                //succesfully made an account
                //remove passwordHash from memory
                Account? currUser = retObj.Data as Account;
                var claims = new List<Claim>();
                if (currUser != null)
                {
                    //cast done succesfully, we can give claims to the user by using AccessModifiers
                    if (currUser.UID != null) claims.Add(new Claim("UID", currUser.UID));
                    claims.Add(new Claim(ClaimTypes.Role, currUser.AccountType.ToString()));
                    claims.Add(new Claim(ClaimTypes.Email, currUser.Email));
                    if (currUser.AccessModifiers != null)
                    {
                        foreach (string key in currUser.AccessModifiers.Keys) claims.Add(new Claim(key, currUser.AccessModifiers[key].ToString()));
                    }
                    //dont send the password hash back
                    currUser.PasswordHash = string.Empty;
                    CurrentUser = currUser;
                }
                //proceed forward with the login
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    AllowRefresh = true,
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(20),
                    IsPersistent = true
                };
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                retObj.Data = currUser;
            }
            return retObj;
        }

    }
}
