// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Threading;

public class EventWaitHandleTests
{
    [Theory]
    [InlineData(false, EventResetMode.AutoReset)]
    [InlineData(false, EventResetMode.ManualReset)]
    [InlineData(true, EventResetMode.AutoReset)]
    [InlineData(true, EventResetMode.ManualReset)]
    public void Ctor_StateMode(bool initialState, EventResetMode mode)
    {
        using (var ewh = new EventWaitHandle(initialState, mode))
            Assert.Equal(initialState, ewh.WaitOne(0));
    }

    [Fact]
    public void Ctor_InvalidMode()
    {
        Assert.Throws<ArgumentException>(() => new EventWaitHandle(true, (EventResetMode)12345));
    }

    [PlatformSpecific(PlatformID.Windows)]
    [Fact]
    public void Ctor_InvalidNames()
    {
        Assert.Throws<ArgumentException>(() => new EventWaitHandle(true, EventResetMode.AutoReset, new string('a', 1000)));
    }

    [PlatformSpecific(PlatformID.AnyUnix)]
    [Fact]
    public void Ctor_NamesArentSupported_Unix()
    {
        Assert.Throws<PlatformNotSupportedException>(() => new EventWaitHandle(false, EventResetMode.AutoReset, "anything"));
        bool createdNew;
        Assert.Throws<PlatformNotSupportedException>(() => new EventWaitHandle(false, EventResetMode.AutoReset, "anything", out createdNew));
    }

    [PlatformSpecific(PlatformID.Windows)]
    [Theory]
    [InlineData(false, EventResetMode.AutoReset)]
    [InlineData(false, EventResetMode.ManualReset)]
    [InlineData(true, EventResetMode.AutoReset)]
    [InlineData(true, EventResetMode.ManualReset)]
    public void Ctor_StateModeNameCreatedNew_Windows(bool initialState, EventResetMode mode)
    {
        string name = Guid.NewGuid().ToString("N");
        bool createdNew;
        using (var ewh = new EventWaitHandle(false, EventResetMode.AutoReset, name, out createdNew))
        {
            Assert.True(createdNew);
            using (new EventWaitHandle(false, EventResetMode.AutoReset, name, out createdNew))
            {
                Assert.False(createdNew);
            }
        }
    }

    [PlatformSpecific(PlatformID.Windows)] // named semaphores aren't supported on Unix
    [Theory]
    [InlineData(EventResetMode.AutoReset)]
    [InlineData(EventResetMode.ManualReset)]
    public void Ctor_NameUsedByOtherSynchronizationPrimitive_Windows(EventResetMode mode)
    {
        string name = Guid.NewGuid().ToString("N");
        using (Mutex m = new Mutex(false, name))
            Assert.Throws<WaitHandleCannotBeOpenedException>(() => new EventWaitHandle(false, mode, name));
    }

    [Fact]
    public void SetReset()
    {
        using (EventWaitHandle are = new EventWaitHandle(false, EventResetMode.AutoReset))
        {
            Assert.False(are.WaitOne(0));
            are.Set();
            Assert.True(are.WaitOne(0));
            Assert.False(are.WaitOne(0));
            are.Set();
            are.Reset();
            Assert.False(are.WaitOne(0));
        }

        using (EventWaitHandle mre = new EventWaitHandle(false, EventResetMode.ManualReset))
        {
            Assert.False(mre.WaitOne(0));
            mre.Set();
            Assert.True(mre.WaitOne(0));
            Assert.True(mre.WaitOne(0));
            mre.Set();
            Assert.True(mre.WaitOne(0));
            mre.Reset();
            Assert.False(mre.WaitOne(0));
        }
    }

    [PlatformSpecific(PlatformID.Windows)]
    [Fact]
    public void OpenExisting_Windows()
    {
        string name = Guid.NewGuid().ToString("N");

        EventWaitHandle resultHandle;
        Assert.False(EventWaitHandle.TryOpenExisting(name, out resultHandle));
        Assert.Null(resultHandle);

        using (EventWaitHandle are1 = new EventWaitHandle(false, EventResetMode.AutoReset, name))
        {
            using (EventWaitHandle are2 = EventWaitHandle.OpenExisting(name))
            {
                are1.Set();
                Assert.True(are2.WaitOne(0));
                Assert.False(are1.WaitOne(0));
                Assert.False(are2.WaitOne(0));

                are2.Set();
                Assert.True(are1.WaitOne(0));
                Assert.False(are2.WaitOne(0));
                Assert.False(are1.WaitOne(0));
            }

            Assert.True(EventWaitHandle.TryOpenExisting(name, out resultHandle));
            Assert.NotNull(resultHandle);
            resultHandle.Dispose();
        }
    }

    [PlatformSpecific(PlatformID.AnyUnix)]
    [Fact]
    public void OpenExisting_NotSupported_Unix()
    {
        Assert.Throws<PlatformNotSupportedException>(() => EventWaitHandle.OpenExisting("anything"));
        EventWaitHandle ewh;
        Assert.Throws<PlatformNotSupportedException>(() => EventWaitHandle.TryOpenExisting("anything", out ewh));
    }

}
