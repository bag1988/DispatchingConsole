using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsoDataProto.V1;

namespace BlazorLibrary.Helpers;

public static class MathFunctions
{
    public static int Percent(int a = 0, int b = 0)
    {
        int percent = 0;

        if (b != 0)
        {
            double xy = (double)a / (double)b;
            percent = (int)Math.Round(xy * 100);
        }
        return percent;
    }
}

