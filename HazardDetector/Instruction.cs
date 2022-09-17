using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HazardDetector
{
    public class Instruction
    {
        // the string that the instruction was constructed from
        string origInstructionStr = "";

        // the type of instruction
        InstructionType type;

        // list of registers involved with instruction
        List<Register> registers = new List<Register>();

        public Instruction(string origInstructionStr, InstructionType type, List<Register> registers)
        {
            this.origInstructionStr = origInstructionStr;
            this.type = type;
            this.registers = registers;
        }

        public string getInstructionStr()
        {
            return origInstructionStr;
        }

        public void setInstructionStr(string str)
        {
            origInstructionStr = str;
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
