use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SessionDescriptionRequest {
    pub uuid: String,
    #[serde(rename = "type")]
    pub ty: String,
    pub sdp: String,
}
