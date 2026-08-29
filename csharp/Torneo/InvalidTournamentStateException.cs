    using System;

    /// <summary>
    /// Thrown when an IPairingsGenerator is asked to load a state that does not represent a valid tournament within its constraints.
    /// </summary>
    public class InvalidTournamentStateException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the InvalidTournamentStateException class.
        /// </summary>
        public InvalidTournamentStateException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidTournamentStateException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public InvalidTournamentStateException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidTournamentStateException class with a specified error
        /// message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public InvalidTournamentStateException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
