namespace grover_packer
{
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Primitive;

    /// @brief Measure a bunch of qubits, and invert the states of those that don't match a desired state so that they do.
    /// @details Modified from code that was shamelessly copied from the Q# documentation.
    operation MultiSet ( desired: Result[], qbits: Qubit[] ) : ()
    {
        body
        {
            AssertIntEqual(Length(desired), Length(qbits), "Error in MultiSet operation: The two registers provided must be equal.");
            mutable current = new Result[ Length(qbits) ];
            for( i in 0 .. Length(qbits) - 1 ) {
                set current[i] = M(qbits[i]);
            }
            for( i in 0 .. Length(desired) - 1 ) {
                if( desired[i] != current[i] ) {
                    X(qbits[i]);
                }
            }
        }
    }

    /// @brief Measure a qubit, and invert its state if it doesn't match a desired state so that it does.
    /// @details Shamelessly copied from the Q# documentation.
    //operation Set (desired: Result, q1: Qubit) : ()
    //{
    //    body
    //    {
    //        let current = M(q1);
    //        if (desired != current)
    //        {
    //            X(q1);
    //        }
    //    }
    //}

    /// @brief Given an array of classical bits representing a key and an array of qbits representing a message, flip qbits whenever a classical bit
    /// is 0.
    operation EncodeDecode( bits: Result[], qubits: Qubit[] ) : () {
        body {
            AssertIntEqual( Length(bits), Length(qubits), "Error in EncodeDecode operation: The two registers provided must be equal." );
            for( i in 0 .. Length(bits) - 1) {
                if( bits[i] == Zero ) {
                    X( qubits[i] );
                }
            }
        }
    }

    /// @brief Given an array of classical bits representing a particular state and an array of classical bits representing the energy
    /// of that state, do a controlled addition of that energy (controlled by a qubit array representing the actual superposition of
    /// states of the system) to an array of qubits representing the superposition of energies of states of the system.
    operation SingleStateAdder ( state_bitstring: Result[], state_energy: Int, state_qstring: Qubit[], total_energy: Qubit[] ) : ()
    {
        body
        {
            EncodeDecode( state_bitstring, state_qstring );
            let total_energy_le = LittleEndian(total_energy);
            Microsoft.Quantum.Canon.IntegerIncrementLE(state_energy, total_energy_le );
            //(Controlled Microsoft.Quantum.Canon.IntegerIncrementLE)( state_qstring, state_energy, total_energy_le );
            EncodeDecode( state_bitstring, state_qstring );
        }
    }

    operation TestAdder() : ( Result[], Int ) {
        body {
            let state_to_add = [One; Zero];
            let energy_to_add = 3;
            mutable result_state = new Result[ 2 ];
            mutable result_energy = new Result [ 4 ];
            using( curstate = Qubit[Length(result_state)] ) {
                ApplyToEach( H, curstate ); //Hadamard transform on each bit, to construct the state 1/2^(N/2)*(|1> + |0>)^N

                using( curenergy = Qubit[ Length(result_energy) ] ) {
                    
                    let endstate1 = [ Zero; Zero ];
                    let endstate2 = [ Zero; Zero; Zero; Zero ];
                    
                    SingleStateAdder( state_to_add, energy_to_add, curstate, curenergy);

                    for( i in 0 .. Length(curstate) - 1 ) {
                        set result_state[i] = M(curstate[i]);
                    }
                    for( i in 0 .. Length(curenergy) - 1 ) {
                        set result_energy[i] = M(curenergy[i]);
                    }

                    //Clean up: reset all qubits to |0> :
                    MultiSet(endstate1, curstate);
                    MultiSet(endstate2, curenergy);
                }
            }
            return (result_state, ResultAsInt(result_energy));
                
        }
    }

 }
