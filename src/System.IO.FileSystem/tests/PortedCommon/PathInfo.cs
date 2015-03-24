﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

internal class PathInfo
{
    private readonly string[] _paths;

    public PathInfo(string[] paths)
    {
        _paths = paths;
    }

    /// <summary>
    ///  Gets the sub paths that make up this path. For example, in "C:\Windows\System32", this would return "C:", "C:\Windows", "C:\Windows\System32".
    /// </summary>
    public string[] SubPaths
    {
        get { return _paths; }
    }

    public string FullPath
    {
        get { return _paths[_paths.Length - 1]; }
    }
}
