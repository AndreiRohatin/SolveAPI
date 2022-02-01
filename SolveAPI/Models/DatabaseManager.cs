using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using System.Collections.Concurrent;
using System.Net.Mail;
using Newtonsoft.Json;

namespace SolveAPI.Models
{
    public static class DatabaseManager
    {
        #region Private Members
        private static IFirebaseClient? _client;
        private static ConcurrentDictionary<string,Account>? _accounts;
        private static ConcurrentDictionary<string, ConcurrentDictionary<string,SolveAPI.Models.Task>>? _tasks;
        private static IFirebaseConfig _config = new FirebaseConfig
        {
            AuthSecret  = "tNq3CPvMuLhJYvAcSPBsnvK3GaJUDcCDzGEz13do",
            BasePath    = "https://solveai-c0a88-default-rtdb.europe-west1.firebasedatabase.app/"
        };
        #endregion


        //cache members so we don't need to have that many server requests
        #region Public Members
        //ignore null reference since api is useless without database connection
        public static IFirebaseClient? Client => _client;
        public static ConcurrentDictionary<string, Account> Accounts => _accounts ?? new ConcurrentDictionary<string, Account>();
        public static ConcurrentDictionary<string, ConcurrentDictionary<string,SolveAPI.Models.Task>> Tasks => _tasks ?? new ConcurrentDictionary<string, ConcurrentDictionary<string,SolveAPI.Models.Task>>();

        #endregion

        #region Public Functions

        public static async Task<TransactionResult> AuthenticateUser(string email, string passwordHash)
        {
            //make a request to the database for all users to be sure that everything is up to date

            //verify data before proceeding forward
            if (_client == null) return new TransactionResult(null, false) { Message = "Database connection is down" };
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(passwordHash)) return new TransactionResult(null, false) { Message = "no email/password" };

            FirebaseResponse? rspv = null;
            Dictionary<string, Account>? result = null;
            try
            {
                rspv = await _client.GetAsync("/users");
                result = rspv.ResultAs<Dictionary<string,Account>>();
            }
            catch
            {
                return new TransactionResult(null, false) { Message = "Error while retriving data" };
            }
            if (result == null) return new TransactionResult(null, false) { Message = "Error while casting response" };

