namespace Oxide.Plugins
{
    public partial class SteamChecks
    {
        /// <summary>
        /// Struct for the GetPlayerSummaries/v2 Web API request
        /// </summary>
        public class PlayerSummary
        {
            #region VisibilityType enum

            /// <summary>
            /// How visible the Steam Profile is
            /// </summary>
            public enum VisibilityType
            {
                Private = 1,
                Friend = 2,
                Public = 3
            }

            #endregion

            #region Properties and Indexers

            /// <summary>
            /// Is the account limited?
            /// </summary>
            /// <remarks>
            /// Will be fulfilled by an additional request directly to the steamprofile with ?xml=1
            /// </remarks>
            public bool LimitedAccount { get; set; }

            /// <summary>
            /// Has the user set up his profile?
            /// </summary>
            /// <remarks>
            /// Will be fulfilled by an additional request directly to the steamprofile with ?xml=1
            /// </remarks>
            public bool NoProfile { get; set; }

            /// <summary>
            /// URL to his steam profile
            /// </summary>
            public string Profileurl { get; set; }

            /// <summary>
            /// When his account was created - in Unix time
            /// </summary>
            /// <remarks>
            /// Will only be filled, if the users profile is public
            /// </remarks>
            public long Timecreated { get; set; }

            public VisibilityType Visibility { get; set; }

            #endregion

            #region Methods (Public)

            public override string ToString()
            {
                return
                    $"Steam profile visibility: {Visibility} - Profile URL: {Profileurl} " +
                    $"- Account created: {Timecreated} - Limited: {LimitedAccount} - NoProfile: {NoProfile}";
            }

            #endregion
        }
    }
}