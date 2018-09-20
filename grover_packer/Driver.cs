using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;
using System;
using System.IO;
using System.Collections.Generic;

namespace grover_packer
{
    class Driver
    {
        /// @brief Parse an array of strings and set up variables describing this packer problem.
        void do_parse( List< string > all_lines ) {
            //TODO
        }

        /// @brief Slurp the contents of a packer description file into an array of strings.
        void read_packer_problem_file( string filename ) {
            string line = "";
            List< string > all_lines = new List<string>();
            //Scope for stream reader:
            using( StreamReader reader = new StreamReader( filename ) ) {
                while( (line = reader.ReadLine()) != null ) {
                    all_lines.Add(line);
                }
            }
            do_parse( all_lines );
        }

        static void Main(string[] args)
        {

        }
    }
}