            foreach (var user in result)
            {
                //verify we have some data
                if (user.Value == null || string.IsNullOrWhiteSpace(user.Value.Email) || string.IsNullOrWhiteSpace(user.Value.PasswordHash)) continue;

                string currEmail = user.Value.Email;
                string currPassword = user.Value.PasswordHash;
                //we found our user
                if (currEmail == email )
                {
                    user.Value.UID = user.Key;
                    return new TransactionResult(user.Value, true) { Message = "Login done", Status = 200 };
                }
            }
            return new TransactionResult(null, false) { Message = "User not found" };

        }
        //Send Account to the database
        public static async Task<TransactionResult> RegisterUser(Account account)
        {
            if (_client == null) return new TransactionResult(null, false) { Message = "Database connection is down", Status = 502 };
            try
            {
                //push data
                if (ValidateAccount(account) && _accounts != null)
                {
                    TransactionResult? rspv = null;
                    Parallel.ForEach(_accounts.Values, (pushedAccount,state) => {
                        //check if email is not in use
                        if (account.Email == pushedAccount.Email)
                        {
                            rspv = new TransactionResult(null, false) { Message = "Email already in use", Status = 400 };
                            state.Break();
                        }
                    });
                    if(rspv != null) return rspv;
                    try
                    {
                        //not sure if I need to lock the account before this
                        account.SetInitialAccess();
                        PushResponse response = await _client.PushAsync("/users", account);
                        account.UID = response.Result.name;
                    }
                    catch
                    {
                        return new TransactionResult(null, false) { Status = 500, Message = "Error while creating account (database related)" };
                    }
                }
                else return new TransactionResult(null, false) { Message = "Invalid Account", Status = 400 };
            }
            catch
            {
                return new TransactionResult(null, false) { Message = "Error while sending data", Status = 500 };
            }
            return new TransactionResult(account, true) { Message = "Account registered", Status = 200 };
        }
        //instatiate the database and get all resources
        public static async void Start()
        {
            _client = new FirebaseClient(_config);
            _accounts = new ConcurrentDictionary<string, Account>();
            _tasks = new ConcurrentDictionary<string, ConcurrentDictionary<string,SolveAPI.Models.Task>>();

            //create real time listeners
            EventRootResponse<dynamic> accountChildChangedListener = await _client.OnChangeGetAsync<dynamic>("users", (events, args) => {
                try
                {
                    //cast doesnt work for some reason
                    Dictionary<string, Account>? accounts = null;
                    string jsonRspv = JsonConvert.SerializeObject(args);
                    accounts = JsonConvert.DeserializeObject<Dictionary<string, Account>>(jsonRspv);
                    if (accounts != null)
                    {
                        Parallel.ForEach(accounts.Keys, key => {
                            Account? currentAccount = null;
                            if (_accounts.TryGetValue(key, out currentAccount))
                            {
                                lock (currentAccount)
                                {
                                    //delete access modifiers that are not anymore active
                                    IEnumerable<string> keysToDelete = currentAccount.AccessModifiers.Keys.Except(accounts[key].AccessModifiers.Keys);
                                    foreach (string deleteKey in keysToDelete) currentAccount.AccessModifiers.TryRemove(deleteKey, out _);
                                    //find fields to update, don't update the whole object
                                    if (currentAccount.Email != accounts[key].Email) currentAccount.Email = accounts[key].Email;
                                    if (currentAccount.FirstName != accounts[key].FirstName) currentAccount.FirstName = accounts[key].FirstName;
                                    if (currentAccount.LastName != accounts[key].LastName) currentAccount.LastName = accounts[key].LastName;
                                    if (currentAccount.ProfilePic != accounts[key].ProfilePic) currentAccount.ProfilePic = accounts[key].ProfilePic;
                                    if (currentAccount.AccountType != accounts[key].AccountType) currentAccount.AccountType = accounts[key].AccountType;
                                    foreach (string accessKey in currentAccount.AccessModifiers.Keys)
                                    {
                                        bool currValue, newValue;
                                        if (currentAccount.AccessModifiers.TryGetValue(accessKey, out currValue) && accounts[key].AccessModifiers.TryGetValue(accessKey, out newValue) && currValue != newValue)
                                        {
                                            currentAccount.AccessModifiers.TryUpdate(accessKey, newValue, currValue);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                accounts[key].UID = key;
                                _accounts.TryAdd(key, accounts[key]);
                            }
                        });
                    }
                }
                catch {/*just to be sure we don't get any exceptions that might stop our api*/}
            });
            EventRootResponse<dynamic> taskChildChangedListener = await _client.OnChangeGetAsync<dynamic>("tasks", (events, args) =>
            {
                try
                {
                    Dictionary<string, Dictionary<string, SolveAPI.Models.Task>>? tasks = null;
                    string jsonRspv = JsonConvert.SerializeObject(args);
                    tasks = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, SolveAPI.Models.Task>>>(jsonRspv);

                    if (tasks != null)
                    {
                        Parallel.ForEach(tasks.Keys, UserID =>
                        {
                            ConcurrentDictionary<string, SolveAPI.Models.Task>? taskList = null;
                            if (_tasks.TryGetValue(UserID, out taskList))
                            {
                                foreach (string key in tasks[UserID].Keys)
                                {
                                    SolveAPI.Models.Task? currentTask = null;
                                    if (_tasks[UserID].TryGetValue(key, out currentTask))
                                    {
                                        lock (currentTask)
                                        {
                                            if (currentTask.Date != tasks[UserID][key].Date) currentTask.Date = tasks[UserID][key].Date;
                                            if (currentTask.Name != tasks[UserID][key].Name) currentTask.Name = tasks[UserID][key].Name;
                                            if (currentTask.Type != tasks[UserID][key].Type) currentTask.Type = tasks[UserID][key].Type;
                                            if (currentTask.Priority != tasks[UserID][key].Priority) currentTask.Priority = tasks[UserID][key].Priority;
                                            if (currentTask.RepetitiveTemplate != tasks[UserID][key].RepetitiveTemplate) currentTask.RepetitiveTemplate = tasks[UserID][key].RepetitiveTemplate;
                                            if (currentTask.IsRepetitive != tasks[UserID][key].IsRepetitive) currentTask.IsRepetitive = tasks[UserID][key].IsRepetitive;
                                        }
                                    }
                                    else _tasks[key].TryAdd(key, tasks[UserID][key]);
                                }
                            }
                            else _tasks.TryAdd(UserID, new ConcurrentDictionary<string, SolveAPI.Models.Task>(tasks[UserID]));
                        });
                    }
                }
                catch { /*in case off some errors thrown by parallel foreach or json deserialization*/ }
            });
        }

        public static bool ValidateAccount(Account account)
        {
            //verify data is not null
            if (account == null || string.IsNullOrEmpty(account.FirstName) || string.IsNullOrEmpty(account.LastName) || string.IsNullOrEmpty(account.PasswordHash) || string.IsNullOrEmpty(account.Email)) return false;
            
            //validate email address
            try
            {
                return account.Email.Equals((new MailAddress(account.Email)).Address);
            }
            catch
            {
                return false;
            }
        }

        public static bool ValidateTask(SolveAPI.Models.Task task)
        {
            if(task == null) return false;
            if(!DateTime.TryParse(task.Date, out _)) return false;
            if(task.IsRepetitive && string.IsNullOrWhiteSpace(task.RepetitiveTemplate)) return false;
            return true;
        }
        #endregion

    }
}
