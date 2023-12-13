/// <summary>
/// An incoming response received from the Match Maker Server once a queue filled.
/// Contains all the necessary information to make a P2P connection via WebRTC possible.
/// </summary>
public class MatchMakingUpdate
{
    public uint currentPeerCount { get; set; }
    public uint requiredPeerCount { get; set; }
}