using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using SIPSorcery.Net;

public class Packet
{
    public PacketType type { get; set; }
    public string uuid { get; set; }
    public string json { get; set; }

    public static Packet FromJSON(string json)
    {
        return JsonSerializer.Deserialize<Packet>(json, new JsonSerializerOptions()
        {
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });
    }

    public string ToJSON()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });
    }

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
