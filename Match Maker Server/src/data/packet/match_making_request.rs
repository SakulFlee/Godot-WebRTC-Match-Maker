use serde::{Deserialize, Serialize};

/// Internal data packet used to signal an incoming Match Making request.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct MatchMakingRequest {
    /// Name of the queue to create or join
    pub name: String,
}
