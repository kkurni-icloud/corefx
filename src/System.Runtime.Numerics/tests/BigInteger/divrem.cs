// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Tools;
using Xunit;

namespace System.Numerics.Tests
{
    public class divremTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunDivRem_TwoLargeBI()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // DivRem Method - Two Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");
            }
        }

        [Fact]
        public static void RunDivRem_TwoSmallBI()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // DivRem Method - Two Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");
            }
        }
        
        [Fact]
        public static void RunDivRem_OneSmallOneLargeBI()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // DivRem Method - One Large and one small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");

                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");
            }
        }

        [Fact]
        public static void RunDivRem_OneLargeOne0BI()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // DivRem Method - One Large BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0 };
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");

                Assert.Throws<DivideByZeroException>(() =>
                {
                    VerifyDivRemString(Print(tempByteArray2) + Print(tempByteArray1) + "bDivRem");
                });
            }
        }

        [Fact]
        public static void RunDivRem_OneSmallOne0BI()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // DivRem Method - One small BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0 };
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");

                Assert.Throws<DivideByZeroException>(() =>
                {
                    VerifyDivRemString(Print(tempByteArray2) + Print(tempByteArray1) + "bDivRem");
                });
            }
        }

        [Fact]
        public static void Boundary()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Check interesting cases for boundary conditions
            // You'll either be shifting a 0 or 1 across the boundary
            // 32 bit boundary  n2=0
            VerifyDivRemString(Math.Pow(2, 32) + " 2 bDivRem");

            // 32 bit boundary  n1=0 n2=1
            VerifyDivRemString(Math.Pow(2, 33) + " 2 bDivRem");
        }

        [Fact]
        public static void RunDivRemTests()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // DivRem Method - Two Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");
            }

            // DivRem Method - Two Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");
            }

            // DivRem Method - One Large and one small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");

                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");
            }

            // DivRem Method - One Large BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0 };
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");

                Assert.Throws<DivideByZeroException>(() => { VerifyDivRemString(Print(tempByteArray2) + Print(tempByteArray1) + "bDivRem"); });
            }

            // DivRem Method - One small BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0 };
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");

                Assert.Throws<DivideByZeroException>(() => { VerifyDivRemString(Print(tempByteArray2) + Print(tempByteArray1) + "bDivRem"); });
            }


            // Check interesting cases for boundary conditions
            // You'll either be shifting a 0 or 1 across the boundary
            // 32 bit boundary  n2=0
            VerifyDivRemString(Math.Pow(2, 32) + " 2 bDivRem");

            // 32 bit boundary  n1=0 n2=1
            VerifyDivRemString(Math.Pow(2, 33) + " 2 bDivRem");
        }

        private static void VerifyDivRemString(string opstring)
        {
            StackCalc sc = new StackCalc(opstring);
            while (sc.DoNextOperation())
            {
                Assert.Equal(sc.snCalc.Peek().ToString(), sc.myCalc.Peek().ToString());
                Assert.True(sc.VerifyOut(), "Out parameters not matching");
            }
        }

        private static Byte[] GetRandomByteArray(Random random)
        {
            return GetRandomByteArray(random, random.Next(1, 100));
        }

        private static Byte[] GetRandomByteArray(Random random, int size)
        {
            byte[] value = new byte[size];

            while (IsZero(value))
            {
                for (int i = 0; i < value.Length; ++i)
                {
                    value[i] = (byte)random.Next(0, 256);
                }
            }

            return value;
        }

        private static bool IsZero(byte[] value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != 0)
                {
                    return false;
                }
            }
            return true;
        }

        private static String Print(byte[] bytes)
        {
            String ret = "make ";

            for (int i = 0; i < bytes.Length; i++)
            {
                ret += bytes[i] + " ";
            }
            ret += "endmake ";

            return ret;
        }
    }
}
