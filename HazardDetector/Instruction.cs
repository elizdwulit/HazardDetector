using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HazardDetector
{
    public class Instruction
    {
        string origInstructionStr = "";
        InstructionType type;
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
