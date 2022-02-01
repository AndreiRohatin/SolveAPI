using System.Collections.Concurrent;

namespace SolveAPI.Models
{
    public class User : Account
    {

        #region Private Properties
        private ConcurrentBag<Task> tasks;
        #endregion

        public User()
        {
            tasks = new ConcurrentBag<Task>();
            this.AccountType = AccountType.User;
        }

        public ConcurrentBag<Task> Tasks
        {
            get => tasks ?? new ConcurrentBag<Task>();
            set => tasks = value ?? new ConcurrentBag<Task>();
        }
        public void AppendTask(Task task)
        {
            tasks.Add(task);
        }
    }
}
