﻿using Xunit;

namespace System.Diagnostics.TraceSourceTests
{
    public class BooleanSwitchClassTests
    {
        private const String Name = "TestSwitch";

        [Fact]
        public void Constructor1()
        {
            var swtch = new BooleanSwitch(Name, "");
            Assert.False(swtch.Enabled);
            // assert that a null name doesn't throw.
            swtch = new BooleanSwitch(null, null);
        }

        [Fact]
        public void Constructor2()
        {
            var swtch = new BooleanSwitch(Name, "", "True");
            Assert.True(swtch.Enabled);
            swtch = new BooleanSwitch(Name, "", "false");
            Assert.False(swtch.Enabled);
            swtch = new BooleanSwitch(Name, "", "BAD_VALUE");
            Assert.Throws<FormatException>(() => swtch.Enabled);
        }

        [Fact]
        public void Enabled()
        {
            var swtch = new BooleanSwitch(Name, "");
            swtch.Enabled = true;
            Assert.True(swtch.Enabled);
            swtch.Enabled = false;
            Assert.False(swtch.Enabled);
        }
    }
}
