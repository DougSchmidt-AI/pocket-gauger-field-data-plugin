﻿using System;
using System.Collections.Generic;
using System.Linq;
using Common.Utils;
using NSubstitute;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Server.BusinessInterfaces.FieldDataPlugInCore.DataModel.DischargeActivities;
using Server.BusinessInterfaces.FieldDataPlugInCore.DataModel.DischargeSubActivities;
using Server.BusinessInterfaces.FieldDataPlugInCore.DataModel.Verticals;
using Server.Plugins.FieldVisit.PocketGauger.Dtos;
using Server.Plugins.FieldVisit.PocketGauger.Interfaces;
using Server.Plugins.FieldVisit.PocketGauger.Mappers;
using Server.Plugins.FieldVisit.PocketGauger.UnitTests.TestData;
using MeterCalibration = Server.BusinessInterfaces.FieldDataPlugInCore.DataModel.Meters.MeterCalibration;

namespace Server.Plugins.FieldVisit.PocketGauger.UnitTests.Mappers
{
    [TestFixture]
    public class VerticalMapperTests
    {
        private IFixture _fixture;

        private VerticalMapper _verticalMapper;
        private IMeterCalibrationMapper _meterCalibrationMapper;

        private GaugingSummaryItem _gaugingSummaryItem;
        private PointVelocityDischarge _pointVelocityDischarge;

        [TestFixtureSetUp]
        public void SetUp()
        {
            _fixture = new Fixture();
            CollectionRegistrar.Register(_fixture);
            _fixture.Customizations.Add(new ProxyTypeSpecimenBuilder());
            _gaugingSummaryItem = _fixture.Create<GaugingSummaryItem>();

            SetUpPointVelocityDischarge();
            SetUpMeterCalibrationMapper();

            _verticalMapper = new VerticalMapper(_meterCalibrationMapper);
        }

        private void SetUpPointVelocityDischarge()
        {
            _pointVelocityDischarge = new PointVelocityDischarge();
            var dischargeChannelMeasurement = new DischargeChannelMeasurement
            {
                DeploymentMethod = _fixture.Create<DeploymentMethodType>()
            };
            _pointVelocityDischarge.ChannelMeasurement = dischargeChannelMeasurement;
        }

        private void SetUpMeterCalibrationMapper()
        {
            _meterCalibrationMapper = Substitute.For<IMeterCalibrationMapper>();
            var meterCalibration = _fixture.Build<MeterCalibration>().Without(m => m.Equations).Create();
            _meterCalibrationMapper.Map(Arg.Any<MeterDetailsItem>()).Returns(meterCalibration);
        }

        [Test]
        public void Map_SetsCorrectVerticalProperties()
        {
            var result = _verticalMapper.Map(_gaugingSummaryItem, _pointVelocityDischarge);

            var inputPanels = _gaugingSummaryItem.PanelItems.ToList();
            for (var i = 0; i < inputPanels.Count; i++)
            {
                Assert.That(result[i].SequenceNumber, Is.EqualTo(inputPanels[i].VerticalNumber));
                Assert.That(result[i].MeasurementConditionData, Is.InstanceOf<OpenWaterData>());
                Assert.That(result[i].FlowDirection, Is.EqualTo(FlowDirectionType.Normal));
                Assert.That(result[i].TaglinePosition, Is.EqualTo(inputPanels[i].Distance));
                Assert.That(result[i].SoundedDepth, Is.EqualTo(inputPanels[i].Depth));
                Assert.That(result[i].IsSoundedDepthEstimated, Is.False);
                Assert.That(result[i].EffectiveDepth, Is.EqualTo(inputPanels[i].Depth));
            }
        }

        [Test]
        public void Map_SetsCorrectVerticalType()
        {
            var result = _verticalMapper.Map(_gaugingSummaryItem, _pointVelocityDischarge);

            Assert.That(result.First().VerticalType == VerticalType.StartEdgeNoWaterBefore);
            var midRiverVerticals = result.Skip(1).Take(result.Count - 2);
            Assert.That(midRiverVerticals, Has.All.Matches<Vertical>(v => v.VerticalType == VerticalType.MidRiver));
            Assert.That(result.Last().VerticalType == VerticalType.EndEdgeNoWaterAfter);
        }

        [Test]
        public void Map_SetsCorrectSegmentProperties()
        {
            var result = _verticalMapper.Map(_gaugingSummaryItem, _pointVelocityDischarge);

            var inputPanels = _gaugingSummaryItem.PanelItems.ToList();
            for (var i = 0; i < inputPanels.Count; i++)
            {
                Assert.That(result[i].Segment.Area, Is.EqualTo(inputPanels[i].Area));
                Assert.That(result[i].Segment.Velocity, Is.EqualTo(inputPanels[i].MeanVelocity));
                Assert.That(result[i].Segment.Discharge, Is.EqualTo(inputPanels[i].Flow));
                Assert.That(result[i].Segment.IsDischargeEstimated, Is.False);
            }
        }

