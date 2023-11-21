use serde::{Deserialize, Serialize};

use crate::SessionDescriptionRequest;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SessionDescriptionResponse {
    pub uuid: String,
    #[serde(rename = "type")]
    pub ty: String,
    pub sdp: String,
}

impl From<SessionDescriptionRequest> for SessionDescriptionResponse {
    fn from(value: SessionDescriptionRequest) -> Self {
        Self {
            uuid: value.uuid,
            ty: value.ty,
            sdp: value.sdp,
        }
    }
}
