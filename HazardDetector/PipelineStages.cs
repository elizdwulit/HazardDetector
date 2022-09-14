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
        FORWARD,
        DECODE,
        EXECUTE,
        MEMORY,
        WRITEBACK,
        STALL
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
