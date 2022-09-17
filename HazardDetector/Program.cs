using System;
using System.Collections;
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
            if (args.Length !=  2)
            {
                Console.WriteLine("Please provide a file path and flag indicating if forwarding unit should be used.");
                Console.WriteLine("Usage: <0 = use fwding unit | 1 = no fwding unit> <full file path to instructions>");
                return;
            }

            bool hasFwdUnit = Int32.Parse(args[0]) == 0;
            string filePath = args[1];

            // read the instructions from the file and create Instruction objects
            List<Instruction> insructionList = new List<Instruction>();
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
            string[,] table = new string[insructionList.Count, 40]; // 100 columns
            table[0, 0] = "F";
            table[0, 1] = "D";
            table[0, 2] = "X";
            table[0, 3] = "m";
            table[0, 4] = "w";

            // keep list of when registers are last available, indexed by register name
            Dictionary<string, (int, int, PipelinePosition?)> availDict = new Dictionary<string, (int, int, PipelinePosition?)>();

            List<int> stallCols = new List<int>(); // keep track of cols where stalls are put
            HashSet<int> rowsStallsAdded = new HashSet<int>(); // keep track of rows where stalls were added
            int currDColIndex = 1; // keep track of where the 'D' of the previous instruction is
            int nextDColIndex = 2; // keep track of where the 'D' of the current instruction is located
            int currNumColsInTable = 5; // keep track of how many cols were used
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
                        int indexAvailable = hasFwdUnit ? (int)r.getAvailableWithFwd().Item1 + i + currNumStalls 
                            : (int)r.getAvailableNoFwd().Item1 + i + currNumStalls;

                        // add to avail dict if newly found or new availability is later than previous availability col index
                        (int, int, PipelinePosition?) availVal;
                        bool registerInAvailDict = availDict.TryGetValue(r.getName(), out availVal);
                        if (!registerInAvailDict || availVal.Item2 < indexAvailable)
                        {
                            PipelinePosition? pos = hasFwdUnit ? r.getAvailableWithFwd().Item2 : r.getAvailableNoFwd().Item2;
                            availDict[r.getName()] = (i, indexAvailable, pos);
                        }
                    }

                    // without adding any new stalls, where is register needed
                    // no needed dependencies in first instruction
                    if (i != 0 && r.getNeededNoFwd() != (null, null))
                    {
                        int indexNeeded = hasFwdUnit ? (int)r.getNeededWithFwd().Item1 + i + currNumStalls
                            : (int)r.getNeededNoFwd().Item1 + i + currNumStalls;
                        PipelinePosition? pos = hasFwdUnit ? r.getNeededWithFwd().Item2 : r.getNeededNoFwd().Item2;
                        neededDict.Add(r.getName(), (indexNeeded, pos));
                    }
                }

                // check if stalls are needed
                if (i != 0)
                {
                    int numAddedStalls = 0;
                    foreach (string registerName in neededDict.Keys)
                    {
                        if (availDict.ContainsKey(registerName))
                        {
                            // get info on where register is available
                            (int, int, PipelinePosition?) positionAvail;
                            availDict.TryGetValue(registerName, out positionAvail);
                            int indexAvail = positionAvail.Item2;
                            PipelinePosition? availPos = positionAvail.Item3;

                            // get info on where register is needed
                            (int, PipelinePosition?) positionNeeded;
                            neededDict.TryGetValue(registerName, out positionNeeded);
                            if (positionNeeded != (null, null))
                            {
                                // if register is needed, check if needed before register is available
                                int indexNeeded = positionNeeded.Item1;
                                PipelinePosition? neededPos = positionNeeded.Item2;
                                if (indexAvail >= indexNeeded) // hazard found
                                {
                                    // add stalls until aligned or arrow would point forward
                                    int posLimitToAddStalls = availPos > neededPos ? indexAvail + 1 : indexAvail;
                                    for (int j = indexNeeded; j < posLimitToAddStalls; j++)
                                    {
                                        stallCols.Add(j);
                                        rowsStallsAdded.Add(i);
                                        numAddedStalls++;
                                    }

                                    // update the next D col index based on stalls locations
                                    if (posLimitToAddStalls >= nextDColIndex)
                                    {
                                        nextDColIndex = posLimitToAddStalls;
                                    }
                                }
                            }
                        }
                    }

                    // construct the row for the table with the pipeline stages and stalls
                    int pipelineStageInt = 0;
                    currNumColsInTable += numAddedStalls <= stallCols.Count ? numAddedStalls + 1 : 1;
                    for (int k = currDColIndex; k < currNumColsInTable; k++)
                    {
                        if (!stallCols.Contains(k))
                        {
                            table[i, k] = ((PipelineStage)pipelineStageInt).ToString(); // i is the curr instruction row
                            pipelineStageInt++;
                        } else
                        {
                            table[i, k] = "S"; // STALL
                        }
                    }

                    // if no stalls added between 'F' and 'D', only shift the 'D' position by 1 col
                    if (currDColIndex == nextDColIndex)
                    {
                        nextDColIndex++;
                    }
                    currDColIndex = nextDColIndex;
                }

                // update the avail col location dict if stall(s) added
                if (stallCols.Count > 0)
                {
                    List<string> availDictKeys = new List<string>(availDict.Keys);
                    foreach (string registerKey in availDictKeys)
                    {
                        (int, int, PipelinePosition?) availDictVal = availDict[registerKey];
                        int rowAvail = availDictVal.Item1;
                        int colAvail = availDictVal.Item2;
                        bool stallAddedInCurrentIteration = rowsStallsAdded.Contains(i);
                        // if available before stalls added, or if added to avail dict in this iteration before stalls added,
                        // update the avail col for that register
                        if (colAvail <= stallCols[stallCols.Count - 1] || (stallAddedInCurrentIteration && rowAvail >= i)) 
                        {
                            availDict[registerKey] = (i, availDictVal.Item2 + stallCols.Count, availDictVal.Item3);
                        }
                    }
                }

            }

            // print the final table
            printTable(insructionList, table);

            // prevent console window from closing
            Console.ReadLine();
        }

        /// <summary>
        /// Print the instruction pipeline table to console
        /// </summary>
        /// <param name="insructionList"></param>
        /// <param name="table"></param>
        private static void printTable(List<Instruction> insructionList, string[,] table)
        {
            string[] rows = new string[insructionList.Count];
            for (int i = 0; i < table.GetLength(0); i++)
            {
                rows[i] = insructionList[i].getInstructionStr().PadRight(20);
                for (int j = 0; j < table.GetLength(1); j++)
                {
                    if (table[i, j] == null)
                    {
                        rows[i] += ("  ");
                    } else
                    {
                        rows[i] += table[i, j] + " ";                    
                    }
                    
                }
                Console.WriteLine(rows[i]);
            }
        }

        /// <summary>
        /// Parse a given instuction line
        /// </summary>
        /// <param name="instruction">instruction string to parse</param>
        /// <returns>instruction object with register availability and needed stages</returns>
        private static Instruction ParseInstruction(string instructionStr)
        {
            // get the instruction type
            string[] splitInstruction = instructionStr.Split(' ');
            string instructionTypeStr = splitInstruction != null ? splitInstruction[0] : "";
            InstructionType instructionType = GetInstructionType(instructionTypeStr);

            // get the registers involved in the instruction
            List<Register> registers = new List<Register>();
            string[] splitRegisters = instructionStr.Split(',').Select(r => r.Trim()).ToArray();
            if (splitRegisters != null && splitRegisters.Length > 0)
            {
                splitRegisters[0] = splitRegisters[0].Split(' ')[1]; // remove the instruction type key
            }

            // handle add and subtract instruction types
            if (instructionType == InstructionType.ADD || instructionType == InstructionType.SUB)
            {
                List<string> evaluatedRegisters = new List<string>();
                for (int i = 0; i < splitRegisters.Length; i++) // should only be 3 registers max
                {
                    // do not re-evaluate registers in the same instruction
                    if (evaluatedRegisters.Contains(splitRegisters[i]))
                    {
                        continue;
                    }

                    // registers can only be prefixed by '$R' or '$t'
                    if (splitRegisters[i].StartsWith("$R") || splitRegisters[i].StartsWith("$t"))
                    {
                        Register r = new Register(splitRegisters[i]);
                        if (i == 0) // first register always where being stored
                        {
                            r.setAvailableNoFwd(PipelineStage.w, PipelinePosition.MIDDLE);
                            r.setAvailableWithFwd(PipelineStage.X, PipelinePosition.END);
                        } else if (i == 1 || i == 2) // 2nd and/or 3rd register is what is being used in ADD/SUB
                        {
                            r.setNeededNoFwd(PipelineStage.D, PipelinePosition.MIDDLE);
                            r.setNeededWithFwd(PipelineStage.X, PipelinePosition.BEGIN);
                        }
                        registers.Add(r);
                    } else
                    {
                        // not a register, no dependency
                    }
                    evaluatedRegisters.Add(splitRegisters[i]);
                }
            } else if (instructionType == InstructionType.LW)
            {
                if (splitRegisters[0].StartsWith("$R") || splitRegisters[0].StartsWith("$t"))
                {
                    Register r = new Register(splitRegisters[0]);
                    r.setAvailableNoFwd(PipelineStage.w, PipelinePosition.MIDDLE);
                    r.setAvailableWithFwd(PipelineStage.m, PipelinePosition.END);
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
                    r.setAvailableNoFwd(PipelineStage.D, PipelinePosition.MIDDLE);
                    r.setAvailableWithFwd(PipelineStage.w, PipelinePosition.BEGIN);
                    registers.Add(r);
                }
                else
                {
                    // SW should start with a register; invalid instruction
                }
            } else
            {
                Console.WriteLine("Unknown instruction type found in instruction: " + instructionStr);
                return null;
            }

            Instruction instructionObj = new Instruction(instructionStr, instructionType, registers);
            return instructionObj;
        }

        /// <summary>
        /// Get the instruction type obj
        /// </summary>
        /// <param name="typeStr"></param>
        /// <returns></returns>
        private static InstructionType GetInstructionType(string typeStr)
        {
            string typeLowercase = typeStr.ToLower();
            switch (typeLowercase)
            {
                case "add":
                    return InstructionType.ADD;
                case "sub":
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
