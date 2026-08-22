using System;
using System.Collections.Generic;

namespace MvcVisionSystem
{
    public class AnomalyClassificationSettings
    {
        public List<string> NormalClassNames { get; set; } = new List<string>();

        public List<string> AbnormalClassNames { get; set; } = new List<string>();

        public double MinimumConfidence { get; set; }

        public void EnsureDefaults()
        {
            NormalClassNames ??= new List<string>();
            AbnormalClassNames ??= new List<string>();
            MinimumConfidence = double.IsNaN(MinimumConfidence) || double.IsInfinity(MinimumConfidence)
                ? 0D
                : Math.Clamp(MinimumConfidence, 0D, 1D);
        }

        public AnomalyClassificationDecisionOptions ToDecisionOptions()
        {
            EnsureDefaults();
            return new AnomalyClassificationDecisionOptions
            {
                NormalClassNames = NormalClassNames,
                AbnormalClassNames = AbnormalClassNames,
                MinimumConfidence = MinimumConfidence
            };
        }
    }
}
