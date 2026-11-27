using System.Text.Json;
using SpaceBattle.Lib.Server;

namespace SpaceBattle.Lib.Tests.Server;

public sealed class JsonServerSnapshotSerializerTests
{
    [Fact]
    public void RoundTripPreservesSnapshotMetadata()
    {
        var state = JsonDocument.Parse("{\"x\":2}").RootElement;
        var snapshot = new ServerSnapshotDto(1, 7, new[] { new GameSnapshotDto("game-1", 3, state) }, new Dictionary<string, string> { ["game-1"] = "worker-1" });
        var serializer = new JsonServerSnapshotSerializer();

        var restored = serializer.Deserialize(serializer.Serialize(snapshot));

        Assert.Equal(1, restored.FormatVersion);
        Assert.Equal(7, restored.Generation);
        Assert.Equal("worker-1", restored.Routes["game-1"]);
        Assert.Equal(3, restored.Games[0].Generation);
    }

    [Fact]
    public void DeserializeRejectsEmptyJson()
    {
        Assert.Throws<ArgumentException>(() => new JsonServerSnapshotSerializer().Deserialize(""));
    }
}
