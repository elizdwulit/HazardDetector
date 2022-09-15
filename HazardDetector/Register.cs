using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HazardDetector
{
    public class Register
    {
        string name;
        (PipelineStage?, PipelinePosition?) availableNoFwd = (null, null);
        (PipelineStage?, PipelinePosition?) availableWithFwd = (null, null);
        (PipelineStage?, PipelinePosition?) neededNoFwd = (null, null);
        (PipelineStage?, PipelinePosition?) neededWithFwd = (null, null);

        public Register(string name)
        {
            this.name = name;
        }

        public void setName(string name)
        {
            this.name = name;
        }

        public string getName()
        {
            return name;
        }

        public void setAvailableNoFwd(PipelineStage stage, PipelinePosition position)
        {
            availableNoFwd = (stage, position);
        }

        public (PipelineStage?, PipelinePosition?) getAvailableNoFwd()
        {
            return availableNoFwd;
        }

        public void setAvailableWithFwd(PipelineStage stage, PipelinePosition position)
        {
            availableWithFwd = (stage, position);
        }

        public (PipelineStage?, PipelinePosition?) getAvailableWithFwd()
        {
            return availableWithFwd;
        }

        public void setNeededNoFwd(PipelineStage stage, PipelinePosition position)
        {
            neededNoFwd = (stage, position);
        }

        public (PipelineStage?, PipelinePosition?) getNeededNoFwd()
        {
            return neededNoFwd;
        }

        public void setNeededWithFwd(PipelineStage stage, PipelinePosition position)
        {
            neededWithFwd = (stage, position);
        }

        public (PipelineStage?, PipelinePosition?) getNeededWithFwd()
        {
            return neededWithFwd;
        }
    }
}
