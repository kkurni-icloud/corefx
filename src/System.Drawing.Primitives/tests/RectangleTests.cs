// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.Drawing.PrimitivesTest
{
    public class RectangleTests
    {
        [Fact]
        public void DefaultConstructorTest()
        {
            Assert.Equal(Rectangle.Empty, new Rectangle());
        }

        [Theory]
        [InlineData(int.MaxValue, int.MinValue, int.MaxValue, int.MinValue)]
        [InlineData(int.MaxValue, 0, int.MinValue, 0)]
        [InlineData(0, 0, 0, 0)]
        [InlineData(0, int.MinValue, 0, int.MaxValue)]
        public void NonDefaultConstructorTest(int x, int y, int width, int height)
        {
            Rectangle rect1 = new Rectangle(x, y, width, height);
            Rectangle rect2 = new Rectangle(new Point(x, y), new Size(width, height));

            Assert.Equal(rect1, rect2);
        }

        [Theory]
        [InlineData(int.MaxValue, int.MinValue, int.MaxValue, int.MinValue)]
        [InlineData(int.MaxValue, 0, int.MinValue, 0)]
        [InlineData(0, 0, 0, 0)]
        [InlineData(0, int.MinValue, 0, int.MaxValue)]
        public void FromLTRBTest(int left, int top, int right, int bottom)
        {
            Rectangle rect1 = new Rectangle(left, top, right - left, bottom - top);
            Rectangle rect2 = Rectangle.FromLTRB(left, top, right, bottom);

            Assert.Equal(rect1, rect2);
        }

        [Fact]
        public void EmptyTest()
        {
            Assert.True(Rectangle.Empty.IsEmpty);
            Assert.True(new Rectangle(0, 0, 0, 0).IsEmpty);
            Assert.True(new Rectangle().IsEmpty);
        }

        [Theory]
        [InlineData(int.MaxValue, int.MinValue, int.MaxValue, int.MinValue)]
        [InlineData(int.MaxValue, 0, int.MinValue, 0)]
        [InlineData(int.MinValue, int.MaxValue, int.MinValue, int.MaxValue)]
        [InlineData(0, int.MinValue, 0, int.MaxValue)]
        public void NonEmptyTest(int x, int y, int width, int height)
        {
            Assert.False(new Rectangle(x, y, width, height).IsEmpty);
        }

        [Theory]
        [InlineData(int.MaxValue, int.MinValue, int.MaxValue, int.MinValue)]
        [InlineData(int.MaxValue, 0, int.MinValue, 0)]
        [InlineData(0, 0, 0, 0)]
        [InlineData(0, int.MinValue, 0, int.MaxValue)]
        [InlineData(int.MinValue, int.MaxValue, int.MinValue, int.MaxValue)]
        public void DimensionsTest(int x, int y, int width, int height)
        {
            Rectangle rect = new Rectangle(x, y, width, height);
            Assert.Equal(new Point(x, y), rect.Location);
            Assert.Equal(new Size(width, height), rect.Size);

            Assert.Equal(x, rect.X);
            Assert.Equal(y, rect.Y);
            Assert.Equal(width, rect.Width);
            Assert.Equal(height, rect.Height);
            Assert.Equal(x, rect.Left);
            Assert.Equal(y, rect.Top);
            Assert.Equal(x + width, rect.Right);
            Assert.Equal(y + height, rect.Bottom);

            Point p = new Point(width, height);
            Size s = new Size(x, y);
            rect.Location = p;
            rect.Size = s;

            Assert.Equal(p, rect.Location);
            Assert.Equal(s, rect.Size);

            Assert.Equal(width, rect.X);
            Assert.Equal(height, rect.Y);
            Assert.Equal(x, rect.Width);
            Assert.Equal(y, rect.Height);
            Assert.Equal(width, rect.Left);
            Assert.Equal(height, rect.Top);
            Assert.Equal(x + width, rect.Right);
            Assert.Equal(y + height, rect.Bottom);
        }

        [Theory]
        [InlineData(int.MaxValue, int.MinValue, int.MaxValue, int.MinValue)]
        [InlineData(int.MaxValue, 0, int.MinValue, 0)]
        [InlineData(0, int.MinValue, 0, int.MaxValue)]
        [InlineData(int.MinValue, int.MaxValue, int.MinValue, int.MaxValue)]
        public void EqualityTest(int x, int y, int width, int height)
        {
            Rectangle rect1 = new Rectangle(x, y, width, height);
            Rectangle rect2 = new Rectangle(width/2, height/2, x, y);

            Assert.True(rect1 != rect2);
            Assert.False(rect1 == rect2);
            Assert.False(rect1.Equals(rect2));
        }

        [Theory]
        [InlineData(float.MaxValue, float.MinValue, float.MaxValue, float.MinValue)]
        [InlineData(float.MinValue, float.MaxValue, float.MinValue, float.MaxValue)]
        [InlineData(0, 0, 0, 0)]
        public void RectangleFConversionTest(float x, float y, float width, float height)
        {
            RectangleF rect = new RectangleF(x, y, width, height);
            Rectangle rCeiling = new Rectangle((int)Math.Ceiling(x), (int)Math.Ceiling(y),
                (int)Math.Ceiling(width), (int)Math.Ceiling(height));
            Rectangle rTruncate = new Rectangle((int)x, (int)y, (int)width, (int)height);
            Rectangle rRound = new Rectangle((int)Math.Round(x), (int)Math.Round(y),
                (int)Math.Round(width), (int)Math.Round(height));

            Assert.Equal(rCeiling, Rectangle.Ceiling(rect));
            Assert.Equal(rTruncate, Rectangle.Truncate(rect));
            Assert.Equal(rRound, Rectangle.Round(rect));
        }

        [Theory]
        [InlineData(int.MaxValue, int.MinValue, int.MinValue, int.MaxValue)]
        [InlineData(0, int.MinValue, int.MaxValue, 0)]
        public void ContainsTest(int x, int y, int width, int height)
        {
            Rectangle rect = new Rectangle(2*x-width, 2*y-height, width, height);
            Point p = new Point(x, y);
            Rectangle r = new Rectangle(x, y, width / 2, height / 2);

            Assert.False(rect.Contains(x, y));
            Assert.False(rect.Contains(p));
            Assert.False(rect.Contains(r));
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(int.MaxValue, int.MinValue, int.MinValue, int.MaxValue)]
        [InlineData(0, int.MinValue, int.MaxValue, 0)]
        public void InflateTest(int x, int y, int width, int height)
        {
            Rectangle rect = new Rectangle(x, y, width, height);
            Rectangle inflatedRect = new Rectangle(x - width, y - height, width + 2 * width, height + 2 * height);

            rect.Inflate(width, height);
            Assert.Equal(inflatedRect, rect);

            Size s = new Size(x, y);
            inflatedRect = new Rectangle(rect.X - x, rect.Y - y, rect.Width + 2 * x, rect.Height + 2 * y);

            rect.Inflate(s);
            Assert.Equal(inflatedRect, rect);
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(int.MaxValue, int.MinValue, int.MinValue, int.MaxValue)]
        [InlineData(0, int.MinValue, int.MaxValue, 0)]
        public void IntersectTest(int x, int y, int width, int height)
        {
            Rectangle rect = new Rectangle(x, y, width, height);
            Rectangle expectedRect = Rectangle.Intersect(rect, rect);
            rect.Intersect(rect);
            Assert.Equal(expectedRect, rect);
            Assert.False(rect.IntersectsWith(expectedRect));
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(int.MaxValue, int.MinValue, int.MinValue, int.MaxValue)]
        [InlineData(int.MaxValue, 0, 0, int.MaxValue)]
        [InlineData(0, int.MinValue, int.MaxValue, 0)]
        public void UnionTest(int x, int y, int width, int height)
        {
            Rectangle a = new Rectangle(x, y, width, height);
            Rectangle b = new Rectangle(width, height, x, y);

            int x1 = Math.Min(a.X, b.X);
            int x2 = Math.Max(a.X + a.Width, b.X + b.Width);
            int y1 = Math.Min(a.Y, b.Y);
            int y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);

            Rectangle expectedRectangle = new Rectangle(x1, y1, x2 - x1, y2 - y1);

            Assert.Equal(expectedRectangle, Rectangle.Union(a, b));
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(int.MaxValue, int.MinValue, int.MinValue, int.MaxValue)]
        [InlineData(int.MaxValue, 0, 0, int.MaxValue)]
        [InlineData(0, int.MinValue, int.MaxValue, 0)]
        public void OffsetTest(int x, int y, int width, int height)
        {
            Rectangle r1 = new Rectangle(x, y, width, height);
            Rectangle expectedRect = new Rectangle(x + width, y + height, width, height);
            Point p = new Point(width, height);

            r1.Offset(p);
            Assert.Equal(expectedRect, r1);

            expectedRect.Offset(p);
            r1.Offset(width, height);
            Assert.Equal(expectedRect, r1);
        }

        [Fact]
        public void ToStringTest()
        {
            string expected = "{X=0,Y=0,Width=0,Height=0}";
            Assert.Equal(expected, Rectangle.Empty.ToString());
        }
    }
}
