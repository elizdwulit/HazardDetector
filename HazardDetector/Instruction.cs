using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HazardDetector
{
    public class Instruction
    {
        InstructionType type;
        List<Register> registers = new List<Register>();

        public Instruction(InstructionType type, List<Register> registers)
        {
            this.type = type;
            this.registers = registers;
        }

        public InstructionType getType()
        {
            return type;
        }

        public List<Register> getRegisters()
        {
            return registers;
        }
    }

    public enum InstructionType
    {
        ADD,
        SUB,
        LW,
        SW,
        UNKNOWN
    }
};
