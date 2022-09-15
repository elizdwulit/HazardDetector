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

            // create the pipeline table
            String[,] table = new String[insructionList.Count, 100]; // 100 columns

            // keep list of when registers are last available, indexed by register name
            Dictionary<string, (int, PipelinePosition?)> availDict = new Dictionary<string, (int, PipelinePosition?)>();

            List<int> stallCols = new List<int>(); // keep track of cols where stalls are put
            int lastDColIndex = 0; // keep track of where to put next F
            for (int i = 0; i < insructionList.Count; i++)
            {
                Instruction inst = insructionList[i];
                List<Register> instRegisters = inst.getRegisters();

                // keep track of which registers are needed in the current instruction
                Dictionary<string, (int, PipelinePosition?)> neededDict = new Dictionary<string, (int, PipelinePosition?)>();

                // iterate through registers in instruction and add to dicts
                foreach (Register r in instRegisters)
                {
                    int currNumStalls = stallCols.Count;

                    if (r.getAvailableNoFwd() != (null, null))
                    {
                        // get the index of the stage where register is available
                        int indexAvailable = (int)r.getAvailableNoFwd().Item1 + i + currNumStalls;

                        // add to avail dict if newly found or new availability is later than previous availability col index
                        (int, PipelinePosition?) availVal;
                        bool registerInAvailDict = availDict.TryGetValue(r.getName(), out availVal);
                        if (!registerInAvailDict || availVal.Item1 < indexAvailable)
                        {
                            availDict.Add(r.getName(), (indexAvailable, r.getAvailableNoFwd().Item2));
                        }
                    }

                    // without adding any new stalls, where is register needed
                    if (r.getNeededNoFwd() != (null, null))
                    {
                        int indexNeeded = (int)r.getNeededNoFwd().Item1 + i + currNumStalls;
                        neededDict.Add(r.getName(), (indexNeeded, r.getNeededNoFwd().Item2));
                    }
                }

                foreach (string registerName in neededDict.Keys)
                {
                    if (availDict.ContainsKey(registerName))
                    {
                        // get info on where register is available
                        (int, PipelinePosition?) positionAvail;
                        availDict.TryGetValue(registerName, out positionAvail);
                        int indexAvail = positionAvail.Item1;
                        PipelinePosition? availPos = positionAvail.Item2;

                        // get info on where register is needed
                        (int, PipelinePosition?) positionNeeded;
                        neededDict.TryGetValue(registerName, out positionNeeded);
                        if (positionNeeded != (null, null))
                        {
                            // if register is needed, check if needed before register is available
                            int indexNeeded = positionNeeded.Item1;
                            PipelinePosition? neededPos = positionNeeded.Item2;
                            if (indexAvail > indexNeeded) // hazard found
                            {
                                // add stalls until aligned or arrow would point forward
                                int posToAddStalls = availPos > neededPos ? indexAvail + 1 : indexAvail;
                                for (int j = indexNeeded; j < posToAddStalls; j++)
                                {
                                    stallCols.Add(j);
                                }
                            }
                        }
                    }
                }

                // first instruction, initialize table
                table[i, lastDColIndex] = "F";
                table[i, 1] = "D";
                table[i, 2] = "X";
                table[i, 3] = "m";
                table[i, 4] = "w";
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
