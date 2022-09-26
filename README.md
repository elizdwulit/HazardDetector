# HazardDetector

This app provides a way to detect data hazards in a set of MIPS instructions. Currently the app only supports Add, Sub, Sw, Lw

It uses the pipeline sequence of (F)orward, D(ecode), e(X)ecute, (m)emory, and (w)riteback.

For each instruction, it determines what registers are used, what stage of the pipeline they are needed, and what stage of the pipeline they are available.

If a register is needed before it is available, there is a data hazard.

To fix the data hazard, it adds (S)talls until the "needed" point is equal to or past the stage in the pipeline the register is available.
