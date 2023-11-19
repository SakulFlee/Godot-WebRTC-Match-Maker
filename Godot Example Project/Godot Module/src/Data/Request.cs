using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

public class Request
{
    public MatchMakingRequest MatchMaking { get; set; }
    public SessionDescriptionRequest SessionDescription { get; set; }
    public ICECandidateRequest ICECandidate { get; set; }

    public bool IsValid()
    {
        var counter = 0;

        if (MatchMaking != null)
        {
            counter++;
        }

        if (SessionDescription != null)
        {
            counter++;
        }

        if (ICECandidate != null)
        {
            counter++;
        }

        return counter == 1;
    }

    public string ToJSON()
    {
        if (!IsValid())
        {
            GD.PrintErr("Request not yet ready but trying to convert to JSON!");
            return "";
        }

        return JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });
    }
}