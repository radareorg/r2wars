    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Encapsulates a list of the names of tournament teams.
    /// </summary>
    public sealed class TournamentNameTable
    {
        /// <summary>
        /// Holds the mappings of team ids to team names.
        /// </summary>
        private readonly Dictionary<long, string> names;

        /// <summary>
        /// Initializes a new instance of the TournamentNameTable class, initialized with the supplied names.
        /// </summary>
        /// <param name="names">A pre-populated mapping of team ids to team names.</param>
        public TournamentNameTable(IDictionary<long, string> names)
        {
            this.names = new Dictionary<long, string>();

            foreach (var key in names.Keys)
            {
                this.names.Add(key, names[key]);
            }
        }

        /// <summary>
        /// Retrieves a team name associated with the supplied team id.
        /// </summary>
        /// <param name="teamId">The id of the team for which to retrieve the name.</param>
        /// <returns>The team name associated with the supplied team id.</returns>
        public string this[long teamId]
        {
            get
            {
                return this.names[teamId];
            }
        }
    }
