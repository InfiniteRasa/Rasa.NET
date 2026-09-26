namespace Rasa.Config
{
    public class GameDataConfig
    {
        public int[] EnabledRaces { get; set; }

        /// <summary>
        /// Server flags that are on from startup, by name or by number. The client's own
        /// spelling works: "MINION_COMMANDS" as well as "MinionCommands".
        /// </summary>
        public string[] ServerFlags { get; set; }

        /// <summary>
        /// The JSON file holding the knowledge-base articles SearchKB and RetrieveKBArticle
        /// answer from, relative to the server's working directory. Nothing in the shipped
        /// client asks for one; see KnowledgeBaseManager.
        /// </summary>
        public string KnowledgeBaseFile { get; set; }

        /// <summary>
        /// The folder holding one &lt;map name&gt;.nav per map, built by Rasa.NavMesh from the
        /// client data. Explicit paths remain relative to the working directory; default
        /// "navmesh" also resolves published assets and the source checkout.
        /// </summary>
        public string NavMeshPath { get; set; }
    }
}
