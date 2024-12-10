using System.Text.Json.Serialization;

/// <summary>
/// An outgoing packet send to the Match Maker Server to request a queue to be created or joined.
/// </summary>
[JsonSerializable(typeof(MatchMakerRequest))]
public class MatchMakerRequest
{
    public string name { get; set; }
}
