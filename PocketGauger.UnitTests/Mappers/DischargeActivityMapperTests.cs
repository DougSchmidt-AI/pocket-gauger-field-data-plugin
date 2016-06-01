﻿using System;
using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Server.BusinessInterfaces.FieldDataPlugInCore.Context;
using Server.BusinessInterfaces.FieldDataPlugInCore.DataModel.DischargeActivities;
using Server.Plugins.FieldVisit.PocketGauger.Dtos;
using Server.Plugins.FieldVisit.PocketGauger.Helpers;
using Server.Plugins.FieldVisit.PocketGauger.Mappers;
using Server.Plugins.FieldVisit.PocketGauger.UnitTests.TestData;

namespace Server.Plugins.FieldVisit.PocketGauger.UnitTests.Mappers
{
    [TestFixture]
    public class DischargeActivityMapperTests
    {
        private IFixture _fixture;
        private IParseContext _context;
        private ILocationInfo _locationInfo;

        private DischargeActivityMapper _mapper;
        private GaugingSummaryItem _gaugingSummaryItem;

        private const int LocationUtcOffset = 3;

        [TestFixtureSetUp]
        public void SetupForAllTests()
        {
            _fixture = new Fixture();
            _fixture.Customizations.Add(new ProxyTypeSpecimenBuilder());

            _context = new ParseContextTestHelper().CreateMockParseContext();

            SetupMockLocationInfo();

            _mapper = new DischargeActivityMapper(_context);
        }

        [SetUp]
        public void SetupForEachTest()
        {
            _gaugingSummaryItem = _fixture.Create<GaugingSummaryItem>();
        }

        private void SetupMockLocationInfo()
        {
            _locationInfo = Substitute.For<ILocationInfo>();

            var channel = Substitute.For<IChannelInfo>();
            _locationInfo.Channels.ReturnsForAnyArgs(new List<IChannelInfo> { channel });
            _locationInfo.UtcOffsetHours.ReturnsForAnyArgs(LocationUtcOffset);
        }

        [Test]
        public void Map_GaugingSummaryStartDateLocalTime_IsMappedDateTimeOffsetWithLocationUtcOffset()
        {
            var dischargeActivity = _mapper.Map(_locationInfo, _gaugingSummaryItem);

            AssertDateTimeOffsetIsNotDefault(dischargeActivity.StartTime);
        }

        private static void AssertDateTimeOffsetIsNotDefault(DateTimeOffset dateTimeOffset)
        {
            Assert.That(dateTimeOffset.Offset, Is.EqualTo(TimeSpan.FromHours(LocationUtcOffset)));
            Assert.That(dateTimeOffset, Is.Not.EqualTo(default(DateTimeOffset)));
        }

        [Test]
        public void Map_GaugingSummaryEndDateLocalTime_IsMappedDateTimeOffsetWithLocationUtcOffset()
        {
            var dischargeActivity = _mapper.Map(_locationInfo, _gaugingSummaryItem);

            AssertDateTimeOffsetIsNotDefault(dischargeActivity.EndTime);
        }

        [Test]
        public void Map_GaugingSummaryFlowCalculationMethodIsMean_SetMonitoringMethodAsMeanSection()
        {
            _gaugingSummaryItem.FlowCalculationMethodProxy = FlowCalculationMethod.Mean.ToString();

            var dischargeActivity = _mapper.Map(_locationInfo, _gaugingSummaryItem);

            Assert.That(dischargeActivity.DischargeMethod.MethodCode, Is.EqualTo(ParametersAndMethodsConstants.MeanSectionMonitoringMethod));
        }

        [Test]
        public void Map_GaugingSummaryFlowCalculationMethodIsMId_SetMonitoringMethodAsMidSection()
        {
            _gaugingSummaryItem.FlowCalculationMethodProxy = FlowCalculationMethod.Mid.ToString();

            var dischargeActivity = _mapper.Map(_locationInfo, _gaugingSummaryItem);

            Assert.That(dischargeActivity.DischargeMethod.MethodCode, Is.EqualTo(ParametersAndMethodsConstants.MidSectionMonitoringMethod));
        }

        [Test]
        public void Map_GaugingSummaryFlowCalculationMethodIsUnknown_SetMonitoringMethodToDefault()
        {
            _gaugingSummaryItem.FlowCalculationMethodProxy = _fixture.Create<string>();

            var dischargeActivity = _mapper.Map(_locationInfo, _gaugingSummaryItem);

            Assert.That(dischargeActivity.DischargeMethod.MethodCode, Is.EqualTo(ParametersAndMethodsConstants.DefaultMonitoringMethod));
        }

        [Test]
        public void Map_GaugingSummaryItem_IsMappedToExpectedDischargeAcitivity()
        {
            var expectedDischargeActivity = CreateExpectedDischargeActivity();

            var dischargeActivity = _mapper.Map(_locationInfo, _gaugingSummaryItem);

            dischargeActivity.ShouldBeEquivalentTo(expectedDischargeActivity, options => options
                .Excluding(activity => activity.StartTime)
                .Excluding(activity => activity.EndTime)
                .Excluding(activity => activity.MeasurementTime)
                .Excluding(activity => activity.DischargeMethod)
                .Excluding(activity => activity.MeanIndexVelocity)
                .Excluding(activity => activity.DischargeSubActivities));
        }

        private DischargeActivity CreateExpectedDischargeActivity()
        {
            return new DischargeActivity
            {
                Party = _gaugingSummaryItem.ObserversName,
                DischargeUnit = _context.DischargeParameter.DefaultUnit,
                GageHeightUnit = _context.GageHeightParameter.DefaultUnit,
                GageHeightMethod = _context.GetDefaultMonitoringMethod()
            };
        }
    }
}
