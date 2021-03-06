using System.Collections.Generic;
using System.Linq;
using PocketGauger.Dtos;
using PocketGauger.Interfaces;

namespace PocketGauger.Parsers
{
    public class MeterDetailsParser : IMeterDetailsParser
    {
        public IReadOnlyDictionary<string, MeterDetailsItem> Parse(PocketGaugerFiles pocketGaugerFiles)
        {
            var meterDetails = pocketGaugerFiles.ParseType<MeterDetails>();
            var meterCalibrations = pocketGaugerFiles.ParseType<MeterCalibration>();

            AssignMeterCalibrations(meterDetails, meterCalibrations);

            return meterDetails.MeterDetailsItems.ToDictionary(item => item.MeterId, item => item);
        }

        private static void AssignMeterCalibrations(MeterDetails meterDetails, MeterCalibration meterCalibrations)
        {
            foreach (var meterDetail in meterDetails.MeterDetailsItems)
            {
                meterDetail.Calibrations =
                    meterCalibrations.MeterCalibrationItems.Where(item => item.MeterId == meterDetail.MeterId).ToList();
            }
        }
    }
}
