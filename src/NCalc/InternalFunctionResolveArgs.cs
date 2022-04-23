using NCalc.Domain;
using System;

namespace NCalc
{
    public class InternalFunctionResolveArgs : EventArgs
    {
        public Func<Function, object> Delegate { get; set; }
    }
}