﻿using System;
using FieldDataPluginFramework.Context;
using Ploeh.AutoFixture;

namespace PocketGauger.UnitTests.TestHelpers
{
    public static class LocationInfoHelper
    {
        public const double ValidUtcOffsetHour = -8.5;

        public static LocationInfo GetTestLocationInfo(IFixture fixture)
        {
            return PrivateConstructorHelper.CreateInstance<LocationInfo>(
                fixture.Create<string>(),
                fixture.Create<string>(),
                fixture.Create<Int64>(),
                fixture.Create<Guid>(),
                ValidUtcOffsetHour);
        }
    }
}
