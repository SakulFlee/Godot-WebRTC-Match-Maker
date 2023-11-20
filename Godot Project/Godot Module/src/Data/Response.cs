using System.Text.Json;

public class Response
{
    public MatchMakingResponse MatchMaking { get; set; }
    public SessionDescriptionResponse SessionDescription { get; set; }
    public ICECandidateResponse ICECandidate { get; set; }

    public static Response FromJson(string json)
    {
        var response = JsonSerializer.Deserialize<Response>(json, new JsonSerializerOptions()
        {
            IncludeFields = true,
        });

        if (!response.IsValid())
        {
            return null;
        }

        return response;
    }

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
}