        private static readonly List<Tuple<double[], double[]>> GetSegmentWidthTestCases =
            new List<Tuple<double[], double[]>>
            {
                Tuple.Create(new double[] {2, 3, 4}, new double[] {2, 1, 1}),
                Tuple.Create(new double[] {400, 600, 1100}, new double[] {400, 200, 500}),
                Tuple.Create(new[] {50.23, 55.5, 60.1111}, new[] {50.23, 5.27, 4.6111})
            };

        [TestCaseSource(nameof(GetSegmentWidthTestCases))]
        public void Map_CalculatesCorrectSegmentWidth(Tuple<double[], double[]> testData)
        {
            var inputPanelDistances = testData.Item1;
            var expectedSegmentWidths = testData.Item2;
            for (var i = 0; i < inputPanelDistances.Length; i++)
            {
                _gaugingSummaryItem.PanelItems.ToList()[i].Distance = inputPanelDistances[i];
            }

            var result = _verticalMapper.Map(_gaugingSummaryItem, _pointVelocityDischarge);

            for (var i = 0; i < expectedSegmentWidths.Length; i++)
            {
                Assert.That(DoubleHelper.AreEqual(result[i].Segment.Width, expectedSegmentWidths[i]));
            }
        }

        [Test]
        public void Map_SetsCorrectVelocityObservationProperties()
        {
            var result = _verticalMapper.Map(_gaugingSummaryItem, _pointVelocityDischarge);

            for (var i = 0; i < _gaugingSummaryItem.PanelItems.Count; i++)
            {
                Assert.That(result[i].VelocityObservation.DeploymentMethod,
                    Is.EqualTo(_pointVelocityDischarge.ChannelMeasurement.DeploymentMethod));
                Assert.That(result[i].VelocityObservation.MeanVelocity,
                    Is.EqualTo(_gaugingSummaryItem.PanelItems.ToList()[i].MeanVelocity));
            }
        }

        [Test]
        public void Map_RetrievesMeterCalibrationMapperFromMapper()
        {
            _verticalMapper.Map(_gaugingSummaryItem, _pointVelocityDischarge);

            _meterCalibrationMapper.Received().Map(_gaugingSummaryItem.MeterDetailsItem);
        }

        [Test]
        public void Map_SetsCorrectVelocityDepthObservationProperties()
        {
            var result = _verticalMapper.Map(_gaugingSummaryItem, _pointVelocityDischarge);

            var panelItems = _gaugingSummaryItem.PanelItems.ToList();
            for (var i = 0; i < panelItems.Count; i++)
            {
                var verticalItems = panelItems[i].Verticals;
                for (var j = 0; j < verticalItems.Count; j++)
                {
                    var resultObservation = result[i].VelocityObservation.Observations.ToList()[j];

                    Assert.That(resultObservation.Depth, Is.EqualTo(verticalItems[j].Depth));
                    Assert.That(resultObservation.RevolutionCount, Is.EqualTo((int)verticalItems[j].Revs));
                    Assert.That(resultObservation.ObservationInterval, Is.EqualTo(verticalItems[j].ExposureTime));
                    Assert.That(resultObservation.Velocity, Is.EqualTo(verticalItems[j].Velocity));
                    Assert.That(resultObservation.DepthMultiplier, Is.EqualTo(1));
                    Assert.That(resultObservation.Weighting, Is.EqualTo(1));
                }
            }
        }

        private static readonly List<Tuple<int, PointVelocityObservationType>> MultipleVelocityObservationTestCases =
            new List<Tuple<int, PointVelocityObservationType>>
            {
                Tuple.Create(2, PointVelocityObservationType.OneAtPointTwoAndPointEight),
                Tuple.Create(3, PointVelocityObservationType.OneAtPointTwoPointSixAndPointEight),
                Tuple.Create(5, PointVelocityObservationType.FivePoint),
                Tuple.Create(6, PointVelocityObservationType.SixPoint),
                Tuple.Create(11, PointVelocityObservationType.ElevenPoint)
            };

        [TestCaseSource(nameof(MultipleVelocityObservationTestCases))]
        public void Map_VerticalWithExpectedNumberOfObservations_SetsExpectedVelocityObservationType(
            Tuple<int, PointVelocityObservationType> testData)
        {
            var velocityObservationCount = testData.Item1;
            var expectedVelocityObservationMethod = testData.Item2;
            _gaugingSummaryItem.PanelItems.First().Verticals = _fixture.CreateMany<VerticalItem>(velocityObservationCount).ToList();

            var result = _verticalMapper.Map(_gaugingSummaryItem, _pointVelocityDischarge);

            AssertVelocityObservationMethodIsExpected(result, expectedVelocityObservationMethod);
        }

