use serde::{Deserialize, Serialize};

/// Packet Type
#[derive(Debug, Clone, Deserialize, Serialize, PartialEq, Eq, PartialOrd, Ord)]
pub enum PacketType {
    MatchMakerRequest,
    MatchMakerResponse,
    ICECandidate,
    SessionDescription,
}
