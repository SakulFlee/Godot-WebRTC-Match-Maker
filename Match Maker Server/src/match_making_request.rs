use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct MatchMakingRequest {
    pub name: String,
    pub slots: u8,
}