        private static void AssertVelocityObservationMethodIsExpected(IEnumerable<Vertical> result,
            PointVelocityObservationType? expectedVelocityObservationMethod)
        {
            var velocityObservationMethod = result.First().VelocityObservation.VelocityObservationMethod;

            Assert.That(velocityObservationMethod, Is.EqualTo(expectedVelocityObservationMethod));
        }

        [Test]
        public void Map_UnknownVelocityObservationCount_SetsVelocityObservationTypeToNull()
        {
            var expectedObservationCounts = new HashSet<int>(MultipleVelocityObservationTestCases.Select(tuple => tuple.Item1));
            var unknownObservationCount = CreateValueNotInSet(expectedObservationCounts);

            _gaugingSummaryItem.PanelItems.First().Verticals = _fixture.CreateMany<VerticalItem>(unknownObservationCount).ToList();

            var result = _verticalMapper.Map(_gaugingSummaryItem, _pointVelocityDischarge);

            AssertVelocityObservationMethodIsExpected(result, null);
        }

        private TValueToCreate CreateValueNotInSet<TValueToCreate>(ISet<TValueToCreate> expectedObservationCounts)
        {
            TValueToCreate value;

            do
            {
                value = _fixture.Create<TValueToCreate>();
            } while (expectedObservationCounts.Contains(value));

            return value;
        }

        private static readonly List<Tuple<double, PointVelocityObservationType>> DepthValueToVelocityObservationTestCases =
            new List<Tuple<double, PointVelocityObservationType>>
            {
                Tuple.Create(0.5, PointVelocityObservationType.OneAtPointFive),
                Tuple.Create(0.6, PointVelocityObservationType.OneAtPointSix)
            };

        [TestCaseSource(nameof(DepthValueToVelocityObservationTestCases))]
        public void Map_VerticalWithSingleObservation_SetsExpectedVelocityObservationType(
            Tuple<double, PointVelocityObservationType> testData)
        {
            var observationDepth = testData.Item1;
            var expectedVelocityObservationMethod = testData.Item2;
            var verticals = _fixture.Build<VerticalItem>()
                .With(item => item.SamplePosition, observationDepth)
                .CreateMany(1)
                .ToList();

            _gaugingSummaryItem.PanelItems.First().Verticals = verticals;

            var result = _verticalMapper.Map(_gaugingSummaryItem, _pointVelocityDischarge);

            AssertVelocityObservationMethodIsExpected(result, expectedVelocityObservationMethod);
        }

        [Test]
        public void Map_VerticalWithSingleObservationWithNonPoint5Or6SampleDepth_SetsVelocityObservationTypeToSurface()
        {
            var expectedObservationCounts = new HashSet<double>(DepthValueToVelocityObservationTestCases.Select(tuple => tuple.Item1));
            var nonPointFiveOrSixDepth = CreateValueNotInSet(expectedObservationCounts);

            var verticals = _fixture.Build<VerticalItem>()
                .With(item => item.SamplePosition, nonPointFiveOrSixDepth)
                .CreateMany(1)
                .ToList();

            _gaugingSummaryItem.PanelItems.First().Verticals = verticals;

            var result = _verticalMapper.Map(_gaugingSummaryItem, _pointVelocityDischarge);

            AssertVelocityObservationMethodIsExpected(result, PointVelocityObservationType.Surface);
        }

        private static readonly List<Tuple<double[], double[]>> GetTotalDischargePortionTestCases =
            new List<Tuple<double[], double[]>>
            {
                Tuple.Create(new double[] {2, 3, 4}, new[] {22.222222222222221, 33.333333333333329, 44.444444444444443}),
                Tuple.Create(new double[] {400, 600, 1100}, new[] {19.047619047619047, 28.571428571428569, 52.380952380952387}),
                Tuple.Create(new[] {50.23, 55.5, 60.1111}, new[] {30.288028721468923, 33.465769341857964, 36.246201936673124})
            };

        [TestCaseSource(nameof(GetTotalDischargePortionTestCases))]
        public void Map_CalculatesCorrectTotalDischargePortionValues(Tuple<double[], double[]> testData )
        {
            var inputDischargeValues = testData.Item1;
            var panelItems = _gaugingSummaryItem.PanelItems.ToList();
            for (var i = 0; i < panelItems.Count; i++)
            {
                panelItems[i].Flow = inputDischargeValues[i];
            }

            var result = _verticalMapper.Map(_gaugingSummaryItem, _pointVelocityDischarge);

            var expectedTotalDischargePortionValues = testData.Item2;
            for (var i = 0; i < result.Count; i++)
            {
                Assert.That(DoubleHelper.AreEqual(result[i].Segment.TotalDischargePortion,
                    expectedTotalDischargePortionValues[i]));
            }
        }
    }
}