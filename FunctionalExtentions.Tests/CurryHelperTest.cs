﻿using FunctionalExtentions.Core.Currying;
using NUnit.Framework;

namespace FunctionalExtentions.Tests
{
    [TestFixture]
    public class CurryHelperTest
    {
        [Test]
        public void ApplyPassedNullValue()
        {
            //Arrange
            string obj = null;

            //Act & Assert
            Assert.IsFalse(CurryHelper.Apply<string, bool>((e) => e == null, obj).HasValue);
        }

        [Test]
        public void ApplyNullableIntValue()
        {
            //Arrange
            int? val = null;

            //Act & Assert
            Assert.IsFalse(CurryHelper.Apply<int?, bool>((e) => e == null, val).HasValue);
        }
    }
}