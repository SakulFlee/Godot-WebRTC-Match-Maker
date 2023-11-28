/// <summary>
/// An incoming response received from the Match Maker Server once a queue filled.
/// Contains all the necessary information to make a P2P connection via WebRTC possible.
/// </summary>
public class MatchMakingResponse
{
    public string ownUUID { get; set; }
    public string hostUUID { get; set; }
    public string[] peers { get; set; }
}