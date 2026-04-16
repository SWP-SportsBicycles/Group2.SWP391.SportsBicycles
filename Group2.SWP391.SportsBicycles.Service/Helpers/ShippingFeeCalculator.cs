using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Helpers
{
    public static class ShippingFeeCalculator
    {
        public static decimal Calculate(decimal distanceKm)
        {
            if (distanceKm <= 0) return 0;

            decimal fee = 15000;

            if (distanceKm <= 5)
            {
                fee += distanceKm * 4000;
            }
            else if (distanceKm <= 20)
            {
                fee += 5 * 4000;
                fee += (distanceKm - 5) * 3000;
            }
            else
            {
                fee += 5 * 4000;
                fee += 15 * 3000;
                fee += (distanceKm - 20) * 2500;
            }

            return Math.Round(fee, 0);
        }
    }
}
