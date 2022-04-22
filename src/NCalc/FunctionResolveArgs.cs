using System;

namespace NCalc
{
    public class FunctionResolveArgs : EventArgs
    {
        public Action<FunctionArgs> Delegate { get; set; }
    }
}