using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;
using System;
using System.IO;
using System.Collections.Generic;

namespace grover_packer
{
    /// @brief A class for information about an individual rotamer.
    public class Rotamer
    {
        /// @brief The sequence position of this rotamer.
        public int SeqPos { get; set; }

        /// @brief The index of the rotamer at this sequence position.
        public int RotIndex { get; set; }

        /// @brief The onebody energy of this rotamer.
        /// @details Includes interactions with background.
        public double OneBodyEnergy { get; set; }
    }

    class Driver
    {
        /// @brief The list of rotamers, indexed as (seqpos, rotindex).
        private List< List< Rotamer > > rotamer_list_;

        private void parse_rotamers_and_onebody_energies( List<string> all_lines ) {
            bool in_field = false;
            bool breaktime = false;
            for( int i=1; i<=all_lines.Count; ++i ) {
                if( in_field ) {
                    //TODO
                } else {
                    if( all_lines[i] == "[BEGIN ONEBODY SEQPOS/ROTINDEX/ENERGY]\n" ) {
                        in_field = true;
                    }
                    continue;
                }
                if( breaktime ) break;
            }
        }

        /// @brief Parse an array of strings and set up variables describing this packer problem.
        void do_parse( List< string > all_lines ) {
            rotamer_list_ = new List< List < Rotamer > >();
            parse_rotamers_and_onebody_energies( all_lines );
            //parse_twobody_energies( all_lines );
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