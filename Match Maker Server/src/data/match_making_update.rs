use serde::{Deserialize, Serialize};

/// Internal data packet used to signal an outgoing Match Making response.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct MatchMakingUpdate {
    /// Count of players that currently are waiting in queue
    #[serde(rename = "currentPeerCount")]
    pub current_peer_count: u8,

    /// Count of players that currently are required to start the match
    #[serde(rename = "requiredPeerCount")]
    pub required_player_count: u8,
}
