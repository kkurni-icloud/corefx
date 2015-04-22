// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;

namespace Microsoft.CSharp.RuntimeBinder
{
    /// <summary>
    /// Represents a dynamic delegate-like call in C#, providing the binding semantics and the details about the operation. 
    /// Instances of this class are generated by the C# compiler.
    /// </summary>
    internal sealed class CSharpInvokeBinder : InvokeBinder, ICSharpInvokeOrInvokeMemberBinder
    {
        bool ICSharpInvokeOrInvokeMemberBinder.StaticCall { get { return _argumentInfo[0] != null && _argumentInfo[0].IsStaticType; } }
        string ICSharpInvokeOrInvokeMemberBinder.Name { get { return "Invoke"; } }
        IList<Type> ICSharpInvokeOrInvokeMemberBinder.TypeArguments { get { return Array.Empty<Type>(); } }

        CSharpCallFlags ICSharpInvokeOrInvokeMemberBinder.Flags { get { return _flags; } }
        private CSharpCallFlags _flags;

        Type ICSharpInvokeOrInvokeMemberBinder.CallingContext { get { return _callingContext; } }
        private Type _callingContext;

        IList<CSharpArgumentInfo> ICSharpInvokeOrInvokeMemberBinder.ArgumentInfo { get { return _argumentInfo.AsReadOnly(); } }
        private List<CSharpArgumentInfo> _argumentInfo;

        bool ICSharpInvokeOrInvokeMemberBinder.ResultDiscarded { get { return (_flags & CSharpCallFlags.ResultDiscarded) != 0; } }

        private RuntimeBinder _binder;

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpInvokeBinder" />.
        /// </summary>
        /// <param name="flags">Extra information about this operation that is not specific to any particular argument.</param>
        /// <param name="callingContext">The <see cref="System.Type"/> that indicates where this operation is defined.</param>
        /// <param name="argumentInfo">The sequence of <see cref="CSharpArgumentInfo"/> instances for the arguments to this operation.</param>
        public CSharpInvokeBinder(
                CSharpCallFlags flags,
                Type callingContext,
                IEnumerable<CSharpArgumentInfo> argumentInfo) :
            base(BinderHelper.CreateCallInfo(argumentInfo, 1)) // discard 1 argument: the target object (even if static, arg is type)
        {
            _flags = flags;
            _callingContext = callingContext;
            _argumentInfo = BinderHelper.ToList(argumentInfo);
            _binder = RuntimeBinder.GetInstance();
        }

        /// <summary>
        /// Performs the binding of the dynamic invoke operation if the target dynamic object cannot bind.
        /// </summary>
        /// <param name="target">The target of the dynamic invoke operation.</param>
        /// <param name="args">The arguments of the dynamic invoke operation.</param>
        /// <param name="errorSuggestion">The binding result to use if binding fails, or null.</param>
        /// <returns>The <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
#if ENABLECOMBINDER

            DynamicMetaObject com;
            if (!BinderHelper.IsWindowsRuntimeObject(target) && ComBinder.TryBindInvoke(this, target, args, out com))
            {
                return com;
            }
#endif
            return BinderHelper.Bind(this, _binder, BinderHelper.Cons(target, args), _argumentInfo, errorSuggestion);
        }
    }
}
