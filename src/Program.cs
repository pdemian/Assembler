using System;
using System.IO;
using Newtonsoft.Json;

namespace Assembler
{
    public class Program
    {
        private class ProgramArgs
        {
            public bool Verbose;
            public string FileIn;
            public string FileOut;

            public ProgramArgs()
            {
                Verbose = false;
                FileIn = "";
                FileOut = null;
            }
        }


        public static void Main(string[] args)
        {
            ProgramArgs arguments = new ProgramArgs();
            Assembler assembler = null;
            try
            {
                arguments = ParseArguments(args);

                if (string.IsNullOrWhiteSpace(arguments.FileIn))
                {
                    throw new Exception("Fatal Error: No iunput files.");
                }
                if (!File.Exists(arguments.FileIn))
                {
                    throw new Exception("Fatal Error: Input file not found.");
                }

                if (arguments.FileOut == null)
                {
                    arguments.FileOut = Path.GetFileNameWithoutExtension(arguments.FileIn) + ".ci";
                }

                //Parse assembly
                assembler = new Assembler(arguments.FileIn);

                assembler.Assemble();

                //write to CodeInfo file
                File.WriteAllText(arguments.FileOut, JsonConvert.SerializeObject(assembler.ci, Formatting.Indented));

                //then print warnings
                if (assembler.warnings != null)
                {
                    foreach (var warning in assembler.warnings)
                    {
                        Console.WriteLine(warning);
                    }
                }
            }
            catch (Exception ex)
            {
                if (assembler != null && assembler.warnings != null)
                {
                    foreach (var warning in assembler.warnings)
                    {
                        Console.WriteLine(warning);
                    }
                }

                Console.WriteLine(ex.Message);
                if (arguments.Verbose)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        private static ProgramArgs ParseArguments(string[] args)
        {
            ProgramArgs arguments = new ProgramArgs();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-verbose":
                    case "-v":
                        arguments.Verbose = true;
                        break;
                    case "-output":
                    case "-o":
                        if (i + 1 >= args.Length)
                        {
                            arguments.FileOut = args[++i];
                        }
                        break;
                    default:
                        arguments.FileIn = args[i];
                        break;
                }
            }

            return arguments;
        }
    }
}