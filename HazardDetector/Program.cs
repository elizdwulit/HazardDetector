using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HazardDetector
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // check that file was provided
            if (args.Length == 0)
            {
                Console.WriteLine("Please provide a file path");
                return;
            }

            // read the instructions from the file and create Instruction objects
            List<Instruction> insructionList = new List<Instruction>();
            string filePath = args[0];
            using (StreamReader sr = File.OpenText(filePath))
            {
                string s = String.Empty;
                while ((s = sr.ReadLine()) != null)
                {
                    Instruction instruction = ParseInstruction(s);
                    insructionList.Add(instruction);
                }
            }

            // keep list of when registers are last available, indexed by register name
            Dictionary<string, (int, PipelinePosition)> availDict = new Dictionary<string, (int, PipelinePosition)>();

            // keep track of column in pipeline execution
            int col = 0;
            int offset = 0;
            foreach (Instruction inst in insructionList)
            {
                List<Register> instRegisters = inst.getRegisters();
                foreach (Register r in instRegisters)
                {
                    bool registerInDict = availDict.ContainsKey(r.getName());
                    if (!registerInDict)
                    {
                        availDict.Add(r.getName(), ((int)r.getAvailableNoFwd().Item1 + offset, r.getAvailableNoFwd().Item2));
                    } else
                    {

                    }
                    
                }
            }


        }

        /// <summary>
        /// Parse a given instuction line
        /// </summary>
        /// <param name="instruction">instruction string to parse</param>
        /// <returns>instruction object with register availability and needed stages</returns>
        private static Instruction ParseInstruction(string instruction)
        {
            // get the instruction type
            string[] splitInstruction = instruction.Split(' ');
            string instructionTypeStr = splitInstruction != null ? splitInstruction[0] : "";
            InstructionType instructionType = GetInstructionType(instructionTypeStr);

            // get the registers involved in the instruction
            List<Register> registers = new List<Register>();
            string[] splitRegisters = instruction.Split(',');
            if (splitRegisters != null && splitRegisters.Length > 0)
            {
                splitRegisters[0] = splitRegisters[0].Split(' ')[1]; // remove the instruction type key
            }

            // handle add and subtract instruction types
            if (instructionType == InstructionType.ADD || instructionType == InstructionType.SUB)
            {
                for (int i = 0; i < splitRegisters.Length; i++) // should only be 3 registers max
                {
                    // registers can only be prefixed by '$R' or '$t'
                    if (splitRegisters[i].StartsWith("$R") || splitRegisters[i].StartsWith("$t"))
                    {
                        Register r = new Register(splitRegisters[i]);
                        if (i == 0) // first register always where being stored
                        {
                            r.setAvailableNoFwd(PipelineStage.WRITEBACK, PipelinePosition.MIDDLE);
                            r.setAvailableWithFwd(PipelineStage.EXECUTE, PipelinePosition.END);
                        } else if (i == 1 || i == 2) // 2nd and/or 3rd register is what is being used in ADD/SUB
                        {
                            r.setNeededNoFwd(PipelineStage.DECODE, PipelinePosition.MIDDLE);
                            r.setNeededWithFwd(PipelineStage.EXECUTE, PipelinePosition.BEGIN);
                        }
                        registers.Add(r);
                    } else
                    {
                        // not a register, no dependency
                    }
                }
            } else if (instructionType == InstructionType.LW)
            {
                if (splitRegisters[0].StartsWith("$R") || splitRegisters[0].StartsWith("$t"))
                {
                    Register r = new Register(splitRegisters[0]);
                    r.setAvailableNoFwd(PipelineStage.WRITEBACK, PipelinePosition.MIDDLE);
                    r.setAvailableWithFwd(PipelineStage.MEMORY, PipelinePosition.END);
                    registers.Add(r);
                }
                else
                {
                    // LW should start with a register; invalid instruction
                }

            } else if (instructionType == InstructionType.SW)
            {
                if (splitRegisters[0].StartsWith("$R") || splitRegisters[0].StartsWith("$t"))
                {
                    Register r = new Register(splitRegisters[0]);
                    r.setAvailableNoFwd(PipelineStage.DECODE, PipelinePosition.MIDDLE);
                    r.setAvailableWithFwd(PipelineStage.WRITEBACK, PipelinePosition.BEGIN);
                    registers.Add(r);
                }
                else
                {
                    // SW should start with a register; invalid instruction
                }
            } else
            {
                Console.WriteLine("Unknown instruction type found in instruction: " + instruction);
                return null;
            }

            Instruction instructionObj = new Instruction(instructionType, registers);
            return instructionObj;
        }

        private static InstructionType GetInstructionType(string typeStr)
        {
            switch(typeStr)
            {
                case "Add":
                    return InstructionType.ADD;
                case "Sub":
                    return InstructionType.SUB;
                case "lw":
                    return InstructionType.LW;
                case "sw":
                    return InstructionType.SW;
                default:
                    return InstructionType.UNKNOWN;
            }
        }
    }
}
