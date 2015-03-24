﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Diagnostics.TraceSourceTests
{
    public class SourceFilterClassTests
    {
        [Fact]
        public void ConstructorTest()
        {
            var filter = new SourceFilter("TestSource");
            Assert.Equal("TestSource", filter.Source);
        }

        [Fact]
        public void ShouldTraceTest()
        {
            var sourceName = "TestSource";
            var cache = new TraceEventCache();
            var filter = new SourceFilter(sourceName);
            Assert.True(filter.ShouldTrace(cache, sourceName, TraceEventType.Critical, 0, null, null, null, null));
            Assert.False(filter.ShouldTrace(cache, "Default", TraceEventType.Warning, 0, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => filter.ShouldTrace(cache, null, TraceEventType.Warning, 0, null, null, null, null));
        }

        [Fact]
        public void SourceTest()
        {
            var filter = new SourceFilter("TestSource");
            Assert.Equal("TestSource", filter.Source);
            filter.Source = "Default";
            Assert.Equal("Default", filter.Source);
            Assert.Throws<ArgumentNullException>(() => filter.Source = null);
        }
    }
}
