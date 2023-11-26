use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Deserialize, Serialize, PartialEq, Eq, PartialOrd, Ord)]
pub enum PacketType {
    MatchMakerRequest,
    MatchMakerResponse,
    ICECandidate,
    SessionDescription,
}
