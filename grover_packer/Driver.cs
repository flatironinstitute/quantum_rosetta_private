using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

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

    /// @brief A packing problem.  This class encapsulates data needed for the problem and 
    /// all functions needed to read and parse input and to provide output.
    class PackerProblem
    {
   /// @brief The list of rotamers, indexed as (seqpos, rotindex).
        private SortedDictionary< Tuple< int, int >, Rotamer> rotamer_list_;

        private void parse_rotamers_and_onebody_energies( List<string> all_lines, string filename ) {
            bool in_field = false;
            bool breaktime = false;
            char[] charSeparators = new char[] {' ', '\t', '\n'};

            for( int i=0; i<all_lines.Count; ++i ) {
                if( in_field ) {
                    if( all_lines[i].TrimEnd(charSeparators) == "[END ONEBODY SEQPOS/ROTINDEX/ENERGY]" ) {
                        breaktime = true;
                        break;
                    }
                    string[] split_string = all_lines[i].Split( charSeparators, StringSplitOptions.RemoveEmptyEntries );
                    if( split_string.Length != 3 ) {
                        throw new FormatException("Could not parse line \"" + all_lines[i] + "\" in file \"" + filename + "\".");
                    }
                    int seqpos, rotno;
                    double energy1body;
                    if( !Int32.TryParse( split_string[0], out seqpos )) {
                        throw new FormatException( "Could not parse first entry in line \"" + all_lines[i] + "\" in file \"" + filename + "\" as an integer." );
                    }
                    if( !Int32.TryParse( split_string[1], out rotno ) ) {
                        throw new FormatException( "Could not parse second entry in line \"" + all_lines[i] + "\" in file \"" + filename + "\" as an integer." );
                    }
                    if( !Double.TryParse( split_string[2], out energy1body ) ) {
                        throw new FormatException( "Could not parse third entry in line \"" + all_lines[i] + "\" in file \"" + filename + "\" as a floating-point number." );
                    }
                    
                    Rotamer newrot = new Rotamer();
                    newrot.SeqPos = seqpos;
                    newrot.RotIndex = rotno;
                    newrot.OneBodyEnergy = energy1body;

                    Tuple<int, int> coord = new Tuple<int, int>(seqpos, rotno);
                    if( rotamer_list_.ContainsKey( coord ) ) {
                        throw new FormatException( "In file \"" + filename + "\", seqpos " + seqpos.ToString() + ", rotamer " + rotno.ToString() + " was specfified more than once." );
                    }
                    rotamer_list_.Add( coord, newrot );
                } else {
                    if( all_lines[i].TrimEnd(charSeparators) == "[BEGIN ONEBODY SEQPOS/ROTINDEX/ENERGY]" ) {
                        in_field = true;
                    }
                    continue;
                }
                if( breaktime ) break;
            }
            Console.WriteLine("Successfully read " + rotamer_list_.Count.ToString() + " rotamers and one-body energies from \"" + filename + "\".");
            Console.WriteLine("Rotamers read:");
            foreach( KeyValuePair< Tuple<int, int>, Rotamer> kvp in rotamer_list_ ) {
                Rotamer rotamer = kvp.Value;
                Console.WriteLine( "\tSeqpos " + rotamer.SeqPos.ToString() + "\tRotamer " + rotamer.RotIndex.ToString() + "\tEnergy " + rotamer.OneBodyEnergy.ToString() );
            }
        }

        /// @brief Parse an array of strings and set up variables describing this packer problem.
        void do_parse( List< string > all_lines, string filename ) {
            rotamer_list_ = new SortedDictionary< Tuple< int, int >, Rotamer>();
            parse_rotamers_and_onebody_energies( all_lines, filename );
            //parse_twobody_energies( all_lines );
        }

        /// @brief Slurp the contents of a packer description file into an array of strings.
        public void read_packer_problem_file( string filename ) {
            Console.WriteLine("Reading packer problem data from \"" + filename + "\".");
            string line = "";
            List< string > all_lines = new List<string>();
            //Scope for stream reader:
            using( StreamReader reader = new StreamReader( filename ) ) {
                while( (line = reader.ReadLine()) != null ) {
                    all_lines.Add(line);
                }
            }
            do_parse( all_lines, filename );
            Console.WriteLine("Completed read from \"" + filename + "\".");
        }
    } //class PackerProblem

    class Driver
    {
     

        static void Main(string[] args)
        {
            PackerProblem packer_problem = new PackerProblem();
            packer_problem.read_packer_problem_file("test.input");
        }
    }
}