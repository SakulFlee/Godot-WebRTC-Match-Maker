use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ICECandidateRequest {
    pub uuid: String,
    #[serde(rename = "mediaId")]
    pub media_id: String,
    pub index: i32,
    pub name: String,
}
