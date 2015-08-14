// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Tests
{
    public class TaskFactoryTests
    {
        #region Test Methods

        // Exercise functionality of TaskFactory and TaskFactory<TResult>
        [Fact]
        public static void RunTaskFactoryTests()
        {
            TaskScheduler tm = TaskScheduler.Default;
            TaskCreationOptions tco = TaskCreationOptions.LongRunning;
            TaskFactory tf;
            TaskFactory<int> tfi;

            tf = new TaskFactory();
            ExerciseTaskFactory(tf, TaskScheduler.Current, TaskCreationOptions.None, CancellationToken.None, TaskContinuationOptions.None);

            CancellationTokenSource cancellationSrc = new CancellationTokenSource();
            tf = new TaskFactory(cancellationSrc.Token);
            var task = tf.StartNew(() => { });
            try
            {
                task.Wait();
            }
            catch (Exception ex)
            {
                Assert.True(false, string.Format("RunTaskFactoryTests: > Task.Wait threw un expected exception when a non cancelled token passed to the factory, exception msg: {0}", ex));
            }

            // Exercising TF(scheduler)
            tf = new TaskFactory(tm);
            ExerciseTaskFactory(tf, tm, TaskCreationOptions.None, CancellationToken.None, TaskContinuationOptions.None);

            //Exercising TF(TCrO, TCoO)
            tf = new TaskFactory(tco, TaskContinuationOptions.None);
            ExerciseTaskFactory(tf, TaskScheduler.Current, tco, CancellationToken.None, TaskContinuationOptions.None);

            // Exercising TF(scheduler, TCrO, TCoO)"
            tf = new TaskFactory(CancellationToken.None, tco, TaskContinuationOptions.None, tm);
            ExerciseTaskFactory(tf, tm, tco, CancellationToken.None, TaskContinuationOptions.None);

            //TaskFactory<TResult> tests

            // Exercising TF<int>()
            tfi = new TaskFactory<int>();
            ExerciseTaskFactoryInt(tfi, TaskScheduler.Current, TaskCreationOptions.None, CancellationToken.None, TaskContinuationOptions.None);

            //Test constructor that accepts cancellationToken

            // Exercising TF<int>(cancellationToken) with a noncancelled token 
            cancellationSrc = new CancellationTokenSource();
            tfi = new TaskFactory<int>(cancellationSrc.Token);
            task = tfi.StartNew(() => 0);
            try
            {
                task.Wait();
            }
            catch (Exception ex)
            {
                Assert.True(false, string.Format("RunTaskFactoryTests:  > Task.Wait threw un expected exception when a non cancelled token passed to the factory, exception msg: {0}", ex));
            }

            // Exercising TF<int>(scheduler)
            tfi = new TaskFactory<int>(tm);
            ExerciseTaskFactoryInt(tfi, tm, TaskCreationOptions.None, CancellationToken.None, TaskContinuationOptions.None);

            // Exercising TF<int>(TCrO, TCoO)
            tfi = new TaskFactory<int>(tco, TaskContinuationOptions.None);
            ExerciseTaskFactoryInt(tfi, TaskScheduler.Current, tco, CancellationToken.None, TaskContinuationOptions.None);

            // Exercising TF<int>(scheduler, TCrO, TCoO)
            tfi = new TaskFactory<int>(CancellationToken.None, tco, TaskContinuationOptions.None, tm);
            ExerciseTaskFactoryInt(tfi, tm, tco, CancellationToken.None, TaskContinuationOptions.None);
        }

        // Exercise functionality of TaskFactory and TaskFactory<TResult>
        [Fact]
        public static void RunTaskFactoryTests_Cancellation_Negative()
        {
            CancellationTokenSource cancellationSrc = new CancellationTokenSource();

            //Test constructor that accepts cancellationToken
            cancellationSrc.Cancel();
            TaskFactory tf = new TaskFactory(cancellationSrc.Token);
            var cancelledTask = tf.StartNew(() => { });
            EnsureTaskCanceledExceptionThrown(
               () => cancelledTask.Wait(),
               "RunTaskFactoryTests:    > TaskFactory.ctor(CancellationToken) failed, the created task is not cancelled when a cancelled token passed.");

            // Exercising TF<int>(cancellationToken) with a cancelled token
            cancellationSrc.Cancel();
            TaskFactory<int> tfi = new TaskFactory<int>(cancellationSrc.Token);
            cancelledTask = tfi.StartNew(() => 0);
            EnsureTaskCanceledExceptionThrown(
               () => cancelledTask.Wait(),
               "RunTaskFactoryTests: > TaskFactory<int>.ctor(CancellationToken) failed, the created task is not cancelled when a cancelled token passed");
        }

        [Fact]
        public static void RunTaskFactoryExceptionTests()
        {
            TaskFactory tf = new TaskFactory();

            // Checking top-level TF exception handling.
            Assert.Throws<ArgumentOutOfRangeException>(
               () => tf = new TaskFactory((TaskCreationOptions)0x40000000, TaskContinuationOptions.None));

            Assert.Throws<ArgumentOutOfRangeException>(
               () => tf = new TaskFactory((TaskCreationOptions)0x100, TaskContinuationOptions.None));

            Assert.Throws<ArgumentOutOfRangeException>(
               () => tf = new TaskFactory(TaskCreationOptions.None, (TaskContinuationOptions)0x40000000));

            Assert.Throws<ArgumentOutOfRangeException>(
               () => tf = new TaskFactory(TaskCreationOptions.None, TaskContinuationOptions.NotOnFaulted));

            Assert.ThrowsAsync<ArgumentNullException>(
               () => tf.FromAsync(null, (obj) => { }, TaskCreationOptions.None));

            // testing exceptions in null endMethods

            Assert.ThrowsAsync<ArgumentNullException>(
               () => tf.FromAsync(new myAsyncResult((obj) => { }, null), null, TaskCreationOptions.None));

            Assert.ThrowsAsync<ArgumentNullException>(
               () => tf.FromAsync<int>(new myAsyncResult((obj) => { }, null), null, TaskCreationOptions.None));

            Assert.ThrowsAsync<ArgumentNullException>(
               () => tf.FromAsync<int>(new myAsyncResult((obj) => { }, null), null, TaskCreationOptions.None, null));

            TaskFactory<int> tfi = new TaskFactory<int>();

            // Checking top-level TF<int> exception handling.
            Assert.Throws<ArgumentOutOfRangeException>(
               () => tfi = new TaskFactory<int>((TaskCreationOptions)0x40000000, TaskContinuationOptions.None));

            Assert.Throws<ArgumentOutOfRangeException>(
               () => tfi = new TaskFactory<int>((TaskCreationOptions)0x100, TaskContinuationOptions.None));

            Assert.Throws<ArgumentOutOfRangeException>(
               () => tfi = new TaskFactory<int>(TaskCreationOptions.None, (TaskContinuationOptions)0x40000000));

            Assert.Throws<ArgumentOutOfRangeException>(
               () => tfi = new TaskFactory<int>(TaskCreationOptions.None, TaskContinuationOptions.NotOnFaulted));
        }

        [Fact]
        public static void RunTaskFactoryFromAsyncExceptionTests()
        {
            // Checking TF special FromAsync exception handling."
            FakeAsyncClass fac = new FakeAsyncClass();
            TaskFactory tf;

            tf = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(
               () => tf.FromAsync(fac.StartWrite, fac.EndWrite, null /* state */));

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(
               () => tf.FromAsync(fac.StartWrite, fac.EndWrite, "abc", null /* state */));

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(
               () => tf.FromAsync(fac.StartWrite, fac.EndWrite, "abc", 2, null /* state */));

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(
               () => tf.FromAsync(fac.StartWrite, fac.EndWrite, "abc", 0, 2, null /* state */));

            // testing exceptions in null endMethods or begin method
            //0 parameter
            Assert.ThrowsAsync<ArgumentNullException>(
               () => tf.FromAsync<string>(fac.StartWrite, null, (Object)null, TaskCreationOptions.None));
            Assert.ThrowsAsync<ArgumentNullException>(
               () => tf.FromAsync<string>(null, fac.EndRead, (Object)null, TaskCreationOptions.None));

            //1 parameter
            Assert.ThrowsAsync<ArgumentNullException>(
               () => tf.FromAsync<string, int>(fac.StartWrite, null, "arg1", (Object)null, TaskCreationOptions.None));
            Assert.ThrowsAsync<ArgumentNullException>(
               () => tf.FromAsync<string, string>(null, fac.EndRead, "arg1", (Object)null, TaskCreationOptions.None));

            //2 parameters
            Assert.ThrowsAsync<ArgumentNullException>(
               () => tf.FromAsync<string, int, int>(fac.StartWrite, null, "arg1", 1, (Object)null, TaskCreationOptions.None));
            Assert.ThrowsAsync<ArgumentNullException>(
               () => tf.FromAsync<string, string, string>(null, fac.EndRead, "arg1", "arg2", (Object)null, TaskCreationOptions.None));

            //3 parameters
            Assert.ThrowsAsync<ArgumentNullException>(
               () => tf.FromAsync<string, int, int, int>(fac.StartWrite, null, "arg1", 1, 2, (Object)null, TaskCreationOptions.None));
            Assert.ThrowsAsync<ArgumentNullException>(
               () => tf.FromAsync<string, string, string, string>(null, fac.EndRead, "arg1", "arg2", "arg3", (Object)null, TaskCreationOptions.None));

            // Checking TF<string> special FromAsync exception handling.
            TaskFactory<string> tfs = new TaskFactory<string>(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
            char[] charbuf = new char[128];

            // Test that we throw on bad default task options
            Assert.Throws<ArgumentOutOfRangeException>(
               () => { tfs.FromAsync(fac.StartRead, fac.EndRead, null); });
            Assert.Throws<ArgumentOutOfRangeException>(
               () => { tfs.FromAsync(fac.StartRead, fac.EndRead, 64, null); });
            Assert.Throws<ArgumentOutOfRangeException>(
               () => { tfs.FromAsync(fac.StartRead, fac.EndRead, 64, charbuf, null); });
            Assert.Throws<ArgumentOutOfRangeException>(
               () => { tfs.FromAsync(fac.StartRead, fac.EndRead, 64, charbuf, 0, null); });

            // Test that we throw on null endMethod
            Assert.Throws<ArgumentNullException>(
               () => { tfs.FromAsync(fac.StartRead, null, null); });
            Assert.Throws<ArgumentNullException>(
               () => { tfs.FromAsync(fac.StartRead, null, 64, null); });
            Assert.Throws<ArgumentNullException>(
               () => { tfs.FromAsync(fac.StartRead, null, 64, charbuf, null); });
            Assert.Throws<ArgumentNullException>(
               () => { tfs.FromAsync(fac.StartRead, null, 64, charbuf, 0, null); });

            Assert.ThrowsAsync<ArgumentNullException>(
               () => tfs.FromAsync(null, (obj) => "", TaskCreationOptions.None));

            //test null begin or end methods with various overloads
            //0 parameter
            Assert.ThrowsAsync<ArgumentNullException>(
               () => tfs.FromAsync(fac.StartWrite, null, null, TaskCreationOptions.None));
            Assert.ThrowsAsync<ArgumentNullException>(
               () => tfs.FromAsync(null, fac.EndRead, null, TaskCreationOptions.None));

            //1 parameter
            Assert.ThrowsAsync<ArgumentNullException>(
               () => tfs.FromAsync<string>(fac.StartWrite, null, "arg1", null, TaskCreationOptions.None));
            Assert.ThrowsAsync<ArgumentNullException>(
               () => tfs.FromAsync<string>(null, fac.EndRead, "arg1", null, TaskCreationOptions.None));

            //2 parameters
            Assert.ThrowsAsync<ArgumentNullException>(
               () => tfs.FromAsync<string, int>(fac.StartWrite, null, "arg1", 2, null, TaskCreationOptions.None));
            Assert.ThrowsAsync<ArgumentNullException>(
               () => tfs.FromAsync<string, int>(null, fac.EndRead, "arg1", 2, null, TaskCreationOptions.None));

            //3 parameters
            Assert.ThrowsAsync<ArgumentNullException>(
               () => tfs.FromAsync<string, int, int>(fac.StartWrite, null, "arg1", 2, 3, null, TaskCreationOptions.None));
            Assert.ThrowsAsync<ArgumentNullException>(
               () => tfs.FromAsync<string, int, int>(null, fac.EndRead, "arg1", 2, 3, null, TaskCreationOptions.None));
        }

        #endregion

        #region Helper Methods

        // Utility method for RunTaskFactoryTests().
        private static void ExerciseTaskFactory(TaskFactory tf, TaskScheduler tmDefault, TaskCreationOptions tcoDefault, CancellationToken tokenDefault, TaskContinuationOptions continuationDefault)
        {
            TaskScheduler myTM = TaskScheduler.Default;
            TaskCreationOptions myTCO = TaskCreationOptions.LongRunning;
            TaskScheduler tmObserved = null;
            Task t;
            Task<int> f;

            //
            // Helper delegates to make the code below a lot shorter
            //
            Action<TaskCreationOptions, TaskCreationOptions, string> TCOchecker = delegate (TaskCreationOptions val1, TaskCreationOptions val2, string failMsg)
            {
                if (val1 != val2)
                {
                    Assert.True(false, string.Format(failMsg));
                }
            };

            Action<object, object, string> checker = delegate (object val1, object val2, string failMsg)
            {
                if (val1 != val2)
                {
                    Assert.True(false, string.Format(failMsg));
                }
            };

            Action init = delegate { tmObserved = null; };

            Action void_delegate = delegate
            {
                tmObserved = TaskScheduler.Current;
            };
            Action<object> voidState_delegate = delegate (object o)
            {
                tmObserved = TaskScheduler.Current;
            };
            Func<int> int_delegate = delegate
            {
                tmObserved = TaskScheduler.Current;
                return 10;
            };
            Func<object, int> intState_delegate = delegate (object o)
            {
                tmObserved = TaskScheduler.Current;
                return 10;
            };

            //check Factory properties
            TCOchecker(tf.CreationOptions, tcoDefault, "ExerciseTaskFactory:      > TaskFactory.Scheduler returned wrong CreationOptions");
            if (tf.Scheduler != null && tmDefault != tf.Scheduler)
            {
                Assert.True(false, string.Format("ExerciseTaskFactory: > TaskFactory.Scheduler is not null and returned wrong scheduler"));
            }
            if (tokenDefault != tf.CancellationToken)
            {
                Assert.True(false, string.Format("ExerciseTaskFactory: > TaskFactory.CancellationToken returned wrong token"));
            }
            if (continuationDefault != tf.ContinuationOptions)
            {
                Assert.True(false, string.Format("ExerciseTaskFactory: > TaskFactory.ContinuationOptions returned wrong value"));
            }


            //
            // StartNew(action)
            //
            init();
            t = tf.StartNew(void_delegate);
            t.Wait();
            checker(tmObserved, tmDefault, "ExerciseTaskFactory:      > FAILED StartNew(action).  Did not see expected TaskScheduler.");
            TCOchecker(t.CreationOptions, tcoDefault, "      > FAILED StartNew(action).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(action, TCO)
            //
            init();
            t = tf.StartNew(void_delegate, myTCO);
            t.Wait();
            checker(tmObserved, tmDefault, "ExerciseTaskFactory:      > FAILED StartNew(action, TCO).  Did not see expected TaskScheduler.");
            TCOchecker(t.CreationOptions, myTCO, "ExerciseTaskFactory:      > FAILED StartNew(action, TCO).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(action, CT, TCO, scheduler)
            //
            init();
            t = tf.StartNew(void_delegate, CancellationToken.None, myTCO, myTM);
            t.Wait();
            checker(tmObserved, myTM, "ExerciseTaskFactory:      > FAILED StartNew(action, TCO, scheduler).  Did not see expected TaskScheduler.");
            TCOchecker(t.CreationOptions, myTCO, "ExerciseTaskFactory:      > FAILED StartNew(action, TCO, scheduler).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(action<object>, object)
            //
            init();
            t = tf.StartNew(voidState_delegate, 100);
            t.Wait();
            checker(tmObserved, tmDefault, "ExerciseTaskFactory:      > FAILED StartNew(action<object>, object).  Did not see expected TaskScheduler.");
            TCOchecker(t.CreationOptions, tcoDefault, "ExerciseTaskFactory:      > FAILED StartNew(action<object>, object).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(action<object>, object, TCO)
            //
            init();
            t = tf.StartNew(voidState_delegate, 100, myTCO);
            t.Wait();
            checker(tmObserved, tmDefault, "ExerciseTaskFactory:      > FAILED StartNew(action<object>, object, TCO).  Did not see expected TaskScheduler.");
            TCOchecker(t.CreationOptions, myTCO, "ExerciseTaskFactory:      > FAILED StartNew(action<object>, object, TCO).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(action<object>, object, CT, TCO, scheduler)
            //
            init();
            t = tf.StartNew(voidState_delegate, 100, CancellationToken.None, myTCO, myTM);
            t.Wait();
            checker(tmObserved, myTM, "ExerciseTaskFactory:      > FAILED StartNew(action<object>, object, TCO, scheduler).  Did not see expected TaskScheduler.");
            TCOchecker(t.CreationOptions, myTCO, "ExerciseTaskFactory:      > FAILED StartNew(action<object>, object, TCO, scheduler).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func)
            //
            init();
            f = tf.StartNew(int_delegate);
            f.Wait();
            checker(tmObserved, tmDefault, "ExerciseTaskFactory:      > FAILED StartNew(func).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, tcoDefault, "ExerciseTaskFactory:      > FAILED StartNew(func).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func, token)
            //
            init();
            f = tf.StartNew(int_delegate, tokenDefault);
            f.Wait();
            checker(tmObserved, tmDefault, "ExerciseTaskFactory:      > FAILED StartNew(func, token).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, tcoDefault, "      > FAILED StartNew(func, token).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func, options)
            //
            init();
            f = tf.StartNew(int_delegate, myTCO);
            f.Wait();
            checker(tmObserved, tmDefault, "ExerciseTaskFactory:      > FAILED StartNew(func, options).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, myTCO, "ExerciseTaskFactory:      > FAILED StartNew(func, options).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func, CT, options, scheduler)
            //
            init();
            f = tf.StartNew(int_delegate, CancellationToken.None, myTCO, myTM);
            f.Wait();
            checker(tmObserved, myTM, "ExerciseTaskFactory:      > FAILED StartNew(func, options, scheduler).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, myTCO, "ExerciseTaskFactory:      > FAILED StartNew(func, options, scheduler).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func<object>, object)
            //
            init();
            f = tf.StartNew(intState_delegate, 100);
            f.Wait();
            checker(tmObserved, tmDefault, "ExerciseTaskFactory:      > FAILED StartNew(func<object>, object).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, tcoDefault, "ExerciseTaskFactory:      > FAILED StartNew(func<object>, object).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func<object>, object, token)
            //
            init();
            f = tf.StartNew(intState_delegate, 100, tokenDefault);
            f.Wait();
            checker(tmObserved, tmDefault, "ExerciseTaskFactory:      > FAILED StartNew(func<object>, object, token).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, tcoDefault, "ExerciseTaskFactory:      > FAILED StartNew(func<object>, object, token).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func<object>, object, options)
            //
            init();
            f = tf.StartNew(intState_delegate, 100, myTCO);
            f.Wait();
            checker(tmObserved, tmDefault, "ExerciseTaskFactory:      > FAILED StartNew(func<object>, object, options).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, myTCO, "ExerciseTaskFactory:      > FAILED StartNew(func<object>, object, options).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func<object>, object, CT, options, scheduler)
            //
            init();
            f = tf.StartNew(intState_delegate, 100, CancellationToken.None, myTCO, myTM);
            f.Wait();
            checker(tmObserved, myTM, "ExerciseTaskFactory:      > FAILED StartNew(func<object>, object, options, scheduler).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, myTCO, "ExerciseTaskFactory:      > FAILED StartNew(func<object>, object, options, scheduler).  Did not see expected TaskCreationOptions.");
        }

        // Utility method for RunTaskFactoryTests().
        private static void ExerciseTaskFactoryInt(TaskFactory<int> tf, TaskScheduler tmDefault, TaskCreationOptions tcoDefault, CancellationToken tokenDefault, TaskContinuationOptions continuationDefault)
        {
            TaskScheduler myTM = TaskScheduler.Default;
            TaskCreationOptions myTCO = TaskCreationOptions.LongRunning;
            TaskScheduler tmObserved = null;
            Task<int> f;

            //
            // Helper delegates to make the code shorter.
            //
            Action<TaskCreationOptions, TaskCreationOptions, string> TCOchecker = delegate (TaskCreationOptions val1, TaskCreationOptions val2, string failMsg)
            {
                if (val1 != val2)
                {
                    Assert.True(false, string.Format(failMsg));
                }
            };

            Action<object, object, string> checker = delegate (object val1, object val2, string failMsg)
            {
                if (val1 != val2)
                {
                    Assert.True(false, string.Format(failMsg));
                }
            };

            Action init = delegate { tmObserved = null; };

            Func<int> int_delegate = delegate
            {
                tmObserved = TaskScheduler.Current;
                return 10;
            };
            Func<object, int> intState_delegate = delegate (object o)
            {
                tmObserved = TaskScheduler.Current;
                return 10;
            };

            //check Factory properties
            TCOchecker(tf.CreationOptions, tcoDefault, "ExerciseTaskFactoryInt:      > TaskFactory.Scheduler returned wrong CreationOptions");
            if (tf.Scheduler != null && tmDefault != tf.Scheduler)
            {
                Assert.True(false, string.Format("ExerciseTaskFactoryInt: > TaskFactory.Scheduler is not null and returned wrong scheduler"));
            }
            if (tokenDefault != tf.CancellationToken)
            {
                Assert.True(false, string.Format("ExerciseTaskFactoryInt: > TaskFactory.CancellationToken returned wrong token"));
            }
            if (continuationDefault != tf.ContinuationOptions)
            {
                Assert.True(false, string.Format("ExerciseTaskFactoryInt: > TaskFactory.ContinuationOptions returned wrong value"));
            }

            //
            // StartNew(func)
            //
            init();
            f = tf.StartNew(int_delegate);
            f.Wait();
            checker(tmObserved, tmDefault, "ExerciseTaskFactoryInt:      > FAILED StartNew(func).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, tcoDefault, "ExerciseTaskFactoryInt:      > FAILED StartNew(func).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func, options)
            //
            init();
            f = tf.StartNew(int_delegate, myTCO);
            f.Wait();
            checker(tmObserved, tmDefault, "ExerciseTaskFactoryInt:      > FAILED StartNew(func, options).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, myTCO, "ExerciseTaskFactoryInt:      > FAILED StartNew(func, options).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func, CT, options, scheduler)
            //
            init();
            f = tf.StartNew(int_delegate, CancellationToken.None, myTCO, myTM);
            f.Wait();
            checker(tmObserved, myTM, "ExerciseTaskFactoryInt:      > FAILED StartNew(func, options, scheduler).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, myTCO, "ExerciseTaskFactoryInt:      > FAILED StartNew(func, options, scheduler).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func<object>, object)
            //
            init();
            f = tf.StartNew(intState_delegate, 100);
            f.Wait();
            checker(tmObserved, tmDefault, "ExerciseTaskFactoryInt:      > FAILED StartNew(func<object>, object).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, tcoDefault, "ExerciseTaskFactoryInt:      > FAILED StartNew(func<object>, object).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func<object>, object, token)
            //
            init();
            f = tf.StartNew(intState_delegate, 100, tokenDefault);
            f.Wait();
            checker(tmObserved, tmDefault, "ExerciseTaskFactoryInt:      > FAILED StartNew(func<object>, object, token).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, tcoDefault, "ExerciseTaskFactoryInt:      > FAILED StartNew(func<object>, object, token).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func<object>, object, options)
            //
            init();
            f = tf.StartNew(intState_delegate, 100, myTCO);
            f.Wait();
            checker(tmObserved, tmDefault, "ExerciseTaskFactoryInt:      > FAILED StartNew(func<object>, object, options).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, myTCO, "ExerciseTaskFactoryInt:      > FAILED StartNew(func<object>, object, options).  Did not see expected TaskCreationOptions.");

            //
            // StartNew(func<object>, object, CT, options, scheduler)
            //
            init();
            f = tf.StartNew(intState_delegate, 100, CancellationToken.None, myTCO, myTM);
            f.Wait();
            checker(tmObserved, myTM, "ExerciseTaskFactoryInt:      > FAILED StartNew(func<object>, object, options, scheduler).  Did not see expected TaskScheduler.");
            TCOchecker(f.CreationOptions, myTCO, "ExerciseTaskFactoryInt:      > FAILED StartNew(func<object>, object, options, scheduler).  Did not see expected TaskCreationOptions.");
        }

        // Ensures that the specified action throws a AggregateException wrapping a TaskCanceledException
        private static void EnsureTaskCanceledExceptionThrown(Action action, string message)
        {
            Exception exception = null;
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception == null)
            {
                Assert.True(false, string.Format(message + " (no exception thrown)")); ;
            }
            else if (exception.GetType() != typeof(AggregateException))
            {
                Assert.True(false, string.Format(message + " (didn't throw aggregate exception)"));
            }
            else if (((AggregateException)exception).InnerException.GetType() != typeof(TaskCanceledException))
            {
                exception = ((AggregateException)exception).InnerException;
                Assert.True(false, string.Format(message + " (threw " + exception.GetType().Name + " instead of TaskCanceledException)"));
            }
        }

        // This class is used in testing Factory tests.
        private class FakeAsyncClass
        {
            private List<char> _list = new List<char>();

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                lock (_list)
                {
                    for (int i = 0; i < _list.Count; i++) sb.Append(_list[i]);
                }
                return sb.ToString();
            }

            // Silly use of Write, but I wanted to test no-argument StartXXX handling.
            public IAsyncResult StartWrite(AsyncCallback cb, object o)
            {
                return StartWrite("", 0, 0, cb, o);
            }

            public IAsyncResult StartWrite(string s, AsyncCallback cb, object o)
            {
                return StartWrite(s, 0, s.Length, cb, o);
            }

            public IAsyncResult StartWrite(string s, int length, AsyncCallback cb, object o)
            {
                return StartWrite(s, 0, length, cb, o);
            }

            public IAsyncResult StartWrite(string s, int offset, int length, AsyncCallback cb, object o)
            {
                myAsyncResult mar = new myAsyncResult(cb, o);

                // Allow for exception throwing to test our handling of that.
                if (s == null) throw new ArgumentNullException("s");

                Task t = Task.Factory.StartNew(delegate
                {
                    //Thread.Sleep(100);
                    try
                    {
                        lock (_list)
                        {
                            for (int i = 0; i < length; i++) _list.Add(s[i + offset]);
                        }
                        mar.Signal();
                    }
                    catch (Exception e) { mar.Signal(e); }
                });


                return mar;
            }

            public void EndWrite(IAsyncResult iar)
            {
                myAsyncResult mar = iar as myAsyncResult;
                mar.Wait();
                if (mar.IsFaulted) throw (mar.Exception);
            }

            public IAsyncResult StartRead(AsyncCallback cb, object o)
            {
                return StartRead(128 /*=maxbytes*/, null, 0, cb, o);
            }

            public IAsyncResult StartRead(int maxBytes, AsyncCallback cb, object o)
            {
                return StartRead(maxBytes, null, 0, cb, o);
            }

            public IAsyncResult StartRead(int maxBytes, char[] buf, AsyncCallback cb, object o)
            {
                return StartRead(maxBytes, buf, 0, cb, o);
            }

            public IAsyncResult StartRead(int maxBytes, char[] buf, int offset, AsyncCallback cb, object o)
            {
                myAsyncResult mar = new myAsyncResult(cb, o);

                // Allow for exception throwing to test our handling of that.
                if (maxBytes == -1) throw new ArgumentException("maxBytes");

                Task t = Task.Factory.StartNew(delegate
                {
                    //Thread.Sleep(100);
                    StringBuilder sb = new StringBuilder();
                    int bytesRead = 0;
                    try
                    {
                        lock (_list)
                        {
                            while ((_list.Count > 0) && (bytesRead < maxBytes))
                            {
                                sb.Append(_list[0]);
                                if (buf != null) { buf[offset] = _list[0]; offset++; }
                                _list.RemoveAt(0);
                                bytesRead++;
                            }
                        }

                        mar.SignalState(sb.ToString());
                    }
                    catch (Exception e) { mar.Signal(e); }
                });

                return mar;
            }

            public string EndRead(IAsyncResult iar)
            {
                myAsyncResult mar = iar as myAsyncResult;
                if (mar.IsFaulted) throw (mar.Exception);
                return (string)mar.AsyncState;
            }

            public void ResetStateTo(string s)
            {
                _list.Clear();
                for (int i = 0; i < s.Length; i++) _list.Add(s[i]);
            }
        }

        // This is an internal class used for a concrete IAsyncResult in the APM Factory tests.
        private class myAsyncResult : IAsyncResult
        {
            private volatile int _isCompleted;
            private ManualResetEvent _asyncWaitHandle;
            private AsyncCallback _callback;
            private object _asyncState;
            private Exception _exception;

            public myAsyncResult(AsyncCallback cb, object o)
            {
                _isCompleted = 0;
                _asyncWaitHandle = new ManualResetEvent(false);
                _callback = cb;
                _asyncState = o;
                _exception = null;
            }

            public bool IsCompleted
            {
                get { return (_isCompleted == 1); }
            }

            public bool CompletedSynchronously
            {
                get { return false; }
            }

            public WaitHandle AsyncWaitHandle
            {
                get { return _asyncWaitHandle; }
            }

            public object AsyncState
            {
                get { return _asyncState; }
            }

            public void Signal()
            {
                _isCompleted = 1;
                _asyncWaitHandle.Set();
                if (_callback != null) _callback(this);
            }

            public void Signal(Exception e)
            {
                _exception = e;
                Signal();
            }

            public void SignalState(object o)
            {
                _asyncState = o;
                Signal();
            }

            public void Wait()
            {
                _asyncWaitHandle.WaitOne();
                if (_exception != null) throw (_exception);
            }

            public bool IsFaulted
            {
                get { return ((_isCompleted == 1) && (_exception != null)); }
            }

            public Exception Exception
            {
                get { return _exception; }
            }
        }

        #endregion
    }
}
