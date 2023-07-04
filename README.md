# HazardDetector

## Problem Overview
Pipelining is the act of executing mutiple computer instructions concurrently. Although each instruction is slower in this architecture, the thoroughput is higher than instructions were executed one at a time, where the next instruction could only begin after the previous one has completed. Pipelining however, can cause data hazards if an instruction is attempted to be executed and it depends on the result of a previous one. This can happen if the pipeline is not properly aligned.

## What this application does
This HazardDetector app provides a way to detect these data hazards in a set of MIPS instructions executed in a pipeline, and indicates where a Stall should be added to fix the hazard. 

Currently the app only supports Add, Sub, Sw (store word), Lw (Load word). It uses the pipeline sequence of (F)orward, D(ecode), e(X)ecute, (m)emory, and (w)riteback. Although there are several reasons why a data hazard can occur, the most common issue is when a command attempts to use the result of a previous instruction and it is not yet available.

For each instruction, it determines what registers are used, what stage of the pipeline they are needed, and what stage of the pipeline they are available. If a register is needed before it is available, there is a data hazard. The location where a register is needed is influenced by the presence of a forwarding unit. If a forwarding unit is available, the result of a previous instruction can be available earlier than it would be in a standard workflow. To fix the data hazard, it adds (S)talls until the "needed" point is equal to or past the stage in the pipeline the register is available.

## How to use the application
The HazardDetector application reads a set of instructions from a provided text file and displays the pipeline table in the console.
The resulting pipeline table may include an "S" to indicate that a stall is needed to fix a data hazard.
The need for a stall can be influenced by the presence of a forwarding unit. The presence of a forwarding unit is defined by the first argument in the program execution command.

The command to run the program is:
HazardDetector <0 = use forwarding unit | 1 = no forwarding unit> <full file path to instructions>
