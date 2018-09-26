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
            AssertIntEqual(Length(desired), Length(qbits), "The two registers provided to MultiSet must be equal.");
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

    /// @brief Given an array of classical bits representing a particular state and an array of classical bits representing the energy
    /// of that state, do a controlled addition of that energy (controlled by a qubit array representing the actual superposition of
    /// states of the system) to an array of qubits representing the superposition of energies of states of the system.
    operation SingleStateAdder ( state_bitstring: Result[], state_energy: Result[], state_qstring: Qubit[], total_energy: Qubit[] ) : ()
    {
        body
        {
            //Microsoft.Quantum.Canon.Add( state_energy, total_energy );
        }
    }

    operation TestAdder() : ( Result[], Result[] ) {
        body {
            let state_to_add = [One; Zero];
            let energy_to_add = [Zero; One; One; Zero];
            mutable result_state = new Result[ Length(state_to_add) ];
            mutable result_energy = new Result [ Length(energy_to_add) ];
            using( curstate = Qubit[Length(state_to_add)] ) {
                ApplyToEach( H, curstate ); //Hadamard transform on each bit, to construct the state 1/2^(N/2)*(|1> + |0>)^N

                using( curenergy = Qubit[Length(energy_to_add)] ) {
                    
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
            return (result_state, result_energy);
                
        }
    }

 }
