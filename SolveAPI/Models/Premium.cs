namespace SolveAPI.Models
{
    public enum BillingCycle
    {
        Monthly = 0,
        Quarterly = 1,
        Yearly = 2
    }

    public class Premium : User
    {
        #region Private properties
        private string? billingDate;
        private BillingCycle billingCycle;
        #endregion

        public string BillingDate
        {
            get => billingDate ?? string.Empty;
            set { if (!string.IsNullOrWhiteSpace(value)) billingDate = value; }
        }
        public BillingCycle BillingCycle
        {
            get => billingCycle;
            set => billingCycle = value;
        }
    }
}
