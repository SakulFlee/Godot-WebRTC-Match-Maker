using System;

[Flags]
public enum CandidateFilter
{
    Host = 1 << 0,
    ServerReflexiv = 1 << 1,
    PeerReflexiv = 1 << 2,
    Relay = 1 << 3,
    HostAndServerReflexiv = Host | ServerReflexiv,
    HostAndPeerReflexiv = Host | PeerReflexiv,
    HostAndRelay = Host | Relay,
    HostAndReflexiv = Host | ReflexivOnly,
    ReflexivOnly = ServerReflexiv | PeerReflexiv,
    All = Host | ServerReflexiv | PeerReflexiv | Relay,
}
