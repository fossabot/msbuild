﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using Microsoft.Build.Utilities;

namespace Microsoft.Build.UnitTests
{
    [TestClass]
    sealed public class CombinePath_Tests
    {
        /// <summary>
        /// Base path is relative.  Paths are relative.
        /// </summary>
        [TestMethod]
        public void RelativeRelative1()
        {
            CombinePath t = new CombinePath();
            t.BuildEngine = new MockEngine();

            t.BasePath = @"abc\def";
            t.Paths = new ITaskItem[] { new TaskItem(@"ghi.txt"), new TaskItem(@"jkl\mno.txt") };
            Assert.IsTrue(t.Execute(), "success");

            ObjectModelHelpers.AssertItemsMatch(@"
                abc\def\ghi.txt
                abc\def\jkl\mno.txt
                ", t.CombinedPaths, true);
        }

        /// <summary>
        /// Base path is relative.  Paths are absolute.
        /// </summary>
        [TestMethod]
        public void RelativeAbsolute1()
        {
            CombinePath t = new CombinePath();
            t.BuildEngine = new MockEngine();

            t.BasePath = @"abc\def";
            t.Paths = new ITaskItem[] { new TaskItem(@"c:\ghi.txt"), new TaskItem(@"d:\jkl\mno.txt"), new TaskItem(@"\\myserver\myshare") };
            Assert.IsTrue(t.Execute(), "success");

            ObjectModelHelpers.AssertItemsMatch(@"
                c:\ghi.txt
                d:\jkl\mno.txt
                \\myserver\myshare
                ", t.CombinedPaths, true);
        }

        /// <summary>
        /// Base path is absolute.  Paths are relative.
        /// </summary>
        [TestMethod]
        public void AbsoluteRelative1()
        {
            CombinePath t = new CombinePath();
            t.BuildEngine = new MockEngine();

            t.BasePath = @"c:\abc\def";
            t.Paths = new ITaskItem[] { new TaskItem(@"\ghi\jkl.txt"), new TaskItem(@"mno\qrs.txt") };
            Assert.IsTrue(t.Execute(), "success");

            ObjectModelHelpers.AssertItemsMatch(
                @"\ghi\jkl.txt" + "\r\n" +      // I think this is a bug in Path.Combine.  It should have been "c:\ghi\jkl.txt".
                @"c:\abc\def\mno\qrs.txt",
                t.CombinedPaths, true);
        }

        /// <summary>
        /// Base path is absolute.  Paths are absolute.
        /// </summary>
        [TestMethod]
        public void AbsoluteAbsolute1()
        {
            CombinePath t = new CombinePath();
            t.BuildEngine = new MockEngine();

            t.BasePath = @"\\fileserver\public";
            t.Paths = new ITaskItem[] { new TaskItem(@"c:\ghi.txt"), new TaskItem(@"d:\jkl\mno.txt"), new TaskItem(@"\\myserver\myshare") };
            Assert.IsTrue(t.Execute(), "success");

            ObjectModelHelpers.AssertItemsMatch(@"
                c:\ghi.txt
                d:\jkl\mno.txt
                \\myserver\myshare
                ", t.CombinedPaths, true);
        }

        /// <summary>
        /// All item metadata from the paths should be preserved when producing the output items.
        /// </summary>
        [TestMethod]
        public void MetadataPreserved()
        {
            CombinePath t = new CombinePath();
            t.BuildEngine = new MockEngine();

            t.BasePath = @"c:\abc\def\";
            t.Paths = new ITaskItem[] { new TaskItem(@"jkl\mno.txt") };
            t.Paths[0].SetMetadata("Culture", "english");
            Assert.IsTrue(t.Execute(), "success");

            ObjectModelHelpers.AssertItemsMatch(@"
                c:\abc\def\jkl\mno.txt : Culture=english
                ", t.CombinedPaths, true);
        }

        /// <summary>
        /// No base path passed in should be treated as a blank base path, which means that
        /// the original paths are returned untouched.
        /// </summary>
        [TestMethod]
        public void NoBasePath()
        {
            CombinePath t = new CombinePath();
            t.BuildEngine = new MockEngine();

            t.Paths = new ITaskItem[] { new TaskItem(@"jkl\mno.txt"), new TaskItem(@"c:\abc\def\ghi.txt") };
            Assert.IsTrue(t.Execute(), "success");

            ObjectModelHelpers.AssertItemsMatch(@"
                jkl\mno.txt
                c:\abc\def\ghi.txt
                ", t.CombinedPaths, true);
        }

        /// <summary>
        /// Passing in an array of zero paths.  Task should succeed and return zero paths.
        /// </summary>
        [TestMethod]
        public void NoPaths()
        {
            CombinePath t = new CombinePath();
            t.BuildEngine = new MockEngine();

            t.BasePath = @"c:\abc\def";
            t.Paths = new ITaskItem[0];
            Assert.IsTrue(t.Execute(), "success");

            ObjectModelHelpers.AssertItemsMatch(@"
                ", t.CombinedPaths, true);
        }

        /// <summary>
        /// Passing in a (blank) path.  Task should simply return the base path.
        /// </summary>
        [TestMethod]
        public void BlankPath()
        {
            CombinePath t = new CombinePath();
            t.BuildEngine = new MockEngine();

            t.BasePath = @"c:\abc\def";
            t.Paths = new ITaskItem[] { new TaskItem("") };
            Assert.IsTrue(t.Execute(), "success");

            ObjectModelHelpers.AssertItemsMatch(@"
                c:\abc\def
                ", t.CombinedPaths, true);
        }

        /// <summary>
        /// Specified paths contain invalid characters.  Task should continue processing remaining items.
        /// </summary>
        [TestMethod]
        public void InvalidPath()
        {
            CombinePath t = new CombinePath();
            t.BuildEngine = new MockEngine(true);

            t.BasePath = @"c:\abc\def";
            t.Paths = new ITaskItem[] { new TaskItem("ghi.txt"), new TaskItem("|.txt"), new TaskItem("jkl.txt") };
            Assert.IsFalse(t.Execute(), "should have failed");
            ((MockEngine)t.BuildEngine).AssertLogContains("MSB3095");

            ObjectModelHelpers.AssertItemsMatch(@"
                c:\abc\def\ghi.txt
                c:\abc\def\jkl.txt
                ", t.CombinedPaths, true);
        }
    }
}
