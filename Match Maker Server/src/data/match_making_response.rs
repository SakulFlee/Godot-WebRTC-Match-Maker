use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct MatchMakingResponse {
    #[serde(rename = "ownUUID")]
    pub own_uuid: String,
    #[serde(rename = "hostUUID")]
    pub host_uuid: String,
    #[serde(rename = "peers")]
    pub peers: Vec<String>,
}
