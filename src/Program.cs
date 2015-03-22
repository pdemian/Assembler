using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Assembler
{
    class Program
    {
        class ProgramArgs
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


        static void Main(string[] args)
        {
            ProgramArgs arguments = new ProgramArgs();
            Assembler assembler = null;
            try
            {
                arguments = ParseArguments(args);
            
                if(string.IsNullOrWhiteSpace(arguments.FileIn))
                {
                    throw new Exception("Fatal Error: No iunput files.");
                }
                if(!File.Exists(arguments.FileIn))
                {
                    throw new Exception("Fatal Error: Input file not found.");
                }
            
                if(arguments.FileOut == null)
                {
                    arguments.FileOut = Path.GetFileNameWithoutExtension(arguments.FileIn) + ".ci";
                }
            
                //Parse assembly
                assembler = new Assembler(arguments.FileIn);

                assembler.Assemble();


                //debug
                //foreach(var v in assembler.ci.Code)
                //{
                //    Console.Write("{0:X2} ", v);
                //}

                
                //write to CodeInfo file
                File.WriteAllText(arguments.FileOut, JsonConvert.SerializeObject(assembler.ci, Formatting.Indented ));

                //then print warnings
                if (assembler != null && assembler.warnings != null)
                    foreach (var warning in assembler.warnings)
                        Console.WriteLine(warning);
            
            }
            catch(Exception ex)
            {
                if (assembler != null && assembler.warnings != null)
                    foreach (var warning in assembler.warnings)
                        Console.WriteLine(warning);
            
                Console.WriteLine(ex.Message);
                if(arguments.Verbose)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        private static ProgramArgs ParseArguments(string[] args)
        {
            ProgramArgs arguments = new ProgramArgs();

            for(int i = 0; i < args.Length; i++)
            {
                switch(args[i].ToLower())
                {
                    case "-verbose":
                    case "-v":
                        arguments.Verbose = true;
                        break;
                    case "-output":
                    case "-o":
                        if (i + 1 >= args.Length)
                            arguments.FileOut = args[++i];
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
