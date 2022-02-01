using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace SolveAPI.Models
{
    public enum AccountType
    {
        User = 0,
        Admin = 1,
        Premium = 2
    }

    public class Account
    {
        #region Private Properties

        private string? uid;
        private string? firstName;
        private string? lastName;
        private string? profilePic;
        private string? passwordHash;
        private string? email;
        private ConcurrentDictionary<string, bool> accessModifiers;
        private AccountType accountType;

        #endregion

        public Account()
        {
            this.accessModifiers = new ConcurrentDictionary<string, bool>();
        }

        //only get we dont need to set the UID
        [JsonIgnore]
        public string UID
        {
            get => uid ?? string.Empty;
            set => uid = value ?? string.Empty;
        }
        public string PasswordHash
        {
            get => passwordHash ?? string.Empty;
            set => passwordHash = value ?? string.Empty;
        }

        public string FirstName
        {
            get => firstName ?? string.Empty;
            set => firstName = value ?? string.Empty;
        }

        public string LastName
        {
            get => lastName ?? string.Empty;
            set => lastName = value ?? string.Empty;
        }

        public string ProfilePic
        {
            get => profilePic ?? string.Empty;
            set => profilePic = value ?? string.Empty;
        }
        public string Email
        {
            get => email ?? string.Empty;
            set => email = value ?? string.Empty;
        }

        /// <summary>
        /// Verifiers that a user has access to a certain feature
        /// true - has access
        /// false - no access
        /// </summary>
        public ConcurrentDictionary<string, bool> AccessModifiers => accessModifiers ?? new ConcurrentDictionary<string, bool>();

        public AccountType AccountType
        {
            get => accountType;
            set => accountType = value;
        }

        public static ConcurrentDictionary<string, bool> CreateInitialAccess()
        {
            ConcurrentDictionary<string, bool> retObj = new ConcurrentDictionary<string, bool>();


            retObj.TryAdd("expenseAccess", true);
            retObj.TryAdd("taskAccess", true);
            retObj.TryAdd("profile", true);
            retObj.TryAdd("modifyOthers", false);
            retObj.TryAdd("notifications", false);
            return retObj;
        }
        public void SetInitialAccess()
        {
            //not sure if it thread safe
            accessModifiers.Clear();

            accessModifiers.TryAdd("expenseAccess", true);
            accessModifiers.TryAdd("taskAccess", true);
            accessModifiers.TryAdd("profile", true);
            accessModifiers.TryAdd("modifyOthers", false);
            accessModifiers.TryAdd("notifications", false);
        }

    }
}
