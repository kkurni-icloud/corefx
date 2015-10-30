﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.InteropServices.RuntimeInformationTests
{
    public class DescriptionNameTests
    {
        [Fact]
        public void VerifyRuntimeDebugName()
        {
            Assert.Equal(".NET Core", RuntimeInformation.FrameworkDescription);
        }

        [Fact, PlatformSpecific(PlatformID.Windows)]
        public void VerifyWindowsDebugName()
        {
            Assert.Contains("windows", RuntimeInformation.OSDescription, StringComparison.OrdinalIgnoreCase);
        }

        [Fact, PlatformSpecific(PlatformID.Linux)]
        public void VerifyLinuxDebugName()
        {
            Assert.Contains("linux", RuntimeInformation.OSDescription, StringComparison.OrdinalIgnoreCase);
        }

        [Fact, PlatformSpecific(PlatformID.OSX)]
        public void VerifyOSXDebugName()
        {
            Assert.Contains("darwin", RuntimeInformation.OSDescription, StringComparison.OrdinalIgnoreCase);
        }
    }
}
