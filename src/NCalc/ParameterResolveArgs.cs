using System;

namespace NCalc
{
    public class ParameterResolveArgs : EventArgs
    {
        private Func<object> _result;
        public Func<object> Result
        {
            get { return _result; }
            set
            {
                _result = value;
                HasResult = true;
            }
        }

        public bool HasResult { get; set; }
    }
}
