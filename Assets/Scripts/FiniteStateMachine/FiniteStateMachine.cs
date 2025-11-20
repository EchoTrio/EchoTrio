// By Terri Lim, CMU ETC Class of 2026. Last updated by me in November 2025. Feel free to judge any code up till then.

namespace FSM {
    /// Finite state machine class to handle state transitions and updates.
    public class FiniteStateMachine {
        public const int INVALID_STATE = -1; // Use negative value to denote an invalid state.

        public readonly int NumStates = 0;
        private int currentState = INVALID_STATE;
        private int nextState = INVALID_STATE;

        /// Define a delegate that takes in 0 arguments and returns void.
        public delegate void FuncPtr();
        /// The entry callback function of states.
        private FuncPtr[] stateEntries = null;
        /// The update callback function of states.
        private FuncPtr[] stateUpdates = null;
        /// The late update callback function of states.
        private FuncPtr[] stateLateUpdates = null;
        /// The exit callback function of states.
        private FuncPtr[] stateExits = null;

        public FiniteStateMachine(int numStates) {
            // Initialise arrays to hold the function pointers to the entry, update, late update and exit callback functions of the states.
            NumStates = numStates;
            stateEntries = new FuncPtr[numStates];
            stateUpdates = new FuncPtr[numStates];
            stateLateUpdates = new FuncPtr[numStates];
            stateExits = new FuncPtr[numStates];
        }

        public int GetCurrentState() { return currentState; }
        public int GetNextState() { return nextState; }

        public void SetStateEntry(int state, FuncPtr funcPtr = null) { stateEntries[state] = funcPtr; }
        public void SetStateUpdate(int state, FuncPtr funcPtr = null) { stateUpdates[state] = funcPtr; }
        public void SetStateLateUpdate(int state, FuncPtr funcPtr = null) { stateLateUpdates[state] = funcPtr; }
        public void SetStateExit(int state, FuncPtr funcPtr = null) { stateExits[state] = funcPtr; }

        /// Sets the next state to transit to.
        /// <param name="nextState">The index of the next state.</param>
        public void ChangeState(int nextState) { this.nextState = nextState; }

        public void Update() {
            // Check if the next state is different from the current state.
            if (nextState != currentState) {
                // Exit current state.
                if (0 <= currentState) { stateExits[currentState]?.Invoke(); }

                // Enter next state.
                currentState = nextState;
                if (0 <= currentState) { stateEntries[currentState]?.Invoke(); }
            }

            // Update current state every frame.
            if (0 <= currentState) { stateUpdates[currentState]?.Invoke(); }
        }

        public void LateUpdate() {
            // Late update current state every frame.
            if (0 <= currentState) { stateLateUpdates[currentState]?.Invoke(); }
        }
    }
}