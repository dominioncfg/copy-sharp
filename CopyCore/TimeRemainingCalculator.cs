using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyCore
{
    static class TimeRemainingCalculator
    {
        public static TimeSpan Calculate(ICopierProgress Copier)
        {
            if (Copier.Speed==0)
            {
                return TimeSpan.FromSeconds(0);
            }
            long remBytes = (Copier.TotalLength - Copier.BytesCopied);
            double time=((double) remBytes)/((double)Copier.Speed);
            double SwitchFile = 0.01;
            int remFiles= Copier.FilesCount-Copier.CopiedFiles;
            time = time + (SwitchFile * remFiles);
            return TimeSpan.FromSeconds(time);
        }
    }
}
