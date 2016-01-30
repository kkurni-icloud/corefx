// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class EventBuilderSetRemoveOnMethod
    {
        public delegate void TestEventHandler(object sender, object arg);
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        private TypeBuilder TypeBuilder
        {
            get
            {
                if (null == _typeBuilder)
                {
                    AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                        new AssemblyName("EventBuilderSetRemoveOnMethod_Assembly"), AssemblyBuilderAccess.Run);
                    ModuleBuilder module = TestLibrary.Utilities.GetModuleBuilder(assembly, "EventBuilderSetRemoveOnMethod_Module");
                    _typeBuilder = module.DefineType("EventBuilderSetRemoveOnMethod_Type", TypeAttributes.Abstract);
                }

                return _typeBuilder;
            }
        }

        private TypeBuilder _typeBuilder;
        private const int MethodBodyLength = 256;

        [Fact]
        public void TestOnAbstractMethod()
        {
            EventBuilder ev = TypeBuilder.DefineEvent("Event_PosTest1", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = TypeBuilder.DefineMethod("Method_PosTest1", MethodAttributes.Abstract | MethodAttributes.Virtual);

            ev.SetRemoveOnMethod(method);

            // add this method again
            ev.SetRemoveOnMethod(method);
        }


        [Fact]
        public void TestOnInstanceMethod()
        {
            byte[] bytes = new byte[MethodBodyLength];
            _generator.GetBytes(bytes);
            EventBuilder ev = TypeBuilder.DefineEvent("Event_PosTest2", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = TypeBuilder.DefineMethod("Method_PosTest2", MethodAttributes.Public);
            ILGenerator ilgen = method.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);

            ev.SetRemoveOnMethod(method);

            // add this method again
            ev.SetRemoveOnMethod(method);
        }


        [Fact]
        public void TestOnStaticMethod()
        {
            byte[] bytes = new byte[MethodBodyLength];
            _generator.GetBytes(bytes);
            EventBuilder ev = TypeBuilder.DefineEvent("Event_PosTest3", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = TypeBuilder.DefineMethod("Method_PosTest3", MethodAttributes.Static);
            ILGenerator ilgen = method.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);

            ev.SetRemoveOnMethod(method);

            // add this method again
            ev.SetRemoveOnMethod(method);
        }

        [Fact]
        public void TestOnPInvokeMethod()
        {
            EventBuilder ev = TypeBuilder.DefineEvent("Event_PosTest4", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = TypeBuilder.DefineMethod("Method_PosTest4", MethodAttributes.PinvokeImpl);

            ev.SetRemoveOnMethod(method);

            // add this method again
            ev.SetRemoveOnMethod(method);
        }


        [Fact]
        public void TestOnMultipleDifferentMethods()
        {
            byte[] bytes = new byte[MethodBodyLength];
            _generator.GetBytes(bytes);

            EventBuilder ev = TypeBuilder.DefineEvent("Event_PosTest5", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method1 = TypeBuilder.DefineMethod("PMethod_PosTest5", MethodAttributes.PinvokeImpl);
            MethodBuilder method2 = TypeBuilder.DefineMethod("IMethod_PosTest5", MethodAttributes.Public);
            ILGenerator ilgen = method2.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);
            MethodBuilder method3 = TypeBuilder.DefineMethod("SMethod_PosTest5", MethodAttributes.Static);
            MethodBuilder method4 = TypeBuilder.DefineMethod("AMethod_PosTest5", MethodAttributes.Abstract | MethodAttributes.Virtual);

            ev.SetRemoveOnMethod(method1);
            ev.SetRemoveOnMethod(method2);
            ev.SetRemoveOnMethod(method3);
            ev.SetRemoveOnMethod(method4);
        }



        [Fact]
        public void TestThrowsExceptionOnNullBuilder()
        {
            EventBuilder ev = TypeBuilder.DefineEvent("Event_NegTest1", EventAttributes.None, typeof(TestEventHandler));
            Assert.Throws<ArgumentNullException>(() => { ev.SetRemoveOnMethod(null); });
        }

        [Fact]
        public void TestThrowsExceptionOnCreateTypeCalled()
        {
            try
            {
                EventBuilder ev = TypeBuilder.DefineEvent("Event_NegTest2", EventAttributes.None, typeof(TestEventHandler));
                MethodBuilder method = TypeBuilder.DefineMethod("Method_NegTest2", MethodAttributes.Abstract | MethodAttributes.Virtual);
                TypeBuilder.CreateTypeInfo().AsType();

                Assert.Throws<InvalidOperationException>(() => { ev.SetRemoveOnMethod(method); });
            }
            finally
            {
                _typeBuilder = null;
            }
        }
    }
}
