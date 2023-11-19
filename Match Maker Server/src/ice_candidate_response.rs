use serde::{Deserialize, Serialize};

use crate::ICECandidateRequest;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ICECandidateResponse {
    pub uuid: String,
    #[serde(rename = "mediaId")]
    pub media_id: String,
    pub index: i32,
    pub name: String,
}

impl From<ICECandidateRequest> for ICECandidateResponse {
    fn from(value: ICECandidateRequest) -> Self {
        Self {
            uuid: value.uuid,
            media_id: value.media_id,
            index: value.index,
            name: value.name,
        }
    }
}
