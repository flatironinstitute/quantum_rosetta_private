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
        /// @brief The list of all packable sequence positions.
        private List< int > packable_sequence_positions_;

        /// @brief The rotamer indices at all packable sequence positions.
        private SortedDictionary< int, List<int> > rotamer_indices_at_packable_positions_;

        /// @brief The list of rotamers, indexed as (seqpos, rotindex).
        private SortedDictionary< Tuple< int, int >, Rotamer> rotamer_list_;

        /// @brief The list of twobody energies, indexed as (seqpos1, rotindex1),(seqpos2, rotindex2);
        private SortedDictionary< Tuple< Tuple<int, int>, Tuple< int, int > >, double > twobody_energies_;

        private void parse_rotamers_and_onebody_energies( List<string> all_lines, string filename ) {
            bool in_field = false;
            char[] charSeparators = new char[] {' ', '\t', '\n'};

            for( int i=0; i<all_lines.Count; ++i ) {
                if( in_field ) {
                    if( all_lines[i].TrimEnd(charSeparators) == "[END ONEBODY SEQPOS/ROTINDEX/ENERGY]" ) {
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

                    if(!packable_sequence_positions_.Contains(seqpos)) {
                        packable_sequence_positions_.Add(seqpos);
                        if( rotamer_indices_at_packable_positions_.ContainsKey( seqpos ) ) {
                            throw new InvalidProgramException( "Program error!  The rotamer_indices_at_packable_positions_ map contains a key that is NOT found in the packable_seqence_positions_ list!" );
                        }
                        List<int> newlist = new List<int>();
                        newlist.Add(rotno);
                        rotamer_indices_at_packable_positions_.Add( seqpos, newlist );
                    } else {
                        if( !rotamer_indices_at_packable_positions_.ContainsKey( seqpos ) ) {
                            throw new InvalidProgramException( "Program error!  The rotamer_indices_at_packable_positions_ map lacks a key that IS found in the packable_seqence_positions_ list!" );
                        }
                        if( rotamer_indices_at_packable_positions_.GetValueOrDefault( seqpos ).Contains(rotno) ) {
                            throw new InvalidProgramException( "Program error!  The rotamer_indices_at_packable_positions_ map's rotamer list for position " + seqpos.ToString() + " contains a duplicate entry for rotamer " + rotno.ToString() + "!" );
                        }
                        rotamer_indices_at_packable_positions_.GetValueOrDefault( seqpos ).Add(rotno);
                    }
                } else {
                    if( all_lines[i].TrimEnd(charSeparators) == "[BEGIN ONEBODY SEQPOS/ROTINDEX/ENERGY]" ) {
                        in_field = true;
                    }
                    continue;
                }
            }

            if( !in_field ) {
                throw new FormatException( "In file \"" + filename + "\", no \"[BEGIN ONEBODY...\" block was found." );
            }

            Console.WriteLine("Successfully read " + rotamer_list_.Count.ToString() + " rotamers and one-body energies from \"" + filename + "\".");
            Console.WriteLine("Rotamers read:");
            foreach( KeyValuePair< Tuple<int, int>, Rotamer> kvp in rotamer_list_ ) {
                Rotamer rotamer = kvp.Value;
                Console.WriteLine( "\tSeqpos " + rotamer.SeqPos.ToString() + "\tRotamer " + rotamer.RotIndex.ToString() + "\tEnergy " + rotamer.OneBodyEnergy.ToString() );
            }
            Console.Write( "The packable sequence positions are: " );
            int count = 0;
            foreach( int seqpos in packable_sequence_positions_ ) {
                ++count;
                Console.Write( seqpos.ToString() );
                if( count < packable_sequence_positions_.Count ) {
                    Console.Write( ", ");
                }
            }
            Console.Write("\n");
            foreach( KeyValuePair<int, List<int> > kvp in rotamer_indices_at_packable_positions_ ) {
                Console.Write( "\tThe rotamers at packable position " + kvp.Key.ToString() + " are: " );
                count=0;
                foreach( int rotno in kvp.Value ) {
                    ++count;
                    Console.Write( rotno.ToString() );
                    if( count < kvp.Value.Count ) {
                        Console.Write(", ");
                    }
                }
                Console.Write("\n");
            }
        }

        /// @brief Given strings corresponding to lines of an input file, parse out the twobody energies and populate the
        /// appropriate objects.
        void parse_twobody_energies( List< string > all_lines, string filename ) {
            bool in_block = false;
            char[] nullchars = new char[] {' ', '\t', '\n'};

            for( int i = 0, imax=all_lines.Count; i<imax; ++i ) {
                if( in_block ) {
                    if( all_lines[i].TrimEnd(nullchars) == "[END TWOBODY SEQPOS1/ROTINDEX1/SEQPOS2/ROTINDEX2/ENERGY]" ) {
                        break;
                    }

                    string[] split_string = all_lines[i].Split( nullchars, StringSplitOptions.RemoveEmptyEntries );
                    if( split_string.Length != 5 ) {
                        throw new FormatException("Could not parse line \"" + all_lines[i] + "\" in file \"" + filename + "\".");
                    }
                    int seqpos1, rotno1, seqpos2, rotno2;
                    double energy2body;
                    if( !Int32.TryParse( split_string[0], out seqpos1 )) {
                        throw new FormatException( "Could not parse first entry in line \"" + all_lines[i] + "\" in file \"" + filename + "\" as an integer." );
                    }
                    if( !Int32.TryParse( split_string[1], out rotno1 ) ) {
                        throw new FormatException( "Could not parse second entry in line \"" + all_lines[i] + "\" in file \"" + filename + "\" as an integer." );
                    }
                    if( !Int32.TryParse( split_string[2], out seqpos2 )) {
                        throw new FormatException( "Could not parse third entry in line \"" + all_lines[i] + "\" in file \"" + filename + "\" as an integer." );
                    }
                    if( !Int32.TryParse( split_string[3], out rotno2 ) ) {
                        throw new FormatException( "Could not parse fourth entry in line \"" + all_lines[i] + "\" in file \"" + filename + "\" as an integer." );
                    }
                    if( !Double.TryParse( split_string[4], out energy2body ) ) {
                        throw new FormatException( "Could not parse fifth entry in line \"" + all_lines[i] + "\" in file \"" + filename + "\" as a floating-point number." );
                    }

                    Tuple< int, int > coord1 = new Tuple<int, int>( seqpos1, rotno1 );
                    Tuple< int, int > coord2 = new Tuple<int, int>( seqpos2, rotno2 );
                    Tuple< Tuple<int, int>, Tuple<int, int> > coords = new Tuple< Tuple<int, int>, Tuple<int, int> >(coord1, coord2);

                    if( twobody_energies_.ContainsKey( coords ) ) {
                        throw new FormatException( "File \"" + filename + "\" contained multiple entries for the pairwise energy of seqpos " + seqpos1.ToString() + ", rotamer " + rotno1.ToString() + " and seqpos " + seqpos2.ToString() + ", rotamer " + rotno2.ToString() + "." );
                    }
                    twobody_energies_.Add( coords, energy2body );
                } else {
                    if( all_lines[i].TrimEnd(nullchars) == "[BEGIN TWOBODY SEQPOS1/ROTINDEX1/SEQPOS2/ROTINDEX2/ENERGY]" ) {
                        in_block = true;
                        continue;
                    }
                }
            }

            if( !in_block ) {
                throw new FormatException( "In file \"" + filename + "\", no \"[BEGIN TWOBODY...\" block was found." );
            }

            Console.WriteLine("Read " + twobody_energies_.Count.ToString() + " pairwise rotamer energies.");
            Console.WriteLine("Pairwise rotamer energies:");
            foreach( KeyValuePair< Tuple< Tuple<int,int>, Tuple<int,int> >, double > kvp in twobody_energies_ ) {
                int seqpos1 = kvp.Key.Item1.Item1;
                int seqpos2 = kvp.Key.Item2.Item1;
                int rotno1 = kvp.Key.Item1.Item2;
                int rotno2 = kvp.Key.Item2.Item2;
                double energy = kvp.Value;
                Console.WriteLine("\tSeqpos1 " + seqpos1.ToString() + "\tRotno1 " + rotno1.ToString() + "\tSeqpos2 " + seqpos2.ToString() + "\tRotno2 " + rotno2.ToString() + "\tEnergy " + energy.ToString() );
            }
        }

        /// @brief Parse an array of strings and set up variables describing this packer problem.
        void do_parse( List< string > all_lines, string filename ) {
            rotamer_list_ = new SortedDictionary< Tuple< int, int >, Rotamer>();
            rotamer_indices_at_packable_positions_ = new SortedDictionary< int, List<int> >();
            packable_sequence_positions_ = new List<int>();
            twobody_energies_ = new SortedDictionary< Tuple< Tuple<int, int>, Tuple<int, int> >, double > ();
            parse_rotamers_and_onebody_energies( all_lines, filename );
            parse_twobody_energies( all_lines, filename );
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
            int numsolutions = 1;
            foreach( KeyValuePair<int, List<int> > kvp in rotamer_indices_at_packable_positions_ ) {
                numsolutions *= kvp.Value.Count;
            }
            Console.WriteLine("Completed read from \"" + filename + "\".  This packer problem has " + numsolutions.ToString() + " possible solutions.");
        }
    } //class PackerProblem

    class Driver
    {
        static void Main(string[] args)
        {
            PackerProblem packer_problem = new PackerProblem();
            packer_problem.read_packer_problem_file("test.input");

            using( var sim = new QuantumSimulator( true ) ) {
                TestAdder.Run();
            }

            Console.WriteLine("--- Press any key to complete program execution. ---");
            Console.ReadKey();
        }
    }
}