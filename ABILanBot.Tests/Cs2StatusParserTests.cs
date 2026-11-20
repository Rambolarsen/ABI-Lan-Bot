using ABILanBot.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ABILanBot.Tests;

[TestClass]
public sealed class Cs2StatusParserTests
{
    [TestMethod]
    public void ParsesCs2SpawngroupStatusFromSpillvert()
    {
        // Adapted from a real console status. IDs, network addresses and
        // unrelated server details were deliberately omitted.
        const string status = """
            ----- Status -----
            source   : console
            hostname : Bouvet-lan
            players  : 0 humans, 0 bots (0 max) (hibernating) (unreserved)
            ---------spawngroups----
            loaded spawngroup(  1)  : SV:  [1: de_dust2 | main lump | mapload]
            loaded spawngroup(  2)  : SV:  [2: maps/prefabs/de_dust2/de_dust2_skybox | main lump | mapload | point_prefab]
            ---------players--------
            #end
            """;

        var parsed = Cs2StatusParser.Parse(status);

        Assert.AreEqual("de_dust2", parsed.Map);
        Assert.AreEqual(0, parsed.HumanPlayers);
    }

    [TestMethod]
    public void StillParsesLegacyMapLine()
    {
        var parsed = Cs2StatusParser.Parse("map : de_mirage\nplayers : 2 humans, 0 bots\n");

        Assert.AreEqual("de_mirage", parsed.Map);
        Assert.AreEqual(2, parsed.HumanPlayers);
    }

    [TestMethod]
    public void RootSpawngroupWinsOverPrefabAndPlayerMapMentions()
    {
        const string status = """
            loaded spawngroup(  2)  : SV:  [2: maps/prefabs/de_mirage/skybox | main lump]
            loaded spawngroup(  1)  : SV:  [1: ar_pool_day | main lump | mapload]
            loaded spawngroup(  3)  : SV:  [3: prefabs/team_select | main lump]
            """;

        var parsed = Cs2StatusParser.Parse(status);

        Assert.AreEqual("ar_pool_day", parsed.Map);
        Assert.IsNull(parsed.HumanPlayers);
    }

    [TestMethod]
    public void PrefabWithoutRootMapIsNotReportedAsCurrentMap()
    {
        const string status = """
            loaded spawngroup(  2)  : SV:  [2: maps/prefabs/de_dust2/skybox | main lump]
            players  : 0 humans, 0 bots
            """;

        var parsed = Cs2StatusParser.Parse(status);

        Assert.IsNull(parsed.Map);
        Assert.AreEqual(0, parsed.HumanPlayers);
    }

    [TestMethod]
    public void MissingFieldsRemainUnknown()
    {
        var parsed = Cs2StatusParser.Parse("Server: Running\n");

        Assert.IsNull(parsed.Map);
        Assert.IsNull(parsed.HumanPlayers);
    }

    [TestMethod]
    public void RejectsNullResponse()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => Cs2StatusParser.Parse(null!));
    }
}
