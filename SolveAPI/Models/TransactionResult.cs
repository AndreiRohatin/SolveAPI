namespace SolveAPI.Models
{
    /// <summary>
    /// Class used to interpret response from firebase database
    /// </summary>
    public class TransactionResult
    {
        #region Private Fields
        private object? _data;
        private bool _isSuccesful;
        private string? _message;
        private string? _userId;
        private int _status;
        #endregion

        #region Public Fields

        public object? Data
        {
            get => _data;
            set => _data = value;
        }
        public bool IsSuccesful
        {
            get => _isSuccesful;
            set => _isSuccesful = value;
        }
        public string? Message
        {
            get => _message;
            set => _message = value;
        }
        public string? UserId
        {
            get => _userId;
            set => _userId = value;
        }
        public int Status
        {
            get => _status;
            set => _status = value;
        }
        #endregion

        public TransactionResult()
        {
            _data = null;
        }

        public TransactionResult(object? data, bool isSucessful)
        {
            this._data = data;
            this._isSuccesful = isSucessful;
        }
    }
}
