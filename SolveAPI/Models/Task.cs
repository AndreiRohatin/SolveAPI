using Newtonsoft.Json;

namespace SolveAPI.Models
{
    public enum PriorityType
    {
        Critical = 0,
        Severe = 1,
        High = 2,
        Medium = 3,
        Low = 4,
        Inexistent = 5
    }

    public enum TaskType
    {
        Mandatory = 0,
        Hobby = 1,
        Break = 2,
        Chore = 3,
        Recreational = 4,
        Others = 5,
    }

    public class Task
    {
        #region Private Properties
        private string? uid;
        private string? name;
        private string? date;
        private string? day;
        private string? repetitiveTemplate;
        #endregion

        [JsonIgnore]
        public string UID
        {
            get => uid ?? string.Empty;
            set => uid = value ?? string.Empty;
        }

        public string Name
        {
            get => name ?? string.Empty;
            set => name = value ?? string.Empty;
        }
        public string Day
        {
            get => day ?? string.Empty;
            set => day = value ?? string.Empty;
        }
        public string Date
        {
            get => date ?? string.Empty;
            set => date = value ?? string.Empty;
        }
        public bool IsRepetitive { get; set; }

        public PriorityType Priority { get; set; }

        public TaskType Type { get; set; }

        public string RepetitiveTemplate
        {
            get => repetitiveTemplate ?? string.Empty;
            set => repetitiveTemplate = value ?? string.Empty;
        }
    }
}
