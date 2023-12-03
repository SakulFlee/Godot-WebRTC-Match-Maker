using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using SIPSorcery.Net;

/// <summary>
/// The general packet/package send and received to and from the Match Maker Server.
/// </summary>
public class Packet : PacketSerializer<Packet>
{
    /// <summary>
    /// Type of the nested JSON
    /// </summary>
    public PacketType type { get; set; }
    /// <summary>
    /// Where this packet came from
    /// </summary>
    public string from { get; set; }
    /// <summary>
    /// Where this packet is going to
    /// </summary>
    public string to { get; set; }
    /// <summary>
    /// The nested JSON data
    /// </summary>
    public string json { get; set; }

    /// <summary>
    /// Special function to parse the nested value as an ICE Candidate
    /// 
    /// ⚠️ Make sure the 'type' is correct!
    /// </summary>
    /// <returns>The parsed ICE Candidate</returns>
    public RTCIceCandidateInit ParseICECandidate()
    {
        if (type != PacketType.ICECandidate)
        {
            GD.PrintErr($"Attempting to parse ICE Candidate from wrong type ({type})");
            return null;
        }

        RTCIceCandidateInit result;
        if (!RTCIceCandidateInit.TryParse(json, out result))
        {
            GD.PrintErr("[MatchMaker] Failed parsing ICE Candidate JSON.");
        }

        return result;
    }

    /// <summary>
    /// Special function to parse the nested value as a Session Description
    /// 
    /// ⚠️ Make sure the 'type' is correct!
    /// </summary>
    /// <returns>The parsed Session Description</returns>
    public RTCSessionDescriptionInit ParseSessionDescription()
    {
        if (type != PacketType.SessionDescription)
        {
            GD.PrintErr($"Attempting to parse Session Description from wrong type ({type})");
            return null;
        }

        RTCSessionDescriptionInit result;
        if (!RTCSessionDescriptionInit.TryParse(json, out result))
        {
            GD.PrintErr("[MatchMaker] Failed parsing Session Description JSON.");
        }

        return result;
    }

    /// <summary>
    /// Special function to parse the nested value as a Match Making Request
    /// 
    /// ⚠️ Make sure the 'type' is correct!
    /// </summary>
    /// <returns>The parsed Match Making Request</returns>
    public MatchMakingRequest ParseMatchMakingRequest()
    {
        if (type != PacketType.MatchMakerRequest)
        {
            GD.PrintErr($"Attempting to parse Match Maker Request from wrong type ({type})");
            return null;
        }

        var result = JsonSerializer.Deserialize<MatchMakingRequest>(json, new JsonSerializerOptions()
        {
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });
        return result;
    }

    /// <summary>
    /// Special function to parse the nested value as a Match Making Response
    /// 
    /// ⚠️ Make sure the 'type' is correct!
    /// </summary>
    /// <returns>The parsed Match Making Response</returns>
    public MatchMakingResponse ParseMatchMakingResponse()
    {
        if (type != PacketType.MatchMakerResponse)
        {
            GD.PrintErr($"Attempting to parse Match Maker Response from wrong type ({type})");
            return null;
        }

        var result = JsonSerializer.Deserialize<MatchMakingResponse>(json, new JsonSerializerOptions()
        {
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });
        return result;
    }
}
