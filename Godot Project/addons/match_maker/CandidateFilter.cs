using System;

/// <summary>
/// Used to filter ICE candidates in <see cref="MatchMaker"=>MatchMaker</see>
/// 
/// Read more about types: <a href="https://developer.mozilla.org/en-US/docs/Web/API/RTCIceCandidate/type#values">here</a>
/// </summary>
[Flags]
public enum CandidateFilter
{
    /// <summary>
    /// Filters ICE Candidate Type 'host'
    /// 
    /// Read more about types: <a href="https://developer.mozilla.org/en-US/docs/Web/API/RTCIceCandidate/type#values">here</a>
    /// </summary>
    Host = 1 << 0,
    /// <summary>
    /// Filters ICE Candidate Type 'srflx'
    /// 
    /// Read more about types: <a href="https://developer.mozilla.org/en-US/docs/Web/API/RTCIceCandidate/type#values">here</a>
    /// </summary>
    ServerReflexiv = 1 << 1,
    /// <summary>
    /// Filters ICE Candidate Type 'prflx'
    /// 
    /// Read more about types: <a href="https://developer.mozilla.org/en-US/docs/Web/API/RTCIceCandidate/type#values">here</a>
    /// </summary>
    PeerReflexiv = 1 << 2,
    /// <summary>
    /// Filters ICE Candidate Type 'relay' (TURN servers)
    /// 
    /// Read more about types: <a href="https://developer.mozilla.org/en-US/docs/Web/API/RTCIceCandidate/type#values">here</a>
    /// </summary>
    Relay = 1 << 3,
    /// <summary>
    /// Combination of <see cref="Host"/> and <see cref="ServerReflexiv"/> 
    /// </summary>
    HostAndServerReflexiv = Host | ServerReflexiv,
    /// <summary>
    /// Combination of <see cref="Host"/> and <see cref="PeerReflexiv"/> 
    /// </summary>
    HostAndPeerReflexiv = Host | PeerReflexiv,
    /// <summary>
    /// Combination of <see cref="Host"/> and <see cref="Relay"/> 
    /// </summary>
    HostAndRelay = Host | Relay,
    /// <summary>
    /// Combination of <see cref="Host"/> and <see cref="ReflexivOnly"/> 
    /// </summary>
    HostAndReflexiv = Host | ReflexivOnly,
    /// <summary>
    /// Combination of <see cref="ServerReflexiv"/> and <see cref="PeerReflexiv"/> 
    /// </summary>
    ReflexivOnly = ServerReflexiv | PeerReflexiv,
    /// <summary>
    /// Applies to all types
    /// </summary>
    All = Host | ServerReflexiv | PeerReflexiv | Relay,
}
