using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HazardDetector
{
    /// <summary>
    /// The stages of the pipeline
    /// </summary>
    public enum PipelineStage
    {
        F,
        D,
        X,
        m,
        w,
        S
    }

    /// <summary>
    /// Location in a pipeline stage
    /// </summary>
    public enum PipelinePosition
    {
        BEGIN,
        MIDDLE,
        END
    }
}
