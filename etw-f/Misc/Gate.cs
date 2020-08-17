namespace etw_f.Misc
{
    using System;
    using System.Threading;

    internal sealed class Gate
    {
        private volatile Int32 _gate = 0;
        private const Int32 OPEN = 0;
        private const Int32 LOCKED = 1;

        /// <summary>
        /// Tried to enter the gate, requiring that it is open.
        /// </summary>
        /// <returns>true if the gate was taken; otherwise false</returns>
        internal Boolean TryEnterGate()
        {
            return Interlocked.CompareExchange(ref this._gate, LOCKED, OPEN) == OPEN;
        }

        /// <summary>
        /// Checks if the gate is open.
        /// </summary>
        /// <returns>true if the gate is open; otherwise false</returns>
        internal Boolean TestGate()
        {
            return this._gate == OPEN;
        }

        /// <summary>
        /// Enters the gate without validating the gate is not already taken.
        /// </summary>
        internal void EnterGate()
        {
            this._gate = LOCKED;
        }

        /// <summary>
        /// Releases the gate with validating the gate is locked.
        /// </summary>
        internal void ReleaseGate()
        {
            this._gate = OPEN;
        }
    }
}