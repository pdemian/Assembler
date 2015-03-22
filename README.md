# Assembler
A Work in progress assembler that assembled x86 code. To be used in conjunction with a work in progress compiler and linker.

## Purpose
This project is part of my work in progress compiler.
The compiler itself parses a pseudo-C# like language.
I've seperated the project into a high level parser, assembler, and linker. 
The other parts will be added onto github later on as they are still a major work in progress.

Technically this project serves no purpose just yet, other than maybe showcasing how to build a .exe from scratch.


## Installation
clone, and run the solution to build the executable

## Usage
Use with the linker project for the following
	//Assemble
	assembler.exe tests/testN.asm -o testN.ci
	//And link
	linker.exe testN.ci -o testN.exe
	
This will (read: should) produce an executable file.

See the tests directory for a number of tests.