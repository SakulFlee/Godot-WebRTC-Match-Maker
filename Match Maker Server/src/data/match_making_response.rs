use serde::{Deserialize, Serialize};

/// Internal data packet used to signal an outgoing Match Making response.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct MatchMakingResponse {
    /// UUID of the current connected peer
    #[serde(rename = "ownUUID")]
    pub own_uuid: String,

    /// UUID of the host, mostly used to identify if a peer is a host or not,
    /// by comparing it's peer UUID (or it's "own" UUID) against this UUID.
    #[serde(rename = "hostUUID")]
    pub host_uuid: String,

    /// List of peers from the queue.
    /// This **does** include your "own" UUID since it's part of the peer list.
    #[serde(rename = "peers")]
    pub peers: Vec<String>,
}
