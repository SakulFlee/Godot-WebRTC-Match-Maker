use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct MatchMakingResponse {
    #[serde(rename = "isHost")]
    pub is_host: bool,
    pub peers: Vec<String>,
}
