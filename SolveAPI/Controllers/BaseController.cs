using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SolveAPI.Models;
using System.Security.Claims;

namespace SolveAPI.Controllers
{
    public abstract class BaseController : ControllerBase
    {
        private Account? _currentUser;
        protected Account? CurrentUser
        {
            get
            {
                if( _currentUser != null ) return _currentUser;
                string id = GetClaim("UID");
                if (string.IsNullOrWhiteSpace(id) || !DatabaseManager.Accounts.TryGetValue(id, out _currentUser)) return null; //we can't find the user
                return _currentUser;
                
            }
            set
            {
                if (_currentUser == null) _currentUser = value;
            }
        }

        protected bool isAuthenticated => User != null && User.Identity != null && User.Identity.IsAuthenticated;


        protected string GetClaim(string value)
        {
            var claims = HttpContext?.User?.Claims;
            if (claims == null || string.IsNullOrWhiteSpace(value)) return string.Empty;
            if (claims == null) return string.Empty;
            return claims?.Where(c => c.Type == value).Select(c => c.Value).FirstOrDefault() ?? string.Empty;
        }
        protected TransactionResult validateAdminOperations(string? id)
        {
            TransactionResult retObj = validateUserOperations(id);
            if(!retObj.IsSuccesful) return retObj;

            if (CurrentUser == null || CurrentUser.AccountType != AccountType.Admin)
            {
                retObj.IsSuccesful  = false;
                retObj.Status       = 403;
                retObj.Message      = "You don't have the right for this operation";
                return retObj;
            }
            if (!DatabaseManager.Accounts.ContainsKey(id))
            {
                retObj.IsSuccesful  = false;
                retObj.Status       = 400;
                retObj.Message      = $"Cannot find account with ID: {id}";
                return retObj;
            }
            retObj.IsSuccesful      = true;
            return retObj;
        }

        protected TransactionResult validateAdminOperations()
        {
            TransactionResult retObj = validateAuth();
            if (!retObj.IsSuccesful) return retObj;

            if (CurrentUser == null || CurrentUser.AccountType != AccountType.Admin)
            {
                retObj.IsSuccesful  = false;
                retObj.Status       = 403;
                retObj.Message      = "You don't have the right for this operation";
                return retObj;
            }
            retObj.IsSuccesful = true;
            return retObj;
        }

        protected TransactionResult validateUserOperations(string? id)
        {
            TransactionResult retObj = validateAuth();
            if (!retObj.IsSuccesful) return retObj;

            if (string.IsNullOrWhiteSpace(id))
            {
                retObj.IsSuccesful  = false;
                retObj.Status       = 400;
                retObj.Message      = "Wrong id";
                return retObj;
            }
            retObj.IsSuccesful = true;
            return retObj;
        }

        protected TransactionResult validateAuth()
        {
            TransactionResult retObj = new TransactionResult();
            if (!this.isAuthenticated)
            {
                retObj.IsSuccesful  = false;
                retObj.Status       = 401;
                retObj.Message      = "Login first";
                return retObj;
            }
            retObj.IsSuccesful = true;
            return retObj;
        }
    }
}
